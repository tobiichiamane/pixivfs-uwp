using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using System.Web;
using Windows.Data.Json;

namespace PixivFSUWP.Data.Collections
{
    public class BookmarkIllustsCollection : IllustsCollectionBase<ViewModels.WaterfallItemViewModel>
    {
        readonly string userID;

        public BookmarkIllustsCollection(string UserID)
        {
            userID = UserID;
        }

        public BookmarkIllustsCollection() : this(OverAll.GlobalBaseAPI.UserID) { }

        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                if (!HasMoreItems) return new LoadMoreItemsResult() { Count = 0 };
                LoadMoreItemsResult toret = new LoadMoreItemsResult() { Count = 0 };
                PixivCS.Objects.UserIllusts bookmarkres = null;
                try
                {
                    if (nexturl == "begin")
                        bookmarkres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetUserBookmarksIllustAsync(userID);
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        bookmarkres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetUserBookmarksIllustAsync(userID, getparam("restrict"),
                            getparam("filter"), getparam("max_bookmark_id"));
                    }
                }
                catch
                {
                    return toret;
                }
                nexturl = bookmarkres.NextUrl?.ToString() ?? "";
                foreach (var recillust in bookmarkres.Illusts)
                {
                    await Task.Run(() => pause.WaitOne());
                    if (_emergencyStop)
                    {
                        nexturl = "";
                        Clear();
                        return new LoadMoreItemsResult() { Count = 0 };
                    }
                    WaterfallItem recommendi = WaterfallItem.FromObject(recillust);
                    var recommendmodel = ViewModels.WaterfallItemViewModel.FromItem(recommendi);
                    await recommendmodel.LoadImageAsync();
                    Add(recommendmodel);
                    toret.Count++;
                }
                return toret;
            }
            finally
            {
                _busy = false;
                if (_emergencyStop)
                {
                    nexturl = "";
                    Clear();
                    GC.Collect();
                }
            }
        }
    }
}

