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
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace NetPresenter {
	/// <summary>
	/// Interaction logic for JumboMenu.xaml
	/// </summary>
	public partial class JumboMenu : Window {
		List<MenuGroup> groups;
		List<ICommand> allCommands;
		JumboMenu() { InitializeComponent(); }

		public static void Show(params MenuGroup[] groups) {
			var menu = new JumboMenu();

			menu.groups = groups.Where(g => g.Any()).ToList();
			menu.groups.Insert(0, new MenuGroup("Menu", new ActionCommand("Back", menu.Close)));

			menu.allCommands = menu.groups.SelectMany(g => g.Items).ToList();
			menu.tree.ItemsSource = menu.groups;

			menu.ShowDialog();
		}

		private TreeViewItem GetItem(ICommand command) {
			var group = groups.Single(g => g.Contains(command));
			var groupItem = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromItem(group);
			return (TreeViewItem)groupItem.ItemContainerGenerator.ContainerFromItem(command);
		}
		void SelectCommand(ICommand command) {
			var menuItem = GetItem(command);
			menuItem.IsSelected = true;
			menuItem.Focus();
		}
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			var screen = App.LogicalScreens.Single(s => s.Contains(App.Cursor)).ToLogicalPixels(this);
			Left = screen.X + (screen.Width - ActualWidth) / 2;
			Top = screen.Y + (screen.Height - ActualHeight) / 2;

			SelectCommand(groups[0].Items[0]);
		}

		private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			var group = e.NewValue as MenuGroup;
			if (group != null) {
				var menuItem = GetItem(group.Items[0]);
				menuItem.IsSelected = true;
				menuItem.Focus();
			} else if (!isMouseSelecting) {
				var command = e.NewValue as ICommand;
				var item = GetItem(command);

				App.Cursor = item.PointToScreen(new Point(item.ActualWidth / 2, item.ActualHeight / 2));
			}
		}

		private void tree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			var command = (ICommand)tree.SelectedItem;
			if (!GetItem(command).IsMouseOver)
				return;
			Close();
			command.Execute();
		}

		private void tree_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
			var item = (ICommand)tree.SelectedItem;

			var newIndex = allCommands.IndexOf(item) - Math.Sign(e.Delta);
			if (newIndex >= 0 && newIndex < allCommands.Count)
				SelectCommand(allCommands[newIndex]);
			e.Handled = true;
		}

		bool isMouseSelecting;
		private void MenuItem_MouseEnter(object sender, MouseEventArgs e) {
			var elem = (FrameworkElement)sender;
			try {
				isMouseSelecting = true;
				SelectCommand((ICommand)elem.DataContext);
			} finally { isMouseSelecting = false; }
		}

		private void Window_KeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Escape:
					Close();
					return;
			}
		}
	}
	//For designer support
	class GroupSet {
		public GroupSet() {
			Groups = new ObservableCollection<MenuGroup> {
				new MenuGroup("Menu", new ActionCommand("Back", null)),
				new MenuGroup("Slideshow Command", new ActionCommand("Start over", null)),
				new MenuGroup("Views", new ActionCommand("Message", null), new ActionCommand("Slideshow", null)),
				new MenuGroup("System", new ActionCommand("Exit", null)),
			};
		}

		public ObservableCollection<MenuGroup> Groups { get; private set; }
	}
	public class MenuGroup : IGrouping<string, ICommand> {
		public MenuGroup(string name, IEnumerable<ICommand> items) { Name = name; Items = new ReadOnlyCollection<ICommand>(items.ToArray()); }
		public MenuGroup(string name, params ICommand[] items) { Name = name; Items = new ReadOnlyCollection<ICommand>(items); }

		public ReadOnlyCollection<ICommand> Items { get; private set; }
		public string Name { get; private set; }
		public string Key { get { return Name; } }

		public IEnumerator<ICommand> GetEnumerator() { return Items.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
