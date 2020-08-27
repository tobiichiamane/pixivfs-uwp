using System;
using System.Threading.Tasks;

using PixivCS;

using PixivFSUWP.Data;
using PixivFSUWP.Data.Collections;
using PixivFSUWP.Interfaces;

using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserDetailPage : Page, IGoBackFlag
    {
        Data.UserDetail detail;
        UserIllustsCollection itemsSource;
        ViewModels.WaterfallItemViewModel tapped = null;
        int userid = 0;
        private bool _backflag { get; set; } = false;

        public UserDetailPage()
        {
            this.InitializeComponent();
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        }
        private void BtnAppLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("pixiv://user?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        private async void BtnFollow_Click(object sender, RoutedEventArgs e)
        {
            var btnSender = sender as ToggleButton;
            btnSender.IsEnabled = false;
            if (btnSender.IsChecked == true)
            {
                btnSender.IsChecked = false;
                //进行关注
                txtBtnFollow.Text = OverAll.GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .PostUserFollowAddAsync(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = true;
                    txtBtnFollow.Text = OverAll.GetResourceString("FollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
            else
            {
                btnSender.IsChecked = true;
                //取消关注
                txtBtnFollow.Text = OverAll.GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .PostUserFollowDeleteAsync(detail.ID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = false;
                    txtBtnFollow.Text = OverAll.GetResourceString("NotFollowingPlain");
                }
                btnSender.IsEnabled = true;
            }
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("https://www.pixiv.net/member.php?id={0}", detail.ID));
            btnShareFlyout.Hide();
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
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

        void copyToClipboard(string content)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(string.Format("{0}\n{1}\n" +
                "{2}：https://www.pixiv.net/member.php?id={3}\n" +
                "PixivFSUWP：pixiv://user?id={3}", OverAll.GetResourceString("PixivUserPlain"), detail.Name, OverAll.GetResourceString("LinkPlain"), detail.ID));
            request.Data.Properties.Title = string.Format("{0}：{1}", OverAll.GetResourceString("ArtistPlain"), detail.Name);
            request.Data.Properties.Description = OverAll.GetResourceString("UserShareTipPlain");
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            btnWorks.IsEnabled = true;
        }

        async Task loadContents()
        {
            var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .GetUserDetailAsync(userid.ToString());
            detail = Data.UserDetail.FromObject(res);
            string _getText(string input) => (input == "") ? OverAll.GetResourceString("PrivatePlain") : input;
            txtUsername.Text = detail.Name;
            txtAuthor.Text = detail.Name;
            txtAccount.Text = "@" + detail.Account;
            txtAuthorAccount.Text = txtAccount.Text;
            txtWebPage.Text = (detail.WebPage == "") ? OverAll.GetResourceString("PrivateOrNonePlain") : detail.WebPage;
            if (detail.Gender == "") txtGender.Text = OverAll.GetResourceString("PrivatePlain");
            else txtGender.Text = (detail.Gender == "male") ? OverAll.GetResourceString("MalePlain") : OverAll.GetResourceString("FemalePlain");
            txtBirthday.Text = _getText(detail.BirthDay);
            txtRegion.Text = _getText(detail.Region);
            txtJob.Text = _getText(detail.Job);
            string _getHW(string input) => (input == "") ? OverAll.GetResourceString("UnknownPlain") : input;
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
            txtBtnFollow.Text = detail.IsFollowed ? OverAll.GetResourceString("FollowingPlain") : OverAll.GetResourceString("NotFollowingPlain");
            btnFollow.IsChecked = detail.IsFollowed;
            imgAvatar.ImageSource = await Data.OverAll.LoadImageAsync(detail.AvatarUrl);
            imgAuthor.ImageSource = imgAvatar.ImageSource;
        }

        private async void QuickSave_Click(object sender, RoutedEventArgs e)
        {
            if (tapped == null) return;
            await IllustDetail.FromObject(await new PixivAppAPI(OverAll.GlobalBaseAPI).GetIllustDetailAsync(tapped.ItemId.ToString())).AutoDownload();
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
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("DeletedBookmarkPlain"), title));
                    }
                    else
                    {
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("BookmarkDeleteFailedPlain"), title));
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
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("WorkBookmarkedPlain"), title));
                    }
                    else
                    {
                        await OverAll.TheMainPage?.ShowTip(string.Format(OverAll.GetResourceString("WorkBookmarkFailedPlain"), title));
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
            if (ActualWidth < 700) (sender as Controls.WaterfallContentPanel).Colums = 3;
            else if (ActualWidth < 900) (sender as Controls.WaterfallContentPanel).Colums = 4;
            else if (ActualWidth < 1100) (sender as Controls.WaterfallContentPanel).Colums = 5;
            else (sender as Controls.WaterfallContentPanel).Colums = 6;
        }

        private void WaterfallListView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        private void WaterfallListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(IllustDetailPage),
                (e.ClickedItem as ViewModels
                .WaterfallItemViewModel).ItemId, App.FromRightTransitionInfo);
        }

        private void WaterfallListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            tapped = ((FrameworkElement)e.OriginalSource).DataContext as ViewModels.WaterfallItemViewModel;
            quickStar.Text = (tapped.IsBookmarked) ?
                OverAll.GetResourceString("DeleteBookmarkPlain") :
                OverAll.GetResourceString("QuickBookmarkPlain");
            quickStar.IsEnabled = tapped.Title != null;
            quickActions.ShowAt(listView, e.GetPosition(listView));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            OverAll.UserList?.StopLoading();
            itemsSource = null;
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(UserDetailPage), userid);
                Data.OverAll.UserList.PauseLoading();
                OverAll.TheMainPage?.UpdateNavButtonState();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            OverAll.TheMainPage?.SelectNavPlaceholder(OverAll.GetResourceString("UserDetailPagePlain"));

            //治标不治本的加载逻辑……反正个人插画也不会太多,全部重载就好)
            if (e.Parameter is ValueTuple<int, bool> tuple)
            {
                Data.OverAll.UserList?.StopLoading();
                userid = tuple.Item1;
                _ = loadContents();
                //Data.OverAll.RefreshUserList(userid.ToString()); 治 本 (需删除85行并修改Collection中的瀑布流控制逻辑)
            }
            else if (e.Parameter is int id)
            {// 只传ID进来
                userid = id;
                _ = loadContents();
                // 只传ID进来 查看用户信息 这把信息隐藏了看什么...
                //grdDetail.Visibility = Visibility.Collapsed;
            }
            Data.OverAll.RefreshUserList(userid.ToString()); //治 标
            Data.OverAll.UserList.ResumeLoading();
            itemsSource = OverAll.UserList;
            itemsSource.CollectionChanged += ItemsSource_CollectionChanged;
            WaterfallListView.ItemsSource = itemsSource;
            base.OnNavigatedTo(e);
        }

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }
    }
}
