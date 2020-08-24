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
    public sealed partial class SearchPage : Page, IGoBackFlag
    {
        string lastWord = null;
        int lastSearchTarget = -1;
        int lastSort = -1;
        int lastDuration = -1;

        public SearchPage()
        {
            this.InitializeComponent();
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.OverAll.SearchResultList?.PauseLoading();
                if (e.Parameter is ValueTuple<int, int?> tuple)
                {
                    Data.Backstack.Default.Push
                        (typeof(SearchPage),
                        new ValueTuple<WaterfallPage.ListContent, int?>
                        (WaterfallPage.ListContent.SearchResult, tuple.Item2));
                }
                else
                    Data.Backstack.Default.Push(typeof(SearchPage), WaterfallPage.ListContent.SearchResult);
            }
            ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
        }

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
            stkMain.Visibility = Visibility.Visible;
            var tags = await getTrendingTags();
            //progressRing.IsActive = false;
            progressRing.Visibility = Visibility.Collapsed;
            panelTags.ItemsSource = tags;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string s)
            {
                txtWord.Text = s;
                TxtWord_QuerySubmitted(txtWord, null);
            }
            else if (e.Parameter is WaterfallPage.ListContent)
            {
                _ = loadContents();
                SearchResultList?.StopLoading();
            }
            else if (e.Parameter is ValueTuple<WaterfallPage.ListContent, int?>)
            {
                resultFrame.Navigate(typeof(WaterfallPage), e.Parameter);
                grdSearchPanel.Visibility = Visibility.Collapsed;
                if (txtWord.Text.Trim() != lastWord || cbSearchTarget.SelectedIndex != lastSearchTarget ||
                    cbSort.SelectedIndex != lastSort || cbDuration.SelectedIndex != lastDuration)
                {
                    lastWord = txtWord.Text.Trim();
                    lastSearchTarget = cbSearchTarget.SelectedIndex;
                    lastSort = cbSort.SelectedIndex;
                    lastDuration = cbDuration.SelectedIndex;
                }
            }
            ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(GetResourceString("SearchPagePlain"));
        }

        public async Task ShowSearch()
        {
            if (grdSearchPanel.Visibility == Visibility.Collapsed)
            {
                searchProgressRing.Visibility = Visibility.Collapsed;
                searchProgressRing.IsActive = false;
                grdSearchPanel.Visibility = Visibility.Visible;
                stkMain.Visibility = Visibility.Collapsed;
                storyShow.Begin();
                await Task.Delay(200);
            }
            else stkMain.Visibility = Visibility.Collapsed;
            //progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;
            (panelTags.ItemsSource as List<ViewModels.TagViewModel>).Clear();
            panelTags.ItemsSource = null;
            await loadContents();
        }

        private async void TxtWord_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(txtWord.Text)) return;
            if (txtWord.Text.Trim() != lastWord || cbSearchTarget.SelectedIndex != lastSearchTarget ||
                cbSort.SelectedIndex != lastSort || cbDuration.SelectedIndex != lastDuration)
            {
                if (resultFrame.Content != null)
                    (resultFrame.Content as WaterfallPage).ItemsSource.CollectionChanged -= ItemsSource_CollectionChanged;
                var param = new OverAll.SearchParam()
                {
                    Word = txtWord.Text.Trim()
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
                OverAll.RefreshSearchResultList(param);
                resultFrame.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.SearchResult, App.FromRightTransitionInfo);
                (resultFrame.Content as WaterfallPage).ItemsSource.CollectionChanged += ItemsSource_CollectionChanged;
            }
            storyFade.Begin();
            await Task.Delay(200);
            grdSearchPanel.Visibility = Visibility.Collapsed;
            if (txtWord.Text.Trim() != lastWord || cbSearchTarget.SelectedIndex != lastSearchTarget ||
                cbSort.SelectedIndex != lastSort || cbDuration.SelectedIndex != lastDuration)
            {
                lastWord = txtWord.Text.Trim();
                lastSearchTarget = cbSearchTarget.SelectedIndex;
                lastSort = cbSort.SelectedIndex;
                lastDuration = cbDuration.SelectedIndex;
                searchProgressRing.IsActive = true;
                searchProgressRing.Visibility = Visibility.Visible;
            }
        }

        private void ItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            searchProgressRing.Visibility = Visibility.Collapsed;
            searchProgressRing.IsActive = false;
        }

        private void BtnTag_Click(object sender, RoutedEventArgs e)
        {
            txtWord.Text = (sender as Button).Tag as string;
            cbSearchTarget.SelectedIndex = 1;
            cbSort.SelectedIndex = 0;
            cbDuration.SelectedIndex = 0;
            TxtWord_QuerySubmitted(null, null);
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
            try
            {
                Frame.Navigate(typeof(IllustDetailPage), int.Parse(asbGTPID.Text), App.FromRightTransitionInfo);
            }
            catch (OverflowException)
            {
                //吞了异常。一般是由于输入的数字过大，超过了Int32的限制导致
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
