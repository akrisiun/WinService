

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
	class WinService : ServiceBase
	{
		private Worker worker;
		private Thread workerThread;

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}



		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.ServiceName = "WinService";
		}

		#endregion

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
