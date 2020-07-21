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
    public sealed partial class DownloadedPage : Page
    {
        public DownloadedPage()
        {
            this.InitializeComponent();
            lstDownloading.ItemsSource = Data.DownloadManager.FinishedJobs;
        }
        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is Data.DownloadJob job)
                Data.DownloadManager.RemoveFinishedJob(job);
        }

        private void Retry_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is Data.DownloadJob job)
            {
                Data.DownloadManager.ResetJob(job);
            }
        }

        private void OpenFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            //if (btn.DataContext is Data.DownloadJob job)
        }
    }
}
