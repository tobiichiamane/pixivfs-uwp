using Lumia.Imaging.Compositing;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Core;

namespace PixivFSUWP.Data
{
    //下载完成时的事件参数
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool HasError { get; set; }
    }

    public class DownloadJob : INotifyPropertyChanged
    {
        public string Title { get; }
        public string Uri { get; }
        public string FilePath { get; }

        private int progress;
        public int Progress
        {
            get => progress;
            protected set
            {
                progress = value;
                _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Progress"));
                    });
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Progress"));
            }
        }
        public DownloadJob(string Title, string Uri, string FilePath)
        {
            this.Title = Title;
            this.Uri = Uri;
            this.FilePath = FilePath;
            Progress = 0;
            Downloading = false;
            isPaused = true;
        }

        //下载状态
        public bool Downloading { get; private set; }
        //用于暂停的ManualResetEvent
        protected ManualResetEvent pauseEvent = new ManualResetEvent(true);

        //用于取消任务的CancellationTokenSource
        protected CancellationTokenSource tokenSource = new CancellationTokenSource();
        private bool isPaused;

        //下载完成时的事件
        public event Action<DownloadJob, DownloadCompletedEventArgs> DownloadCompleted;

        public event Action<DownloadJob> DownloadPause;
        public event Action<DownloadJob> DownloadResume;
        public event Action<DownloadJob> DownloadCancel;

        //通知属性更改
        public event PropertyChangedEventHandler PropertyChanged;

        //进行下载
        public virtual async Task Download()
        {
            if (!Downloading)
            {
                Downloading = true;
                isPaused = false;
                using (var memStream = await OverAll.DownloadImage(Uri, tokenSource.Token, pauseEvent, async (loaded, length) =>
                {
                    await Task.Run(() =>
                    {
                        Progress = (int)(loaded * 100 / length);
                    });
                }))
                {
                    if (tokenSource.IsCancellationRequested) return;
                    if (await WriteToFile(memStream) == FileUpdateStatus.Complete)
                    {
                        DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs() { HasError = false });
                    }
                    else
                    {
                        DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs() { HasError = true });
                    }
                }
            }
        }
        // 文件的写入方法
        protected virtual async Task<FileUpdateStatus> WriteToFile(Stream memStream)
        {
            var file = await StorageFile.GetFileFromPathAsync(FilePath);
            CachedFileManager.DeferUpdates(file);
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await memStream.CopyToAsync(fileStream.AsStream());
            }
            return await CachedFileManager.CompleteUpdatesAsync(file);

        }

        public bool IsPaused
        {
            get => isPaused; private set
            {
                isPaused = value;
                _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPaused)));
                    });
            }
        }

        //暂停下载
        public void Pause()
        {
            pauseEvent.Reset();
            IsPaused = true;
            DownloadPause?.Invoke(this);
        }

        //恢复下载
        public void Resume()
        {
            pauseEvent.Set();
            IsPaused = false;
            DownloadResume?.Invoke(this);
        }

        //取消下载
        public void Cancel()
        {
            tokenSource.Cancel();
            DownloadCancel?.Invoke(this);
        }
        // 重置状态
        public void Reset()
        {
            Progress = 0;
            Downloading = false;
        }
    }

    // 直接传StorageFile对象的...可以避免用FileSavePicker选中的文件传过来没有权限的问题
    // 对FileSavePicker选择的文件直接修改不需要对应的文件系统访问权限
    public class DownloadJobPlus : DownloadJob
    {
        private readonly StorageFile File;
        public DownloadJobPlus(string Title, string Uri, StorageFile File) : base(Title, Uri, File.Path)
        {
            this.File = File;
        }
        protected override async Task<FileUpdateStatus> WriteToFile(Stream memStream)
        {
            CachedFileManager.DeferUpdates(File);
            using (var fileStream = await File.OpenAsync(FileAccessMode.ReadWrite))
            {
                await memStream.CopyToAsync(fileStream.AsStream());
            }
            return await CachedFileManager.CompleteUpdatesAsync(File);
        }
    }
    public class FinishedJob : DownloadJob
    {
        public FinishedJob(string Title, string Uri, string FilePath) : base(Title, Uri, FilePath)
        {
            Progress = 100;
        }
    }

    //静态的下载管理器。应用程序不会有多个下载管理器实例。
    public static class DownloadManager
    {
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
        private static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            for (int i = 0; i < list.Count; i++) action.Invoke(list[i]);
        }

        //下载任务列表
        public static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();

        //完成任务列表
        public static ObservableCollection<DownloadJob> FinishedJobs = new ObservableCollection<DownloadJob>();
        // 添加任务
        private static void AddJob(DownloadJob job)
        {
            job.DownloadCompleted += Job_DownloadCompleted;
            job.DownloadCancel += Job_DownloadCancel;
            DownloadJobs.Add(job);
            Downloader.Add(job);
            Downloader.Run();
            //_ = job.Download();
        }

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath) => AddJob(new DownloadJob(Title, Uri, FilePath));
        //添加下载任务
        public static void NewJob(string Title, string Uri, StorageFile File) => AddJob(new DownloadJobPlus(Title, Uri, File));

        public static void ResetJob(DownloadJob job)
        {
            RemoveFinishedJob(job);
            job.Reset();
            AddJob(job);
        }
        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            source.DownloadCompleted -= Job_DownloadCompleted;
            source.DownloadCancel -= Job_DownloadCancel;
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () =>
                {
                    DownloadJobs.Remove(source);
                    FinishedJobs.Add(source);
                });
            DownloadCompleted?.Invoke(source.Title, args.HasError);
        }
        // 下载被取消时
        private static void Job_DownloadCancel(DownloadJob job)
        {
            job.DownloadCompleted -= Job_DownloadCompleted;
            job.DownloadCancel -= Job_DownloadCancel;
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => DownloadJobs.Remove(job));
            FinishedJobs.Add(job);
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
        //移除已完成下载任务
        public static void RemoveFinishedJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.DownloadCancel -= Job_DownloadCancel;
            Job.Cancel();
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => FinishedJobs.Remove(Job));
        }

        // 本地设置
        private static readonly Windows.Foundation.Collections.IPropertySet LocalSettings = ApplicationData.Current.LocalSettings.Values;

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
                    new List<string>() {
                            (illust.Type.Equals("ugoira", StringComparison.OrdinalIgnoreCase))
                                ? "gif"
                                : illust.OriginalUrls[p].Split('.').Last()
                    });
                picker.SuggestedFileName = illust.Title;
                return await picker.PickSaveFileAsync();
            }
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

        // 下载管理
        private static class Downloader
        {
            public static int MaxJob { get; set; } = 3;// 同时下载最大进程数 为0不限制
            public static bool IsDownloading
            {
                get => isDownloading; private set
                {
                    //if (isDownloading == value) return;// 不需要重复执行相同的操作
                    // 不  需要重复执行相同的操作
                    if (isDownloading = value)// 开始下载线程
                        lock (downloadJobs) _ = Task.Run(downloaderd);
                    else  // 结束下载线程
                        downloadingJobs.ForEach(i => i.Pause());
                }
            }// 下载管理是否正在运行

            static Downloader()
            {
                // TODO 设置同时下载任务数
                //LocalSettings[]
            }

            public static void Run() => IsDownloading = true;
            public static void Stop() => IsDownloading = false;
            public static void Add(DownloadJob job) => downloadJobs.Enqueue(job);// 排队

            private static Mutex downloaderd_lock = new Mutex(false, "downloaderd_lock");// 下载进程锁
            private static Queue<DownloadJob> downloadJobs = new Queue<DownloadJob>();// 下载队列
            private static List<DownloadJob> pausedJobs = new List<DownloadJob>();// 暂停任务列表
            private static List<DownloadJob> downloadingJobs = new List<DownloadJob>();// 正在下载任务列表
            private static bool isDownloading = false;

            // 下载管理线程
            private static void downloaderd()// Downloader Daemon
            {
                if (!downloaderd_lock.WaitOne(3)) return;
                if (downloadingJobs.Count > 0)// 继续下载未完成的任务
                    downloadingJobs.ForEach(i => _ = i.Download());
                while (true)
                {
                    if (!IsDownloading) break;// 停止下载线程
                    if (downloadingJobs.Count < MaxJob || MaxJob == 0)
                    {
                        if (downloadJobs.Count == 0) break;// 下载队列空
                        var job = downloadJobs.Dequeue();// 出队
                        downloadingJobs.Add(job);
                        job.DownloadPause += job_pause;
                        job.DownloadCancel += job_cancel;
                        job.DownloadCompleted += job_completed;
                        _ = job.Download();
                    }
                }
                downloaderd_lock.ReleaseMutex();
                return;
            }
            // 下载完成
            private static void job_completed(DownloadJob job, DownloadCompletedEventArgs args)
            {
                job_cancel(job);
            }
            // 下载取消 注销事件
            private static void job_cancel(DownloadJob job)
            {
                job.DownloadPause -= job_pause;
                job.DownloadCancel -= job_cancel;
                job.DownloadCompleted -= job_completed;
                downloadingJobs.Remove(job);// 从正在下载列表中移除
            }
            // 下载暂停
            private static void job_pause(DownloadJob job)
            {
                job_cancel(job);
                pausedJobs.Add(job);// 添加任务到暂停列表
                job.DownloadResume += job_resume;// 等待继续
            }
            // 下载继续
            private static void job_resume(DownloadJob job)
            {
                pausedJobs.Remove(job);// 从暂停列表中移除任务
                downloadJobs.Enqueue(job);// 排队
                job.DownloadResume -= job_resume;
            }
        }
    }
}
