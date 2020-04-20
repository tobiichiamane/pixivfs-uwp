using static PixivFSUWP.Data.OverAll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using PixivCS;
using System.IO;
using Windows.Storage.Provider;
using Windows.UI.Xaml.Controls;
using PixivFSUWP.Data;
using PixivFSUWP.ViewModels;
using System.Collections;

namespace PixivFSUWP
{
    public static class DownloadQueue
    {
        private static Queue<(WaterfallItemViewModel, Frame)> Queue = new Queue<(WaterfallItemViewModel, Frame)>();
        public static Queue<(WaterfallItemViewModel, Frame)> FailQueue = new Queue<(WaterfallItemViewModel, Frame)>();

        public static bool Downloading { get; private set; } = false;
        public static int Count => Queue.Count;

        static async Task Download((WaterfallItemViewModel, Frame) task)
        {
            Frame Frame;
            WaterfallItemViewModel tapped;
            (tapped, Frame) = task;
            // null检查
            if (tapped == null) return;
            if (!(ApplicationData.Current.LocalSettings.Values["PictureSaveName"] is string picName) ||
                string.IsNullOrWhiteSpace(picName))
            {
                picName = "$P_$p";
                ApplicationData.Current.LocalSettings.Values["PictureSaveName"] = picName;
            }
            if (!(ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] is string picDir) ||
              string.IsNullOrWhiteSpace(picDir))
            {
                picDir = Windows.Storage.KnownFolders.SavedPictures.Path;
                ApplicationData.Current.LocalSettings.Values["PictureSaveDirectory"] = picDir;
            }

            Windows.Data.Json.JsonObject res;
            var i = tapped;
            try
            {
                System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]发送请求:{0}", i.Title));
                res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI).IllustDetail(i.ItemId.ToString());
            }
            catch (System.Net.Http.HttpRequestException ex)// 发送请求时发生错误。
            {
                FailQueue.Enqueue(task);
                System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]请求失败:{0}", ex.Message));
                System.Diagnostics.Debug.WriteLine("[DownloadQueue]已添加到失败列表");
                await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                    ShowTip(string.Format(GetResourceString("Error_DownloadFailed").Replace(@"\n", "\n"), i.Title, ex.Message));
                return;
            }
            var illust = Data.IllustDetail.FromJsonValue(res);

            // 如果存在多张图片 一起下载下来
            for (ushort loopnum = 0; loopnum < illust.OriginalUrls.Count; loopnum++)
            {
                var file = await (await StorageFolder.GetFolderFromPathAsync(picDir)).CreateFileAsync(GetPicName(picName, illust, loopnum), CreationCollisionOption.GenerateUniqueName);
                System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]开始下载:{0};{1}", i.Title, file.Name));
                await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("DownloadStart").Replace(@"\n", "\n"), i.Title, file.Name));
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    System.Diagnostics.Debug.WriteLine("[DownloadQueue]From:" + illust.OriginalUrls[loopnum]);
                    System.Diagnostics.Debug.WriteLine("[DownloadQueue]To  :" + file.Path);
                    try
                    {
                        // 是GIF
                        if (illust.Type == "ugoira")
                        {
                            System.Diagnostics.Debug.WriteLine("[DownloadQueue]是一个动图");
                            using (var ugoira = await Data.UgoiraHelper.GetUgoiraAsync(illust.IllustID.ToString()))
                            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                            using (var renderer = new Lumia.Imaging.GifRenderer())
                            {
                                renderer.Duration = ugoira.Frames[0].Delay;
                                renderer.Size = new Windows.Foundation.Size(ugoira.Frames[0].Image.PixelWidth, ugoira.Frames[0].Image.PixelHeight);
                                var sources = new List<Lumia.Imaging.IImageProvider>();
                                foreach (var img in ugoira.Frames)
                                    sources.Add(new Lumia.Imaging.SoftwareBitmapImageSource(img.Image));
                                renderer.Sources = sources;
                                await stream.WriteAsync(await renderer.RenderAsync());
                            }
                        }
                        // 不是Gif
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DownloadQueue]不是一个动图");
                            using (var imgstream = await Data.OverAll.DownloadImage(illust.OriginalUrls[loopnum]))
                            using (var filestream = await file.OpenAsync(FileAccessMode.ReadWrite))
                                await imgstream.CopyToAsync(filestream.AsStream());
                        }
                    }
                    catch (Exception ex)
                    {
                        FailQueue.Enqueue(task);
                        System.Diagnostics.Debug.WriteLine(string.Format("[DownloadQueue]下载失败:{0}", ex.Message));
                        System.Diagnostics.Debug.WriteLine("[DownloadQueue]已添加到失败列表");
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("Error_DownloadFailed").Replace(@"\n", "\n"), i.Title, ex.Message));
                    }
                    var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (updateStatus == FileUpdateStatus.Complete)
                    {
                        System.Diagnostics.Debug.WriteLine("[DownloadQueue]下载完成:" + file.Name);
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("WorkSavedPlain"), i.Title));
                    }
                    else
                    {
                        FailQueue.Enqueue(task);
                        System.Diagnostics.Debug.WriteLine("[DownloadQueue]下载失败 已添加到失败列表");
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("WorkSaveFailedPlain"), i.Title));
                    }
                }
            }

        }
        public static async void Start()
        {
            if (!Downloading)
            {
                Downloading = true;
                while (Downloading && Queue.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DownloadQueue]开始下载任务 当前队列" + Count);
                    await Download(Queue.Dequeue());
                }
                System.Diagnostics.Debug.WriteLine("[DownloadQueue]下载任务完成 当前队列" + Count);
                Downloading = false;
            }
        }
        public static void ReDownload(Frame Frame)
        {
            System.Diagnostics.Debug.WriteLine("[DownloadQueue]重新添加失败任务 当前队列" + Count);
            while (FailQueue.Count>0)
            {
                Queue.Enqueue(FailQueue.Dequeue());
            }
            System.Diagnostics.Debug.WriteLine("[DownloadQueue]重新添加失败任务结束 当前队列" + Count);
        }
        static string GetPicName(string s, IllustDetail illust, ushort p)
        {
            return System.Text.RegularExpressions.Regex.Replace(s
                // PID
                .Replace("$P", illust.IllustID.ToString())
                // 分P数
                .Replace("$p", p.ToString())
                // 标题
                .Replace("$t", illust.Title)
                // 上传日期
                .Replace("$d", illust.CreateDate)
                // 来源URL
                .Replace("$l", illust.OriginalUrls[p])
                // 作者UID
                .Replace("$A", illust.AuthorID.ToString())
                // 作者名
                .Replace("$a", illust.Author),
                 // 文件名合法化 \ / : * ? " < > | 
                 "\\\\|\\/|\\:|\\*|\\?|\\<|\\>|\\||\"", " ") + "." + ((illust.Type == "ugoira") ? "gif" : illust.OriginalUrls[p].Split('.').Last());
        }

        public static void Add(WaterfallItemViewModel tapped, Frame Frame)
        {
            Queue.Enqueue((tapped, Frame));
            System.Diagnostics.Debug.WriteLine("[DownloadQueue]添加到队列 当前队列" + Count);
            Start();
        }

        public static void Clear() => Queue.Clear();


    }
}
