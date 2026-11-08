using System;
using System.Linq;
using EPiServer.DataAbstraction;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Tests.Support;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagServiceTests
{
    private readonly InMemoryTagRepository _repository = new();
    private readonly TagService _service;

    public TagServiceTests()
    {
        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.Load(TestContent.TypeId).Returns(TestContent.CreateContentType());
        _service = new TagService(_repository, contentTypeRepository);
    }

    [Fact]
    public void UpdateContentTags_creates_tags_under_the_property_group_key()
    {
        var content = new TestContent(Guid.NewGuid())
            .With(nameof(TestContent.Tags), "plain")
            .With(nameof(TestContent.ArticleTags), "alpha,beta")
            .With(nameof(TestContent.LocalizedTags), "local");

        _service.UpdateContentTags(content);

        Assert.Contains(content.ContentGuid, _repository.Find("plain", "en").PermanentLinks);
        Assert.Contains(content.ContentGuid, _repository.Find("alpha", "ArticleTags").PermanentLinks);
        Assert.Contains(content.ContentGuid, _repository.Find("beta", "ArticleTags").PermanentLinks);
        Assert.Contains(content.ContentGuid, _repository.Find("local", "en").PermanentLinks);
    }

    [Fact]
    public void UpdateContentTags_unlinks_a_tag_removed_from_a_property_with_a_group_key()
    {
        // Issue #20: removed tags were only unlinked when the group key ended with the language code.
        var content = new TestContent(Guid.NewGuid()).With(nameof(TestContent.ArticleTags), "alpha,beta");
        _service.UpdateContentTags(content);

        _service.UpdateContentTags(content.With(nameof(TestContent.ArticleTags), "alpha"));

        Assert.Contains(content.ContentGuid, _repository.Find("alpha", "ArticleTags").PermanentLinks);
        Assert.DoesNotContain(content.ContentGuid, _repository.Find("beta", "ArticleTags").PermanentLinks);
    }

    [Fact]
    public void UpdateContentTags_unlinks_all_tags_when_a_property_is_cleared()
    {
        var content = new TestContent(Guid.NewGuid()).With(nameof(TestContent.ArticleTags), "alpha,beta");
        _service.UpdateContentTags(content);

        _service.UpdateContentTags(content.With(nameof(TestContent.ArticleTags), null));

        Assert.Empty(_repository.GetTagsByContent(content.ContentGuid));
    }

    [Fact]
    public void UpdateContentTags_keeps_culture_specific_tags_of_other_languages()
    {
        var contentGuid = Guid.NewGuid();
        _service.UpdateContentTags(new TestContent(contentGuid).With(nameof(TestContent.LocalizedTags), "english"));

        _service.UpdateContentTags(new TestContent(contentGuid, "sv").With(nameof(TestContent.LocalizedTags), "svenska"));

        Assert.Contains(contentGuid, _repository.Find("english", "en").PermanentLinks);
        Assert.Contains(contentGuid, _repository.Find("svenska", "sv").PermanentLinks);
    }

    [Fact]
    public void UpdateContentTags_does_not_touch_the_same_tag_name_in_another_group()
    {
        // Issue #28: the same tag name can be used in two groups.
        var article = new TestContent(Guid.NewGuid()).With(nameof(TestContent.ArticleTags), "Cancer Care");
        var story = new TestContent(Guid.NewGuid()).With(nameof(TestContent.StoryTags), "Cancer Care");

        _service.UpdateContentTags(article);
        _service.UpdateContentTags(story);

        Assert.Equal(new[] { article.ContentGuid }, _repository.Find("Cancer Care", "ArticleTags").PermanentLinks);
        Assert.Equal(new[] { story.ContentGuid }, _repository.Find("Cancer Care", "StoryTags").PermanentLinks);
    }

    [Fact]
    public void UpdateContentTags_does_not_read_the_tag_store_for_content_without_tag_properties()
    {
        var repository = Substitute.For<ITagRepository>();
        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.Load(7).Returns(new ContentType { ID = 7, ModelType = typeof(TestContent) });

        new TagService(repository, contentTypeRepository).UpdateContentTags(new TestContent(Guid.NewGuid()) { ContentTypeID = 7 });

        repository.DidNotReceive().GetTagsByContent(Arg.Any<Guid>());
    }

    [Fact]
    public void UpdateContentTags_keeps_a_tag_used_by_two_properties_with_the_same_group_key()
    {
        var content = new TestContent(Guid.NewGuid())
            .With(nameof(TestContent.Tags), "shared")
            .With(nameof(TestContent.LocalizedTags), "shared");
        _service.UpdateContentTags(content);

        _service.UpdateContentTags(content.With(nameof(TestContent.Tags), null));

        Assert.Contains(content.ContentGuid, _repository.Find("shared", "en").PermanentLinks);
        Assert.Single(_repository.Stored, t => t.Name == "shared");
    }
}
