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
            private set
            {
                progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Progress"));
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
        ManualResetEvent pauseEvent = new ManualResetEvent(true);

        //用于取消任务的CancellationTokenSource
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        //下载完成时的事件
        public event Action<DownloadJob, DownloadCompletedEventArgs> DownloadCompleted;

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
                    //await Task.Run(() =>
                    //{
                        Progress = (int)(loaded * 100 / length);
                    //});
                }))
                {
                    if (tokenSource.IsCancellationRequested) return;
                    var file = await StorageFile.GetFileFromPathAsync(FilePath);
                    CachedFileManager.DeferUpdates(file);
                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await memStream.CopyToAsync(fileStream.AsStream());
                    }
                    var result = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (result == Windows.Storage.Provider.FileUpdateStatus.Complete)
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

        //暂停下载
        public void Pause()
        {
            pauseEvent.Reset();
        }

        //恢复下载
        public void Resume()
        {
            pauseEvent.Set();
        }

        //取消下载
        public void Cancel()
        {
            tokenSource.Cancel();
        }
    }

    //静态的下载管理器。应用程序不会有多个下载管理器实例。
    public static class DownloadManager
    {
        //下载任务列表
        public static ObservableCollection<DownloadJob> DownloadJobs = new ObservableCollection<DownloadJob>();

        //添加下载任务
        public static void NewJob(string Title, string Uri, string FilePath)
        {
            var job = new DownloadJob(Title, Uri, FilePath);
            job.DownloadCompleted += Job_DownloadCompleted;
            DownloadJobs.Add(job);
            _ = job.Download();
        }

        //有任务下载完成时的事件
        public static event Action<string, bool> DownloadCompleted;

        //下载完成时
        private static void Job_DownloadCompleted(DownloadJob source, DownloadCompletedEventArgs args)
        {
            DownloadJobs.Remove(source);
            DownloadCompleted?.Invoke(source.Title, args.HasError);
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
    }
}
