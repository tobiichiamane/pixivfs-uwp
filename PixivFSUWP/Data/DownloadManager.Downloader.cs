using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using PixivFSUWP.Data.DownloadJobs;

namespace PixivFSUWP.Data
{
    public static partial class DownloadManager
    {
        // 下载管理
        private static class Downloader
        {
            private static readonly Mutex download_lock = new Mutex();
            private static readonly List<DownloadJob> downloadingJobs = new List<DownloadJob>();// 正在下载任务列表
            private static readonly Queue<DownloadJob> downloadJobs = new Queue<DownloadJob>();// 下载队列
            private static readonly List<DownloadJob> waitingJobs = new List<DownloadJob>();// 暂停任务列表

            private static void AutoRemove(List<DownloadJob> list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].Status == DownloadJobStatus.Finished
                        || list[i].Status == DownloadJobStatus.Failed
                        || list[i].Status == DownloadJobStatus.Cancel)
                        list.RemoveAt(i);
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

            private static void Job_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

            private static void job_resume(DownloadJob job)// 下载继续
            {
                waitingJobs.RemoveJob(job);// 从暂停列表中移除任务
                downloadJobs.Enqueue(job);// 排队
                job.DownloadResume -= job_resume;
            }

            public static int MaxJob { get; set; } = 3;// 同时下载最大进程数 为0不限制

            public static event Action<DownloadJob> JobFinished;

            static Downloader() => _ = Task.Run(Downloaderd);

            public static void Add(DownloadJob job)
            {
                job.PropertyChanged += Job_PropertyChanged;
                if (!waitingJobs.Contains(job)) waitingJobs.Add(job);// 添加任务到等待列表
                job.Resume();
            }
        }
    }
}