using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using PixivFSUWP.Data.DownloadJobs;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
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
            if (btn.DataContext is DownloadJob job)
                _ = Data.DownloadManager.RemoveJobFromFinished(job);
        }

        private void Retry_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is DownloadJob job)
            {
                _ = Data.DownloadManager.ResetJob(job);
            }
        }

        private async void OpenFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.DataContext is Data.DownloadJobs.DownloadJob job)
            {
                await Windows.System.Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(job.FilePath));
            }
        }

        private void WaterfallContent_Loaded(object sender, RoutedEventArgs e)
        {
            var WaterfallContent = sender as Controls.WaterfallContentPanel;
            if (ActualWidth < 700) WaterfallContent.Colums = 3;
            else if (ActualWidth < 900) WaterfallContent.Colums = 4;
            else if (ActualWidth < 1100) WaterfallContent.Colums = 5;
            else WaterfallContent.Colums = 6;
        }

        private async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            if (img.DataContext is DownloadJob job && job.Status != DownloadJobStatus.Finished) return;
            if (img.Tag is string imagePath)
            {
                var file = await StorageFile.GetFileFromPathAsync(imagePath);
                if (file != null)
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                    img.Source = bitmap;
                }
            }
        }

        private async void Downloaded_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is DownloadJob job)
                await Windows.System.Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(job.FilePath));
        }
    }
}
