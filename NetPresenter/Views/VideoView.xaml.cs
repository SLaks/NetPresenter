﻿using System;
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

namespace NetPresenter.Views {
	/// <summary>
	/// Interaction logic for VideoView.xaml
	/// </summary>
	public partial class VideoView : ViewBase {
		readonly Orchestrator orchestrator;
		readonly string viewName;
		readonly List<string> fileNames;
		public VideoView(Orchestrator orchestrator, string viewName, string directory) {
			InitializeComponent();

			this.viewName = viewName;
			this.orchestrator = orchestrator;

			fileNames = Directory.EnumerateFiles(directory)
								 .Where(p => extensions.Contains(Path.GetExtension(p)))
								 .OrderBy(p => p)
								 .ToList();

			Loaded += delegate {
				Focus();
				VideoIndex = 0;
			};
			player.MediaOpened += delegate {
				if (player.NaturalDuration.HasTimeSpan)
					trackBar.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
			};
			player.MediaEnded += delegate {
				SetState(videoIndex: videoIndex - 1, position: TimeSpan.Zero, isPlaying: false);
			};
		}
		public override string ViewName { get { return viewName; } }

		public static readonly ISet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			".avi", ".mp4", ".wmv"
		};

		bool isPlaying;
		public bool IsPlaying {
			get { return isPlaying; }
			set {
				isPlaying = value;
				if (value)
					player.Play();
				else
					player.Pause();
			}
		}

		int videoIndex = -1;
		public int VideoIndex {
			get { return videoIndex; }
			set {
				if (videoIndex == value)
					return;
				videoIndex = (fileNames.Count + value) % fileNames.Count;
				player.Source = new Uri(fileNames[videoIndex]);
				videoName.Content = Path.GetFileNameWithoutExtension(fileNames[videoIndex]);
			}
		}

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

		private void trackBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			SetState(position: TimeSpan.FromSeconds(e.NewValue));
		}
		#endregion

		void SetState(int? videoIndex = null, TimeSpan? position = null, bool? isPlaying = null) {
			orchestrator.BroadcastViewCommand(SetStateCommand,
				videoIndex ?? VideoIndex, (position ?? player.Position).Ticks, isPlaying ?? IsPlaying);
		}

		const string SetStateCommand = "SetState";
		public override void ExecuteCommand(string name, CommandArguments parameters) {
			switch (name) {
				case SetStateCommand:
					VideoIndex = parameters.ReadParameter<int>();
					player.Position = TimeSpan.FromTicks(parameters.ReadParameter<long>());
					IsPlaying = parameters.ReadParameter<bool>();
					break;
				default:
					MessageBox.Show("Unknown command: " + name);
					break;
			}
		}
	}
}
