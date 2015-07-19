using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using TransitionEffects;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NetPresenter.Controls {
	static partial class Transitioner {
		static readonly Random Rand = new Random((int)DateTime.Now.Ticks ^ Environment.TickCount ^ Environment.MachineName.GetHashCode());

		static TransitionEffect GetEffect() {
			var retVal = AllEffects[Rand.Next(AllEffects.Count)]();

			var randEffect = retVal as RandomizedTransitionEffect;
			if (randEffect != null)
				randEffect.RandomSeed = Rand.NextDouble();

			return retVal;
		}

		public static void DoTransition(FrameworkElement target, Action newContentCreator, Action onCompleted) {
			var originalBitmap = CaptureScreenBitmap(target, (int)target.ActualWidth, (int)target.ActualHeight);
			var originalBrush = new ImageBrush(originalBitmap);	//I can't use a VisualBrush because the visual is about to change.

			newContentCreator();

			var effect = GetEffect();
			effect.OldImage = originalBrush;

			var animation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(2)), FillBehavior.HoldEnd);
			animation.AccelerationRatio = 0.5;
			animation.DecelerationRatio = 0.5;
			animation.Completed += delegate {
				if (target.Effect == effect) {
					target.Effect = null;
					onCompleted();
				}
				effect.OldImage = null;
			};
			effect.BeginAnimation(TransitionEffect.ProgressProperty, animation);
			target.Effect = effect;
		}


		private static BitmapSource CaptureScreenBitmap(Visual target, int width, int height) {
			Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
			var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

			DrawingVisual visual = new DrawingVisual();
			using (DrawingContext context = visual.RenderOpen()) {
				context.DrawRectangle(new VisualBrush(target), null, new Rect(new Point(), bounds.Size));
			}
			renderBitmap.Render(visual);
			return renderBitmap;
		}
	}
}
