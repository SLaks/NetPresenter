using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace NetPresenter {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private void Application_Startup(object sender, StartupEventArgs e) {
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			Orchestrator.Run(LogicalScreens);
		}

		public static IEnumerable<Rect> PhysicalScreens { get { return WinForms.Screen.AllScreens.Select(s => s.Bounds.ToRect()); } }
		public static IEnumerable<Rect> LogicalScreens { get { return PhysicalScreens.SelectMany(SplitScreen); } }

		static IEnumerable<Rect> SplitScreen(Rect screen) {
			if (screen.Width > screen.Height * 2) {
				yield return new Rect(screen.X, screen.Y, screen.Width / 2, screen.Height);
				yield return new Rect(screen.X + screen.Width / 2, screen.Y, screen.Width / 2, screen.Height);
			} else
				yield return screen;
		}

		public static Point Cursor {
			get {
				var mouse = WinForms.Cursor.Position;
				return new Point(mouse.X, mouse.Y);
			}
			set { WinForms.Cursor.Position = new System.Drawing.Point((int)value.X, (int)value.Y); }
		}
	}
}
