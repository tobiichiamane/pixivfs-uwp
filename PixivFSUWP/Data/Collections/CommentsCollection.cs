using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace PixivFSUWP.Data.Collections
{
    public class CommentsCollection : IllustsCollectionBase<ViewModels.CommentViewModel>
    {
        readonly string illustid;
        List<ViewModels.CommentViewModel> ChildrenComments = new List<ViewModels.CommentViewModel>();
        public CommentAvatarLoader AvatarLoader;

        public CommentsCollection(string IllustID)
        {
            illustid = IllustID;
            AvatarLoader = new CommentAvatarLoader(this);
        }


        protected override async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
        {
            try
            {
                if (!HasMoreItems) return new LoadMoreItemsResult() { Count = 0 };
                LoadMoreItemsResult toret = new LoadMoreItemsResult() { Count = 0 };
                PixivCS.Objects.IllustComments commentres = null;
                try
                {
                    if (nexturl == "begin")
                        commentres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustCommentsAsync(illustid, IncludeTotalComments: true);
                    else
                    {
                        Uri next = new Uri(nexturl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        commentres = await new PixivCS
                            .PixivAppAPI(OverAll.GlobalBaseAPI)
                            .GetIllustCommentsAsync(illustid, getparam("offset"), bool.Parse(getparam("include_total_comments")));
                    }
                }
                catch
                {
                    return toret;
                }
                nexturl = commentres.NextUrl?.ToString() ?? "";
                foreach (var recillust in commentres.Comments)
                {
                    await Task.Run(() => pause.WaitOne());
                    if (_emergencyStop)
                    {
                        nexturl = "";
                        Clear();
                        return new LoadMoreItemsResult() { Count = 0 };
                    }
                    IllustCommentItem recommendi = IllustCommentItem.FromObject(recillust);
                    var recommendmodel = ViewModels.CommentViewModel.FromItem(recommendi);
                    //查找是否存在子回复
                    var children = from item
                                   in ChildrenComments
                                   where item.ParentID == recommendmodel.ID
                                   select item;
                    children = children.ToList();
                    if (children.Count() > 0)
                    {
                        //存在子回复
                        recommendmodel.ChildrenComments = new ObservableCollection<ViewModels.CommentViewModel>();
                        foreach (var child in children)
                        {
                            if (child.ChildrenComments != null)
                            {
                                foreach (var childschild in child.ChildrenComments)
                                {
                                    childschild.Comment = string.Format("Re: {0}: {1}",
                                        child.UserName, childschild.Comment);
                                    recommendmodel.ChildrenComments.Add(childschild);
                                }
                                child.ChildrenComments.Clear();
                                child.ChildrenComments = null;
                                GC.Collect();
                            }
                            recommendmodel.ChildrenComments.Insert(0, child);
                            ChildrenComments.Remove(child);
                        }
                    }
                    //检查自己是不是子回复
                    if (recommendmodel.ParentID != -1)
                    {
                        //自己也是子回复
                        ChildrenComments.Add(recommendmodel);
                    }
                    else
                    {
                        //自己并非子回复
                        Add(recommendmodel);
                        toret.Count++;
                    }
                }
                return toret;
            }
            finally
            {
                _busy = false;
                _ = AvatarLoader.LoadAvatars();
                if (_emergencyStop)
                {
                    AvatarLoader.EmergencyStop();
                    nexturl = "";
                    Clear();
                    GC.Collect();
                }
            }
        }
    }
}
