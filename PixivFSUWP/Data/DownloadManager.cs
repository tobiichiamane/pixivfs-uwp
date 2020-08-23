using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PixivFSUWP.Data.DownloadJobs;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;

namespace PixivFSUWP.Data
{
    //静态的下载管理器。应用程序不会有多个下载管理器实例。
    public static partial class DownloadManager
    {
        #region 私有成员
        // 本地设置
        private static readonly Windows.Foundation.Collections.IPropertySet LocalSettings = ApplicationData.Current.LocalSettings.Values;

        // 添加任务
        private static void AddJob(DownloadJob job)
        {
            job.DownloadCompleted += Job_DownloadCompleted;
            job.DownloadCancel += Job_DownloadCancel;
            DownloadJobs.Add(job);
            Downloader.Add(job);
        }

        // 获取文件名
        private static string GetDownloadTargetFileName(string template, IllustDetail illust, ushort p)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < template.Length; i++)
            {
                var ch = template[i];
                if (ch.Equals('$'))
                {
                    i++;
                    switch (ch = template[i])
                    {
                        case 'P': // PID
                            sb.Append(illust.IllustID);
                            break;

                        case 'p': // 分P数
                            sb.Append(p);
                            break;

                        case 't': // 标题
                            sb.Append(illust.Title);
                            break;

                        case 'l': // 来源URL
                            sb.Append(illust.OriginalUrls[p]);
                            break;

                        case 'd': // 上传日期
                            sb.Append(illust.CreateDate);
                            break;

                        case 'A': // 作者UID
                            sb.Append(illust.AuthorID);
                            break;

                        case 'a': // 作者名
                            sb.Append(illust.Author);
                            break;

                        case '{':
                            var longname = new StringBuilder();
                            i++;
                            ch = template[i];
                            while (!ch.Equals('}'))
                            {
                                longname.Append(ch);
                                i++;
                                ch = template[i];
                            }
                            switch (longname.ToString())
                            {
                                case "picture_id": // PID
                                    sb.Append(illust.IllustID);
                                    break;

                                case "picture_page": // 分P数
                                    sb.Append(p);
                                    break;

                                case "picture_title": // 标题
                                    sb.Append(illust.Title);
                                    break;

                                case "picture_url": // 来源URL
                                    sb.Append(illust.OriginalUrls[p]);
                                    break;

                                case "upload_date": // 上传日期
                                    sb.Append(illust.CreateDate);
                                    break;

                                case "author_id": // 作者UID
                                    sb.Append(illust.AuthorID);
                                    break;

                                case "author_name": // 作者名
                                    sb.Append(illust.Author);
                                    break;
                            }
                            break;

                        case '\\':
                        case '/':
                        case ':':
                        case '*':
                        case '?':
                        case '"':
                        case '<':
                        case '>':
                        case '|':
                            sb.Append(' ');
                            break;
                    }
                }
                else sb.Append(ch);
            }
            sb.Append('.');
            if (illust.Type.Equals("ugoira", StringComparison.OrdinalIgnoreCase))
                sb.Append("gif");
            else sb.Append(illust.OriginalUrls[p].Split('.').Last());

            return sb.ToString();
        }

        // 下载被取消时
        private static async void Job_DownloadCancel(DownloadJob job)
        {
            job.DownloadCompleted -= Job_DownloadCompleted;
            job.DownloadCancel -= Job_DownloadCancel;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    DownloadJobs.Remove(job);
                    FinishedJobs.Add(job);
                });
        }

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            Job_DownloadCancel(source);
            DownloadCompleted?.Invoke(source.Title, args.HasError);
        }

        private static void RemoveJob(this List<DownloadJob> list, DownloadJob job)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].FilePath.Equals(job.FilePath)) list.RemoveAt(i);
        }
        #endregion

        #region 添加下载任务
        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="title">下载任务标题</param>
        /// <param name="uri">下载地址</param>
        /// <param name="filePath">目标文件地址</param>
        public static void NewJob(string title, string uri, string filePath) => AddJob(new DownloadJob(title, uri, filePath));

        /// <summary>
        /// 添加下载任务
        /// </summary>
        /// <param name="title">下载任务标题</param>
        /// <param name="uri">下载地址</param>
        /// <param name="file">目标文件</param>
        public static void NewJob(string title, string uri, StorageFile file) => AddJob(new DownloadJobPlus(title, uri, file));

        /// <summary>
        /// 添加动画下载任务
        /// </summary>
        /// <param name="title">下载任务标题</param>
        /// <param name="zipurl">下载地址</param>
        /// <param name="file">目标文件</param>
        /// <param name="res"></param>
        public static void NewUgoiraJob(string title, string zipurl, StorageFile file, PixivCS.Objects.UgoiraMetadata res) => AddJob(new UgoiraDownloadJob(title, zipurl, file, res));
        #endregion

        /// <summary>
        /// 下载任务列表
        /// </summary>
        public readonly static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();

        /// <summary>
        /// 完成任务列表
        /// </summary>
        public readonly static ObservableCollection<DownloadJob> FinishedJobs = new ObservableCollection<DownloadJob>();

        /// <summary>
        /// 有任务下载完成时的事件
        /// </summary>
        public static event Action<string, bool> DownloadCompleted;

        /// <summary>
        /// 有任务开始时的事件
        /// </summary>
        public static event Action<string> DownloadJobsAdd;

        #region 扩展方法-下载
        /// <summary>
        /// 自动判断<see cref="IllustDetail"/>的类型并选择合适的下载方法
        /// </summary>
        public static Task AutoDownload(this IllustDetail illust, bool forcsSaveAll = false) =>
                illust.Type == "ugoira"// 判断类型
                ? illust.DownloadUgoiraImage()// 保存动图
                                              // TODO: 把保存全部图片做成设置
                : (illust.OriginalUrls.Count > 1) || forcsSaveAll// 不是动图 读取设置
                ? illust.DownloadAllImage()// 保存全部图片
                : illust.DownloadFirstImage();// 保存第一张图片

        /// <summary>
        /// 下载全部 分P
        /// </summary>
        public static async Task DownloadAllImage(this IllustDetail illust)
        {
            DownloadJobsAdd?.Invoke(illust.Title);
            for (ushort i = 0; i < illust.OriginalUrls.Count; i++)
                NewJob(illust.Title, illust.OriginalUrls[i], await illust.GetDownloadTargetFile(i));
        }

        /// <summary>
        /// 下载第一张图片
        /// </summary>
        public static async Task DownloadFirstImage(this IllustDetail illust)
        {
            DownloadJobsAdd?.Invoke(illust.Title);
            NewJob(illust.Title, illust.OriginalUrls[0], await illust.GetDownloadTargetFile(0));
        }

        /// <summary>
        /// 下载动图
        /// </summary>
        public static async Task DownloadUgoiraImage(this IllustDetail illust)
        {
            DownloadJobsAdd?.Invoke(illust.Title);
            var file = await illust.GetDownloadTargetFile(0);
            var res = await new PixivCS.PixivAppAPI(OverAll.GlobalBaseAPI).GetUgoiraMetadataAsync(illust.IllustID.ToString());
            var zipurl = res.UgoiraMetadataUgoiraMetadata.ZipUrls.Medium?.ToString() ?? string.Empty;
            NewUgoiraJob(illust.Title, zipurl, file, res);
        }
        #endregion

        #region 其他扩展方法
        /// <summary>
        /// 获取下载管理器将要下载到的文件对象
        /// </summary>
        public static async Task<StorageFile> GetDownloadTargetFile(this IllustDetail illust, ushort p)
        {
            var fileName = GetDownloadTargetFileName(LocalSettings["PictureSaveName"] as string, illust, p);
            var folder = await StorageFolder.GetFolderFromPathAsync(LocalSettings["PictureSaveDirectory"] as string);
            if (LocalSettings["PictureAutoSave"] is bool b && b)// 启用自动保存
                return await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            else// 不启用自动保存 将使用 FileSavePicke
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add(OverAll.GetResourceString("ImageFilePlain"),
                    new List<string>() {"."+
                            (illust.Type.Equals("ugoira", StringComparison.OrdinalIgnoreCase)
                                ? "gif"
                                : illust.OriginalUrls[p].Split('.').Last())
                    });
                picker.SuggestedFileName = fileName;
                return await picker.PickSaveFileAsync();
            }
        }

        /// <summary>
        /// 从队列中移除下载任务
        /// </summary>
        /// <param name="jobs">队列 <see cref="DownloadJobs"/> 或 <see cref="FinishedJobs"/></param>
        /// <param name="Job">下载任务</param>
        public static async Task RemoveJob(this ObservableCollection<DownloadJob> jobs, DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.DownloadCancel -= Job_DownloadCancel;
            Job.Cancel();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => jobs.Remove(Job));
        }

        /// <summary>
        /// 从正在下载队列中移除下载任务
        /// </summary>
        public static Task RemoveJobFromDownloading(this DownloadJob job) => DownloadJobs.RemoveJob(job);

        /// <summary>
        /// 从已完成下载队列中移除下载任务
        /// </summary>
        public static Task RemoveJobFromFinished(this DownloadJob job) => FinishedJobs.RemoveJob(job);
        /// <summary>
        /// 重置任务状态
        /// </summary>
        public static async Task ResetJob(this DownloadJob job)
        {
            await FinishedJobs.RemoveJob(job);
            job.Reset();
            AddJob(job);
        }
        #endregion
    }
}