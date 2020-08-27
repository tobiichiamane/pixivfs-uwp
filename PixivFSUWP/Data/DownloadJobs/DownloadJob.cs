using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Provider;
using Windows.UI.Core;

namespace PixivFSUWP.Data.DownloadJobs
{
    /// <summary>
    /// 下载任务
    /// </summary>
    [Serializable]
    public class DownloadJob : INotifyPropertyChanged
    {
        private int progress;

        //用于暂停的ManualResetEvent
        protected ManualResetEvent pauseEvent = new ManualResetEvent(true);

        //用于取消任务的CancellationTokenSource
        protected CancellationTokenSource tokenSource = new CancellationTokenSource();

        /// <summary>
        /// 下载过程中出现的异常
        /// </summary>
        public Exception Exception { get; protected set; } = null;

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
        /// 下载任务状态
        /// </summary>
        public DownloadJobStatus Status { get; private set; }

        /// <summary>
        /// 任务标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 任务下载来源
        /// </summary>
        public string Uri { get; }
        public event Action<DownloadJob> DownloadCancel;

        //下载完成时的事件
        public event Action<DownloadJob, DownloadCompletedEventArgs> DownloadCompleted;

        public event Action<DownloadJob> DownloadPause;

        public event Action<DownloadJob> DownloadResume;

        //通知属性更改
        public event PropertyChangedEventHandler PropertyChanged;

        public DownloadJob(string Title, string Uri, string FilePath)
        {
            this.Title = Title;
            this.Uri = Uri;
            this.FilePath = FilePath;
            _ = SetStatus(DownloadJobStatus.Created);
        }
        // 设置下载状态的方法
        protected async Task SetStatus(DownloadJobStatus value)
        {
            Status = value;
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
        // 这东西最万能了 只要订阅Status属性更改 啥都能干

        // 文件的写入方法
        protected virtual async Task<FileUpdateStatus> WriteToFile(Stream memStream)
        {
            var file = await StorageFile.GetFileFromPathAsync(FilePath);
            CachedFileManager.DeferUpdates(file);
            using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                await memStream.CopyToAsync(fileStream.AsStream());
            return await CachedFileManager.CompleteUpdatesAsync(file);
        }

        /// <summary>
        /// 取消下载
        /// </summary>
        public async void Cancel() => await SetStatus(DownloadJobStatus.Cancel);

        //进行下载
        public async Task Download()
        {
            if (Status == DownloadJobStatus.Ready)
            {
                await SetStatus(DownloadJobStatus.Running);
                try
                {
                    using (var memStream =
                        await OverAll.DownloadImage(Uri, tokenSource.Token, pauseEvent, async (loaded, length)
                        => await Task.Run(() => Progress = (int)(loaded * 99 / length))))
                    {
                        if (tokenSource.IsCancellationRequested) return;
                        if (await WriteToFile(memStream) == FileUpdateStatus.Complete)
                            await SetStatus(DownloadJobStatus.Finished);
                        else
                            await SetStatus(DownloadJobStatus.Failed);
                    }
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    await SetStatus(DownloadJobStatus.Failed);
                }
                finally
                {
                    Progress = 100;
                }
            }
        }
        /// <summary>
        /// 暂停下载
        /// </summary>
        public async void Pause() => await SetStatus(DownloadJobStatus.Pause);

        /// <summary>
        /// 重置状态
        /// </summary>
        public async void Reset() => await SetStatus(DownloadJobStatus.Created);

        /// <summary>
        /// 恢复下载
        /// </summary>
        public async void Resume() => await SetStatus(DownloadJobStatus.Ready);
    }
}