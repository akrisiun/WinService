using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WinService
{
	class Program
	{
		private static readonly string _exePath = Assembly.GetExecutingAssembly().Location;
		internal static Object globalLock = new Object();
		static void Main(string[] args)
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "Program.Main");
#endif
			WinService service = new WinService();
			Boolean consoleRun = Environment.UserInteractive;
			if (args.Length > 0)
			{
				String cmd = args[0].Trim().Trim('-').Trim('/').Trim();
				switch (cmd)
				{
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
						break;
				}
			}
			else
			{
				if (consoleRun)
				{

					Logger.WriteMessage(LogLevel.Info, "Starting Console Service");
					service.Start(args);
					Console.WriteLine("Press any key to exit");
					Console.ReadKey();
					service.Stop();
					Logger.WriteMessage(LogLevel.Info, "Stopped Console Service");
				}
				else
				{
					System.ServiceProcess.ServiceBase.Run(service);
				}

			}
		}

		static void SelfInstall()
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "Program.SelfInstall");
#endif
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
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "Program.SelfUninstall");
#endif
			try
			{
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
