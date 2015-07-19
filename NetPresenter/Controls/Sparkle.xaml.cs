using System;
using System.Collections.Generic;
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

namespace NetPresenter.Controls {
	/// <summary>
	/// Interaction logic for Sparkle.xaml
	/// </summary>
	public partial class Sparkle {
		public Sparkle() {
			this.InitializeComponent();
		}

		private void Storyboard_Completed(object sender, EventArgs e) {
			((Panel)Parent).Children.Remove(this);
		}
	}
}