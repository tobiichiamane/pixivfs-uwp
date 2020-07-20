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
        protected DownloadJob()
        {
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
        public bool Downloading { get; protected set; }

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

        //进行下载
        public virtual async Task Download()
        {
            if (!Downloading)
            {
                Downloading = true;
                using (var memStream = await OverAll.DownloadImage(Uri, tokenSource.Token, pauseEvent, async (loaded, length) =>
                {
                    await Task.Run(() =>
                    {
                        Progress = (int)(loaded * 100 / length);
                    });
                }))
                {
                    if (tokenSource.IsCancellationRequested) return;
                    Check(await WriteToFile(memStream));
                }
            }
        }

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
        protected void Check(FileUpdateStatus result)
        {
            if (result == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs() { HasError = false });
            }
            else
            {
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs() { HasError = true });
            }
        }
        //暂停下载
        public void Pause()
        {
            pauseEvent.Reset();
            DownloadPause?.Invoke(this);
        }

        //恢复下载
        public void Resume()
        {
            pauseEvent.Set();
            DownloadResume?.Invoke(this);
        }

        //取消下载
        public void Cancel()
        {
            tokenSource.Cancel();
            DownloadCancel?.Invoke(this);
        }
    }

    // 直接传StorageFile对象的...可以避免用FileSavePicker选中的文件传过来没有权限的问题
    // 对FileSavePicker选择的文件直接修改不需要应用访问权限
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

    //静态的下载管理器。应用程序不会有多个下载管理器实例。
    public static class DownloadManager
    {
        private static bool downloaderd_running = false;
        private static void downloaderd()
        {
            if (downloaderd_running) return;
            while (true)
            {
                if (downloadingJobs.Count < MaxJob)
                {
                    if (downloadJobs.Count == 0) break;
                    var job = downloadJobs.Dequeue();
                    downloadingJobs.Add(job);
                    job.DownloadPause += job_pause;
                    job.DownloadCancel += job_cancel;
                    job.DownloadCompleted += job_completed;
                    _ = job.Download();

                }
                if (StopDownloader)
                {
                    downloadingJobs.ForEach(i => i.Pause());
                    break;
                }
            }
            downloaderd_running = false;
            return;
            // 下载完成
            void job_completed(DownloadJob job, DownloadCompletedEventArgs args)
            {
                job_cancel(job);
            }
            // 下载取消 注销事件
            void job_cancel(DownloadJob job)
            {
                job.DownloadPause -= job_pause;
                job.DownloadCancel -= job_cancel;
                job.DownloadCompleted -= job_completed;
                downloadingJobs.Remove(job);
            }
            // 下载暂停
            void job_pause(DownloadJob job)
            {
                job_cancel(job);
                pausedJobs.Add(job);
                job.DownloadResume += job_resume;// 等待继续
            }
            // 下载继续
            void job_resume(DownloadJob job)
            {
                pausedJobs.Remove(job);
                downloadJobs.Enqueue(job);
                job.DownloadResume -= job_resume;
            }
        }

        public static void systemctl_start_downloaderd()// 
        {
            StopDownloader = false;
            lock (downloadJobs)
                _ = Task.Run(downloaderd);
        }
        public static void systemctl_stop_downloaderd()
        {
            StopDownloader = true;
        }

        public static int MaxJob = 3;
        public static bool StopDownloader = false;

        private static Queue<DownloadJob> downloadJobs = new Queue<DownloadJob>();
        private static List<DownloadJob> pausedJobs = new List<DownloadJob>();
        private static List<DownloadJob> downloadingJobs = new List<DownloadJob>();

        //下载任务列表
        public static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();

        //完成任务列表
        public static ObservableCollection<DownloadJob> FinishedJobs = new ObservableCollection<DownloadJob>();

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath)
        {
            var job = new DownloadJob(Title, Uri, FilePath);
            job.DownloadCompleted += Job_DownloadCompleted;
            job.DownloadCancel += Job_DownloadCancel;
            downloadJobs.Enqueue(job);
            DownloadJobs.Add(job);
            systemctl_start_downloaderd();
            //_ = job.Download();
        }


        public static void NewJob(string Title, string Uri, StorageFile File)
        {
            var job = new DownloadJobPlus(Title, Uri, File);
            job.DownloadCompleted += Job_DownloadCompleted;
            job.DownloadCancel += Job_DownloadCancel;
            downloadJobs.Enqueue(job);
            DownloadJobs.Add(job);
            systemctl_start_downloaderd();
        }

        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            RemoveJob(source);
            FinishedJobs.Add(source);
            DownloadCompleted?.Invoke(source.Title, args.HasError);
        }

        private static void Job_DownloadCancel(DownloadJob job)
        {
            RemoveJob(job);
            FinishedJobs.Add(job);
        }
        //移除下载任务
        public static void RemoveJob(int Index) => RemoveJob(DownloadJobs[Index]);

        //移除下载任务
        public static void RemoveJob(DownloadJob Job)
        {
            Job.DownloadCompleted -= Job_DownloadCompleted;
            Job.Cancel();
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => DownloadJobs.Remove(Job));
        }
        // 获取文件对象
        public static async Task<StorageFile> GetPicFile(IllustDetail illust, ushort p)
        {
            var LocalSettings = ApplicationData.Current.LocalSettings.Values;
            if (LocalSettings["PictureAutoSave"] is bool b && b)
            {
                var fileName = GetPicName(LocalSettings["PictureSaveName"] as string, illust, p);
                var folder = await StorageFolder.GetFolderFromPathAsync(LocalSettings["PictureSaveDirectory"] as string);
                return await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }
            else
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
        public static string GetPicName(string template, IllustDetail illust, ushort p)
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
            }
            sb.Append('.');
            if (illust.Type.Equals("ugoira", StringComparison.OrdinalIgnoreCase))
                sb.Append("gif");
            else sb.Append(illust.OriginalUrls[p].Split('.').Last());

            return sb.ToString();
        }
    }
}
