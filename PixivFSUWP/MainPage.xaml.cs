using PixivFSUWP.Data;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using static PixivFSUWP.Data.OverAll;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly List<(string, int)> tips = new List<(string, int)>();
        private bool _programmablechange = false;

        private bool _tip_busy = false;
        public MainPage()
        {
            this.InitializeComponent();
            NavControl.SelectedItem = NavControl.MenuItems[0];
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonForegroundColor = Colors.Black;
            view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            view.Title = "";
            btnExperimentalWarning.Visibility = GlobalBaseAPI.ExperimentalConnection ? Visibility.Visible : Visibility.Collapsed;
            TheMainPage = this;

            Data.DownloadManager.DownloadJobsAdd += async (s) =>
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () => await TheMainPage?.ShowTip(string.Format(GetResourceString("WorkSavePlainAdd"), s)));

            Data.DownloadManager.DownloadCompleted += async (s, b) =>
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () => await TheMainPage?.ShowTip(
                    string.Format(b
                        ? GetResourceString("WorkSaveFailedPlain")
                        : GetResourceString("WorkSavedPlain"),
                        s)));

            btnDownload.Flyout = new Flyout { Content = new DownloadingPage { Tag = ContentFrame } };
        }

        /// <summary>
        /// 实验性功能警告。可以用来关闭实验性功能。
        /// </summary>
        private async void btnExperimentalWarning_Click(object sender, RoutedEventArgs e)
        {
            var warningDialog = new MessageDialog(GetResourceString("ExperimentalWarningPlain"));
            warningDialog.Commands.Add(new UICommand("Yes", async (_) =>
             {
                 //关闭直连
                 Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                 localSettings.Values["directConnection"] = false;
                 //通知重启应用生效
                 var restartDialog = new MessageDialog("请重启本程序来应用更改。\nPlease restart this app to apply the changes.");
                 await restartDialog.ShowAsync();
             }));
            warningDialog.Commands.Add(new UICommand("No"));
            await warningDialog.ShowAsync();
        }

        private void BtnMe_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(typeof(UserDetailPage), currentUser.ID, App.FromRightTransitionInfo);

        //在新窗口中打开发送反馈的窗口
        private async void BtnReport_Click(object sender, RoutedEventArgs e) => await ShowNewWindow(typeof(ReportIssuePage), null);

        private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await new SearchDialog(ContentFrame).ShowAsync();

        private async void BtnSetting_Click(object sender, RoutedEventArgs e) => await new SettingsDialog().ShowAsync();

        private async Task HideNacPlaceHolder()
        {
            NavPlaceholder.IsEnabled = false;
            await Task.Delay(TimeSpan.FromMilliseconds(350));
            NavSeparator.Visibility = Visibility.Collapsed;
            NavPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void NavControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (Backstack.Default.CanBack)
            {
                var param = ContentFrame.Back();
                if (param is WaterfallPage.ListContent content) select(content);
                else if (param is ValueTuple<WaterfallPage.ListContent, double> tuple) select(tuple.Item1);
                UpdateNavButtonState();

                // 本地方法
                void select(WaterfallPage.ListContent type)
                {
                    _programmablechange = true;
                    switch (type)
                    {
                        case WaterfallPage.ListContent.Recommend:
                            NavSelect(0);
                            break;
                        case WaterfallPage.ListContent.Bookmark:
                            NavSelect(1);
                            break;
                        case WaterfallPage.ListContent.Following:
                            NavSelect(2);
                            break;
                        case WaterfallPage.ListContent.Ranking:
                            NavSelect(3);
                            break;
                    }
                }
            }
        }

        private async void NavControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (OverAll.AppUri != null) return;
            if (_programmablechange)
            {
                _programmablechange = false;
                await HideNacPlaceHolder();
                return;
            }
            switch (sender.MenuItems.IndexOf(args.SelectedItem))
            {
                case 0:
                    OverAll.RefreshRecommendList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Recommend, App.FromRightTransitionInfo);
                    await HideNacPlaceHolder();
                    break;
                case 1:
                    OverAll.RefreshBookmarkList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Bookmark, App.FromRightTransitionInfo);
                    await HideNacPlaceHolder();
                    break;
                case 2:
                    OverAll.RefreshFollowingList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Following, App.FromRightTransitionInfo);
                    await HideNacPlaceHolder();
                    break;
                case 3:
                    OverAll.RefreshRankingList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Ranking, App.FromRightTransitionInfo);
                    await HideNacPlaceHolder();
                    break;
            }
        }

        private void NavSelect(int index) => NavControl.SelectedItem = NavControl.MenuItems[index];

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var imgTask = LoadImageAsync(currentUser.Avatar170);
            HandleUri();
            var img = await imgTask;
            img.DecodePixelHeight = 24;
            img.DecodePixelWidth = 24;
            imgAvatar.ImageSource = img;
            avatarRing.IsActive = false;
            avatarRing.Visibility = Visibility.Collapsed;
        }
        public void GoBack() => NavControl_BackRequested(NavControl, null);

        public void HandleUri()
        {
            if (OverAll.AppUri != null)
            {
                //从Uri启动
                var host = OverAll.AppUri.Host;
                switch (host)
                {
                    case "illust":
                        try
                        {
                            var query = OverAll.AppUri.Query;
                            var id = Convert.ToInt32(query.Split('=')[1]);
                            ContentFrame.Navigate(typeof(IllustDetailPage), id, App.FromRightTransitionInfo);
                            //已经处理完了
                            OverAll.AppUri = null;
                        }
                        catch
                        {
                            //不符合要求
                            goto default;
                        }
                        break;
                    case "user":
                        try
                        {
                            var query = OverAll.AppUri.Query;
                            var id = Convert.ToInt32(query.Split('=')[1]);
                            ContentFrame.Navigate(typeof(UserDetailPage), id, App.FromRightTransitionInfo);
                            //已经处理完了
                            OverAll.AppUri = null;
                        }
                        catch
                        {
                            //不符合要求
                            goto default;
                        }
                        break;
                    default:
                        //不符合要求的Uri
                        OverAll.AppUri = null;
                        break;
                }
            }
        }

        public async void SelectNavPlaceholder(string title)
        {
            NavPlaceholder.IsEnabled = true;
            NavPlaceholder.Content = title;
            NavSeparator.Visibility = Visibility.Visible;
            NavPlaceholder.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            NavSelect(5);
        }
        public async Task ShowTip(string Message, int Seconds = 3)
        {
            tips.Add((Message, Seconds));
            if (!_tip_busy)
            {
                _tip_busy = true;
                while (tips.Count > 0)
                {
                    (var m, var s) = tips[0];
                    txtTip.Text = m;
                    grdTip.Visibility = Visibility.Visible;
                    storyTipShow.Begin();
                    await Task.Delay(200);
                    await Task.Delay(TimeSpan.FromSeconds(s));
                    storyTipHide.Begin();
                    await Task.Delay(200);
                    grdTip.Visibility = Visibility.Collapsed;
                    tips.RemoveAt(0);
                }
                _tip_busy = false;
            }
        }
        public void UpdateNavButtonState() => NavControl.IsBackEnabled = Backstack.Default.CanBack;
    }
}
