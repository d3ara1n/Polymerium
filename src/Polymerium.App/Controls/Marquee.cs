using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Controls
{
    public class Marquee : ItemsControl
    {
        public int SelectedIndex { get; private set; } = -1;

        private readonly Storyboard enter;
        private readonly Storyboard leave;

        private ContentPresenter _container = null!;
        private TranslateTransform _translate = null!;

        public Marquee()
        {
            enter = new Storyboard();
            enter.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                To = 0
            });
            enter.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(100)),
                To = 1
            });
            leave = new Storyboard();
            leave.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                From = 0
            });
            leave.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(100)),
                To = 0
            });
            leave.Completed += Leave_Completed;
        }

        protected override void OnApplyTemplate()
        {
            _container = (ContentPresenter)GetTemplateChild("PART_Container");
            _translate = (TranslateTransform)GetTemplateChild("PART_Translate");
            Storyboard.SetTarget(leave.Children[0], _translate);
            Storyboard.SetTarget(enter.Children[0], _translate);
            Storyboard.SetTargetProperty(leave.Children[0], "Y");
            Storyboard.SetTargetProperty(enter.Children[0], "Y");
            Storyboard.SetTarget(leave.Children[1], _container);
            Storyboard.SetTarget(enter.Children[1], _container);
            Storyboard.SetTargetProperty(leave.Children[1], "Opacity");
            Storyboard.SetTargetProperty(enter.Children[1], "Opacity");
        }

        public void Switch(int index)
        {
            SelectedIndex = index;
            ((DoubleAnimation)leave.Children[0]).To = -ActualHeight / 2;
            leave.Begin();
        }

        private void Leave_Completed(object? sender, object e)
        {
            var obj = Items[SelectedIndex];
            _container.Content = obj;
            ((DoubleAnimation)enter.Children[0]).From = ActualHeight / 2;
            enter.Begin();
        }
    }
}
