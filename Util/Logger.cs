using System;
using System.Text;

namespace WinService
{

    enum LogLevel : byte
    {
        All = 0,
        Trace = 1,
        Debug = 2,
        Info = 3,
        Warn = 4,
        Error = 5,
        Fatal = 6
    }

    static class Logger
    {
        private static readonly object consoleLockObj;
        private static readonly object fileLockObj;

        private static LogLevel CurrentLevel;
        private static String logFile = null;

        static Logger()
        {
            consoleLockObj = new object();
            fileLockObj = new object();
            CurrentLevel = LogLevel.Info;

            String appLogLevel = System.Configuration.ConfigurationManager.AppSettings["SelfLogLevel"];
            CurrentLevel = (LogLevel)Enum.Parse(typeof(LogLevel), appLogLevel);
            logFile = System.Configuration.ConfigurationManager.AppSettings["SelfLogFile"];
            if (!String.IsNullOrEmpty(logFile)) {
                logFile = Environment.ExpandEnvironmentVariables(logFile);
            }
        }

        private static Boolean CanLog(LogLevel level)
        {
            return level >= CurrentLevel;
        }
        private static ConsoleColor getColor(LogLevel level)
        {
            ConsoleColor ans = Console.ForegroundColor;
            switch (level) {
                case LogLevel.All:
                    ans = ConsoleColor.White;
                    break;
                case LogLevel.Trace:
                    ans = ConsoleColor.Green;
                    break;
                case LogLevel.Debug:
                    ans = ConsoleColor.Yellow;
                    break;
                case LogLevel.Info:
                    ans = ConsoleColor.Gray;
                    break;
                case LogLevel.Warn:
                    ans = ConsoleColor.Cyan;
                    break;
                case LogLevel.Error:
                    ans = ConsoleColor.Red;
                    break;
                case LogLevel.Fatal:
                    ans = ConsoleColor.Magenta;

                    break;
                default:
                    break;
            }
            return ans;
        }

        public static void WriteMessage(LogLevel level, String msg)
        {
            if (CanLog(level)) {
                if (Environment.UserInteractive) {
                    lock (consoleLockObj) {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = getColor(level);
                        Console.WriteLine("{0} - {1}", DateTime.Now, msg);
                        Console.ForegroundColor = currentColor;
                    }
                }
                if (!String.IsNullOrEmpty(logFile)) {
                    lock (fileLockObj) {
                        //TODO: Roll log (max size / daily)
                        System.IO.File.AppendAllText(logFile, String.Format("{0} - {1}{2}", DateTime.Now, msg, Environment.NewLine), Encoding.UTF8);
                    }
                }
            }
        }

        public static void WriteMessage(String msg)
        {
            WriteMessage(LogLevel.Info, msg);
        }
        public static void WriteMessage(LogLevel level, String format, params String[] args)
        {
            WriteMessage(level, String.Format(format, args));
        }

        public static void WriteMessage(String format, params String[] args)
        {
            WriteMessage(String.Format(format, args));
        }

        public static void WriteException(Exception e, String msg)
        {
            WriteMessage(LogLevel.Error, "{0} - {1} - {2}", msg, e.Message, e.StackTrace);
        }
    }
}
