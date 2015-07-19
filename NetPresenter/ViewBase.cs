using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace NetPresenter {
	public class ViewBase : UserControl {
		public virtual string ViewName { get { return GetType().Name; } }

		public virtual IEnumerable<ICommand> GetMenuCommands() { yield break; }
		public virtual void ExecuteCommand(string name, CommandArguments parameters) { }
	}
}
