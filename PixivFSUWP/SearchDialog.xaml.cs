using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using PixivCS;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace PixivFSUWP
{
    public sealed partial class SearchDialog : ContentDialog
    {
        private string lastWord = null;
        private int lastSearchTarget = -1;
        private int lastSort = -1;
        private int lastDuration = -1;

        private readonly Frame Frame;
        public SearchDialog()
        {
            this.InitializeComponent();
            _ = LoadContents();
        }
        public SearchDialog(Frame frame) : this() => Frame = frame;

        private async Task<List<ViewModels.TagViewModel>> GetTrendingTags()
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
        /// <summary>
        /// Command: 搜索<br/>
        /// 输入关键词, 导航至一个搜索结果页面
        /// </summary>
        /// <param name="s"></param>
        public void Search(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            if (s.Trim() != lastWord || cbSearchTarget.SelectedIndex != lastSearchTarget ||
                cbSort.SelectedIndex != lastSort || cbDuration.SelectedIndex != lastDuration)
            {
                var param = new Data.OverAll.SearchParam() { Word = s.Trim() };
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
                Data.OverAll.RefreshSearchResultList(param);
                Hide();
                Frame?.Navigate(typeof(WaterfallPage), WaterfallPage.ListContent.SearchResult, App.FromRightTransitionInfo);
            }
            if (s.Trim() != lastWord || cbSearchTarget.SelectedIndex != lastSearchTarget ||
                cbSort.SelectedIndex != lastSort || cbDuration.SelectedIndex != lastDuration)
            {
                lastWord = s.Trim();
                lastSearchTarget = cbSearchTarget.SelectedIndex;
                lastSort = cbSort.SelectedIndex;
                lastDuration = cbDuration.SelectedIndex;
            }
        }
        private async Task LoadContents()
        {
            stkMain.Visibility = Visibility.Visible;
            var tags = await GetTrendingTags();
            progressRing.Visibility = Visibility.Collapsed;
            panelTags.ItemsSource = tags;
        }

        private void TxtWord_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => Search(txtWord.Text);

        private void GoPixivID_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            try
            {
                if (int.TryParse(asbGTPID.Text, out int id))
                {
                    Hide();
                    Frame?.Navigate(typeof(IllustDetailPage), id, App.FromRightTransitionInfo);
                }
            }
            catch (OverflowException)
            {
                //吞了异常。一般是由于输入的数字过大，超过了Int32的限制导致
            }
        }
        private void BtnTag_Click(object sender, RoutedEventArgs e)
        {
            txtWord.Text = (sender as Button).Tag as string;
            cbSearchTarget.SelectedIndex = 1;
            cbSort.SelectedIndex = 0;
            cbDuration.SelectedIndex = 0;
            TxtWord_QuerySubmitted(null, null);
        }

        private void Style_TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            //IME输入不能触发BeforeTextChanging，我估计是个Bug
            //只能在此确保绝对没有不是数字的东西混进来
            sender.Text = new string(sender.Text.Where(char.IsDigit).ToArray());
        }

        private void Style_TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }
    }
}
