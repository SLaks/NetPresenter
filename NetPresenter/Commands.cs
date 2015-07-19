using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace NetPresenter {
	public interface ICommand {
		string Name { get; }
		void Execute();
	}
	public class ActionCommand : ICommand {
		readonly Action method;
		public string Name { get; private set; }

		public ActionCommand(string name, Action method) { Name = name; this.method = method; }
		public void Execute() { method(); }
	}
	public class ViewCommand : ICommand {
		readonly Orchestrator Orchestrator;
		readonly Func<ViewBase> creator;

		public ViewCommand(Orchestrator o, string viewName, Func<Orchestrator, ViewBase> creator) {
			ViewName = viewName;
			Orchestrator = o;
			this.creator = () => creator(o);
		}

		public string Name { get { return "Open " + ViewName; } }
		public string ViewName { get; private set; }

		public void Execute() { Orchestrator.SetView(this); }
		public ViewBase CreateView() { return creator(); }
	}
	public class ViewCommandCollection : KeyedCollection<string, ViewCommand> {
		public ViewCommandCollection(IEnumerable<ViewCommand> commands) {
			foreach (var command in commands)
				Add(command);
		}

		protected override string GetKeyForItem(ViewCommand item) { return item.ViewName; }
		protected override void InsertItem(int index, ViewCommand item) {
			if (item == null) return;
			base.InsertItem(index, item);
		}
	}
}
