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

namespace NetPresenter.Views {
	/// <summary>
	/// Interaction logic for MessageView.xaml
	/// </summary>
	public partial class MessageView : ViewBase {
		readonly Orchestrator Orchestrator;
		public MessageView(Orchestrator orchestrator) {
			InitializeComponent();
			Orchestrator = orchestrator;
			DoSetMessage("Welcome!");
		}

		public override string ViewName { get { return "Message"; } }

		public override IEnumerable<ICommand> GetMenuCommands() {
			yield return new ActionCommand("Enter message", delegate {
				inputPanel.Visibility = Visibility.Visible;
				newMessage.Focus();
			});
		}

		private void SendMessage_Click(object sender, RoutedEventArgs e) {
			inputPanel.Visibility = Visibility.Collapsed;
			SetMessage(newMessage.Text);
		}

		const string SetMessageCommand = "SetMessage";
		public override void ExecuteCommand(string name, CommandArguments parameters) {
			switch (name) {
				case SetMessageCommand:
					DoSetMessage(parameters.ReadParameter<string>());
					break;
				default:
					MessageBox.Show("Unknown command: " + name);
					break;
			}
		}
		void SetMessage(string message) { Orchestrator.BroadcastViewCommand(SetMessageCommand, message); }
		void DoSetMessage(string message) {
			this.message.Text = newMessage.Text = message;
		}
	}
}
