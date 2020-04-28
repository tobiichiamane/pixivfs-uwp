using PixivCS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using AdaptiveCards;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Threading;
using Lumia.Imaging;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using PixivFSUWP.Interfaces;
using static PixivFSUWP.Data.OverAll;
using PixivFSUWP.Data.Collections;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class IllustDetailPage : Page, IGoBackFlag
    {
        int illustID;
        Data.IllustDetail illust;
        IBuffer buffer;

        Data.Ugoira ugoira;

        bool _emergencyStop = false;
        bool _busy = false;
        bool _playing = true;

        public IllustDetailPage()
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

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            request.Data.SetText(string.Format("{0}\n{1} by {2}\n" +
                "{3}：https://www.pixiv.net/artworks/{4}\n" +
                "PixivFSUWP：pixiv://illust?id={4}", GetResourceString("WorkPlain"), illust.Title, illust.Author, GetResourceString("LinkPlain"), illustID));
            request.Data.Properties.Title = string.Format("{0}：{1}", GetResourceString("SharePlain"), illust.Title);
            request.Data.Properties.Description = GetResourceString("SharingPlain");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(GetResourceString("DetailPlain"));
            illustID = (int)e.Parameter;
            txtPID.Text = illustID.ToString();
            //System.Diagnostics.Debug.WriteLine("View Pixiv ID = " + illustID.ToString());
            base.OnNavigatedTo(e);
            _ = loadContent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _emergencyStop = true;
            ugoiraPlayer.Source = null;
            if (!_busy)
                (ImageList.ItemsSource as ObservableCollection<ViewModels.ImageItemViewModel>)?.Clear();
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(IllustDetailPage), illustID);
                ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
            }
            (listComments.ItemsSource as CommentsCollection)?.AvatarLoader?.EmergencyStop();
            (listComments.ItemsSource as CommentsCollection)?.StopLoading();
            (listComments.ItemsSource as CommentsCollection)?.Clear();
            GC.Collect();
            base.OnNavigatedFrom(e);
        }

        private async Task loadContent()
        {
            try
            {
                _busy = true;
                txtTitle.Text = GetResourceString("LoadingPlain");
                iconView.Visibility = Visibility.Collapsed;
                iconStar.Visibility = Visibility.Collapsed;
                stkAuthor.Visibility = Visibility.Collapsed;
                var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .IllustDetail(illustID.ToString());
                ImageList.ItemsSource = new ObservableCollection<ViewModels.ImageItemViewModel>();
                illust = Data.IllustDetail.FromJsonValue(res);
                imgAuthor.ImageSource = await Data.OverAll.LoadImageAsync(illust.AuthorAvatarUrl);
                txtTitle.Text = illust.Title;
                btnBookmark.IsChecked = illust.IsBookmarked;
                btnFollow.IsChecked = illust.IsUserFollowed;
                txtBtnBookmark.Text = illust.IsBookmarked ? GetResourceString("BookmarkedPlain") : GetResourceString("NotBookmarkedPlain");
                txtBtnFollow.Text = illust.IsUserFollowed ? GetResourceString("FollowingPlain") : GetResourceString("NotFollowingPlain");
                iconView.Visibility = Visibility.Visible;
                iconStar.Visibility = Visibility.Visible;
                stkAuthor.Visibility = Visibility.Visible;
                txtViewStatus.Text = illust.TotalView.ToString();
                txtBookmarkStatus.Text = illust.TotalBookmarks.ToString();
                txtAuthor.Text = illust.Author;
                txtAuthorAccount.Text = string.Format("@{0}", illust.AuthorAccount);
                txtCaption.Text = (illust.Caption == "") ? GetResourceString("NoCaptionPlain") : Regex.Replace(illust.Caption.Replace("<br />", "\n"), "<[^>]+>", "");
                txtCommentTitle.Text = GetResourceString("CommentsPlain");
                btnNewComment.Visibility = Visibility.Visible;
                listComments.ItemsSource = new CommentsCollection(illustID.ToString());
                txtLoadingStatus.Text = GetResourceString("CreatingTimelinePlain");
                AdaptiveCard card = new AdaptiveCard("1.1");
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = illust.Title,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Large,
                    Wrap = true,
                    MaxLines = 3
                });
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = string.Format("by {0}", illust.Author),
                    Size = AdaptiveTextSize.Default,
                    Wrap = true,
                    MaxLines = 3
                });
                var build = SystemInformation.OperatingSystemVersion.Build;
                if (build >= 18362)
                {
                    var data = await Data.OverAll.GetDataUri(illust.MediumUrl);
                    card.BackgroundImage = new AdaptiveBackgroundImage(new Uri(data));
                }
                await Data.OverAll.GenerateActivityAsync(illust.Title, card, new Uri(string.Format("pixiv://illust?id={0}", illustID)), illustID.ToString());
                if (illust.Type == "ugoira")
                {
                    ugoiraPlayer.Visibility = Visibility.Visible;
                    ImageList.Visibility = Visibility.Collapsed;
                    btnPlay.IsEnabled = false;
                    btnPlay.Visibility = Visibility.Visible;
                    btnSaveGif.IsEnabled = false;
                    btnSaveGif.Visibility = Visibility.Visible;
                    txtLoadingStatus.Text = GetResourceString("LoadingPreviewPlain");
                    ugoiraPlayer.Source = await Data.OverAll.LoadImageAsync(illust.OriginalUrls[0]);
                    txtLoadingStatus.Text = GetResourceString("DownloadingUgoiraPlain");
                    if (_emergencyStop)
                    {
                        return;
                    }
                    ugoira = await Data.UgoiraHelper.GetUgoiraAsync(illust.IllustID.ToString());
                    txtLoadingStatus.Text = GetResourceString("GeneratingGifPlain");
                    await playUgoira();
                    txtLoadingStatus.Text = GetResourceString("PlayingUgoiraPlain");
                    btnPlay.IsEnabled = true;
                    btnSaveGif.IsEnabled = true;
                }
                else
                {
                    int counter = 0;
                    foreach (var i in illust.OriginalUrls)
                    {
                        if (_emergencyStop)
                        {
                            return;
                        }
                        txtLoadingStatus.Text = string.Format(GetResourceString("LoadingStatusPlain"), ++counter, illust.OriginalUrls.Count);
                        (ImageList.ItemsSource as ObservableCollection<ViewModels.ImageItemViewModel>)
                            .Add(new ViewModels.ImageItemViewModel()
                            {
                                ImageSource = await Data.OverAll.LoadImageAsync(i, 1, 1)
                            });
                    }
                    txtLoadingStatus.Text = string.Format(GetResourceString("PageInfoPlain"), illust.OriginalUrls.Count);
                }
            }
            finally
            {
                _busy = false;
                if (_emergencyStop)
                {
                    (ImageList.ItemsSource as ObservableCollection<ViewModels.ImageItemViewModel>)?.Clear();
                    GC.Collect();
                }
            }
        }

        //当场生成Gif
        async Task playUgoira()
        {
            try
            {
                using (GifRenderer renderer = new GifRenderer())
                {
                    renderer.Duration = ugoira.Frames[0].Delay;
                    renderer.Size = new Size(ugoira.Frames[0].Image.PixelWidth, ugoira.Frames[0].Image.PixelHeight);
                    List<IImageProvider> sources = new List<IImageProvider>();
                    foreach (var i in ugoira.Frames)
                        sources.Add(new SoftwareBitmapImageSource(i.Image));
                    renderer.Sources = sources;
                    buffer = await renderer.RenderAsync();
                }
                var gif = new BitmapImage();
                await gif.SetSourceAsync(buffer.AsStream().AsRandomAccessStream());
                ugoiraPlayer.Source = gif;
            }
            finally
            {
                ugoira.Dispose();
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ImageList.MaxHeight = Frame.ActualHeight - 265;
            ugoiraPlayer.MaxHeight = Frame.ActualHeight - 265;
            ugoiraPlayer.Height = Frame.ActualHeight - 265;
        }

        private async void ImageList_ItemClick(object sender, ItemClickEventArgs e)
        {

            var Item = e.ClickedItem as ViewModels.ImageItemViewModel;
            await Data.OverAll.ShowNewWindow(typeof(BigImage), new Data.BigImageDetail()
            {
                Title = illust.Title,
                Width = Item.ImageSource.PixelWidth,
                Height = Item.ImageSource.PixelHeight,
                Author = illust.Author,
                Image = await Data.OverAll.ImageToBytes(Item.ImageSource)
            });
        }

        private async void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if ((sender as ScrollViewer).VerticalOffset >= (sender as ScrollViewer).ScrollableHeight - 500)
            {
                try
                {
                    await (listComments.ItemsSource as ISupportIncrementalLoading)?.LoadMoreItemsAsync(0);
                }
                catch { }
            }
        }

        private async void BtnBookmark_Click(object sender, RoutedEventArgs e)
        {
            var btnSender = sender as ToggleButton;
            btnSender.IsEnabled = false;
            if (btnSender.IsChecked == true)
            {
                btnSender.IsChecked = false;
                //进行关注
                txtBtnBookmark.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .IllustBookmarkAdd(illustID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = true;
                    txtBtnBookmark.Text = GetResourceString("BookmarkedPlain");
                    txtBookmarkStatus.Text = illust.IsBookmarked ? illust.TotalBookmarks.ToString() : (illust.TotalBookmarks + 1).ToString();
                }
                btnSender.IsEnabled = true;
            }
            else
            {
                btnSender.IsChecked = true;
                //取消关注
                txtBtnBookmark.Text = GetResourceString("RequestingPlain");
                bool res;
                try
                {
                    await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                        .IllustBookmarkDelete(illustID.ToString());
                    res = true;
                }
                catch
                {
                    res = false;
                }
                if (res)
                {
                    btnSender.IsChecked = false;
                    txtBtnBookmark.Text = GetResourceString("NotBookmarkedPlain");
                    txtBookmarkStatus.Text = illust.IsBookmarked ? (illust.TotalBookmarks - 1).ToString() : illust.TotalBookmarks.ToString();
                }
                btnSender.IsEnabled = true;
            }
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
                        .UserFollowAdd(illust.AuthorID.ToString());
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
                        .UserFollowDelete(illust.AuthorID.ToString());
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
            copyToClipboard(string.Format("https://www.pixiv.net/artworks/{0}", illustID));
            btnShareFlyout.Hide();
        }

        private void BtnAppLink_Click(object sender, RoutedEventArgs e)
        {
            copyToClipboard(string.Format("pixiv://illust?id={0}", illustID));
            btnShareFlyout.Hide();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(UserDetailPage), illust.AuthorID);
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_playing)
            {
                (ugoiraPlayer.Source as BitmapImage).Stop();
                txtPlay.Text = GetResourceString("PlayPlain");
                iconPlay.Glyph = "";
            }
            else
            {
                (ugoiraPlayer.Source as BitmapImage).Play();
                txtPlay.Text = GetResourceString("StopPlain");
                iconPlay.Glyph = "";
            }
            _playing = !_playing;
        }

        private async void BtnSaveGif_Click(object sender, RoutedEventArgs e)
        {
            await saveImage();
        }

        async Task saveImage()
        {
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add(GetResourceString("GifFilePlain"), new List<string>() { ".gif" });
            picker.SuggestedFileName = illust.Title;
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await stream.WriteAsync(buffer);
                }
                var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                if (updateStatus != FileUpdateStatus.Complete)
                {
                    var messageDialog = new MessageDialog(GetResourceString("SaveUgoiraFailedPlain"));
                    messageDialog.Commands.Add(new UICommand(GetResourceString("RetryPlain"), async (a) => { await saveImage(); }));
                    messageDialog.Commands.Add(new UICommand(GetResourceString("CancelPlain")));
                    messageDialog.DefaultCommandIndex = 0;
                    messageDialog.CancelCommandIndex = 1;
                    await messageDialog.ShowAsync();
                }
                else
                {
                    var messageDialog = new MessageDialog(GetResourceString("SaveUgoiraSucceededPlain"));
                    messageDialog.Commands.Add(new UICommand(GetResourceString("OKPlain")));
                    messageDialog.DefaultCommandIndex = 0;
                    await messageDialog.ShowAsync();
                }
            }
        }

        private async void btnPublishComment_Click(object sender, RoutedEventArgs e)
        {
            //发表评论
            try
            {
                if (string.IsNullOrEmpty(txtComment.Text)) throw new Exception("评论不能为空");
                txtComment.IsEnabled = false;
                btnPublishComment.IsEnabled = false;
                btnNewComment.IsEnabled = false;
                var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI)
                    .IllustCommentAdd(illustID.ToString(), txtComment.Text);
                Data.IllustCommentItem newItem = Data.IllustCommentItem.FromJsonValue(res["comment"].GetObject());
                ViewModels.CommentViewModel viewModel = ViewModels.CommentViewModel.FromItem(newItem);
                _ = viewModel.LoadAvatarAsync();
                (listComments.ItemsSource as CommentsCollection).Insert(0, viewModel);
                (((FrameworkElement)Frame?.Parent)?.Parent as MainPage)
                    ?.ShowTip("评论已发表");
            }
            catch (Exception exception)
            {
                (((FrameworkElement)Frame?.Parent)?.Parent as MainPage)
                    ?.ShowTip(string.Format("评论未能发表：{0}", exception.Message));
            }
            finally
            {
                btnNewComment.IsChecked = false;
                txtComment.IsEnabled = true;
                btnPublishComment.IsEnabled = true;
                btnNewComment.IsEnabled = true;
                txtComment.Text = "";
            }
        }
    }
}
