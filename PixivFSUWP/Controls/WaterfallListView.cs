using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Controls
{
    //似乎是我的panel弄坏了ListView，这里只能自己造一个(lll￢ω￢)
    public class WaterfallListView : ListView
    {
        bool busyLoading = false;
        private ScrollViewer ScrollViewer;

        public WaterfallListView() : base()
        {
            //不使用base的增量加载
            IncrementalLoadingTrigger = IncrementalLoadingTrigger.None;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ScrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            ScrollViewer.ViewChanged += ScrollViewer_ViewChanged;
            _ = LoadPage();
        }

        private async Task LoadPage()
        {
            busyLoading = true;
            try
            {
                while (ScrollViewer.ScrollableHeight == 0)
                    try
                    {
                        var res = await (ItemsSource as ISupportIncrementalLoading)?.LoadMoreItemsAsync(0);
                        if (res.Count == 0) return;
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
            }
            finally
            {
                busyLoading = false;
            }
        }

        public double VerticalOffset => ScrollViewer.VerticalOffset;
        public void ScrollToOffset(double? verticalOffset = null, float? zoomFactor = null) => ScrollViewer.ChangeView(null, verticalOffset, null, true);

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (busyLoading) return;
            busyLoading = true;
            try
            {
                while ((sender as ScrollViewer).VerticalOffset >= (sender as ScrollViewer).ScrollableHeight - 500)
                {
                    try
                    {
                        var res = await (ItemsSource as ISupportIncrementalLoading)?.LoadMoreItemsAsync(0);
                        if (res.Count == 0) return;
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                    catch { }
                }
            }
            finally
            {
                busyLoading = false;
            }
        }

        public void ScrollToItem(UIElement element)
        {
            var transform = TransformToVisual(element);
            Point absolutePosition = transform.TransformPoint(new Point(0, 0));
            ScrollViewer.ChangeView(null, -absolutePosition.Y - 75, null, true);
        }
    }
}
