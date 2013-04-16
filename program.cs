using System;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
namespace saa
{
    class Program
    {
        private static string formatMessage = @"See Saa DTD";
        private static string command = "";
        private static string path = "";
        private static string mask = "";
        private static int daysOld = 0;
        private static string targetPath = "";
        private static bool inverseMask = false;
        private static bool test;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide a path to an saa directive XML file.");
                return;
            }
            // does this file exist?
            if (!File.Exists(args[0]))
            {
                Console.WriteLine(string.Format("File does not exist: {0}", args[0]));
                return;
            }
            // checkout the XML file to see if it is 1) an XML file and 2) is the correct type
            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(File.ReadAllText(args[0]));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Cannot parse XML: {0}", ex.Message));
                return;
            }
            if (!Regex.IsMatch(doc.DocumentElement.Name, "^directives$", RegexOptions.IgnoreCase))
            {
                formatError();
                return;
            }
            if (doc.DocumentElement.ChildNodes.Count == 0)
            {
                formatError();
                return;
            }
            // check for test attribute on root node
            XmlAttribute t = doc.DocumentElement.Attributes["test"];
            if (t != null)
            {
                bool.TryParse(t.Value, out test);
            }
            // loop thru each node and do what it says
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                resetVars();
                foreach (XmlAttribute atr in node.Attributes)
                {
                    if (!setAttribute(atr))
                    {
                        formatError();
                        return;
                    }
                }
                // attributes didn't cause an error, check and see if we have what we need to continue
                if (!validateCommandArgs())
                {
                    formatError();
                    return;
                }
                // command args are valid - run the command
                exec(path);
            }
            Console.WriteLine();
            return;
        }
        static bool exec(string p)
        {
            // get the files in the path
            try
            {
                var files = Directory.GetFiles(p);
                foreach (var f in files)
                {
                    var file = new FileInfo(f);
                    // check if this file matches days old
                    if (file.LastWriteTime < DateTime.Now.AddDays(daysOld * -1))
                    {
                        if (Regex.IsMatch(file.Name, mask, RegexOptions.IgnoreCase) ^ inverseMask)
                        {
                            // file matches criteria do command
                            var target = Path.Combine(targetPath, file.Name);
                            if (test)
                            {
                                Console.WriteLine("Test mode: {0} {1} {2}", command.ToLower(), file.FullName, target);
                            }
                            else
                            {
                                switch (command.ToLower())
                                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred:{0}", ex.Message);
                return false;
            }
        }
        static bool validateCommandArgs()
        {
            //check if command is one of copy/move/delete
            if (!Regex.IsMatch(command, "copy|move|delete", RegexOptions.IgnoreCase))
            {
                return false;
            }
            // check if source path exists
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Source directory does not exist. {0}", path);
                return false;
            }
            // check if mask is a valid regex pattern
            try
            {
                var r = new Regex(mask);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not parse file mask: {0} \n {1}", mask, ex.Message);
                return false;
            }
            // make sure day's old is a positive number
            if (daysOld < 0)
            {
                Console.WriteLine("DaysOld attribute must be a positive number.");
                return false;
            }
            // make sure we can write to the target path if one exists
            if (targetPath.Length > 0)
            {
                if (!Directory.Exists(targetPath))
                {
                    try
                    {
                        Directory.CreateDirectory(targetPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not create path {0} \n {1}", targetPath, ex.Message);
                        return false;
                    }
                }
                // try and write a file to see if we can write there
                try
                {
                    var tempFileName = Path.Combine(targetPath, Guid.NewGuid() + ".txt");
                    File.WriteAllText(tempFileName, "blah");
                    File.Delete(tempFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not write to path {0} \n {1}", targetPath, ex.Message);
                    return false;
                }
            }
            return true;
        }
        static bool setAttribute(XmlAttribute atr)
        {
            switch (atr.Name)
            {
                case "name":
                    command = atr.Value;
                    break;
                case "path":
                    path = atr.Value;
                    break;
                case "mask":
                    mask = atr.Value;
                    break;
                case "daysOld":
                    if (!int.TryParse(atr.Value, out daysOld))
                    {
                        return false;
                    }
                    break;
                case "targetPath":
                    targetPath = atr.Value;
                    break;
                case "inverseMask":
                    if (!bool.TryParse(atr.Value, out inverseMask))
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }
        static void resetVars()
        {
            command = "";
            path = "";
            mask = "";
            daysOld = 0;
            targetPath = "";
            inverseMask = false;
        }
        static void formatError()
        {
            Console.WriteLine("File must be in ssa XML format {0}", formatMessage);
        }
    }

}

