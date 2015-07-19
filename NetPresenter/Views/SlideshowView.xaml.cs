using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace NetPresenter.Views {
	/// <summary>
	/// Interaction logic for SlideshowView.xaml
	/// </summary>
	public partial class SlideshowView : ViewBase {
		readonly Orchestrator Orchestrator;
		readonly string viewName;
		public SlideshowView(Orchestrator orchestrator, string viewName, string filePath) {
			InitializeComponent();
			this.viewName = viewName;
			Orchestrator = orchestrator;

			HideName = (Storyboard)Resources["HideName"];
			ShowName = (Storyboard)Resources["ShowName"];

			var names = Directory.GetFiles(filePath, "*.jpg", SearchOption.AllDirectories);
			Array.Sort(names);
			allFilenames = new ReadOnlyCollection<string>(Array.ConvertAll(names, Path.GetFullPath));
			loadedImages = new BitmapImage[allFilenames.Count];

			Focusable = true;
			MouseEnter += delegate { Focus(); };
		}
		public override string ViewName { get { return viewName; } }
		readonly Storyboard HideName, ShowName;

		void SlideshowView_Loaded(object sender, RoutedEventArgs e) {
			if (loader != null)
				return;

			ImageHost.Width = ActualWidth;
			ImageHost.Height = ActualHeight;
			loader = new ImageLoader(allFilenames);
			loader.ImageLoaded += loader_ImageLoaded;
			loader.BeginLoad((int)ActualWidth, (int)ActualHeight);
			DoNavigateTo(0);
		}

		ImageLoader loader;

		readonly ReadOnlyCollection<string> allFilenames;
		readonly BitmapImage[] loadedImages;
		string pendingImage;

		#region Loader
		class ImageLoader {
			public ImageLoader(ReadOnlyCollection<string> files) { allFilenames = files; }
			readonly ReadOnlyCollection<string> allFilenames;

			int width, height;
			public void BeginLoad(int width, int height) {
				this.width = width;
				this.height = height;
				ThreadPool.QueueUserWorkItem(delegate { LoadImages(); });
			}
			public void SetPending(string name) { pendingFilename = name; }

			volatile string pendingFilename;

			HashSet<string> loadedFiles = new HashSet<string>();
			void LoadImages() {
				foreach (var file in allFilenames) {
					var pending = pendingFilename;
					if (!String.IsNullOrEmpty(pending))
						LoadImage(pending);

					LoadImage(file);
				}
			}
			void LoadImage(string filename) {
				if (loadedFiles.Contains(filename))
					return;

				var image = new BitmapImage();
				image.BeginInit();
				image.DecodePixelWidth = width;
				image.UriSource = new Uri(filename, UriKind.Absolute);
				image.EndInit();
				image.Freeze();

				loadedFiles.Add(filename);
				OnImageLoaded(new ImageLoadedEventArgs(image, filename));
			}

			///<summary>Occurs when an image is loaded.</summary>
			public event EventHandler<ImageLoadedEventArgs> ImageLoaded;
			///<summary>Raises the ImageLoaded event.</summary>
			///<param name="e">An ImageLoadedEventArgs object that provides the event data.</param>
			internal protected virtual void OnImageLoaded(ImageLoadedEventArgs e) {
				if (ImageLoaded != null)
					ImageLoaded(this, e);
			}

		}
		///<summary>Provides data for the ImageLoaded event.</summary>
		public class ImageLoadedEventArgs : EventArgs {
			///<summary>Creates a new ImageLoadedEventArgs instance.</summary>
			public ImageLoadedEventArgs(BitmapImage image, string name) { Image = image; Name = name; }

			///<summary>Gets the image.</summary>
			public BitmapImage Image { get; private set; }
			public string Name { get; private set; }
		}
		#endregion

		void loader_ImageLoaded(object sender, SlideshowView.ImageLoadedEventArgs e) {
			Dispatcher.BeginInvoke(new Action(delegate {
				var index = allFilenames.IndexOf(e.Name);
				loadedImages[index] = e.Image;
				if (e.Name == pendingImage && index == currentIndex) {
					ShowImage(e.Image);
					pendingImage = null;
				}
			}));
		}

		void ShowImage(BitmapSource image) {
			HideName.Begin();
			Controls.Transitioner.DoTransition(
				ImageHost,
				delegate {			//NewContent callback
					ImageControl.Source = image;
					ImageHost.Child.Opacity = 1;
				},
				ShowName.Begin		//Completed  callback
			);
			IsLoading = false;
		}

		bool IsLoading {
			get { return loadingPanel.Visibility == Visibility.Visible; }
			set {
				loadingPanel.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
				if (value) {
					loadingPanel.Child = new Controls.LoadingAnimation {
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					};
				} else {
					var c = loadingPanel.Child as Controls.LoadingAnimation;
					if (c != null)
						c.Stop();
					loadingPanel.Child = null;
				}
			}
		}

		int currentIndex = int.MinValue;
		void DoNavigateTo(int index) {
			if (index < 0) index += allFilenames.Count * (1 + -index / allFilenames.Count);
			index %= allFilenames.Count;
			if (currentIndex == index) return;

			currentIndex = index;
			canvas.Children.Clear();
			if (loadedImages[index] == null) {
				loader.SetPending(allFilenames[index]);
				IsLoading = true;
				pendingImage = allFilenames[index];
			} else {
				IsLoading = false;
				ShowImage(loadedImages[index]);
			}
			imageName.Text = Path.GetFileName(allFilenames[index]);
		}

		private void ViewBase_PreviewKeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Left: NavigateBy(-1); break;
				case Key.Right: NavigateBy(1); break;
			}
		}
		private void ViewBase_MouseWheel(object sender, MouseWheelEventArgs e) { NavigateBy(-Math.Sign(e.Delta)); }

		private void ViewBase_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (ImageControl.Source == null) return;
			var pos = e.GetPosition(ImageControl);
			var x = pos.X / ImageControl.ActualWidth;
			var y = pos.Y / ImageControl.ActualHeight;
			if (x < 0 || y < 0 || x > 1 || y > 1) return;

			ShowHighlight(x, y);
		}
		public override IEnumerable<ICommand> GetMenuCommands() {
			yield return new ActionCommand("Start over", () => BroadcastNavigateTo(0));
		}

		const string ShowHighlightCommand = "ShowHighlight", NavigateCommand = "Navigate";
		public override void ExecuteCommand(string name, CommandArguments args) {
			//If the view was set when the command was
			//received, it won't be loaded yet.
			if (loader == null) {
				Dispatcher.BeginInvoke(new Action<string, CommandArguments>(ExecuteCommand), DispatcherPriority.Loaded, name, args);
				return;
			}
			switch (name) {
				case NavigateCommand:
					DoNavigateTo(args.ReadParameter<int>());
					break;
				case ShowHighlightCommand:
					DoNavigateTo(args.ReadParameter<int>());
					DoShowHighlight(args.ReadParameter<double>(), args.ReadParameter<double>());
					break;
				default:
					MessageBox.Show("Unknown command: " + name);
					break;
			}
		}
		void BroadcastNavigateTo(int index) { Orchestrator.BroadcastViewCommand(NavigateCommand, index); }

		void NavigateBy(int delta) { BroadcastNavigateTo(currentIndex + delta); }
		void ShowHighlight(double imageX, double imageY) {
			Orchestrator.BroadcastViewCommand(ShowHighlightCommand, currentIndex, imageX, imageY);
		}
		void DoShowHighlight(double imageX, double imageY) {
			//If we were out of sync, and the first command 
			//was a ShowHighlight, ignore it, since we don't
			//have an image yet.
			if (ImageControl.Source == null) return;

			var imagePos = new Point(imageX * ImageControl.ActualWidth, imageY * ImageControl.ActualHeight);
			var canvasPos = ImageControl.TranslatePoint(imagePos, canvas);

			var h = new Controls.CircleHighlight();
			Canvas.SetLeft(h, canvasPos.X - h.Width / 2);
			Canvas.SetTop(h, canvasPos.Y - h.Height / 2);
			canvas.Children.Add(h);
		}
	}
}
