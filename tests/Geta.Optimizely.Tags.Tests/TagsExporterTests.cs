using EPiServer.Enterprise.Transfer;
using Geta.Optimizely.Tags.Core.Export;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsExporterTests
{
    [Fact]
    public void AddTagsToExport_does_not_throw()
    {
        // Issue #34: this threw a NullReferenceException. Tags are not exported; importing the content publishes it,
        // which links it to its tags.
        var exception = Record.Exception(() => new TagsExporter().AddTagsToExport(Substitute.For<ITransferContext>()));

        Assert.Null(exception);
    }
}
