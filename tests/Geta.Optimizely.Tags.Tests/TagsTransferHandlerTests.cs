using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPiServer.Core;
using EPiServer.Core.Transfer;
using EPiServer.DataAbstraction;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Core.Export;
using Geta.Optimizely.Tags.Tests.Support;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsTransferHandlerTests
{
    // Issue #34: exported content carries its tags, and importing it adds them without relying on a republish.
    private readonly InMemoryTagRepository _source = new();
    private readonly InMemoryTagRepository _target = new();

    private static TagsTransferHandler CreateHandler(ITagRepository repository) =>
        new(new TagService(repository, Substitute.For<IContentTypeRepository>()));

    private MemoryStream Export(params Guid[] exportedContent)
    {
        var handler = CreateHandler(_source);
        foreach (var contentGuid in exportedContent)
        {
            handler.ContentTransferring(new RawContent
            {
                Property = new[] { new RawProperty { Name = "PageGUID", Value = contentGuid.ToString() } }
            });
        }

        var package = new MemoryStream();
        handler.Write(package);
        package.Position = 0;
        return package;
    }

    [Fact]
    public void Import_adds_the_tags_of_the_exported_content_linked_to_the_imported_content()
    {
        var exported = Guid.NewGuid();
        var imported = Guid.NewGuid();
        _source.Save(new Tag { Name = "alpha", GroupKey = "ArticleTags", Slug = "alpha-slug", Description = "About alpha", PermanentLinks = new List<Guid> { exported } });
        _source.Add("beta", "en", exported);

        var changed = CreateHandler(_target).Import(Export(exported), new Dictionary<Guid, Guid> { [exported] = imported });

        Assert.Equal(2, changed);
        var alpha = _target.Find("alpha", "ArticleTags");
        Assert.Equal(new[] { imported }, alpha.PermanentLinks);
        Assert.Equal("alpha-slug", alpha.Slug);
        Assert.Equal("About alpha", alpha.Description);
        Assert.Equal(new[] { imported }, _target.Find("beta", "en").PermanentLinks);
    }

    [Fact]
    public void Export_only_includes_tags_and_links_of_the_exported_content()
    {
        var exported = Guid.NewGuid();
        var other = Guid.NewGuid();
        _source.Add("shared", "en", exported, other);
        _source.Add("unrelated", "en", other);

        CreateHandler(_target).Import(Export(exported), new Dictionary<Guid, Guid> { [exported] = exported, [other] = other });

        Assert.Equal(new[] { exported }, _target.Find("shared", "en").PermanentLinks);
        Assert.Null(_target.Find("unrelated", "en"));
    }

    [Fact]
    public void Import_merges_into_an_existing_tag_and_keeps_its_details()
    {
        var exported = Guid.NewGuid();
        var imported = Guid.NewGuid();
        var existingContent = Guid.NewGuid();
        _source.Save(new Tag { Name = "alpha", GroupKey = "en", Description = "Source", PermanentLinks = new List<Guid> { exported } });
        _target.Save(new Tag { Name = "Alpha", GroupKey = "en", Description = "Target", PermanentLinks = new List<Guid> { existingContent } });
        var map = new Dictionary<Guid, Guid> { [exported] = imported };

        CreateHandler(_target).Import(Export(exported), map);
        var changedOnSecondImport = CreateHandler(_target).Import(Export(exported), map);

        var tag = Assert.Single(_target.Stored);
        Assert.Equal("Target", tag.Description);
        Assert.Equal(new[] { existingContent, imported }, tag.PermanentLinks);
        Assert.Equal(0, changedOnSecondImport);
    }

    [Fact]
    public void Import_skips_links_to_content_that_was_not_imported()
    {
        var exported = Guid.NewGuid();
        _source.Add("alpha", "en", exported);

        var changed = CreateHandler(_target).Import(Export(exported), new Dictionary<Guid, Guid>());

        Assert.Equal(0, changed);
        Assert.Empty(_target.Stored);
    }
}
