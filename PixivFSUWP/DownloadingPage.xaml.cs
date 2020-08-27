using PixivFSUWP.Data.DownloadJobs;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
        }

        private void Downloaded_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Tag is Frame frame) frame.Navigate(typeof(DownloadedPage));
        }

        private void Pause_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is DownloadJob job) job.Pause();
        }

        private void Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is DownloadJob job) Data.DownloadManager.RemoveJobFromDownloading(job);
        }
        private void Resume_Button_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement).DataContext is DownloadJob job) job.Resume();
        }
    }
}
