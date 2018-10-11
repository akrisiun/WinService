using System;
using System.Configuration.Install;
using System.Reflection;

namespace WinService
{
    public class Program
    {
        static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
        static object globalLock = new object();

        public static WinService Service  { get; set; }

        static void Main(string[] args)
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var envDir = Environment.CurrentDirectory;
            Logger.WriteMessage(LogLevel.Trace, $"Program.Main {envDir} Base: {dir}");
            Environment.CurrentDirectory = dir;

            Service = new WinService();
            bool consoleRun = Environment.UserInteractive;
            if (args.Length > 0) {
                String cmd = args[0].Trim().Trim('-').Trim('/').Trim();
                switch (cmd) {
                    case "install":
                    case "i":
                        SelfInstall();
                        break;
                    case "uninstall":
                    case "u":
                        SelfUninstall();
                        break;
                    case "console":
                    case "c":
                        consoleRun = true;
                        break;
                    default:
                        consoleRun = true;
                        break;
                }
            }

            if (consoleRun) {

                Logger.WriteMessage(LogLevel.Info, "Starting Console Service");
                Service.Start(args);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Service.Stop();
                Logger.WriteMessage(LogLevel.Info, "Stopped Console Service");

            } else {
                System.ServiceProcess.ServiceBase.Run(Service);
            }
        }

		static void SelfInstall()
		{
			Logger.WriteMessage(LogLevel.Trace, "Program.SelfInstall");

			try
			{
				ManagedInstallerClass.InstallHelper(new string[] { _exePath });
				Logger.WriteMessage(LogLevel.Info, "Service Installed");
			}
			catch (Exception e)
			{
				Logger.WriteException(e, "Could not self-install service");
			}
		}

		static void SelfUninstall()
		{
			Logger.WriteMessage(LogLevel.Trace, "Program.SelfUninstall");
			try
			{
                // System.Configuration.Install
                ManagedInstallerClass.InstallHelper(new string[] { "/u", _exePath });
				Logger.WriteMessage(LogLevel.Info, "Service Uninstalled");
			}
			catch (Exception e)
			{
				Logger.WriteException(e, "Could not self-uninstall service");
			}
		}
	}
}
