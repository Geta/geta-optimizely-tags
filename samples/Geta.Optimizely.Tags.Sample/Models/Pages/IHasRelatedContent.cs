using EPiServer.Core;

namespace Geta.Tags.Sample.Models.Pages
{
    public interface IHasRelatedContent
    {
        ContentArea RelatedContentArea { get; }
    }
}