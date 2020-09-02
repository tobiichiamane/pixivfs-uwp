using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using PixivCS;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace PixivFSUWP
{
    public sealed partial class SearchDialog : ContentDialog
    {
        private readonly Frame Frame;
        private int lastDuration = -1;
        private int lastSearchTarget = -1;
        private int lastSort = -1;
        private string lastWord = null;
        public SearchDialog()
        {
            this.InitializeComponent();
            _ = LoadContents();
        }
        public SearchDialog(Frame frame) : this() => Frame = frame;

        private void BtnTag_Click(object sender, RoutedEventArgs e)
        {
            txtWord.Text = (sender as Button).Tag as string;
            cbSearchTarget.SelectedIndex = 1;
            cbSort.SelectedIndex = 0;
            cbDuration.SelectedIndex = 0;
            TxtWord_QuerySubmitted(null, null);
        }

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
        private void GoPixivID_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (int.TryParse(asbGTPID.Text, out int id))
            {
                Hide();
                Frame?.Navigate(typeof(IllustDetailPage), id, App.FromRightTransitionInfo);
            }
        }

        private async Task LoadContents()
        {
            stkMain.Visibility = Visibility.Visible;
            var tags = await GetTrendingTags();
            progressRing.Visibility = Visibility.Collapsed;
            panelTags.ItemsSource = tags;
        }

        private void Style_TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args) => args.Cancel = args.NewText.Any(c => !char.IsDigit(c));

        //IME输入不能触发BeforeTextChanging，我估计是个Bug
        //只能在此确保绝对没有不是数字的东西混进来
        private void Style_TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args) => sender.Text = new string(sender.Text.Where(char.IsDigit).ToArray());

        private void TxtWord_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => Search(txtWord.Text);

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
                Data.Collections.IllustsCollectionManager.RefreshSearchResultList(param);
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
    }
}
