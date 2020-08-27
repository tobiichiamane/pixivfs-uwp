using System;

using PixivCS;

using PixivFSUWP.Data;
using PixivFSUWP.Data.Collections;
using PixivFSUWP.Interfaces;

using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class WaterfallPage : Page, IGoBackFlag
    {
        ListContent listContent;

        ViewModels.WaterfallItemViewModel tapped = null;

        //int? clicked = null;
        private double? verticalOffset;

        public SearchResultIllustsCollection ItemsSource;
        private bool _backflag { get; set; } = false;

        public WaterfallPage()
        {
            this.InitializeComponent();
        }

        private async void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (tapped is null) return;
            await IllustDetail.FromObject(await new PixivAppAPI(OverAll.GlobalBaseAPI).GetIllustDetailAsync(tapped.ItemId.ToString())).DownloadFirstImage();
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
                            .PostIllustBookmarkDeleteAsync(i.ItemId.ToString());
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
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("DeletedBookmarkPlain"), title));
                    }
                    else
                    {
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("BookmarkDeleteFailedPlain"), title));
                    }
                }
                else
                {
                    bool res;
                    try
                    {
                        await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                            .PostIllustBookmarkAddAsync(i.ItemId.ToString());
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
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkBookmarkedPlain"), title));
                    }
                    else
                    {
                        await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkBookmarkFailedPlain"), title));
                    }
                }
            }
            finally
            {
                //确保出错时数据不被破坏
                i.Title = title;
            }
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            var WaterfallContent = sender as Controls.WaterfallContentPanel;
            if (ActualWidth < 700) WaterfallContent.Colums = 3;
            else if (ActualWidth < 900) WaterfallContent.Colums = 4;
            else if (ActualWidth < 1100) WaterfallContent.Colums = 5;
            else WaterfallContent.Colums = 6;
            if (verticalOffset != null)
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        WaterfallListView.ScrollToOffset(verticalOffset);
                    }
                    catch
                    { }
                });
            }
        }

        private void WaterfallListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e) => Frame.Navigate(
            typeof(IllustDetailPage),
            (e.ClickedItem as ViewModels.WaterfallItemViewModel).ItemId,
            App.DrillInTransitionInfo);

        //记录点击的项目索引
        //int? clickedIndex = null;
        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            if (tapped == null) return;
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
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
                case ListContent.SearchResult:
                    Data.OverAll.SearchResultList.PauseLoading();
                    break;
            }
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(WaterfallPage), (listContent, WaterfallListView.VerticalOffset));
                OverAll.TheMainPage?.UpdateNavButtonState();
            }
            ItemsSource = null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ListContent content) listContent = content;
            else if (e.Parameter is ValueTuple<ListContent, double> tuple)
            {
                (listContent, verticalOffset) = tuple;
            }
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
                case ListContent.SearchResult:
                    ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(GetResourceString("SearchPagePlain"));
                    ItemsSource = Data.OverAll.SearchResultList;
                    Data.OverAll.SearchResultList.ResumeLoading();
                    WaterfallListView.ItemsSource = ItemsSource;
                    break;
            }
        }
        public void SetBackFlag(bool value) => _backflag = value;

        public enum ListContent
        {
            Recommend,
            Bookmark,
            Following,
            Ranking,
            SearchResult,
        }
    }
}