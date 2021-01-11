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
using Windows.UI.Core;
using PixivFSUWP.Data.Collections;

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
            Ranking,
            SearchResult,
        }

        public SearchResultIllustsCollection ItemsSource;
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

        int? clicked = null;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ListContent) listContent = (ListContent)e.Parameter;
            else if(e.Parameter is ValueTuple<ListContent,int?> tuple)
            {
                (listContent, clicked) = tuple;
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
                    ItemsSource = Data.OverAll.SearchResultList;
                    Data.OverAll.SearchResultList.ResumeLoading();
                    WaterfallListView.ItemsSource = ItemsSource;
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
                case ListContent.SearchResult:
                    Data.OverAll.SearchResultList.PauseLoading();
                    break;
            }
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(WaterfallPage), (listContent, clickedIndex));
                TheMainPage?.UpdateNavButtonState();
            }
            ItemsSource = null;
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            var WaterfallContent = sender as Controls.WaterfallContentPanel;
            if (ActualWidth < 700) WaterfallContent.Colums = 3;
            else if (ActualWidth < 900) WaterfallContent.Colums = 4;
            else if (ActualWidth < 1100) WaterfallContent.Colums = 5;
            else WaterfallContent.Colums = 6;
            if (clicked != null)
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    try
                    {
                        var element = WaterfallListView.ContainerFromIndex(clicked.Value) as UIElement;
                        WaterfallListView.ScrollToItem(element);
                    }
                    catch
                    { }
                });
            }
        }

        //记录点击的项目索引
        int? clickedIndex = null;

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            switch (listContent)
            {
                case ListContent.Recommend:
                    clickedIndex = Data.OverAll.RecommendList.IndexOf(e.ClickedItem as ViewModels.WaterfallItemViewModel);
                    break;
                case ListContent.Bookmark:
                    clickedIndex = Data.OverAll.BookmarkList.IndexOf(e.ClickedItem as ViewModels.WaterfallItemViewModel);
                    break;
                case ListContent.Following:
                    clickedIndex = Data.OverAll.FollowingList.IndexOf(e.ClickedItem as ViewModels.WaterfallItemViewModel);
                    break;
                case ListContent.Ranking:
                    clickedIndex = Data.OverAll.RankingList.IndexOf(e.ClickedItem as ViewModels.WaterfallItemViewModel);
                    break;
                case ListContent.SearchResult:
                    clickedIndex = Data.OverAll.SearchResultList.IndexOf(e.ClickedItem as ViewModels.WaterfallItemViewModel);
                    break;
            }

            if (ItemsSource != null)
                (((Frame.Parent as Grid).Parent as Page).Parent as Frame)
                .Navigate(typeof(IllustDetailPage),
                (new ValueTuple<int, int?>((e.ClickedItem as ViewModels
                .WaterfallItemViewModel).ItemId, clickedIndex)), 
                App.DrillInTransitionInfo);
            else
                Frame.Navigate(typeof(IllustDetailPage),
                (e.ClickedItem as ViewModels
                .WaterfallItemViewModel).ItemId, App.DrillInTransitionInfo);
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

        private async void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            var i = tapped;
            try
            {
                await TheMainPage?.ShowTip("Getting File Format ...");
                var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .GetIllustDetailAsync(i.ItemId.ToString());
                var illust = Data.IllustDetail.FromObject(res);
                FileSavePicker picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add(GetResourceString("ImageFilePlain"), new List<string>() { Path.GetExtension(illust.OriginalUrls[0]) });
                picker.SuggestedFileName = i.Title;
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    Data.DownloadManager.NewJob(illust.Title, illust.OriginalUrls[0], file);
                    //using (var imgstream = await Data.OverAll.DownloadImage(illust.OriginalUrls[0]))
                    //{
                    //    using (var filestream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    //    {
                    //        await imgstream.CopyToAsync(filestream.AsStream());
                    //    }
                    //}
                    //var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                    //if (updateStatus == FileUpdateStatus.Complete)
                    //    await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSavedPlain"), i.Title));
                    //else
                    //    await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSaveFailedPlain"), i.Title));
                }
            }
            catch
            {
                await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSaveFailedPlain"), i.Title));
            }
        }
    }
}