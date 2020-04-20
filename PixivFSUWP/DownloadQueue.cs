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
    public class DownloadQueue
    {
        public static DownloadQueue Queue = new DownloadQueue();

        private Queue<(WaterfallItemViewModel, Frame)> list = new Queue<(WaterfallItemViewModel, Frame)>();

        public bool Downloading { get; private set; } = false;
        public int Count => list.Count;

        public bool IsReadOnly => false;

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
                res = await new PixivAppAPI(Data.OverAll.GlobalBaseAPI).IllustDetail(i.ItemId.ToString());
            }
            catch (System.Net.Http.HttpRequestException ex)// 发送请求时发生错误。
            {
                System.Diagnostics.Debug.WriteLine(string.Format(GetResourceString("Error_DownloadFailed"), i.Title, ex.Message));
                await ((Frame.Parent as Grid)?.Parent as MainPage)?.
                    ShowTip(string.Format(GetResourceString("Error_DownloadFailed").Replace(@"\n", "\n"), i.Title, ex.Message));
                return;
            }
            var illust = Data.IllustDetail.FromJsonValue(res);

            // 如果存在多张图片 一起下载下来
            for (ushort loopnum = 0; loopnum < illust.OriginalUrls.Count; loopnum++)
            {
                var file = await (await StorageFolder.GetFolderFromPathAsync(picDir)).CreateFileAsync(GetPicName(picName, illust, loopnum), CreationCollisionOption.GenerateUniqueName);
                System.Diagnostics.Debug.WriteLine(string.Format(GetResourceString("DownloadStart").Replace(@"\n", "\n"), i.Title, file.Name));
                await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("DownloadStart").Replace(@"\n", "\n"), i.Title, file.Name));
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    System.Diagnostics.Debug.WriteLine("Download From = " + illust.OriginalUrls[loopnum]);
                    System.Diagnostics.Debug.WriteLine("Download To = " + file.Path);
                    try
                    {
                        // 是GIF
                        if (illust.Type == "ugoira")
                        {
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
                            using (var imgstream = await Data.OverAll.DownloadImage(illust.OriginalUrls[loopnum]))
                            using (var filestream = await file.OpenAsync(FileAccessMode.ReadWrite))
                                await imgstream.CopyToAsync(filestream.AsStream());
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(string.Format(GetResourceString("Error_DownloadFailed").Replace(@"\n", "\n"), i.Title, ex.Message));
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("Error_DownloadFailed").Replace(@"\n", "\n"), i.Title, ex.Message));
                    }
                    var updateStatus = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (updateStatus == FileUpdateStatus.Complete)
                    {
                        System.Diagnostics.Debug.WriteLine("Download Complete = " + file.Name);
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("WorkSavedPlain"), i.Title));
                    }
                    else
                        await ((Frame.Parent as Grid)?.Parent as MainPage)?.ShowTip(string.Format(GetResourceString("WorkSaveFailedPlain"), i.Title));
                }
            }

        }
        public async void Start()
        {
            if (!Downloading)
            {
                Downloading = true;
                while (Downloading && list.Count > 0)
                {
                    await Download(list.Dequeue());
                }
                Downloading = false;
            }
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

        public void Add(WaterfallItemViewModel tapped, Frame Frame)
        {
            list.Enqueue((tapped, Frame));
            Start();
        }

        public void Clear() => list.Clear();


    }
}
