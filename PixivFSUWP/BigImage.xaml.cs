using Microsoft.Graphics.Canvas;
using PixivFSUWP.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static PixivFSUWP.Data.OverAll;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class BigImage : Page
    {
        bool _locked = false;
        Data.BigImageDetail parameter;
        public BigImage()
        {
            this.InitializeComponent();
            mainCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse
                | Windows.UI.Core.CoreInputDeviceTypes.Pen;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            parameter = e.Parameter as Data.BigImageDetail;
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            view.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;         
            view.Title = string.Format("{0} by {1} - {2}x{3}",
                parameter.Title, parameter.Author,
                parameter.Width, parameter.Height);
            txtTitle.Text = view.Title;
            mainImg.Source = await Data.OverAll.BytesToImage(parameter.Image, parameter.Width, parameter.Height);
            parameter.Image = null;
            base.OnNavigatedTo(e);
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_locked) return;
            gridImg.MaxHeight = scrollRoot.ActualHeight;
            gridImg.MaxWidth = scrollRoot.ActualWidth;
            gridImg.MinHeight = scrollRoot.ActualHeight;
            gridImg.MinWidth = scrollRoot.ActualWidth;
        }

        void lockView()
        {
            _locked = true;
            gridImg.MaxHeight = gridImg.ActualHeight;
            gridImg.MaxWidth = gridImg.ActualWidth;
            gridImg.MinHeight = gridImg.ActualHeight;
            gridImg.MinWidth = gridImg.ActualWidth;
            mainImg.Opacity = 0.3;
            paper.MinHeight = mainImg.ActualHeight;
            paper.MinWidth = mainImg.ActualWidth;
            paper.MaxHeight = mainImg.ActualHeight;
            paper.MaxWidth = mainImg.ActualWidth;
            paper.Height = mainImg.ActualHeight;
            paper.Width = mainImg.ActualWidth;
            mainCanvas.MinHeight = mainImg.ActualHeight;
            mainCanvas.MinWidth = mainImg.ActualWidth;
            mainCanvas.MaxHeight = mainImg.ActualHeight;
            mainCanvas.MaxWidth = mainImg.ActualWidth;
            mainCanvas.Height = mainImg.ActualHeight;
            mainCanvas.Width = mainImg.ActualWidth;
        }

        void unlockView()
        {
            _locked = false;
            mainImg.Opacity = 1;
            gridImg.Height = scrollRoot.ActualHeight;
            gridImg.Width = scrollRoot.ActualWidth;
            gridImg.MaxHeight = scrollRoot.ActualHeight;
            gridImg.MaxWidth = scrollRoot.ActualWidth;
            gridImg.MinHeight = scrollRoot.ActualHeight;
            gridImg.MinWidth = scrollRoot.ActualWidth;
        }

        private void BtnDraw_Checked(object sender, RoutedEventArgs e)
        {
            mainCanvas.Visibility = Visibility.Visible;
            inkToolbar.Visibility = Visibility.Visible;
            paper.Visibility = Visibility.Visible;
            btnSaveImage.Label = GetResourceString("SaveInkPlain");
            lockView();
        }

        private void BtnDraw_Unchecked(object sender, RoutedEventArgs e)
        {
            mainCanvas.Visibility = Visibility.Collapsed;
            inkToolbar.Visibility = Visibility.Collapsed;
            paper.Visibility = Visibility.Collapsed;
            mainCanvas.InkPresenter.StrokeContainer.Clear();
            btnSaveImage.Label = GetResourceString("SaveImagePlain");
            unlockView();
        }

        private async void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (_locked)
                await saveStrokes();
            else
                await saveImage();
        }

        private async void btnSetWallpaper_Click(object sender, RoutedEventArgs e)
        {
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                UserProfilePersonalizationSettings settings = UserProfilePersonalizationSettings.Current;
                StorageFile cacheFile = await CacheManager.GetCachedFileAsync(parameter.FileName);
                StorageFile file = await cacheFile.CopyAsync(ApplicationData.Current.LocalFolder, "WallpaperImage", NameCollisionOption.ReplaceExisting);
                if (!await settings.TrySetWallpaperImageAsync(file))
                {
                    var messageDialog = new MessageDialog("更换壁纸失败");
                    await messageDialog.ShowAsync();
                }
                else
                    await ShowTip("已更换壁纸");
            }
            else
            {
                var messageDialog = new MessageDialog("您的设备不支持更换壁纸");
                await messageDialog.ShowAsync();
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

        private async Task saveImage()
        {
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add(GetResourceString("ImageFilePlain"), new List<string>() { ".png" });
            picker.SuggestedFileName = parameter.Title;
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    var image = mainImg.Source as WriteableBitmap;
                    var imageStream = image.PixelBuffer.AsStream();
                    byte[] raw = new byte[imageStream.Length];
                    await imageStream.ReadAsync(raw, 0, raw.Length);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)image.PixelWidth, (uint)image.PixelHeight, 96, 96, raw);
                    await encoder.FlushAsync();
                }
                var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                if (updateStatus != FileUpdateStatus.Complete)
                {
                    var messageDialog = new MessageDialog(GetResourceString("SaveImageFailedPlain"));
                    messageDialog.Commands.Add(new UICommand(GetResourceString("RetryPlain"), async (a) => { await saveImage(); }));
                    messageDialog.Commands.Add(new UICommand(GetResourceString("CancelPlain")));
                    messageDialog.DefaultCommandIndex = 0;
                    messageDialog.CancelCommandIndex = 1;
                    await messageDialog.ShowAsync();
                }
                else
                {
                    await ShowTip(GetResourceString("SaveImageSucceededPlain"));
                }
            }
        }

        private async Task saveStrokes()
        {
            var strokes = mainCanvas.InkPresenter.StrokeContainer.GetStrokes();
            if (strokes.Count > 0)
            {
                FileSavePicker picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add(GetResourceString("SaveImagePlain"), new List<string>() { ".png" });
                picker.FileTypeChoices.Add(GetResourceString("RawInkPlain"), new List<string>() { ".gif" });
                picker.SuggestedFileName = GetResourceString("MyInkPlain");
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        if (file.FileType == ".png")
                        {
                            var width = (int)mainCanvas.ActualWidth;
                            var height = (int)mainCanvas.ActualHeight;
                            var device = CanvasDevice.GetSharedDevice();
                            var renderTarget = new CanvasRenderTarget(device, width, height, 96);
                            using (var ds = renderTarget.CreateDrawingSession())
                            {
                                ds.Clear(Colors.White);
                                ds.DrawInk(strokes);
                            }
                            await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                        }
                        else
                        {
                            using (var outputStream = stream.GetOutputStreamAt(0))
                            {
                                await mainCanvas.InkPresenter.StrokeContainer.SaveAsync(outputStream);
                                await outputStream.FlushAsync();
                            }
                        }
                    }
                    var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (updateStatus != FileUpdateStatus.Complete)
                    {
                        var messageDialog = new MessageDialog(GetResourceString("SaveInkFailedPlain"));
                        messageDialog.Commands.Add(new UICommand(GetResourceString("RetryPlain"), async (a) => { await saveStrokes(); }));
                        messageDialog.Commands.Add(new UICommand(GetResourceString("CancelPlain")));
                        messageDialog.DefaultCommandIndex = 0;
                        messageDialog.CancelCommandIndex = 1;
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        await ShowTip(GetResourceString("SaveInkSucceededPlain"));
                    }
                }
            }
            else
            {
                await ShowTip(GetResourceString("NoInkPlain"));
            }
        }

    }
}
