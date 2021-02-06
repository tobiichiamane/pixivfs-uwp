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
using Windows.UI.Core;

namespace PixivFSUWP.Data
{
    //下载完成时的事件参数
    public class DownloadCompletedEventArgs : EventArgs
    {
        public DownloadCompletedEventArgs()
        {
        }

        public DownloadCompletedEventArgs(bool hasError) => HasError = hasError;

        public bool HasError { get; set; }
    }
    public class DownloadJobStatusChangedEventArgs : EventArgs
    {
        public DownloadJobStatusChangedEventArgs(DownloadJobStatus status) => Status = status;
        public DownloadJobStatus Status { get; }
    }
    public enum DownloadJobStatus
    {
        Readying,
        Running,
        Pausing,
        Canceled,
        Finished,
        Faild,
    }
    public class DownloadJob : INotifyPropertyChanged
    {
        public string Title { get; }
        public string Uri { get; }
        public string FilePath { get; }
        public bool IsPause
        {
            get => isPause; set
            {
                isPause = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPause)));
            }
        }
        public DownloadJobStatus Status
        {
            get => status; protected set
            {
                if (status == value)
                    return;
                status = value;
                StatusChanged?.Invoke(this, new DownloadJobStatusChangedEventArgs(status));
            }
        }
        private int progress;
        private DownloadJobStatus status = DownloadJobStatus.Readying;
        private bool isPause = false;

        public int Progress
        {
            get => progress;
            private set
            {
                progress = value;
                _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.High,
                    () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress))));
            }
        }

        public DownloadJob(string Title, string Uri, string FilePath)
        {
            this.Title = Title;
            this.Uri = Uri;
            this.FilePath = FilePath;
            Progress = 0;
            Downloading = false;
        }

        //下载状态
        public bool Downloading { get; private set; }

        //用于暂停的ManualResetEvent
        readonly ManualResetEvent pauseEvent = new ManualResetEvent(true);

        //用于取消任务的CancellationTokenSource
        readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        //下载完成时的事件
        public event Action<DownloadJob, DownloadCompletedEventArgs> DownloadCompleted;

        public event Action<DownloadJob, DownloadJobStatusChangedEventArgs> StatusChanged;

        //通知属性更改
        public event PropertyChangedEventHandler PropertyChanged;

        //进行下载
        public async Task Download()
        {
            if (!Downloading)
            {
                Downloading = true;
                using (var memStream = await OverAll.DownloadImage(Uri, tokenSource.Token, pauseEvent, async (loaded, length) =>
                    {
                        await Task.Run(() =>
                        {
                            Status = DownloadJobStatus.Running;
                            Progress = (int)(loaded * 100 / length);
                        });
                    }))
                {
                    if (tokenSource.IsCancellationRequested)
                        return;

                    var result = await WriteToFile(memStream);
                    Status = DownloadJobStatus.Finished;
                    _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                        () => DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(result != Windows.Storage.Provider.FileUpdateStatus.Complete)));

                }
                Downloading = false;
            }
        }
        // 文件的写入方法
        protected virtual async Task<Windows.Storage.Provider.FileUpdateStatus> WriteToFile(Stream memStream)
        {
            var file = await StorageFile.GetFileFromPathAsync(FilePath);
            CachedFileManager.DeferUpdates(file);
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                await memStream.CopyToAsync(fileStream.AsStream());
            return await CachedFileManager.CompleteUpdatesAsync(file);
        }

        //暂停下载
        public void Pause()
        {
            IsPause = true;
            pauseEvent.Reset();
            Status = DownloadJobStatus.Pausing;
        }

        //恢复下载
        public void Resume()
        {
            IsPause = false;
            pauseEvent.Set();
            Status = DownloadJobStatus.Readying;
        }

        //取消下载
        public void Cancel()
        {
            tokenSource.Cancel();
            Status = DownloadJobStatus.Canceled;
        }
    }
    public class DownloadJobPlus : DownloadJob
    {
        protected readonly StorageFile File;

        public DownloadJobPlus(string Title, string Uri, StorageFile File) : base(Title, Uri, File.Path) => this.File = File;

        protected override async Task<Windows.Storage.Provider.FileUpdateStatus> WriteToFile(Stream memStream)
        {
            CachedFileManager.DeferUpdates(File);
            using (var fileStream = await File.OpenAsync(FileAccessMode.ReadWrite))
                await memStream.CopyToAsync(fileStream.AsStream());
            return await CachedFileManager.CompleteUpdatesAsync(File);
        }
    }
    //静态的下载管理器。应用程序不会有多个下载管理器实例。
    public static class DownloadManager
    {
        //下载任务列表
        public static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();
        public static ObservableCollection<DownloadJob> FinishedJobs = new ObservableCollection<DownloadJob>();

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath)
            => JobBackgroundWork(new DownloadJob(Title, Uri, FilePath));
        public static void NewJob(string Title, string Uri, StorageFile File)
            => JobBackgroundWork(new DownloadJobPlus(Title, Uri, File));

        private static void JobBackgroundWork(DownloadJob job)
        {
            job.DownloadCompleted += Job_DownloadCompleted;
            DownloadJobs.Add(job);
            DownloadProcessManager.Enqueue(job);
        }

        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("[Job_DownloadCompleted]: " + source.Title);
            source.DownloadCompleted -= Job_DownloadCompleted;
            DownloadCompleted?.Invoke(source.Title, args.HasError);
            FinishedJobs.Add(source);
            DownloadJobs.Remove(source);
        }

        //移除下载任务
        public static void RemoveJob(int Index)
        {
            var job = DownloadJobs[Index];
            job.DownloadCompleted -= Job_DownloadCompleted;
            job.Cancel();
            DownloadJobs.Remove(job);
        }

        //移除下载任务
        public static void RemoveJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.Cancel();
            DownloadJobs.Remove(Job);
        }

        private static class DownloadProcessManager
        {
            private static readonly Mutex download_lock = new Mutex();// 同步锁
            private static readonly List<DownloadJob> downloadingJobs = new List<DownloadJob>();// 正在下载任务列表
            private static readonly Queue<DownloadJob> downloadJobs = new Queue<DownloadJob>();// 下载队列
            private static readonly List<DownloadJob> waitingJobs = new List<DownloadJob>();// 暂停任务列表
            private static Thread DownloadProcessThread = null;

            /// <summary>
            /// 同时下载最大进程数
            /// </summary>
            public static int MaxJobs { get; set; } = 3;

            static DownloadProcessManager() => new Thread(DownloadProcessDaemon).Start();

            public static void Enqueue(DownloadJob job)
            {
                job.StatusChanged += Job_StatusChanged;
                downloadJobs.Enqueue(job);
            }

            private static void DownloadProcess()
            {
                System.Diagnostics.Debug.WriteLine("[DownloadProcess]: 启动");
                if (!download_lock.WaitOne(200))
                {
                    System.Diagnostics.Debug.WriteLine("[DownloadProcess]: 程序正在运行");
                    return;
                }
                try
                {
                    System.Diagnostics.Debug.WriteLine("[DownloadProcess]: 运行中");
                    while (true)
                    {
                        lock (downloadingJobs)
                        {
                            if (downloadingJobs.Count < MaxJobs)
                            {
                                if (downloadJobs.Count > 0)
                                {
                                    var job = downloadJobs.Dequeue();
                                    downloadingJobs.Add(job);
                                    System.Diagnostics.Debug.WriteLine("[DownloadProcess]: 开始下载: " + job.Title);
                                    _ = job.Download();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    System.Diagnostics.Debug.WriteLine("[DownloadProcess]: 意外结束");
                    download_lock.ReleaseMutex();
                }
            }

            private static void Job_StatusChanged(DownloadJob job, DownloadJobStatusChangedEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine($"[Job_StatusChanged]: {job.Title} = {e.Status}");
                switch (e.Status)
                {
                    case DownloadJobStatus.Readying:
                        waitingJobs.Remove(job);
                        downloadJobs.Enqueue(job);
                        break;
                    case DownloadJobStatus.Running:
                        break;
                    case DownloadJobStatus.Pausing:
                        lock (downloadingJobs)
                            downloadingJobs.Remove(job);
                        waitingJobs.Add(job);
                        break;
                    case DownloadJobStatus.Canceled:
                    case DownloadJobStatus.Finished:
                        job.StatusChanged -= Job_StatusChanged;
                        lock (downloadingJobs)
                            downloadingJobs.Remove(job);
                        break;
                    default:
                        break;
                }
            }

            private static void DownloadProcessDaemon()
            {
                System.Diagnostics.Debug.WriteLine("[DownloadProcessDaemon]: 启动");
                DownloadProcessThread = new Thread(DownloadProcess);
                while (true)
                {
                    if (!DownloadProcessThread.IsAlive)
                        (DownloadProcessThread = new Thread(DownloadProcess)).Start();
                    else
                        Thread.Sleep(2000);
                }
            }
        }
    }
}
