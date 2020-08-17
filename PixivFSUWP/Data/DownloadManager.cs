using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

        private static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            for (int i = 0; i < list.Count; i++) action.Invoke(list[i]);
        }

        // 获取文件名
        private static string GetPicName(string template, IllustDetail illust, ushort p)
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

        //下载任务列表
        public static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();

        //完成任务列表
        public static ObservableCollection<DownloadJob> FinishedJobs = new ObservableCollection<DownloadJob>();

        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        // 获取文件对象
        public static async Task<StorageFile> GetPicFile(IllustDetail illust, ushort p)
        {
            if (LocalSettings["PictureAutoSave"] is bool b && b)// 启用自动保存
            {
                var fileName = GetPicName(LocalSettings["PictureSaveName"] as string, illust, p);
                var folder = await StorageFolder.GetFolderFromPathAsync(LocalSettings["PictureSaveDirectory"] as string);
                return await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }
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
                picker.SuggestedFileName = illust.Title;
                return await picker.PickSaveFileAsync();
            }
        }

        // 从文件反序列化下载任务
        public static async Task Init()
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("DownloadManager.json");
            if (file is null) return;
            var json = Windows.Data.Json.JsonValue.Parse(await FileIO.ReadTextAsync(file)).GetObject();
            var downloading = json["Downloading"].GetArray().Select(i =>
            new DownloadJob(i.GetObject()["Title"].GetString(), i.GetObject()["Uri"].GetString(), i.GetObject()["FilePath"].GetString()));
            var downloaded = json["Downloaded"].GetArray().Select(i =>
            new FinishedJob(i.GetObject()["Title"].GetString(), i.GetObject()["Uri"].GetString(), i.GetObject()["FilePath"].GetString()));
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    DownloadJobs = new ObservableCollection<DownloadJob>(downloading);
                    FinishedJobs = new ObservableCollection<DownloadJob>(downloaded);
                });
        }

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath) => AddJob(new DownloadJob(Title, Uri, FilePath));

        //添加下载任务
        public static void NewJob(string Title, string Uri, StorageFile File) => AddJob(new DownloadJobPlus(Title, Uri, File));

        public static void NewUgoiraJob(string title, string zipurl, StorageFile file, PixivCS.Objects.UgoiraMetadata res) => AddJob(new UgoiraDownloadJob(title, zipurl, file, res));

        //移除已完成下载任务
        public static async Task RemoveFinishedJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.DownloadCancel -= Job_DownloadCancel;
            Job.Cancel();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => FinishedJobs.Remove(Job));
        }

        //移除下载任务
        [Obsolete]
        public static void RemoveJob(int Index) => RemoveJob(DownloadJobs[Index]);

        //移除下载任务
        public static void RemoveJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.DownloadCancel -= Job_DownloadCancel;
            Job.Cancel();
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => DownloadJobs.Remove(Job));
        }

        // 重置任务状态
        public static async Task ResetJob(DownloadJob job)
        {
            await RemoveFinishedJob(job);
            job.Reset();
            AddJob(job);
        }

        // 将下载任务序列化到文件
        public static async Task Save()
        {
            var fileTask = ApplicationData.Current.LocalFolder.GetFileAsync("DownloadManager.json");
            var downloadings = new Windows.Data.Json.JsonArray();
            var downloaded = new Windows.Data.Json.JsonArray();
            var t1 = Task.Run(() => DownloadJobs.ForEach(i =>
                 downloadings.Add(new Windows.Data.Json.JsonObject
                 {
                    { "Title", Windows.Data.Json.JsonValue.CreateStringValue(i.Title) },
                    { "Uri", Windows.Data.Json.JsonValue.CreateStringValue(i.Uri) },
                    { "FilePath", Windows.Data.Json.JsonValue.CreateStringValue(i.FilePath) }
                 }
             )));
            var t2 = Task.Run(() => FinishedJobs.ForEach(i =>
                    downloaded.Add(new Windows.Data.Json.JsonObject
                    {
                    { "Title", Windows.Data.Json.JsonValue.CreateStringValue(i.Title) },
                    { "Uri", Windows.Data.Json.JsonValue.CreateStringValue(i.Uri) },
                    { "FilePath", Windows.Data.Json.JsonValue.CreateStringValue(i.FilePath) }
                    }
                )));
            var file = await fileTask;
            if (file is null) return;
            await Task.WhenAll(t1, t2);
            var json = new Windows.Data.Json.JsonObject
            {
                { "Downloading", downloadings },
                { "Downloaded", downloaded }
            };
            using (var fs = await file.OpenStreamForWriteAsync())
            using (var sw = new StreamWriter(fs))
                sw.Write(json.ToString());
        }
    }
}