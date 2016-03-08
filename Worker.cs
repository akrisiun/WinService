using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinService
{
	class Worker
	{
		private bool isworking = false;
		internal void Start()
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "Worker.Start");
#endif
			//Do Work
			UInt64 i = 0;
			isworking = true;
			while (isworking)
			{
				System.Threading.Thread.Sleep(1000);
				Logger.WriteMessage(LogLevel.Debug, "i = {0}", i.ToString());
				i++;
			}
		}

		internal void Stop()
		{
#if TRACE
			Logger.WriteMessage(LogLevel.Trace, "Worker.Stop");
#endif
			//Stop doing work
			isworking = false;
		}
	}
}
