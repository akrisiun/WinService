

namespace WinService
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Diagnostics;
	using System.Linq;
	using System.ServiceProcess;
	using System.Text;
	using System.Threading;
	partial class WinService : ServiceBase
	{
		private Worker worker;
		private Thread workerThread;

		public WinService()
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "WinService.cTor");
#endif
			InitializeComponent();
			worker = new Worker();
			workerThread = new Thread(new ThreadStart(worker.Start));
			workerThread.Name = "Worker Thread";
		}

		protected override void OnStart(string[] args)
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "WinService.OnStart");
#endif
			if (workerThread != null && !workerThread.IsAlive)
			{
				workerThread.Start();
			}
		}

		protected override void OnStop()
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "WinService.OnStop");
#endif
			if (workerThread != null && workerThread.IsAlive)
			{
				worker.Stop();
				workerThread.Abort();				
			}
		}

		internal void Start(string[] args)
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "WinService.Start");
#endif
			this.OnStart(args);
		}
	}
}
