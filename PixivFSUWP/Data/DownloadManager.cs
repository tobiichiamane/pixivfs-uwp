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
