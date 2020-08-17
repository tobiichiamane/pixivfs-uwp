using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using PixivFSUWP.Data.DownloadJobs;

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
    public sealed partial class DownloadingPage : Page
    {
        public DownloadingPage()
        {
            this.InitializeComponent();

            //直接进行数据绑定
            lstDownloading.ItemsSource = Data.DownloadManager.DownloadJobs;
            ////！测试数据！
            //Data.DownloadManager.DownloadJobs.Add(new Data.DownloadJob("Foo1", "i.pximg.net/114514", "d:\\14514.png"));
            //Data.DownloadManager.DownloadJobs.Add(new Data.DownloadJob("Foo2", "i.pximg.net/98e", "d:\\98e.png"));
        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is DownloadJob job)
                Data.DownloadManager.RemoveJob(job);
        }
        private void Pause_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is DownloadJob job)
                job.Pause();
        }

        private void Resume_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is DownloadJob job)
                job.Resume();
        }
    }
}
