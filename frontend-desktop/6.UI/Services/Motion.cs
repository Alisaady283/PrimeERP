using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;

namespace PrimeERP.UI.Services
{
    /// <summary>
    /// رموز الحركة — نظير frontend-web/src/lib/motion.ts (Material 3: material-web/tokens/_md-sys-motion.scss).
    /// WPF بلا منحنى cubic-bezier، فالمنحنى KeySpline بنقطتيه نفسيهما.
    /// </summary>
    public static class Motion
    {
        public static readonly TimeSpan Short3 = TimeSpan.FromMilliseconds(150);
        public static readonly TimeSpan Short4 = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan Medium2 = TimeSpan.FromMilliseconds(300);
        public static readonly TimeSpan Medium4 = TimeSpan.FromMilliseconds(400);
        public static readonly TimeSpan Long2 = TimeSpan.FromMilliseconds(500);

        /// <summary>حركةٌ داخل الشاشة</summary>
        public static KeySpline Emphasized => new(0.2, 0, 0, 1);
        /// <summary>دخول</summary>
        public static KeySpline Decelerate => new(0.05, 0.7, 0.1, 1);
        /// <summary>خروج</summary>
        public static KeySpline Accelerate => new(0.3, 0, 0.8, 0.15);

        /// <summary>الفاصل بين العناصر وأقصى امتداده — كالويب: 50ms ومجموعٌ لا يتجاوز 600ms</summary>
        public const double StaggerStep = 50;
        public const double StaggerBudget = 600;

        /// <summary>فاصل دخول البطاقات واللوحات — ENTRANCE_STEP في الويب (البحث: 70–300ms بين العناصر)</summary>
        public const double EntranceStep = 80;

        public static TimeSpan EnterAt(int index) => TimeSpan.FromMilliseconds(index * EntranceStep);

        public static TimeSpan Stagger(int index, int count) =>
            TimeSpan.FromMilliseconds(count <= 1 ? 0 : index * Math.Min(StaggerStep, StaggerBudget / (count - 1)));

        /// <summary>حركة قيمةٍ إلى هدفها بمدّةٍ ومنحنى وتأخير</summary>
        public static AnimationTimeline To(double to, TimeSpan duration, KeySpline spline, TimeSpan? delay = null)
        {
            var animation = new DoubleAnimationUsingKeyFrames { BeginTime = delay ?? TimeSpan.Zero };
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(duration), spline));
            return animation;
        }

        /// <summary>حركة قيمةٍ من بدايةٍ إلى هدف</summary>
        public static AnimationTimeline FromTo(double from, double to, TimeSpan duration, KeySpline spline, TimeSpan? delay = null)
        {
            var animation = new DoubleAnimationUsingKeyFrames { BeginTime = delay ?? TimeSpan.Zero };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromTimeSpan(duration), spline));
            return animation;
        }

        /// <summary>دخول عنصر: يظهر وينزلق من أسفل — كدخول القوائم في الويب</summary>
        public static void Enter(UIElement element, TimeSpan? delay = null, double rise = 8, TimeSpan? duration = null)
        {
            var shift = element.RenderTransform as System.Windows.Media.TranslateTransform
                        ?? (element.RenderTransform as System.Windows.Media.TransformGroup)?.Children.OfType<System.Windows.Media.TranslateTransform>().FirstOrDefault();
            if (shift == null)
            {
                shift = new System.Windows.Media.TranslateTransform();
                element.RenderTransform = shift;
            }
            element.Opacity = 0;
            element.BeginAnimation(UIElement.OpacityProperty, FromTo(0, 1, duration ?? Medium2, Decelerate, delay));
            shift.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, FromTo(rise, 0, duration ?? Medium2, Decelerate, delay));
        }
    }
}
