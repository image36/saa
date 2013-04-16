using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
namespace saa {
    /// <summary>
    /// Writes a log.
    /// Safe for multi threaded writes.
    /// </summary>
    public class Log : IDisposable {
        /// <summary>
        /// The current log verbosity.  0 = less detail 10 = more detail.  Max value is Int32.MaxValue.
        /// </summary>
        public int Verbosity { get; set; }
        /// <summary>
        /// Include a timestamp on each log entry. Example: 1/1/2013 11:27AM : Blah blah blah.
        /// </summary>
        public bool IncludeTimestamp { get; set; }
        /// <summary>
        /// The log cache that is output to the log file every log_thread_sleep_time miliseconds.
        /// </summary>
        private readonly List<string> _logStreamIn = new List<string>();
        /// <summary>
        /// Lock for the shared object log_stream_in.
        /// </summary>
        private readonly object _padlock = new object();
        /// <summary>
        /// The thread the log writter is running on.
        /// </summary>
        private readonly Thread _logThread;
        /// <summary>
        /// When true the thread is running.
        /// </summary>
        private bool _threadIsRunning;
        /// <summary>
        /// The path to the log file.
        /// </summary>
        private readonly string _logFilePath = "";
        /// <summary>
        /// How long the writer thread sleeps between write attempts.
        /// </summary>
        private const int LogThreadSleepTime = 1000;
        /// <summary>
        /// Initializes a new instance of the <see cref="Log"/> class.
        /// </summary>
        public Log(string logPath, int verbosity, bool includeTimestamp) {
            Verbosity = verbosity;
            IncludeTimestamp = includeTimestamp;
            _logThread = new Thread(StartLogWriter) { Name = "Log." };
            _logThread.SetApartmentState(ApartmentState.MTA);
            _threadIsRunning = true;
            _logThread.Start();
            _logFilePath = logPath;
        }
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool managed) {
            if (managed) {
                GC.SuppressFinalize(this);
                Dispose();
            }
            _threadIsRunning = false;
            _logThread.Abort();
        }
        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose() {
            Dispose(false);
        }
        /// <summary>
        /// Starts the log writer thread.
        /// </summary>
        private void StartLogWriter() {
            while (_threadIsRunning){
                Flush();
                Thread.Sleep(LogThreadSleepTime);
            }
        }
        /// <summary>
        /// Flushes any pending writes to the log.
        /// </summary>
        public void Flush(){
            if (_logStreamIn.Count > 0) {
                string logStreamOut;
                lock (_padlock) {
                    logStreamOut = String.Join(Environment.NewLine, _logStreamIn.ToArray());
                    _logStreamIn.RemoveRange(0, _logStreamIn.Count);
                }
                // make sure directory exists.
                var dir = Path.GetDirectoryName(_logFilePath);
                if (dir == null) {
                    var e = new NullReferenceException("Log directory path is null");
                    throw e;
                }
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                using (var w = File.AppendText(_logFilePath)) {
                    w.WriteLine(logStreamOut);
                    w.Flush();
                }
            }
        }
        /// <summary>
        /// Write a line to the log.
        /// </summary>
        /// <param name="dataToLog">The data to log.</param>
        public void WriteLine(string dataToLog) {
            WriteLine(dataToLog, 0);
        }
        /// <summary>
        /// Write a line to the log.
        /// </summary>
        /// <param name="dataToLog">The data to log.</param>
        /// <param name="verbosity">The verbosity level of this message. Low numbers show up sooner than high numbers.</param>
        /// <seealso cref="Verbosity"/>
        public void WriteLine(string dataToLog, int verbosity) {
            if (Verbosity < verbosity) { return; }
            lock (_padlock) {
                var timestamp = string.Empty;
                if (IncludeTimestamp) {
                    timestamp = string.Format("{0} : ", DateTime.Now.ToString("G"));
                }
                _logStreamIn.Add(string.Format("{0}{1}", timestamp, dataToLog));
            }
        }
    }
}
