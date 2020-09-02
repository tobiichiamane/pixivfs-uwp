using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixivFSUWP.Data.Collections
{
    public static class IllustsCollectionManager
    {
        public static RecommendIllustsCollection RecommendList { get; private set; }
        public static BookmarkIllustsCollection BookmarkList { get; private set; }
        public static FollowingIllustsCollection FollowingList { get; private set; }
        public static RankingIllustsCollection RankingList { get; private set; }
        //private static SearchResultIllustsCollection SearchResultList { get; set; }

        public static Stack<SearchResultIllustsCollection> SearchResults { get; } = new Stack<SearchResultIllustsCollection>();

        //public static IllustsCollectionBase<ViewModels.WaterfallItemViewModel> Instence;

        public static void RefreshRecommendList()
        {
            RecommendList?.StopLoading();
            RecommendList = new RecommendIllustsCollection();
        }

        public static void RefreshBookmarkList()
        {
            BookmarkList?.StopLoading();
            BookmarkList = new BookmarkIllustsCollection();
        }

        public static void RefreshFollowingList()
        {
            FollowingList?.StopLoading();
            FollowingList = new FollowingIllustsCollection();
        }

        public static void RefreshRankingList()
        {
            RankingList?.StopLoading();
            RankingList = new RankingIllustsCollection();
        }
        public static void RefreshSearchResultList(OverAll.SearchParam param)
        {
            if (SearchResults.Count > 0) SearchResults.Peek().PauseLoading();
            SearchResults.Push(new SearchResultIllustsCollection(param.Word, param.SearchTarget, param.Sort, param.Duration));
        }
    }
}
