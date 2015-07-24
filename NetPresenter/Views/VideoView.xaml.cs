using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace NetPresenter.Views {
	/// <summary>
	/// Base class for clone and master video views.
	/// </summary>
	public abstract partial class VideoView : ViewBase {
		readonly Orchestrator orchestrator;
		readonly string viewName;
		readonly IReadOnlyList<string> fileNames;

		readonly DispatcherTimer trackBarTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(.1) };


		private VideoView(Orchestrator orchestrator, string viewName, IReadOnlyList<string> fileNamess) {
			InitializeComponent();

			this.viewName = viewName;
			this.fileNames = fileNamess;
			this.orchestrator = orchestrator;

			Loaded += delegate {
				Focus();
				VideoIndex = 0;
			};
			trackBarTimer.Tick += delegate { UpdateTrackBar(); };
		}

		private void DisplayDuration(MediaElement player) {
			if (!player.NaturalDuration.HasTimeSpan)
				return;
			trackBar.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
			duration.Content = player.NaturalDuration.TimeSpan.ToString(@"h\:mm\:ss");
		}
		private void SetPlayerContent(UIElement visual) {
			Grid.SetRowSpan(visual, 2);
			Grid.SetColumnSpan(visual, 3);
			// Place the video behind the other UI.
			grid.Children.Insert(0, visual);
		}

		public override string ViewName { get { return viewName; } }

		public static readonly ISet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			".avi", ".mp4", ".wmv"
		};

		public virtual bool IsPlaying {
			get { return trackBarTimer.IsEnabled; }
			set { trackBarTimer.IsEnabled = value; }
		}

		int videoIndex = -1;
		public int VideoIndex {
			get { return videoIndex; }
			set {
				if (videoIndex == value)
					return;
				videoIndex = (fileNames.Count + value) % fileNames.Count;
				SetVideo(fileNames[videoIndex]);
			}
		}
		protected virtual void SetVideo(string filenName) {
			videoName.Content = Path.GetFileNameWithoutExtension(filenName);
			UpdateTrackBar();
		}

		public abstract TimeSpan Position { get; set; }

		#region UI Handlers
		private void ViewBase_PreviewKeyDown(object sender, KeyEventArgs e) {
			e.Handled = true;
			switch (e.Key) {
				case Key.Space:
					SetState(isPlaying: !IsPlaying);
					return;
				case Key.Left:
					SetState(videoIndex: videoIndex - 1, position: TimeSpan.Zero, isPlaying: false);
					return;
				case Key.Right:
					SetState(videoIndex: videoIndex + 1, position: TimeSpan.Zero, isPlaying: false);
					return;
			}
			e.Handled = false;
		}
		private void ViewBase_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			SetState(isPlaying: !IsPlaying);
		}

		bool isTrackBarUpdating;
		void UpdateTrackBar() {
			try {
				isTrackBarUpdating = true;
				trackBar.Value = Position.TotalSeconds;
			} finally { isTrackBarUpdating = false; }

			currentTime.Content = Position.ToString(@"h\:mm\:ss");
		}
		private void trackBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (!isTrackBarUpdating)
				SetState(position: TimeSpan.FromSeconds(e.NewValue));
		}
		#endregion

		void SetState(int? videoIndex = null, TimeSpan? position = null, bool? isPlaying = null) {
			orchestrator.BroadcastViewCommand(SetStateCommand,
				videoIndex ?? VideoIndex, (position ?? Position).Ticks, isPlaying ?? IsPlaying);
		}

		const string SetStateCommand = "SetState";
		public override void ExecuteCommand(string name, CommandArguments parameters) {
			switch (name) {
				case SetStateCommand:
					VideoIndex = parameters.ReadParameter<int>();
					Position = TimeSpan.FromTicks(parameters.ReadParameter<long>());
					IsPlaying = parameters.ReadParameter<bool>();
					UpdateTrackBar();
					break;
				default:
					MessageBox.Show("Unknown command: " + name);
					break;
			}
		}

		///<summary>A VideoView that creates and owns the <see cref="MediaElement"/> displaying the video.</summary>
		class MasterView : VideoView {
			public MediaElement Player { get; }

			public MasterView(Orchestrator orchestrator, string viewName, string directory)
				: base(orchestrator, viewName, GetVideos(directory)) {
				Player = new MediaElement {
					ScrubbingEnabled = true,
					LoadedBehavior = MediaState.Manual,
					UnloadedBehavior = MediaState.Manual,
					Volume = 1
				};
				SetPlayerContent(Player);

				Loaded += delegate {
					// Force the player to render a frame.
					Player.Play();
					Player.Pause();
				};
				Unloaded += delegate {
					IsPlaying = false;
				};

				Player.MediaOpened += delegate {
					DisplayDuration(Player);
				};
				Player.MediaEnded += delegate {
					SetState(videoIndex: videoIndex + 1, position: TimeSpan.Zero, isPlaying: false);
				};

				Player.MediaFailed += (s, e) => MessageBox.Show(e.ErrorException.Message, "Video - NetPresenter", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			static IReadOnlyList<string> GetVideos(string directory) {
				return Directory.EnumerateFiles(directory)
								.Where(p => extensions.Contains(Path.GetExtension(p)))
								.OrderBy(p => p)
								.ToList();
			}
			protected override void SetVideo(string filenName) {
				Player.Source = new Uri(fileNames[VideoIndex]);
				base.SetVideo(filenName);
			}

			public override bool IsPlaying {
				get { return base.IsPlaying; }
				set {
					base.IsPlaying = value;
					if (value)
						Player.Play();
					else
						Player.Pause();
				}
			}
			public override TimeSpan Position {
				get { return Player.Position; }
				set { Player.Position = value; }
			}
		}

		///<summary>A VideoView that mirrors the player from an existing <see cref="MasterView"/>.</summary>
		class CloneView : VideoView {
			readonly MediaElement ownerPlayer;

			public CloneView(MasterView owner) : base(owner.orchestrator, owner.viewName, owner.fileNames) {
				ownerPlayer = owner.Player;
				ownerPlayer.MediaOpened += delegate {
					DisplayDuration(ownerPlayer);
				};

				SetPlayerContent(new Border {
					Background = new VisualBrush(ownerPlayer) { Stretch = Stretch.Uniform }
				});
			}


			public override TimeSpan Position {
				get { return ownerPlayer.Position; }
				set { }     // Let the master view set the position; we don't need to do anything
			}
		}

		public static ViewCommand CreateFactory(Orchestrator orchestrator, string directory) {
			string viewName = "Videos: " + Path.GetFileName(directory);

			MasterView master = null;

			return new ViewCommand(orchestrator, viewName, o => {
				if (master != null)
					return new CloneView(master);
				master = new MasterView(o, viewName, directory);
				master.Unloaded += delegate { master = null; };
				return master;
			});
		}
	}
}
