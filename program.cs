using System;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
namespace saa {
    class Program {
        private static string formatMessage = @"See Saa DTD";
        private static string logName = "log_{0}.txt";
        private static Log log;
        private static int logVerbosity = 0;
        private static string command = "";
        private static string path = "";
        private static string mask = "";
        private static int daysOld = 0;
        private static string targetPath = "";
        private static bool inverseMask = false;
        private static bool test;
        private static string cmdId = "";
        static void Err(string msg){
            Console.WriteLine(msg);
            log.WriteLine(msg);
        }
        static void Main(string[] args){
            var execDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var logDateFormat = DateTime.Now.ToString("G").Replace("/", ".").Replace(":", ".").Replace(" ", "_");
            var logPath = Path.Combine(execDir, string.Format(logName, DateTime.Now.ToString(logDateFormat)));
            log = new Log(logPath, logVerbosity, true);
            log.WriteLine("===================================== Started ======================================");
            if (args.Length == 0){
                Err("Please provide a path to an saa directive XML file.");
                return;
            }
            // does this file exist?
            if (!File.Exists(args[0])) {
                Err(string.Format("File does not exist: {0}", args[0]));
                return;
            }
            // checkout the XML file to see if it is 1) an XML file and 2) is the correct type
            var doc = new XmlDocument();
            try {
                doc.LoadXml(File.ReadAllText(args[0]));
            } catch (Exception ex) {
                Err(string.Format("Cannot parse XML: {0}", ex.Message));
                return;
            }
            if (!Regex.IsMatch(doc.DocumentElement.Name, "^directives$", RegexOptions.IgnoreCase)) {
                formatError();
                return;
            }
            if (doc.DocumentElement.ChildNodes.Count == 0) {
                formatError();
                return;
            }
            // check for test attribute on root node
            var t = doc.DocumentElement.Attributes["test"];
            if (t != null) {
                bool.TryParse(t.Value, out test);
            }
            // check for log verbosity on root node
            var l = doc.DocumentElement.Attributes["verbosity"];
            if (l != null) {
                int.TryParse(l.Value, out logVerbosity);
            }
            // loop thru each node and do what it says
            log.WriteLine(string.Format("Executing {0}", args[0]),5);
            try{
                foreach(XmlNode node in doc.DocumentElement.ChildNodes){
                    if(node.Name.ToLower() == "command"){
                        resetVars();
                        foreach(XmlAttribute atr in node.Attributes){
                            if(!setAttribute(atr)){
                                formatError();
                                return;
                            }
                        }
                        // attributes didn't cause an error, check and see if we have what we need to continue
                        if(!validateCommandArgs()){
                            formatError();
                            return;
                        }
                        // command args are valid - run the command
                        log.WriteLine(string.Format("Executing command {0} from {1}", cmdId, args[0]), 5);
                        exec(path);
                    }
                }
            } catch(Exception ex){
                Err(string.Format("Exception occurred:{0}", ex.Message));
            }
            log.WriteLine("===================================== Finished =====================================");
            log.Dispose();
        }
        static bool exec(string p) {
            // get the files in the path
            try {
                log.WriteLine(string.Format("Checking directory {0}",p),6);
                var files = Directory.GetFiles(p);
                foreach (var f in files) {
                    log.WriteLine(string.Format("Checking file {0}", f), 8);
                    var file = new FileInfo(f);
                    // check if this file matches days old
                    log.WriteLine(string.Format("Checking last write date {0} for file {1}",file.LastWriteTime, f), 9);
                    if (file.LastWriteTime < DateTime.Now.AddDays(daysOld * -1)) {
                        log.WriteLine(string.Format("Checking file name {0} against pattern {1}", file.Name, mask), 7);
                        if (Regex.IsMatch(file.Name, mask, RegexOptions.IgnoreCase) ^ inverseMask) {
                            // file matches criteria do command
                            var target = Path.Combine(targetPath, file.Name);
                            var msg = string.Format("{0} {1} {2}", command.ToLower(), file.FullName, target);
                            log.WriteLine(msg, 1);
                            if (test){
                                Console.WriteLine(msg);
                            } else {
                                switch (command.ToLower()) {
                                    case "move":
                                        //check if file exists in the target directory
                                        if (File.Exists(target)) {
                                            File.Delete(target);
                                        }
                                        // move file to target path
                                        file.LastWriteTime = DateTime.Now;
                                        File.Move(file.FullName, target);
                                        break;
                                    case "copy":
                                        File.Copy(file.FullName, target);
                                        break;
                                    case "delete":
                                        File.Delete(file.FullName);
                                        break;
                                }
                            }
                        }
                    }
                }
                return true;
            } catch (Exception ex) {
                Err(string.Format("Exception occurred:{0}", ex.Message));
                return false;
            }
        }
        static bool validateCommandArgs() {
            //check if command is one of copy/move/delete
            if (!Regex.IsMatch(command, "copy|move|delete", RegexOptions.IgnoreCase)) {
                return false;
            }
            // check if source path exists
            if (!Directory.Exists(path)) {
                Err(string.Format("Source directory does not exist. {0}", path));
                return false;
            }
            // check if mask is a valid regex pattern
            try {
                var r = new Regex(mask);
            } catch (Exception ex) {
                Err(string.Format("Could not parse file mask: {0} \n {1}", mask, ex.Message));
                return false;
            }
            // make sure day's old is a positive number
            if (daysOld < 0) {
                Err(string.Format("DaysOld attribute must be a positive number."));
                return false;
            }
            // make sure we can write to the target path if one exists
            if (targetPath.Length > 0) {
                if (!Directory.Exists(targetPath)) {
                    try {
                        Directory.CreateDirectory(targetPath);
                    } catch (Exception ex) {
                        Err(string.Format("Could not create path {0} \n {1}", targetPath, ex.Message));
                        return false;
                    }
                }
                // try and write a file to see if we can write there
                try {
                    var tempFileName = Path.Combine(targetPath, Guid.NewGuid() + ".txt");
                    File.WriteAllText(tempFileName, "blah");
                    File.Delete(tempFileName);
                } catch (Exception ex) {
                    Err(string.Format("Could not write to path {0} \n {1}", targetPath, ex.Message));
                    return false;
                }
            }
            return true;
        }
        static bool setAttribute(XmlAttribute atr) {
            switch (atr.Name) {
                case "name":
                    command = atr.Value;
                    break;
                case "id":
                    cmdId = atr.Value;
                    break;
                case "path":
                    path = atr.Value;
                    break;
                case "mask":
                    mask = atr.Value;
                    break;
                case "daysOld":
                    if (!int.TryParse(atr.Value, out daysOld)) {
                        return false;
                    }
                    break;
                case "targetPath":
                    targetPath = atr.Value;
                    break;
                case "inverseMask":
                    if (!bool.TryParse(atr.Value, out inverseMask)) {
                        return false;
                    }
                    break;
            }
            return true;
        }
        static void resetVars() {
            command = "";
            path = "";
            mask = "";
            daysOld = 0;
            targetPath = "";
            inverseMask = false;
        }
        static void formatError() {
            Err(string.Format("File must be in ssa XML format {0}", formatMessage));
        }
    }

}

