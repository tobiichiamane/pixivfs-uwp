using PixivCS;
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
using System.Diagnostics;

namespace PixivFSUWP.Data.Collections
{
    public class RecommendIllustsCollection : IllustsCollectionBase<ViewModels.WaterfallItemViewModel>
    {
        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                LoadMoreItemsResult toret = new LoadMoreItemsResult{ Count = 0 };
                if (!HasMoreItems) return toret;
                PixivCS.Objects.IllustRecommended recommendres = null;
                try
                {
                    Trace.WriteLine(nexturl);
                    if (nexturl == "begin")
                        recommendres = await new PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRecommendedAsync();
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        recommendres = await new PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustRecommendedAsync(ContentType:
                                getparam("content_type"),
                                IncludeRankingLabel: bool.Parse(getparam("include_ranking_label")),
                                Filter: getparam("filter"),
                                MinBookmarkIDForRecentIllust: getparam("min_bookmark_id_for_recent_illust"),
                                MaxBookmarkIDForRecommended: getparam("max_bookmark_id_for_recommend"),
                                Offset: getparam("offset"),
                                IncludeRankingIllusts: bool.Parse(getparam("include_ranking_illusts")),
                                IncludePrivacyPolicy: getparam("include_privacy_policy"));
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex);
                    return toret;
                }
                nexturl = recommendres.NextUrl?.ToString() ?? "";
                Trace.WriteLine(nexturl);
                foreach (var recillust in recommendres.Illusts)
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

