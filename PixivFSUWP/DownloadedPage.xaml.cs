using System;

using PixivFSUWP.Data;
using PixivFSUWP.Data.DownloadJobs;
using PixivFSUWP.Interfaces;

using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class DownloadedPage : Page, IGoBackFlag
    {
        public DownloadedPage()
        {
            this.InitializeComponent();
        }
        private async void Downloaded_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (e.ClickedItem is DownloadJob job)
                    await Windows.System.Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(job.FilePath));
            }
            catch (UnauthorizedAccessException) { }
        }

        private async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            var img = sender as Image;
            if (img.DataContext is DownloadJob job && job.Status != DownloadJobStatus.Finished) return;
            if (img.Tag is string imagePath)
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(imagePath);
                    if (file != null)
                    {
                        var bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(await file.OpenAsync(FileAccessMode.Read));
                        img.Source = bitmap;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(OverAll.GetResourceString("DownloadedCaption\\Content"));
        }

        public void SetBackFlag(bool value) { }
    }
}
