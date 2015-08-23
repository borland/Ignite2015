using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace ConditionalWeakTable
{
    static class UIElementExtensions
    {
        class IconOverlay
        {
            public BitmapIcon Icon;
            public SolidColorBrush Brush;
        }

        static ConditionalWeakTable<UIElement, IconOverlay> s_iconOverlays = new ConditionalWeakTable<UIElement, IconOverlay>();

        public static void SetIconOverlay(this UIElement view, BitmapIcon icon)
        {
            var overlay = s_iconOverlays.GetOrCreateValue(view);
            overlay.Icon = icon;
            view.GotFocus += View_GotFocus; // hrm weak event?
        }

        private static void View_GotFocus(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
