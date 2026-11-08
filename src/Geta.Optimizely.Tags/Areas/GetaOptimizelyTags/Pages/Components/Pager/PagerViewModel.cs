using System.Web;

namespace Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags.Components.Pager
{
    public class PagerViewModel
    {
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public string QueryString { get; set; }

        public string PageUrl(int page)
        {
            var qs = HttpUtility.ParseQueryString(QueryString);
            qs.Remove("id");
            qs["pageNumber"] = page.ToString();
            return $"?{qs}";
        }
    }
}
