using PixivFSUWP.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static PixivFSUWP.Data.OverAll;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            NavControl.SelectedItem = NavControl.MenuItems[0];
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonForegroundColor = Colors.Black;
            view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            view.Title = "";
        }

        bool _programmablechange = false;

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
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Recommend);
                    await HideNacPlaceHolder();
                    break;
                case 1:
                    OverAll.RefreshBookmarkList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Bookmark);
                    await HideNacPlaceHolder();
                    break;
                case 2:
                    OverAll.RefreshFollowingList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Following);
                    await HideNacPlaceHolder();
                    break;
                case 3:
                    OverAll.RefreshRankingList();
                    ContentFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.Ranking);
                    await HideNacPlaceHolder();
                    break;
            }
        }

        private async Task HideNacPlaceHolder()
        {
            NavPlaceholder.IsEnabled = false;
            await Task.Delay(TimeSpan.FromMilliseconds(350));
            NavSeparator.Visibility = Visibility.Collapsed;
            NavPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void NavSelect(int index)
            => NavControl.SelectedItem = NavControl.MenuItems[index];

        public async void SelectNavPlaceholder(string title)
        {
            NavPlaceholder.IsEnabled = true;
            NavPlaceholder.Content = title;
            NavSeparator.Visibility = Visibility.Visible;
            NavPlaceholder.Visibility = Visibility.Visible;
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            NavSelect(5);
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var imgTask = LoadImageAsync(currentUser.Avatar170);
            HandleUri();
            imgAvatar.ImageSource = await imgTask;
            avatarRing.IsActive = false;
            avatarRing.Visibility = Visibility.Collapsed;
        }

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
                            ContentFrame.Navigate(typeof(IllustDetailPage), id);
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
                            ContentFrame.Navigate(typeof(UserDetailPage), id);
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

        List<(string, int)> tips = new List<(string, int)>();
        bool _tip_busy = false;

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

        private void BtnMe_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(typeof(UserDetailPage), currentUser.ID);
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ContentFrame.Content is SearchPage)
                    await (ContentFrame.Content as SearchPage).ShowSearch();
                else
                    ContentFrame.Navigate(typeof(SearchPage));
            }
            //吞掉异常，这个异常没有意义
            catch { }
        }

        public void UpdateNavButtonState()
        {
            NavControl.IsBackEnabled = Backstack.Default.CanBack;
        }

        private void NavControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (Backstack.Default.CanBack)
            {
                var param = ContentFrame.Back();
                if (param is WaterfallPage.ListContent)
                {
                    _programmablechange = true;
                    switch ((WaterfallPage.ListContent)param)
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
                UpdateNavButtonState();
            }
        }
    }
}
