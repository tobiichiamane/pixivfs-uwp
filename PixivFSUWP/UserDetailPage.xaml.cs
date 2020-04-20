using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using PixivCS;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Provider;
using PixivFSUWP.Interfaces;
using static PixivFSUWP.Data.OverAll;
using PixivFSUWP.Data.Collections;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserDetailPage : Page, IGoBackFlag
    {
        UserIllustsCollection itemsSource;
        int userid = 0;
        Data.UserDetail detail;
        public UserDetailPage()
        {
            this.InitializeComponent();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnWorks.IsEnabled = true;
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(string.Format("{0}\n{1}\n" +
                "{2}：https://www.pixiv.net/member.php?id={3}\n" +
                "PixivFSUWP：pixiv://user?id={3}", GetResourceString("PixivUserPlain"), detail.Name, GetResourceString("LinkPlain"), detail.ID));
            request.Data.Properties.Title = string.Format("{0}：{1}", GetResourceString("ArtistPlain"), detail.Name);
            request.Data.Properties.Description = GetResourceString("UserShareTipPlain");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(GetResourceString("UserDetailPagePlain"));
            userid = (int)e.Parameter;
            itemsSource = new UserIllustsCollection(userid.ToString());
            itemsSource.CollectionChanged += ItemsSource_CollectionChanged;
            WaterfallListView.ItemsSource = itemsSource;
            base.OnNavigatedTo(e);
            _ = loadContents();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            itemsSource?.StopLoading();
            itemsSource = null;
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(UserDetailPage), userid);
                ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
            }
        }

        async Task loadContents()
        {
            var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .UserDetail(userid.ToString());
            detail = Data.UserDetail.FromJsomValue(res);
            string _getText(string input) => (input == "") ? GetResourceString("PrivatePlain") : input;
            txtUsername.Text = detail.Name;
            txtAuthor.Text = detail.Name;
            txtAccount.Text = "@" + detail.Account;
            txtAuthorAccount.Text = txtAccount.Text;
            txtWebPage.Text = (detail.WebPage == "") ? GetResourceString("PrivateOrNonePlain") : detail.WebPage;
            if (detail.Gender == "") txtGender.Text = GetResourceString("PrivatePlain");
            else txtGender.Text = (detail.Gender == "male") ? GetResourceString("MalePlain") : GetResourceString("FemalePlain");
            txtBirthday.Text = _getText(detail.BirthDay);
            txtRegion.Text = _getText(detail.Region);
            txtJob.Text = _getText(detail.Job);
            string _getHW(string input) => (input == "") ? GetResourceString("UnknownPlain") : input;
            txtPC.Text = _getHW(detail.PC);
            txtMonitor.Text = _getHW(detail.Monitor);
            txtTool.Text = _getHW(detail.Tool);
            txtScanner.Text = _getHW(detail.Scanner);
            txtTablet.Text = _getHW(detail.Tablet);
            txtMouse.Text = _getHW(detail.Mouse);
            txtPrinter.Text = _getHW(detail.Printer);
            txtDesktop.Text = _getHW(detail.Desktop);
            txtMusic.Text = _getHW(detail.Music);
            txtDesk.Text = _getHW(detail.Desk);
            txtChair.Text = _getHW(detail.Chair);
            txtBtnFollow.Text = detail.IsFollowed ? GetResourceString("FollowingPlain") : GetResourceString("NotFollowingPlain");
            btnFollow.IsChecked = detail.IsFollowed;
            imgAvatar.ImageSource = await Data.OverAll.LoadImageAsync(detail.AvatarUrl);
            imgAuthor.ImageSource = imgAvatar.ImageSource;
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

        private async void BtnWorks_Click(object sender, RoutedEventArgs e)
        {
            grdUserButton.Visibility = Visibility.Visible;
            storyFade.Begin();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            grdDetail.Visibility = Visibility.Collapsed;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            grdDetail.Visibility = Visibility.Visible;
            storyShow.Begin();
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            grdUserButton.Visibility = Visibility.Collapsed;
        }

        private async void BtnFollow_Click(object sender, RoutedEventArgs e)
        {
            var btnSender = sender as ToggleButton;
            btnSender.IsEnabled = false;
            if (btnSender.IsChecked == true)
            {
                btnSender.IsChecked = false;
                //进行关注
                txtBtnFollow.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .UserFollowAdd(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = true;
                    txtBtnFollow.Text = GetResourceString("FollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
            else
            {
                btnSender.IsChecked = true;
                //取消关注
                txtBtnFollow.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .UserFollowDelete(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = false;
                    txtBtnFollow.Text = GetResourceString("NotFollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        void copyToClipboard(string content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("https://www.pixiv.net/member.php?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        private void BtnAppLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("pixiv://user?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        ViewModels.WaterfallItemViewModel tapped = null;

        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
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
            DownloadQueue.Queue.Add(tapped, Frame);
        }
    }
}
