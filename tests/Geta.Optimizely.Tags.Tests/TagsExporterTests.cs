using System.Collections;
using System.Linq;
using System.Reflection;
using EPiServer.Core.Transfer;
using EPiServer.Enterprise;
using EPiServer.Enterprise.Transfer;
using Geta.Optimizely.Tags.Core.Export;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsExporterTests
{
    [Fact]
    public void AddTagsToExport_does_not_throw_and_adds_nothing_to_the_export()
    {
        // Issue #34: this threw a NullReferenceException. Tags are rebuilt when the import publishes the content, so
        // exporting the stored tags would only add copies of them on every import.
        var ddsHandler = new DynamicDataTransferHandler();
        var context = Substitute.For<ITransferContext, ITransferHandlerContext>();
        ((ITransferHandlerContext)context).TransferType.Returns(TypeOfTransfer.Exporting);
        ((ITransferHandlerContext)context).TransferHandlers.Returns(new TransferHandlerBase[] { ddsHandler });

        var exception = Record.Exception(() => new TagsExporter().AddTagsToExport(context));

        Assert.Null(exception);
        // DynamicDataTransferHandler keeps the export entries private.
        var objects = typeof(DynamicDataTransferHandler).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.Empty(((IEnumerable)objects!.GetValue(ddsHandler)!).Cast<object>());
    }
}
