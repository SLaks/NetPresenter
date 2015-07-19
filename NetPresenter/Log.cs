using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace NetPresenter {
	static class Log {
		static readonly string LogPath = Path.Combine(Environment.CurrentDirectory, "NetPresenter-" + Environment.MachineName + "." + Environment.UserName + ".log");
		static readonly object locker = new object();

		public static void Write(string message) {
			ThreadPool.QueueUserWorkItem(delegate {
				try {
					var prefix = DateTime.Now.ToString("MM/dd/yyyy, hh:mm:ss tt:   ");

					message = String.Join(
						Environment.NewLine,
						message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(m => prefix + m)
					) + Environment.NewLine;

					lock (locker) {
						File.AppendAllText(LogPath, message);
					}
				} catch { }
			});
		}
	}
}
