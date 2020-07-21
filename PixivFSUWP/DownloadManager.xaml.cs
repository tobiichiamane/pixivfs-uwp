using PixivFSUWP.Data;
using PixivFSUWP.Interfaces;
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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DownloadManager : Page, IGoBackFlag
    {
        bool _backflag = false;

        public DownloadManager()
        {
            this.InitializeComponent();
        }

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            OverAll.TheMainPage.SelectNavPlaceholder(OverAll.GetResourceString("DownloadsPlain"));
            NavControl.SelectedItem = NavControl.MenuItems[1];
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (!_backflag)
            {
                Backstack.Default.Push(typeof(DownloadManager), null);
                ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
            }
            base.OnNavigatedFrom(e);
        }

        private void NavControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem == NavControl.MenuItems[1])
            {
                //选择了“下载中”
                ContentFrame.Navigate(typeof(DownloadingPage));
            }
            else
            {
                //选择了“下载完毕”
                ContentFrame.Navigate(typeof(DownloadedPage));
            }
        }
    }
}
