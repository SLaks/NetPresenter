using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetPresenter {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class ViewHost : Window {
		readonly Orchestrator Orchestrator;
		readonly bool shouldMaximize;
		public ViewHost(Orchestrator orchestrator, Rect bounds) {
			InitializeComponent();

			Orchestrator = orchestrator;
			shouldMaximize = App.PhysicalScreens.Contains(bounds);
			Left = bounds.X;
			Top = bounds.Y;
			Width = bounds.Width;
			Height = bounds.Height;

			hideCursorTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Normal, HideCursor_Tick, Dispatcher.CurrentDispatcher); 
			Cursor = Cursors.None;
			ForceCursor = true;
		}

		public ViewBase View { get; private set; }
		public void SetView(Func<ViewBase> viewCreator) {
			View = viewCreator();
			Content = View;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (shouldMaximize)
				WindowState = WindowState.Maximized;
		}

		private void Window_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			Orchestrator.ShowMenu(View);
		}

		DispatcherTimer hideCursorTimer;
		void HideCursor_Tick(object sender, EventArgs e) {
			Cursor = Cursors.None;
			hideCursorTimer.Stop();
		}

		static Point mousePos = App.Cursor;
		private void Window_PreviewMouseMove(object sender, MouseEventArgs e) {
			if (mousePos == App.Cursor) return;
			mousePos = App.Cursor;
			Cursor = null;

			hideCursorTimer.Stop();
			hideCursorTimer.Start();
		}
	}
}
