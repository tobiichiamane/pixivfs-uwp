using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using PixivCS; // 
using Windows.UI.Xaml.Media.Imaging;
using PixivFSUWP.Interfaces;
using static PixivFSUWP.Data.OverAll;
using Windows.Data.Json;
using PixivFSUWP.Data;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchDialog
    {
        private readonly Frame Frame;
        SearchParam last = default;

        public SearchDialog()
        {
            this.InitializeComponent();
            Title = GetResourceString("SearchPagePlain");
            CloseButtonText = GetResourceString("CancelPlain");

            _ = loadContents();
        }
        public SearchDialog(Frame frame) : this() => Frame = frame;

        async Task<List<ViewModels.TagViewModel>> getTrendingTags()
        {
            try
            {
                var res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI).GetTrendingTagsIllustAsync();
                var array = res.TrendTags;
                List<ViewModels.TagViewModel> toret = new List<ViewModels.TagViewModel>();
                foreach (var i in array)
                    toret.Add(new ViewModels.TagViewModel() { Tag = i.Tag });
                return toret;
            }
            catch
            {
                return null;
            }
        }

        async Task loadContents()
        {
            var tags = await getTrendingTags();
            progressRing.Visibility = Visibility.Collapsed;
            panelTags.ItemsSource = tags;
        }

        private void TxtWord_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => Search(sender.Text);

        public void Search(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            var param = new SearchParam
            {
                Word = word.Trim()
            };
            switch (cbSearchTarget.SelectedIndex)
            {
                case 0:
                    param.SearchTarget = "partial_match_for_tags";
                    break;
                case 1:
                    param.SearchTarget = "exact_match_for_tags";
                    break;
                case 2:
                    param.SearchTarget = "title_and_caption";
                    break;
            }
            switch (cbSort.SelectedIndex)
            {
                case 0:
                    param.Sort = "date_desc";
                    break;
                case 1:
                    param.Sort = "date_asc";
                    break;
            }
            switch (cbDuration.SelectedIndex)
            {
                case 0:
                    param.Duration = null;
                    break;
                case 1:
                    param.Duration = "within_last_day";
                    break;
                case 2:
                    param.Duration = "within_last_week";
                    break;
                case 3:
                    param.Duration = "within_last_month";
                    break;
            }
            Search(param);
        }
        public void Search(SearchParam param)
        {
            if (param != last)
            {
                RefreshSearchResultList(param);
                Frame?.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.SearchResult, App.FromRightTransitionInfo);
            }
            Hide();
            if (param != last)
                last = param;
        }

        private void BtnTag_Click(object sender, RoutedEventArgs e)
        {
            Search(new SearchParam
            {
                Word = (sender as Button).Tag as string,
                SearchTarget = "exact_match_for_tags",
                Sort = "date_desc",
                Duration = null
            });
        }

        private async void btnSauceNAO_Click(object sender, RoutedEventArgs e)
        {
            const string sauceNAOAPI = null;
            const string imgurAPI = null;
            string SAUCENAO_API_KEY, IMGUR_API_KEY;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            //读取设置项
            if (localSettings.Values["SauceNAOAPI"] as string == null)
            {
                Frame.Navigate(typeof(SettingsPage), null, App.FromRightTransitionInfo);
                SAUCENAO_API_KEY = sauceNAOAPI;
                return;
            }
            else if ((localSettings.Values["SauceNAOAPI"] as string).Length == 0)
            {
                Frame.Navigate(typeof(SettingsPage), null, App.FromRightTransitionInfo);
                SAUCENAO_API_KEY = sauceNAOAPI;
                return;
            }
            if (localSettings.Values["ImgurAPI"] as string == null)
            {
                Frame.Navigate(typeof(SettingsPage), null, App.FromRightTransitionInfo);
                IMGUR_API_KEY = imgurAPI;
                return;
            }
            else if ((localSettings.Values["ImgurAPI"] as string).Length == 0)
            {
                Frame.Navigate(typeof(SettingsPage), null, App.FromRightTransitionInfo);
                IMGUR_API_KEY = imgurAPI;
                return;
            }
            SAUCENAO_API_KEY = localSettings.Values["SauceNAOAPI"] as string;
            IMGUR_API_KEY = localSettings.Values["ImgurAPI"] as string;
            // 选择文件
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            // 检测文件
            if (file == null)
            {
                Frame.GoBack();
                return;
            }
            //
            //ImgurNaoAPI imgurNaoApi = new ImgurNaoAPI(SAUCENAO_API_KEY, IMGUR_API_KEY);
            //string image = imgurNaoApi.UpLoad(await StorageFileExt.AsByteArray(file)).GetNamedString("link");
            //int retPid = (int)imgurNaoApi.DownLoad(image).GetNamedNumber("pixiv_id");
            //Frame.Navigate(typeof(IllustDetailPage), retPid);
        }
        private void GoPixivID_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (int.TryParse(sender.Text, out var id))
            {
                Frame.Navigate(typeof(IllustDetailPage), id, App.FromRightTransitionInfo);
                Hide();
            }
        }

        private void style_TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            //IME输入不能触发BeforeTextChanging，我估计是个Bug
            //只能在此确保绝对没有不是数字的东西混进来
            sender.Text = new string(sender.Text.Where(char.IsDigit).ToArray());
        }

        private void style_TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }
    }

    static class StorageFileExt
    {
        /// <summary>
        /// 将文件转换为字节数组
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<byte[]> AsByteArray(this Windows.Storage.StorageFile file)
        {
            Windows.Storage.Streams.IRandomAccessStream fileStream =
                await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            var reader = new Windows.Storage.Streams.DataReader(fileStream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)fileStream.Size);
            byte[] pixels = new byte[fileStream.Size];
            reader.ReadBytes(pixels);
            return pixels;
        }
    }
}
