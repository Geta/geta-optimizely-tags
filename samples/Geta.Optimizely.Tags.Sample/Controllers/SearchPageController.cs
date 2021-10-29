using System.Linq;
using Geta.Tags.Sample.Controllers;
using Geta.Tags.Sample.Models.Pages;
using Geta.Tags.Sample.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Tags.Sample.Controllers
{
    public class SearchPageController : PageControllerBase<SearchPage>
    {
        public ViewResult Index(SearchPage currentPage, string q)
        {
            var model = new SearchContentModel(currentPage)
            {
                Hits = Enumerable.Empty<SearchContentModel.SearchHit>(),
                NumberOfHits = 0,
                SearchServiceDisabled = true,
                SearchedQuery = q
            };

            return View(model);
        }
    }
}
