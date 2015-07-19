using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPresenter {
	public abstract class CommandArguments {
		public abstract T ReadParameter<T>();
	}
	public class LocalCommandArguments : CommandArguments {
		public LocalCommandArguments(params object[] arguments) { this.arguments = arguments; }
		readonly object[] arguments;
		int currentIndex = -1;

		public override T ReadParameter<T>() {
			return (T)arguments[++currentIndex];
		}
	}
}
