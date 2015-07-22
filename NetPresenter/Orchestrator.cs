using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;

namespace NetPresenter {
	public class Orchestrator {
		static Orchestrator Instance { get; set; }
		public static void Run(IEnumerable<Rect> screens) {
			if (Instance != null)
				throw new InvalidOperationException();
			Log.Write("---------------------------\n---------------------------\nStarting with " + screens.Count() + " screens");

			Instance = new Orchestrator(screens);

			foreach (var host in Instance.Hosts) {
				host.Show();
			}
			Instance.SetLocalViews(Instance.AvailableViews[0]);
			Instance.Multicaster.StartListener();
		}

		readonly Multicaster Multicaster;

		private Orchestrator(IEnumerable<Rect> screens) {
			Multicaster = new Multicaster();
			Multicaster.CommandReceived += Multicaster_CommandReceived;

			Hosts = new ReadOnlyCollection<ViewHost>(screens.Select(s => new ViewHost(this, s)).ToArray());
			AvailableViews = new ViewCommandCollection(new[] {
				TryCreateSingletonCommand("Intro", Views.IntroView.TryCreateFactory(@"Intro\")),
				new ViewCommand(this, "Message", o => new Views.MessageView(o))
			}.Concat(Directory.EnumerateDirectories(Environment.CurrentDirectory)
							  .Where(d => !Path.GetFileName(d).Equals("Intro", StringComparison.OrdinalIgnoreCase)
										&& Directory.EnumerateFiles(d, "*.jpg").Any())
							  .Select(d => new ViewCommand(this,
									"Photos: " + Path.GetFileName(d),
									o => new Views.SlideshowView(o, "Photos: " + Path.GetFileName(d), d)
								))
			).Concat(Directory.EnumerateDirectories(Environment.CurrentDirectory)
							  .Where(d => Directory.EnumerateFiles(d)
												   .Any(p => Views.VideoView.extensions.Contains(Path.GetExtension(p))))
							  .Select(d => new ViewCommand(this,
									"Videos: " + Path.GetFileName(d),
									o => new Views.VideoView(o, "Photos: " + Path.GetFileName(d), d)
								))
			));
		}

		ViewCommand TryCreateSingletonCommand(string name, Func<ViewBase> viewFactory) {
			return viewFactory == null ? null : new ViewCommand(this, name, o => viewFactory());
		}

		const string SetViewCommand = "SetView";
		void Multicaster_CommandReceived(object sender, CommandEventArgs e) {
			if (IsOffline)
				return;
			Log.Write("Received " + e.Name);
			switch (e.Name) {
				case SetViewCommand:
					SetLocalViews(AvailableViews[e.CreateArguments().ReadParameter<string>()]);
					break;
				default:
					foreach (var host in Hosts) {
						var args = e.CreateArguments();
						var viewName = args.ReadParameter<string>();

						if (viewName != host.View.ViewName) {
							Log.Write("WRONG VIEW!   Switching from " + host.View.ViewName + " to " + viewName);
							host.SetView(AvailableViews[viewName].CreateView);
						}

						currentViewName = viewName;

						host.View.ExecuteCommand(e.Name, args);
					}
					break;
			}
		}

		public ReadOnlyCollection<ViewHost> Hosts { get; private set; }

		///<summary>If true, network command will be ignored, creating a standalone instance within a network.</summary>
		public bool IsOffline { get; private set; }

		public void SendCommand(string commandName, params object[] args) {
			if (!IsOffline)
				Multicaster.SendCommand(commandName, args);
		}

		public void SetView(ViewCommand view) {
			SendCommand(SetViewCommand, view.ViewName);
			SetLocalViews(view);
		}
		string currentViewName;
		void SetLocalViews(ViewCommand view) {
			currentViewName = view.ViewName;
			foreach (var host in Hosts) {
				host.SetView(view.CreateView);
			}
		}

		public ViewCommandCollection AvailableViews { get; private set; }
		public void ShowMenu(ViewBase view) {
			JumboMenu.Show(
				new MenuGroup(view.ViewName + " Commands", view.GetMenuCommands()),
				new MenuGroup("Views", AvailableViews),
				new MenuGroup("System", new ToggleOfflineCommand(this), ExitCommand.Instance)
			);
		}
		class ExitCommand : ICommand {
			private ExitCommand() { }
			public static readonly ExitCommand Instance = new ExitCommand();

			public string Name { get { return "Exit"; } }

			public void Execute() {
				if (MessageBoxResult.Yes == MessageBox.Show("Are you sure you want to exit?", "NetPresenter",
															MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No))
					Application.Current.Shutdown();
			}
		}

		class ToggleOfflineCommand : ICommand {
			readonly Orchestrator orchestrator;

			public ToggleOfflineCommand(Orchestrator orchestrator) {
				this.orchestrator = orchestrator;
			}

			public string Name { get { return orchestrator.IsOffline ? "Reconnect" : "Go offline"; } }

			public void Execute() { orchestrator.IsOffline = !orchestrator.IsOffline; }
		}

		public void BroadcastViewCommand(string name, params object[] parameters) {
			var broadcastParams = new object[parameters.Length + 1];
			broadcastParams[0] = currentViewName;
			Array.Copy(parameters, 0, broadcastParams, 1, parameters.Length);

			SendCommand(name, broadcastParams);
			foreach (var host in Hosts) {
				host.View.ExecuteCommand(name, new LocalCommandArguments(parameters));
			}
		}
	}
}
