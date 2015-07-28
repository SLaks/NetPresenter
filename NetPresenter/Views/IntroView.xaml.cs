using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

		public IntroView(IReadOnlyDictionary<string, string> set)
			: this(set["Background"], set["Foreground"]) {
		}


		///<summary>Creates an <see cref="IntroView"/> creator function from background & foreground images in the specified directory, or null if no such images exist.</summary>
		public static Func<IntroView> TryCreateFactory(string directory) {
			directory = IO.Path.GetFullPath(directory);
			if (!Directory.Exists(directory))
				return null;
			var sets = GetImageSets(directory, "Foreground", "Background");
			if (!sets.Any())
				return null;

			Random rand = new Random();
			return () => new IntroView(sets[rand.Next(sets.Count)]);
		}

		static ISet<string> imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };
		///<summary>Returns all complete image groups with the specified keys in a directory.</summary>
		///<returns>A collection of maps of keys to full paths.</returns>
		///<remarks>
		/// An image group is a set of images with the same suffix with one prefix for each key.
		/// For example, if the keys set contains "Background" and "Foreground", and the folder
		/// holds "Background-Light", "Background-Dark", "Foreground-Light", "Foreground-Dark",
		/// and "Foreground-Other", this method will return two groups:
		/// ("Background-Light", "Foreground-Light") and ("Background-Dark", "Foreground-Dark").
		///</remarks>
		static IReadOnlyList<IReadOnlyDictionary<string, string>> GetImageSets(string directory, params string[] keys) {
			var nameMatcher = new Regex(
				@"^(?<key>" + string.Join("|", keys.Select(Regex.Escape)) + ")"
			  + @"(?<suffix>.*)"
			  + @"\.(?:" + string.Join("|", imageExtensions.Select(s => s.TrimStart('.'))) + @")$",
				RegexOptions.IgnoreCase
			);

			return Directory.EnumerateFiles(directory)
							.Select(IO.Path.GetFileName)
							.Select(p => nameMatcher.Match(p))
							.Where(m => m.Success)
							.GroupBy(m => m.Groups["suffix"].Value)
							.Where(g => !keys.Except(
								g.Select(m => m.Groups["key"].Value),
								StringComparer.OrdinalIgnoreCase
							).Any())
							.Select(g => g.ToDictionary(
								m => m.Groups["key"].Value,
								m => IO.Path.GetFullPath(IO.Path.Combine(directory, m.Value)),
								StringComparer.OrdinalIgnoreCase
							))
							.ToList();
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
