using PixivCS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PixivFSUWP.Interfaces;
using static PixivFSUWP.Data.OverAll;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WaterfallPage : Page, IGoBackFlag
    {
        public enum ListContent
        {
            Recommend,
            Bookmark,
            Following,
            Ranking
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        ListContent listContent;

        public WaterfallPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ListContent) listContent = (ListContent)e.Parameter;
            switch (listContent)
            {
                case ListContent.Recommend:
                    WaterfallListView.ItemsSource = Data.OverAll.RecommendList;
                    Data.OverAll.RecommendList.ResumeLoading();
                    break;
                case ListContent.Bookmark:
                    WaterfallListView.ItemsSource = Data.OverAll.BookmarkList;
                    Data.OverAll.BookmarkList.ResumeLoading();
                    break;
                case ListContent.Following:
                    WaterfallListView.ItemsSource = Data.OverAll.FollowingList;
                    Data.OverAll.FollowingList.ResumeLoading();
                    break;
                case ListContent.Ranking:
                    WaterfallListView.ItemsSource = Data.OverAll.RankingList;
                    Data.OverAll.RankingList.ResumeLoading();
                    break;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            switch (listContent)
            {
                case ListContent.Recommend:
                    Data.OverAll.RecommendList.PauseLoading();
                    break;
                case ListContent.Bookmark:
                    Data.OverAll.BookmarkList.PauseLoading();
                    break;
                case ListContent.Following:
                    Data.OverAll.FollowingList.PauseLoading();
                    break;
                case ListContent.Ranking:
                    Data.OverAll.RankingList.PauseLoading();
                    break;
            }
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(WaterfallPage), listContent);
                ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
            }
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActualWidth < 700) (sender as Controls.WaterfallContentPanel).Colums = 3;
            else if (ActualWidth < 900) (sender as Controls.WaterfallContentPanel).Colums = 4;
            else if (ActualWidth < 1100) (sender as Controls.WaterfallContentPanel).Colums = 5;
            else (sender as Controls.WaterfallContentPanel).Colums = 6;
        }

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(IllustDetailPage),
                (e.ClickedItem as ViewModels
                .WaterfallItemViewModel).ItemId);
        }

        ViewModels.WaterfallItemViewModel tapped = null;

        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            quickStar.Text = (tapped.IsBookmarked) ?
                GetResourceString("DeleteBookmarkPlain") :
                GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private void WaterfallListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            quickStar.Text = (tapped.IsBookmarked) ?
                GetResourceString("DeleteBookmarkPlain") :
                GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private async void QuickStar_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            var i = tapped;
            var title = i.Title;
            try
            {
                //用Title作标识，表明任务是否在执行
                i.Title = null;
                if (i.IsBookmarked)
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .IllustBookmarkDelete(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = false;
                        i.Stars--;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                            ShowTip(string.Format(GetResourceString("DeletedBookmarkPlain"), title));
                    }
                    else
                    {
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                            ShowTip(string.Format(GetResourceString("BookmarkDeleteFailedPlain"), title));
                    }
                }
                else
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .IllustBookmarkAdd(i.ItemId.ToString());
                        res = true;
                    }
                    catch
                    {
                        res = false;
                    }
                    i.Title = title;
                    if (res)
                    {
                        i.IsBookmarked = true;
                        i.Stars++;
                        i.NotifyChange("StarsString");
                        i.NotifyChange("IsBookmarked");
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                            ShowTip(string.Format(GetResourceString("WorkBookmarkedPlain"), title));
                    }
                    else
                    {
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                            ShowTip(string.Format(GetResourceString("WorkBookmarkFailedPlain"), title));
                    }
                }
            }
            finally
            {
                //确保出错时数据不被破坏
                i.Title = title;
            }
        }

        private void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            DownloadQueue.Add(tapped, Frame);
        }
    }
}