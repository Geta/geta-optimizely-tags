using Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags.Components.Pager;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class PagerViewModelTests
{
    [Fact]
    public void PageUrl_keeps_the_search_query_and_sets_the_page_number()
    {
        // Issue #17: paging and searching state must survive navigation.
        var pager = new PagerViewModel { QueryString = "?q=care&pageNumber=1" };

        Assert.Equal("?q=care&pageNumber=2", pager.PageUrl(2));
    }

    [Fact]
    public void PageUrl_drops_the_edited_tag_id()
    {
        var pager = new PagerViewModel { QueryString = "?pageNumber=2&id=12%3Aabc" };

        Assert.Equal("?pageNumber=3", pager.PageUrl(3));
    }

    [Fact]
    public void PageUrl_works_without_a_query_string()
    {
        var pager = new PagerViewModel { QueryString = string.Empty };

        Assert.Equal("?pageNumber=1", pager.PageUrl(1));
    }
}
