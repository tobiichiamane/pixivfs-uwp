using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace PixivFSUWP
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            _ = LoadContentsAsync();
        }

        private void API_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["SauceNAOAPI"] = tbSauceNAO.Text;
            localSettings.Values["ImgurAPI"] = tbImgur.Text;
        }

        private void AutoSave_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked ?? false)
            {
                AutoSavePanel.Visibility = Visibility.Visible;
                ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] = true;
            }
            else
            {
                AutoSavePanel.Visibility = Visibility.Collapsed;
                ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] = false;
            }
        }

        private async void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = Data.OverAll.GetResourceString("Recalculating");
            await Data.CacheManager.ClearCacheAsync();
            await CalculateCacheSize();
        }

        private async void btnDelInvalid_Click(object sender, RoutedEventArgs e)
        {
            txtCacheSize.Text = Data.OverAll.GetResourceString("Recalculating");
            await Data.CacheManager.ClearTempFilesAsync();
            await CalculateCacheSize();
        }

        private async void BtnGithub_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/tobiichiamane/pixivfs-uwp"));

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var vault = new PasswordVault();
            try
            {
                vault.Remove(Data.OverAll.GetCredentialFromLocker(Data.OverAll.passwordResource));
                vault.Remove(Data.OverAll.GetCredentialFromLocker(Data.OverAll.refreshTokenResource));
            }
            catch { }
            finally
            {
                Data.OverAll.TheMainPage.Frame.Navigate(typeof(LoginPage), null, App.FromRightTransitionInfo);
            }
        }
        //腾讯的一键加群
        private async void BtnQQGroup_Click(object sender, RoutedEventArgs e) =>
            await Launcher.LaunchUriAsync(new Uri(@"https://shang.qq.com/wpa/qunwpa?idkey=d6ba54103ced0e2d7c5bbf6422e4f9f6fa4849c91d4521fe9a1beec72626bbb6"));

        private async Task CalculateCacheSize()
        {
            //计算缓存大小
            var cacheSize = await Data.CacheManager.GetCacheSizeAsync();
            decimal sizeInMB = new decimal(cacheSize) / new decimal(1048576);
            txtCacheSize.Text = decimal.Round(sizeInMB, 2).ToString() + "MB";
        }

        private void ComboBox_DropDownClosed(object sender, object e)
        {
            if (sender is ComboBox cb)
            {
                // 保存颜色主题信息
                switch (cb.SelectedIndex)
                {
                    case 2:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = null;
                        break;
                    case 0:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = false;
                        break;
                    case 1:
                        ApplicationData.Current.LocalSettings.Values["ColorTheme"] = true;
                        break;
                }
                _ = Data.OverAll.TheMainPage?.ShowTip(Data.OverAll.GetResourceString("RestartApplyColorTheme"));
            }
        }

        private void FileSystemHelp_HyperlinkButton_Click(object sender, RoutedEventArgs e) => FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);

        private async Task LoadContentsAsync()
        {
            var imgTask = Data.OverAll.LoadImageAsync(Data.OverAll.currentUser.Avatar170);
            txtVersion.Text = Data.OverAll.GetResourceString("ReleasedVersion") + string.Format("{0} version-{1}.{2}.{3} {4}",
                Package.Current.DisplayName,
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Architecture);
            txtPkgName.Text = Data.OverAll.GetResourceString("ReleasedID") + string.Format("{0}", Package.Current.Id.Name);
            txtInsDate.Text = Data.OverAll.GetResourceString("ReleasedTime") + string.Format("{0}", Package.Current.InstalledDate.ToLocalTime().DateTime);
            txtID.Text = Data.OverAll.currentUser.ID.ToString();
            txtName.Text = Data.OverAll.currentUser.Username;
            txtAccount.Text = "@" + Data.OverAll.currentUser.UserAccount;
            txtEmail.Text = Data.OverAll.currentUser.Email;
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
            _ = LoadContributors();
            //TODO: 考虑设置项不存在的情况
            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //tbSauceNAO.Text = localSettings.Values["SauceNAOAPI"] as string;//读取设置项
            //tbImgur.Text = localSettings.Values["ImgurAPI"] as string;
            // 获取储存的颜色主题信息
            switch (ApplicationData.Current.LocalSettings.Values["ColorTheme"])
            {
                case false:
                    cboxColorTheme.SelectedIndex = 0;
                    break;
                case true:
                    cboxColorTheme.SelectedIndex = 1;
                    break;
                default:
                    cboxColorTheme.SelectedIndex = 2;
                    break;
            }
            // 检查文件系统访问权限
            //_ = FileAccessPermissionsCheck();

            // 自动保存
            if (ApplicationData.Current.LocalSettings.Values["PictureAutoSave"] is bool autosave)
                UseAutoSave_CB.IsChecked = autosave;
            else UseAutoSave_CB.IsChecked = false;

            // 图片名格式
            if (ApplicationData.Current.LocalSettings.Values["PictureSaveName"] is string psn &&
                !string.IsNullOrWhiteSpace(psn))
                PicName_ASB.Text = psn;
            else PicName_ASB.Text = "${picture_id}_${picture_page}";
            PicName_ASB.PlaceholderText = PicName_ASB.Text;

            // 图片保存位置
            if (ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] is string psd &&
                !string.IsNullOrWhiteSpace(psd))
                PicSaveDir_ASB.Text = psd;
            else
                PicSaveDir_ASB.Text = KnownFolders.SavedPictures.Path;
            PicSaveDir_ASB.PlaceholderText = PicSaveDir_ASB.Text;
            _ = CalculateCacheSize();
            //等待头像加载完毕
            imgAvatar.ImageSource = await imgTask;
        }

        private async Task LoadContributors()
        {
            progressLoadingContributors.Visibility = Visibility.Visible;
            progressLoadingContributors.IsActive = true;
            var res = await Data.ContributorsHelper.GetContributorsAsync();
            if (res == null)
            {
                progressLoadingContributors.Visibility = Visibility.Collapsed;
                progressLoadingContributors.IsActive = false;
                txtContributors.Text = Data.OverAll.GetResourceString("ContributorsReadingErrorPlain");
                return;
            }
            var enumerable = from item in res select ViewModels.ContributorViewModel.FromItem(item);
            lstContributors.ItemsSource = enumerable.ToList();
            progressLoadingContributors.Visibility = Visibility.Collapsed;
            progressLoadingContributors.IsActive = false;
            lstContributors.Visibility = Visibility.Visible;
        }
        private async void LstMainDev_ItemClick(object sender, ItemClickEventArgs e) => await Launcher.LaunchUriAsync(new Uri((e.ClickedItem as ViewModels.ContributorViewModel).ProfileUrl));
        private void PicName_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(sender.Text))
            {
                ApplicationData.Current.LocalSettings.Values["PictureSaveName"] = sender.Text;
            }
        }

        private void PicNameHelp_QS(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => FlyoutBase.ShowAttachedFlyout(sender);

        private async void PicSaveDir_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var text = sender.Text;

            try
            {
                _ = await StorageFolder.GetFolderFromPathAsync(text);
                ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] = text;
            }
            catch (UnauthorizedAccessException)
            {
                FSProblem_LinkBtn.Visibility = Visibility.Visible;
                sender.Text = ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] as string;
            }
        }

        private async void SelectSaveDir_QS(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                sender.Text = folder.Path;
            }
        }
    }
}
