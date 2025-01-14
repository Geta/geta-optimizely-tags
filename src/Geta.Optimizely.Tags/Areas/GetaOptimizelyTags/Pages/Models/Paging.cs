using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags.Models;

public class Paging
{
    [FromQuery(Name = "page")]
    public int PageNumber { get; set; } = 1;

    public static int PageSize { get => 50; }
}
