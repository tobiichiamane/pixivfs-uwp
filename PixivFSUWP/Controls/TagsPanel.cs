using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PixivFSUWP.Controls
{
    public class TagsPanel : Panel
    {
        //行距
        public static readonly DependencyProperty ItemVerticalMarginProperty =
            DependencyProperty.Register("ItemVerticalMargin", typeof(double),
        typeof(TagsPanel), new PropertyMetadata((double)0,
            (DepObj, e) =>
            {
                (DepObj as TagsPanel).InvalidateMeasure();
                (DepObj as TagsPanel).InvalidateArrange();
            }));

        public double ItemVerticalMargin
        {
            get => (double)GetValue(ItemVerticalMarginProperty);
            set => SetValue(ItemVerticalMarginProperty, value);
        }

        //项目水平间隔
        public static readonly DependencyProperty ItemHorizontalMarginProperty =
            DependencyProperty.Register("ItemHorizontalMargin", typeof(double),
        typeof(TagsPanel), new PropertyMetadata((double)0,
            (DepObj, e) =>
            {
                (DepObj as TagsPanel).InvalidateMeasure();
                (DepObj as TagsPanel).InvalidateArrange();
            }));

        public double ItemHorizontalMargin
        {
            get => (double)GetValue(ItemHorizontalMarginProperty);
            set => SetValue(ItemHorizontalMarginProperty, value);
        }

        //行高
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(double),
        typeof(TagsPanel), new PropertyMetadata((double)20,
            (DepObj, e) =>
            {
                (DepObj as TagsPanel).InvalidateMeasure();
                (DepObj as TagsPanel).InvalidateArrange();
            }));

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0) return new Size(0, 0);
            List<double> widths = new List<double>();
            widths.Add(-ItemHorizontalMargin);
            int rowCount = 1;
            foreach (var child in Children)
            {
                child.Measure(new Size(availableSize.Width, ItemHeight));
                double tmpWidth = widths[rowCount - 1] + ItemHorizontalMargin + child.DesiredSize.Width;
                if (tmpWidth > availableSize.Width)
                {
                    rowCount++;
                    widths.Add(child.DesiredSize.Width);
                }
                else
                    widths[rowCount - 1] = tmpWidth;
            }
            return new Size(0, rowCount * ItemHeight + (rowCount - 1) * ItemVerticalMargin);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            List<double> widths = new List<double>();
            widths.Add(-ItemHorizontalMargin);
            int rowCount = 1;
            foreach (var child in Children)
            {
                double tmpWidth = widths[rowCount - 1] + ItemHorizontalMargin + child.DesiredSize.Width;
                if (tmpWidth > finalSize.Width)
                {
                    rowCount++;
                    widths.Add(child.DesiredSize.Width);
                    child.Arrange(new Rect(0, (rowCount - 1) * (ItemHeight + ItemVerticalMargin),
                        child.DesiredSize.Width, child.DesiredSize.Height));
                }
                else
                {
                    child.Arrange(new Rect(widths[rowCount - 1] + ItemHorizontalMargin,
                        (rowCount - 1) * (ItemHeight + ItemVerticalMargin),
                        child.DesiredSize.Width, child.DesiredSize.Height));
                    widths[rowCount - 1] = tmpWidth;
                }
            }
            return finalSize;
        }
    }
}
