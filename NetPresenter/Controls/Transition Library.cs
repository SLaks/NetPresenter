using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TransitionEffects;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Windows;

namespace NetPresenter.Controls {
	partial class Transitioner {
		private static ReadOnlyCollection<Func<TransitionEffect>> AllEffects = new ReadOnlyCollection<Func<TransitionEffect>>(new Func<TransitionEffect>[] {
				() => new ShrinkTransitionEffect(),
				() => new BlindsTransitionEffect(),
				() => new CloudRevealTransitionEffect(),
				() => new RandomCircleRevealTransitionEffect(),
				() => new FadeTransitionEffect(),

				() => new WaveTransitionEffect(),
				() => new RadialWiggleTransitionEffect(),
  
				() => new BloodTransitionEffect(),
				() => new CircleStretchTransitionEffect(),
  
				() => new DisolveTransitionEffect(),
				() => new DropFadeTransitionEffect(),   

				() => new RotateCrumbleTransitionEffect(),
				() => new WaterTransitionEffect(),
				() => new CrumbleTransitionEffect(),

				() => new RadialBlurTransitionEffect(),
				() => new CircularBlurTransitionEffect(),

				() => new PixelateTransitionEffect(),
				() => new PixelateInTransitionEffect(),
				() => new PixelateOutTransitionEffect(),
				//new SwirlGridTransitionEffect(Math.PI * 4), 
				//new SwirlGridTransitionEffect(Math.PI * 16),
				//new SmoothSwirlGridTransitionEffect( Math.PI * 4),
				//new SmoothSwirlGridTransitionEffect( Math.PI * 16),
				//new SmoothSwirlGridTransitionEffect(-Math.PI * 8),
				//new SmoothSwirlGridTransitionEffect(-Math.PI * 6),

				() => new MostBrightTransitionEffect(),
				() => new LeastBrightTransitionEffect(),

				() => new SaturateTransitionEffect(),

				() => new BandedSwirlTransitionEffect( Math.PI / 5.0, 50.0),
				() => new BandedSwirlTransitionEffect( Math.PI, 10.0),
				() => new BandedSwirlTransitionEffect(-Math.PI, 10.0),

				() => new CircleRevealTransitionEffect { FuzzyAmount = 0.0 }, 
				() => new CircleRevealTransitionEffect { FuzzyAmount = 0.1 },
				() => new CircleRevealTransitionEffect { FuzzyAmount = 0.5 },

				/* (Point origin, Point normal, Point offset, double fuzziness)*/ 
				() => new LineRevealTransitionEffect { LineOrigin = new Point(-0.2, -0.2),	LineNormal = new Point(1, 0),	LineOffset = new Point(1.4, 0),		FuzzyAmount = 0.2 },
				() => new LineRevealTransitionEffect { LineOrigin = new Point(1.2, -0.2),		LineNormal = new Point(-1, 0),	LineOffset = new Point(-1.4, 0),	FuzzyAmount = 0.2 },
				//new LineRevealTransitionEffect { LineOrigin = new Point(-.2, -0.2),		LineNormal = new Point(0, 1),	LineOffset = new Point(0, 1.4),		FuzzyAmount = 0.2 },
				//new LineRevealTransitionEffect { LineOrigin = new Point(-0.2, 1.2),		LineNormal = new Point(0, -1),	LineOffset = new Point(0, -1.4),	FuzzyAmount = 0.2 },
				//new LineRevealTransitionEffect { LineOrigin = new Point(-0.2, -0.2),	LineNormal = new Point(1, 1),	LineOffset = new Point(1.4, 1.4),	FuzzyAmount = 0.2 },
				//new LineRevealTransitionEffect { LineOrigin = new Point(1.2,	1.2),	LineNormal = new Point(-1, -1), LineOffset = new Point(-1.4, -1.4),	FuzzyAmount = 0.2 },
				() => new LineRevealTransitionEffect { LineOrigin = new Point(1.2, -0.2),		LineNormal = new Point(-1, 1),	LineOffset = new Point(-1.4, 1.4),	FuzzyAmount = 0.2 },
				() => new LineRevealTransitionEffect { LineOrigin = new Point(-0.2, 1.2),		LineNormal = new Point(1, -1),	LineOffset = new Point(1.4, -1.4),	FuzzyAmount = 0.2 },

				() => new RippleTransitionEffect(),

				() => new SlideInTransitionEffect{ SlideAmount= new Point(1, 0)},
				() => new SlideInTransitionEffect{ SlideAmount=new Point(0, 1)},
				() => new SlideInTransitionEffect{ SlideAmount=new Point(-1, 0)},
				() => new SlideInTransitionEffect{ SlideAmount=new Point(0, -1)},

				() => new SwirlTransitionEffect( Math.PI * 4),
				//new SwirlTransitionEffect(-Math.PI * 4),
				//new SwirlTransitionEffect( Math.PI * 4),
				() => new SwirlTransitionEffect(-Math.PI * 4),
		});
	}
}
