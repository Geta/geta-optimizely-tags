using System;
using System.Collections.Generic;
using EPiServer.Data.Dynamic;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Tests.Support;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsLegacyImportJobTests
{
    private readonly InMemoryTagRepository _repository = new();

    [Fact]
    public void ToTag_reads_the_legacy_property_bag()
    {
        var link = Guid.NewGuid();
        var bag = new PropertyBag
        {
            { "Name", "alpha" },
            { "GroupKey", "en" },
            { "Description", "first" },
            { "PermanentLinks", new List<Guid> { link, link } }
        };

        var tag = TagsLegacyImportJob.ToTag(bag);

        Assert.Equal("alpha", tag.Name);
        Assert.Equal("en", tag.GroupKey);
        Assert.Equal("first", tag.Description);
        Assert.Equal(new[] { link }, tag.PermanentLinks);
    }

    [Fact]
    public void ToTag_handles_missing_values()
    {
        var tag = TagsLegacyImportJob.ToTag(new PropertyBag { { "Name", "alpha" } });

        Assert.Equal(string.Empty, tag.GroupKey);
        Assert.Empty(tag.PermanentLinks);
    }

    [Fact]
    public void Import_creates_new_tags_and_merges_links_into_existing_tags()
    {
        var existingLink = Guid.NewGuid();
        var legacyLink = Guid.NewGuid();
        _repository.Add("alpha", "en", existingLink);

        var count = TagsLegacyImportJob.Import(new[]
        {
            new Tag { Name = "alpha", GroupKey = "en", PermanentLinks = new List<Guid> { existingLink, legacyLink } },
            new Tag { Name = "beta", GroupKey = "", PermanentLinks = new List<Guid> { legacyLink } },
            new Tag { Name = null, PermanentLinks = new List<Guid>() }
        }, _repository);

        Assert.Equal(2, count);
        Assert.Equal(new[] { existingLink, legacyLink }, _repository.Find("alpha", "en").PermanentLinks);
        Assert.Equal(new[] { legacyLink }, _repository.Find("beta", "").PermanentLinks);
        Assert.Equal(2, _repository.Stored.Count);
    }

    [Fact]
    public void Import_can_run_twice_without_duplicating()
    {
        var link = Guid.NewGuid();
        var legacy = new[] { new Tag { Name = "alpha", GroupKey = "en", PermanentLinks = new List<Guid> { link } } };

        TagsLegacyImportJob.Import(legacy, _repository);
        TagsLegacyImportJob.Import(legacy, _repository);

        Assert.Single(_repository.Stored);
        Assert.Equal(new[] { link }, _repository.Find("alpha", "en").PermanentLinks);
    }
}
