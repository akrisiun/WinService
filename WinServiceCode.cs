using System.ServiceProcess;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using System.IO;

namespace WinService
{
    public class PowershellRun
    {
        public string Script { [DebuggerStepThrough] get; set; }
        public bool IsWorking { [DebuggerStepThrough] get; protected set; } = false;

        public PSDataCollection<PSObject> Output { [DebuggerStepThrough] get; set; }
        public PowerShell PS { get; set; }

        public static PowershellRun Run()
        {
            var ps1 = new PowershellRun { Script = "run.ps1" };
            ps1.Start();
            return ps1;
        }

        public IAsyncResult RunScript()
        {
            IAsyncResult ar = null;
            Output = new PSDataCollection<PSObject>();
            Output.DataAdded += Output_DataAdded;

            // the streams (Error, Debug, Progress, etc) are available on the PowerShell instance.
            // we can review them during or after execution.
            // we can also be notified when a new item is written to the stream (like this):


            // \Windows\Microsoft.NET\assembly\GAC_MSIL\System.Management.Automation\v4.0_3.0.0.0__31bf3856ad364e35\System.Management.Automation.dll
            var initial = InitialSessionState.CreateDefault();
            using (Runspace runspace = RunspaceFactory.CreateRunspace(initial)) {

                runspace.Open();

                PowerShell ps = PowerShell.Create();
                ps.Runspace = runspace;

                RunspaceInvoke runSpaceInvoker = new RunspaceInvoke(runspace);
                //try {
                //    Logger.WriteMessage(LogLevel.Info, $"Set-ExecutionPolicy Unrestricted");
                //    runSpaceInvoker.Invoke("Set-ExecutionPolicy Unrestricted -Force");
                //} catch (Exception ex) { Logger.WriteMessage(LogLevel.Error, $"{ex.Message}"); }
                try {
                    Logger.WriteMessage(LogLevel.Info, $"Set-ExecutionPolicy Unrestricted -Scope CurrentUser -Force");
                    runSpaceInvoker.Invoke("Set-ExecutionPolicy Unrestricted -Scope CurrentUser -Force");
                }
                catch (Exception ex) { Logger.WriteMessage(LogLevel.Error, $"{ex.Message}"); }

                var cmd = new Command(@"invoke-command");
                cmd.Parameters.Add(new CommandParameter("Scriptblock",
                        //, text));
                        ScriptBlock.Create(@".\" + Script)));
                cmd.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                ps.Commands.AddCommand(cmd);

                this.PS = ps;
                ps.Streams.Error.DataAdded += Error_DataAdded;
                try {
                    Parse.TryBind(ps);
                }
                catch (MissingMethodException) { ; }
                // Consul failure System.MissingMethodException: Method not found: 
                // 'System.Management.Automatio Management.Automation.InformationRecord > System.Management.Automation.PSDataStreams.get_Info

                IAsyncResult async = ps.BeginInvoke<PSObject, PSObject>(null, Output);

                ar = async;

                if (!async.IsCompleted) {
                    try {
                        ar.AsyncWaitHandle.WaitOne();

                        StringBuilder sb = new StringBuilder();
                        foreach (PSObject result in ps.EndInvoke(async)) {
                            sb.AppendLine(result.ToString());
                        }
                        Logger.WriteMessage(LogLevel.Info, $"{sb.ToString()}");
                    }
                    catch {; }
                }
            }

            return ar;
        }

        public static void Output_DataAdded(object sender, DataAddedEventArgs e)
        {
            var output = sender as PSDataCollection<PSObject>;
            int last = output.Count - 1;
            var item1 = output[last] as PSObject;

            // do something when an object is written to the output stream
            Logger.WriteMessage(LogLevel.Info, $"{item1.ToString()}");
        }

        /// <summary>
        /// Event handler for when Data is added to the Error stream.
        /// </summary>
        /// <param name="sender">Contains the complete PSDataCollection of all error output items.</param>
        /// <param name="e">Contains the index ID of the added collection item and the ID of the PowerShell instance this event belongs to.</param>
        public static void Error_DataAdded(object sender, DataAddedEventArgs e)
        {
            var output = sender as PSDataCollection<ErrorRecord>;
            int last = output.Count - 1;
            var item1 = output[last] as ErrorRecord;

            // do something when an error is written to the error stream
            Logger.WriteMessage(LogLevel.Warn, $"{item1?.Exception?.Message ?? "-"}");
        }

        public void CallBack(IAsyncResult ar)
        {
            var sb = new StringBuilder();

            var ps = ar.AsyncState as System.Management.Automation.PowerShell;
            var output = Output as PSDataCollection<PSObject>;

            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject result in ps.EndInvoke(ar)) 
            {
                sb.AppendLine(result.ToString());
            }

            Logger.WriteMessage(LogLevel.Trace, $"{WinService.Service} {sb.ToString()}");
        }

        public void Start()
        {
            var dir = Environment.CurrentDirectory;
            var psfile = $"{dir}\\{Script}";
            Logger.WriteMessage(LogLevel.Trace, $"{WinService.Service} Worker.Start {psfile}");

            //Do Work
            int i = 0;
            IsWorking = true;
            IAsyncResult waiter = null;

            var text = File.ReadAllText(psfile);
            Logger.WriteMessage(LogLevel.Info, text);

            while (IsWorking) {

                try {
                    if (!File.Exists(psfile)) {

                        Logger.WriteMessage(LogLevel.Debug, $"{WinService.Service} no file {psfile}");
                        waiter = null;
                        Thread.Sleep(25000); // 25 sec

                    } else {
                        waiter = RunScript();
                        waiter.AsyncWaitHandle.WaitOne();
                    }
                } catch (Exception ex) {
                    Logger.WriteMessage(LogLevel.Debug, $"{WinService.Service} failure {ex}");
                    waiter = null;
                }

                if (waiter == null || waiter.IsCompleted) {

                    waiter = null;

                    Logger.WriteMessage(LogLevel.Debug, $"{WinService.Service} Restarting {psfile}");

                    Thread.Sleep(5000); // 5 sec
                    Logger.WriteMessage(LogLevel.Debug, "i = {0}", i.ToString());
                    i++;
                }
            }

            if (!IsWorking && waiter != null) 
            {
                try {
                    waiter.AsyncWaitHandle.Dispose(); // .Close();
                    waiter = null;
                }
                catch {; }
            }
        }

        public void Stop()
        {
            Logger.WriteMessage(LogLevel.Trace, $"{WinService.Service} Worker.Stop");

            //Stop doing work
            IsWorking = false;
        }
    }


    public partial class WinService : ServiceBase
	{
        protected PowershellRun worker;
        protected Thread workerThread;

        public static string Service { get => "Consul"; }

        public WinService()
		{
			Logger.WriteMessage(LogLevel.Trace, $"{Service}.ctor");

			InitializeComponent();
            worker = new PowershellRun() { Script = "run.ps1" }; // Worker();

			workerThread = new Thread(new ThreadStart(worker.Start));
			workerThread.Name = $"{Service} Thread";
		}

		protected override void OnStart(string[] args)
		{

			Logger.WriteMessage(LogLevel.Trace, $"{Service}.OnStart");
			if (workerThread != null && !workerThread.IsAlive)
			{
				workerThread.Start();
			}
		}

		protected override void OnStop()
		{

			Logger.WriteMessage(LogLevel.Trace, $"{Service}.OnStop");
			if (workerThread != null && workerThread.IsAlive)
			{
				worker.Stop();
				workerThread.Abort();
			}
		}

		public void Start(string[] args)
		{
			Logger.WriteMessage(LogLevel.Trace, $"{Service}.Start");
			this.OnStart(args);
		}
	}

    public static class Parse
    {
        public static void TryBind(PowerShell ps)
        {
            try {
                ps.Streams.Information.DataAdded += PowershellRun.Output_DataAdded;
                ps.Streams.Warning.DataAdded += PowershellRun.Output_DataAdded;
            }
            catch (Exception ex) {
                Logger.WriteMessage(LogLevel.Error, $"Failue {ex.Message}");
            }
        }
    }
}
