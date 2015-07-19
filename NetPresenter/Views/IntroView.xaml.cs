using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using IO = System.IO;

namespace NetPresenter.Views {
	/// <summary>
	/// Interaction logic for IntroView.xaml
	/// </summary>
	public partial class IntroView : ViewBase {
		public override string ViewName { get { return "Intro"; } }

		readonly DispatcherTimer sparkleTimer;
		public IntroView(string backgroundPath, string foregroundPath) {
			InitializeComponent();

			background.Source = new BitmapImage(new Uri(backgroundPath));
			foreground.Source = new BitmapImage(new Uri(foregroundPath));

			sparkleTimer = new DispatcherTimer(TimeSpan.FromSeconds(.1), DispatcherPriority.Background, AddSparkle, Dispatcher);
			Unloaded += delegate { sparkleTimer.Stop(); };
			Loaded += delegate { sparkleTimer.Start(); };
		}


		///<summary>Creates an <see cref="IntroView"/> instance from background & foreground images in the specified directory, or null if no such images exist.</summary>
		public static IntroView TryCreate(string directory) {
			if (!Directory.Exists(directory))
				return null;
			var foreground = TryGetImage(directory, "Foreground");
			var background = TryGetImage(directory, "Background");
			if (foreground == null || background == null)
				return null;
			return new IntroView(background, foreground);
		}

		static ISet<string> imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
		///<summary>Attempts to locate an image file with the specified name.</summary>
		static string TryGetImage(string directory, string name) {
			return Directory.EnumerateFiles(directory, IO.Path.ChangeExtension(name, null) + ".*")
				.FirstOrDefault(p => imageExtensions.Contains(IO.Path.GetExtension(p)));
		}

		readonly Random rand = new Random();
		void AddSparkle(object sender, EventArgs e) {
			AddSparkle(rand.NextDouble() * (sparkleCanvas.ActualWidth - 45), rand.NextDouble() * (sparkleCanvas.ActualHeight - 45));
		}
		void AddSparkle(double x, double y) {
			var newSparkle = new Controls.Sparkle();
			Canvas.SetTop(newSparkle, x);
			Canvas.SetLeft(newSparkle, y);
			sparkleCanvas.Children.Add(newSparkle);
		}
	}
}
