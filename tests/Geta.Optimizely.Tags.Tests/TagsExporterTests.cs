using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EPiServer.Core;
using EPiServer.Core.Transfer;
using EPiServer.Enterprise;
using EPiServer.Enterprise.Transfer;
using EPiServer.Web;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Core.Export;
using Geta.Optimizely.Tags.Tests.Support;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsExporterTests
{
    private readonly InMemoryTagRepository _repository = new();
    private readonly IPermanentLinkMapper _permanentLinkMapper = Substitute.For<IPermanentLinkMapper>();
    private readonly DynamicDataTransferHandler _ddsHandler = new();
    private readonly TagsExporter _exporter;

    public TagsExporterTests()
    {
        _exporter = new TagsExporter(_repository, _permanentLinkMapper);
    }

    private ITransferContext CreateContext(TypeOfTransfer transferType, params TransferHandlerBase[] handlers)
    {
        var context = Substitute.For<ITransferContext, ITransferHandlerContext>();
        ((ITransferHandlerContext)context).TransferType.Returns(transferType);
        ((ITransferHandlerContext)context).TransferHandlers.Returns(handlers);
        return context;
    }

    private int ExportedObjectCount()
    {
        // DynamicDataTransferHandler keeps the export entries private.
        var field = typeof(DynamicDataTransferHandler).GetField("_objects", BindingFlags.Instance | BindingFlags.NonPublic);
        return ((IEnumerable)field!.GetValue(_ddsHandler)!).Cast<object>().Count();
    }

    [Fact]
    public void AddTagsToExport_adds_the_tags_linked_to_the_exported_content()
    {
        // Issue #34: this threw a NullReferenceException because a store has no identity.
        var contentLink = new ContentReference(5);
        var contentGuid = Guid.NewGuid();
        _permanentLinkMapper.Find(contentLink).Returns(new PermanentLinkMap(contentGuid, contentLink));
        _repository.Add("alpha", "en", contentGuid);
        _repository.Add("beta", "en", contentGuid);
        _repository.Add("unrelated", "en", Guid.NewGuid());

        _exporter.AddTagsToExport(CreateContext(TypeOfTransfer.Exporting, _ddsHandler), contentLink);

        Assert.Equal(2, ExportedObjectCount());
    }

    [Fact]
    public void AddTagsToExport_adds_every_tag_when_no_content_is_given()
    {
        _repository.Add("alpha", "en", Guid.NewGuid());
        _repository.Add("beta", "ArticleTags");

        _exporter.AddTagsToExport(CreateContext(TypeOfTransfer.Exporting, _ddsHandler));

        Assert.Equal(2, ExportedObjectCount());
    }

    [Fact]
    public void AddTagsToExport_ignores_transfers_that_are_not_exports()
    {
        _repository.Add("alpha", "en", Guid.NewGuid());

        _exporter.AddTagsToExport(CreateContext(TypeOfTransfer.Importing, _ddsHandler));

        Assert.Equal(0, ExportedObjectCount());
    }

    [Fact]
    public void AddTagsToExport_does_nothing_without_a_dynamic_data_handler()
    {
        _repository.Add("alpha", "en", Guid.NewGuid());

        var exception = Record.Exception(() => _exporter.AddTagsToExport(CreateContext(TypeOfTransfer.Exporting)));

        Assert.Null(exception);
    }

    [Fact]
    public void AddTagsToExport_does_nothing_for_content_without_a_permanent_link()
    {
        _repository.Add("alpha", "en", Guid.NewGuid());

        _exporter.AddTagsToExport(CreateContext(TypeOfTransfer.Exporting, _ddsHandler), new ContentReference(99));

        Assert.Equal(0, ExportedObjectCount());
    }
}
