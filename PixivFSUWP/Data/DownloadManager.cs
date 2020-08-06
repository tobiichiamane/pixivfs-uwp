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
    /// <summary>
    /// 下载任务状态
    /// </summary>
    public enum DownloadJobStatus
    {
        Created,    // 任务被创建
        Ready,      // 任务就绪
        Running,    // 任务正在进行
        Pause,      // 任务暂停
        Cancel,     // 任务取消
        Finished,   // 任务完成
        Failed      // 任务出现错误
    }
    //          +<=====================================+
    // Created =+=> Ready => Running =+=> Finished    =+
    //                                +=> Failed      =+
    //                                +=> Cancel      =+
    //                                +=> Pause       =+

    /// <summary>
    /// 下载任务
    /// </summary>
    public class DownloadJob : INotifyPropertyChanged
    {
        private int progress;
        private DownloadJobStatus status;

        /// <summary>
        /// 任务标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 任务下载来源
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// 文件地址
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 下载进度
        /// </summary>
        public int Progress
        {
            get => progress;
            protected set
            {
                progress = value;
                _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress))));
            }
        }

        /// <summary>
        /// 下载过程中出现的异常
        /// </summary>
        public Exception Exception { get; protected set; } = null;

        public DownloadJob(string Title, string Uri, string FilePath)
        {
            this.Title = Title;
            this.Uri = Uri;
            this.FilePath = FilePath;
            _ = SetStatus(DownloadJobStatus.Created);
        }

        /// <summary>
        /// 下载任务状态
        /// </summary>
        public DownloadJobStatus Status => status;

        // 设置下载状态的方法
        protected async Task SetStatus(DownloadJobStatus value)
        {
            status = value;
            var task = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.High, () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status))));
            switch (value)
            {
                case DownloadJobStatus.Created:
                    Progress = 0;
                    break;

                case DownloadJobStatus.Ready:
                    break;

                case DownloadJobStatus.Running:
                    pauseEvent.Set();
                    DownloadResume?.Invoke(this);
                    break;

                case DownloadJobStatus.Pause:
                    pauseEvent.Reset();
                    DownloadPause?.Invoke(this);
                    break;

                case DownloadJobStatus.Cancel:
                    tokenSource.Cancel();
                    DownloadCancel?.Invoke(this);
                    break;

                case DownloadJobStatus.Finished:
                    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs { HasError = false });
                    break;

                case DownloadJobStatus.Failed:
                    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs { HasError = true });
                    break;

                default:
                    break;
            }
            await task;
        }

        //用于暂停的ManualResetEvent
        protected ManualResetEvent pauseEvent = new ManualResetEvent(true);

        //用于取消任务的CancellationTokenSource
        protected CancellationTokenSource tokenSource = new CancellationTokenSource();

        //下载完成时的事件
        public event Action<DownloadJob, DownloadCompletedEventArgs> DownloadCompleted;

        public event Action<DownloadJob> DownloadPause;

        public event Action<DownloadJob> DownloadResume;

        public event Action<DownloadJob> DownloadCancel;

        //通知属性更改
        public event PropertyChangedEventHandler PropertyChanged;
        // 这东西最万能了 只要订阅Status属性更改 啥都能干

        //进行下载
        public async Task Download()
        {
            if (status == DownloadJobStatus.Ready)
            {
                await SetStatus(DownloadJobStatus.Running);
                try
                {
                    using (var memStream = await OverAll.DownloadImage(Uri, tokenSource.Token, pauseEvent, async (loaded, length) =>
                    {
                        await Task.Run(() =>
                        {
                            Progress = (int)(loaded * 99 / length);
                        });
                    }))
                    {
                        if (tokenSource.IsCancellationRequested) return;
                        if (await WriteToFile(memStream) == FileUpdateStatus.Complete)
                        {
                            await SetStatus(DownloadJobStatus.Finished);
                        }
                        else
                        {
                            await SetStatus(DownloadJobStatus.Failed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.Exception = ex;
                    await SetStatus(DownloadJobStatus.Failed);
                }
                finally
                {
                    Progress = 100;
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

        //暂停下载
        public async void Pause() => await SetStatus(DownloadJobStatus.Pause);

        //恢复下载
        public async void Resume() => await SetStatus(DownloadJobStatus.Ready);

        //取消下载
        public async void Cancel() => await SetStatus(DownloadJobStatus.Cancel);

        // 重置状态
        public async void Reset() => await SetStatus(DownloadJobStatus.Created);
    }

    // 直接传StorageFile对象的...可以避免用FileSavePicker选中的文件传过来没有权限的问题
    // 对FileSavePicker选择的文件直接修改不需要对应的文件系统访问权限
    public class DownloadJobPlus : DownloadJob
    {
        private readonly StorageFile File;

        public DownloadJobPlus(string Title, string Uri, StorageFile File) : base(Title, Uri, File.Path) => this.File = File;

        protected override async Task<FileUpdateStatus> WriteToFile(Stream memStream)
        {
            CachedFileManager.DeferUpdates(File);
            using (var fileStream = await File.OpenAsync(FileAccessMode.ReadWrite))
                await memStream.CopyToAsync(fileStream.AsStream());
            return await CachedFileManager.CompleteUpdatesAsync(File);
        }
    }

    public class FinishedJob : DownloadJob
    {
        public FinishedJob(string Title, string Uri, string FilePath, DownloadJobStatus status = DownloadJobStatus.Finished) : base(Title, Uri, FilePath) => _ = SetStatus(status);
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
        }

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath) => AddJob(new DownloadJob(Title, Uri, FilePath));

        //添加下载任务
        public static void NewJob(string Title, string Uri, StorageFile File) => AddJob(new DownloadJobPlus(Title, Uri, File));

        // 重置任务状态
        public static async Task ResetJob(DownloadJob job)
        {
            await RemoveFinishedJob(job);
            job.Reset();
            AddJob(job);
        }

        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            Job_DownloadCancel(source);
            DownloadCompleted?.Invoke(source.Title, args.HasError);
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
        public static async Task RemoveFinishedJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.DownloadCancel -= Job_DownloadCancel;
            Job.Cancel();
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
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
                    new List<string>() {"."+
                            (illust.Type.Equals("ugoira", StringComparison.OrdinalIgnoreCase)
                                ? "gif"
                                : illust.OriginalUrls[p].Split('.').Last())
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

        private static void RemoveJob(this List<DownloadJob> list, DownloadJob job)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].FilePath.Equals(job.FilePath)) list.RemoveAt(i);
        }
        // 下载管理
        private static class Downloader
        {
            public static int MaxJob { get; set; } = 3;// 同时下载最大进程数 为0不限制

            public static void Add(DownloadJob job)
            {
                job.PropertyChanged += Job_PropertyChanged;
                if (!waitingJobs.Contains(job)) waitingJobs.Add(job);// 添加任务到等待列表
                job.Resume();
            }

            private static Queue<DownloadJob> downloadJobs = new Queue<DownloadJob>();// 下载队列
            private static List<DownloadJob> waitingJobs = new List<DownloadJob>();// 暂停任务列表
            private static List<DownloadJob> downloadingJobs = new List<DownloadJob>();// 正在下载任务列表
            private static readonly Mutex download_lock = new Mutex();
            static Downloader()
            {
                _ = Task.Run(Downloaderd);
            }
            // 下载管理线程
            private static void Downloaderd() // Downloader Daemon
            {
                while (true)
                {
                    //System.Diagnostics.Debug.WriteLine("下载管理线程:运行中");
                    if (!download_lock.WaitOne(3)) continue;
                    try
                    {
                        if (downloadingJobs.Count < MaxJob || MaxJob == 0)// 同时进行的任务数限制
                        {
                            if (downloadJobs.Count > 0)// 判断队列是否为空
                            {
                                var job = downloadJobs.Dequeue();
                                System.Diagnostics.Debug.WriteLine("下载管理线程:出队 " + job.Title);
                                _ = job.Download();
                            }
                        }
                    }
                    finally
                    {
                        download_lock.ReleaseMutex();
                        //Thread.Sleep(500);
                    }
                }
            }

            public static void Job_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (sender is DownloadJob job && e.PropertyName.Equals(nameof(job.Status)))
                {
                    System.Diagnostics.Debug.WriteLine("下载任务状态改变:" + "[" + job.Status + "]" + job.Title);
                    switch (job.Status)
                    {
                        case DownloadJobStatus.Created:// 是不会被用到的case
                            waitingJobs.Add(job);// 添加任务到等待列表
                            break;
                        case DownloadJobStatus.Ready:// Job就绪 等待运行
                            waitingJobs.RemoveJob(job);// 从等待列表移除
                            downloadJobs.Enqueue(job);// 任务排队
                            break;
                        case DownloadJobStatus.Running:// 开始下载
                            downloadingJobs.Add(job); // 任务开始
                            break;
                        case DownloadJobStatus.Pause:// 下载暂停
                            downloadingJobs.RemoveJob(job);// 从正在下载列表中移除
                            waitingJobs.Add(job);// 添加任务到暂停列表
                            job.DownloadResume += job_resume;// 等待继续
                            break;
                        case DownloadJobStatus.Cancel:// 下载取消 
                        case DownloadJobStatus.Failed:// 下载失败
                        case DownloadJobStatus.Finished:// 下载完成
                            downloadingJobs.RemoveJob(job);// 从正在下载列表中移除
                            //AutoRemove(downloadingJobs);// 自动从列表中移除已完成任务
                            job.PropertyChanged -= Job_PropertyChanged;// 取消订阅
                            JobFinished?.Invoke(job);
                            break;
                        default:
                            break;
                    }
                }
            }
            private static void AutoRemove(List<DownloadJob> list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].Status == DownloadJobStatus.Finished
                        || list[i].Status == DownloadJobStatus.Failed
                        || list[i].Status == DownloadJobStatus.Cancel)
                        list.RemoveAt(i);
            }
            // 下载继续
            private static void job_resume(DownloadJob job)
            {
                waitingJobs.RemoveJob(job);// 从暂停列表中移除任务
                downloadJobs.Enqueue(job);// 排队
                job.DownloadResume -= job_resume;
            }
            public static event Action<DownloadJob> JobFinished;
        }
    }
}