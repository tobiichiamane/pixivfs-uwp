using PixivFSUWP.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static PixivFSUWP.Data.OverAll;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace PixivFSUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingsPage : Page, IGoBackFlag
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            _ = loadContentsAsync();
        }

        private bool _backflag { get; set; } = false;

        public void SetBackFlag(bool value)
        {
            _backflag = value;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ((Frame.Parent as Grid)?.Parent as MainPage)?.SelectNavPlaceholder(GetResourceString("SettingsPagePlain"));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (!_backflag)
            {
                Data.Backstack.Default.Push(typeof(SettingsPage), null);
                ((Frame.Parent as Grid)?.Parent as MainPage)?.UpdateNavButtonState();
            }
        }

        async Task loadContentsAsync()
        {
            var imgTask = LoadImageAsync(currentUser.Avatar170);
            txtVersion.Text = string.Format("版本：{0} version-{1}.{2}.{3} {4}",
                Package.Current.DisplayName,
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Architecture);
            txtPkgName.Text = string.Format("包名：{0}", Package.Current.Id.Name);
            txtInsDate.Text = string.Format("时间：{0}", Package.Current.InstalledDate.ToLocalTime().DateTime);
            txtID.Text = currentUser.ID.ToString();
            txtName.Text = currentUser.Username;
            txtAccount.Text = "@" + currentUser.UserAccount;
            txtEmail.Text = currentUser.Email;
            //硬编码主要开发者信息
            List<ViewModels.ContributorViewModel> mainDevs = new List<ViewModels.ContributorViewModel>();
            mainDevs.Add(new ViewModels.ContributorViewModel()
            {
                Account = "@tobiichiamane",
                DisplayName = "Communist Fish",
                AvatarUrl = "https://avatars2.githubusercontent.com/u/14824064?v=4&s=45",
                ProfileUrl = "https://github.com/tobiichiamane",
                Contributions = new List<Data.Contribution>()
                {
                    Data.Contribution.bug,
                    Data.Contribution.code,
                    Data.Contribution.doc,
                    Data.Contribution.idea,
                    Data.Contribution.translation
                }
            });
            lstMainDev.ItemsSource = mainDevs;
            //加载贡献者信息
            _ = loadContributors();
            //TODO: 考虑设置项不存在的情况
            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //tbSauceNAO.Text = localSettings.Values["SauceNAOAPI"] as string;//读取设置项
            //tbImgur.Text = localSettings.Values["ImgurAPI"] as string;
            _ = calculateCacheSize();
            //等待头像加载完毕
            imgAvatar.ImageSource = await imgTask;
        }

        async Task loadContributors()
        {
            progressLoadingContributors.Visibility = Visibility.Visible;
            progressLoadingContributors.IsActive = true;
            var res = await Data.ContributorsHelper.GetContributorsAsync();
            if (res == null)
            {
                progressLoadingContributors.Visibility = Visibility.Collapsed;
                progressLoadingContributors.IsActive = false;
                txtContributors.Text = "Failed to load contributors.";
                return;
            }
            var enumerable = from item in res select ViewModels.ContributorViewModel.FromItem(item);
            lstContributors.ItemsSource = enumerable.ToList();
            progressLoadingContributors.Visibility = Visibility.Collapsed;
            progressLoadingContributors.IsActive = false;
            lstContributors.Visibility = Visibility.Visible;
        }

        async Task calculateCacheSize()
        {
            //计算缓存大小
            var cacheSize = await Data.CacheManager.GetCacheSizeAsync();
            decimal sizeInMB = new decimal(cacheSize) / new decimal(1048576);
            txtCacheSize.Text = decimal.Round(sizeInMB, 2).ToString() + "MB";
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var vault = new PasswordVault();
            try
            {
                vault.Remove(GetCredentialFromLocker(passwordResource));
                vault.Remove(GetCredentialFromLocker(refreshTokenResource));
            }
            catch { }
            finally
            {
                ((Frame.Parent as Grid).Parent as MainPage).Frame.Navigate(typeof(LoginPage));
            }
        }

        private async void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new
                Uri(@"https://github.com/tobiichiamane/pixivfs-uwp"));
        }

        private void API_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["SauceNAOAPI"] = tbSauceNAO.Text;
            localSettings.Values["ImgurAPI"] = tbImgur.Text;
        }

        private async void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = GetResourceString("Recalculating");
            await Data.CacheManager.ClearCacheAsync();
            await calculateCacheSize();
        }

        private async void btnDelInvalid_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = GetResourceString("Recalculating");
            await Data.CacheManager.ClearTempFilesAsync();
            await calculateCacheSize();
        }

        private async void lstMainDev_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as ViewModels.ContributorViewModel;
            await Launcher.LaunchUriAsync(new Uri(item.ProfileUrl));
        }

        private async void btnQQGroup_Click(object sender, RoutedEventArgs e)
        {
            //腾讯的一键加群
            await Launcher.LaunchUriAsync(new
                Uri(@"https://shang.qq.com/wpa/qunwpa?idkey=d6ba54103ced0e2d7c5bbf6422e4f9f6fa4849c91d4521fe9a1beec72626bbb6"));
        }
    }
}
