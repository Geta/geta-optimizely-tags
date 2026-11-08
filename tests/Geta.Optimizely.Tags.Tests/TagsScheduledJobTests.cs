using System;
using System.Collections.Generic;
using System.Globalization;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Tests.Support;
using NSubstitute;
using Xunit;

namespace Geta.Optimizely.Tags.Tests;

public class TagsScheduledJobTests
{
    private readonly InMemoryTagRepository _repository = new();
    private readonly IContentLoader _contentLoader = Substitute.For<IContentLoader>();
    private readonly Dictionary<Guid, TestContent> _contents = new();
    private readonly TagsScheduledJob _job;

    public TagsScheduledJobTests()
    {
        var contentTypeRepository = Substitute.For<IContentTypeRepository>();
        contentTypeRepository.Load(TestContent.TypeId).Returns(TestContent.CreateContentType());

        _contentLoader.TryGet(Arg.Any<Guid>(), out Arg.Any<IContent>()).Returns(call =>
        {
            var found = _contents.TryGetValue(call.ArgAt<Guid>(0), out var content);
            call[1] = content;
            return found;
        });
        _contentLoader.TryGet(Arg.Any<Guid>(), Arg.Any<CultureInfo>(), out Arg.Any<IContent>()).Returns(call =>
        {
            var found = _contents.TryGetValue(call.ArgAt<Guid>(0), out var content);
            call[2] = content;
            return found;
        });

        _job = new TagsScheduledJob(contentTypeRepository, new TagService(_repository, contentTypeRepository), _contentLoader);
    }

    private TestContent AddContent(string property, string tags)
    {
        var content = new TestContent(Guid.NewGuid()).With(property, tags);
        _contents[content.ContentGuid] = content;
        return content;
    }

    [Fact]
    public void Execute_unlinks_tags_that_the_content_no_longer_uses()
    {
        var content = AddContent(nameof(TestContent.ArticleTags), "alpha");
        _repository.Add("alpha", "ArticleTags", content.ContentGuid);
        _repository.Add("beta", "ArticleTags", content.ContentGuid);

        _job.Execute();

        Assert.Contains(content.ContentGuid, _repository.Find("alpha", "ArticleTags").PermanentLinks);
        Assert.Empty(_repository.Find("beta", "ArticleTags").PermanentLinks);
    }

    [Fact]
    public void Execute_unlinks_deleted_and_missing_content()
    {
        var deleted = AddContent(nameof(TestContent.Tags), "alpha");
        deleted.IsDeleted = true;
        var missing = Guid.NewGuid();
        _repository.Add("alpha", "en", deleted.ContentGuid, missing);

        _job.Execute();

        Assert.Empty(_repository.Find("alpha", "en").PermanentLinks);
    }

    [Fact]
    public void Execute_does_not_link_content_to_the_same_tag_name_in_another_group()
    {
        // Issue #19/#28: the job used to link content to every tag with a matching name, in any group.
        var article = AddContent(nameof(TestContent.ArticleTags), "Cancer Care");
        var story = AddContent(nameof(TestContent.StoryTags), "Cancer Care");
        _repository.Add("Cancer Care", "ArticleTags", article.ContentGuid);
        _repository.Add("Cancer Care", "StoryTags", story.ContentGuid);

        _job.Execute();

        Assert.Equal(new[] { article.ContentGuid }, _repository.Find("Cancer Care", "ArticleTags").PermanentLinks);
        Assert.Equal(new[] { story.ContentGuid }, _repository.Find("Cancer Care", "StoryTags").PermanentLinks);
    }

    [Fact]
    public void Execute_keeps_links_added_for_one_content_item_when_processing_another()
    {
        // Saving stale copies of a tag used to overwrite links added earlier in the same run.
        var first = AddContent(nameof(TestContent.Tags), "shared,other");
        var second = AddContent(nameof(TestContent.Tags), "shared");
        _repository.Add("shared", "en", second.ContentGuid);
        _repository.Add("other", "en", first.ContentGuid);

        _job.Execute();

        var shared = _repository.Find("shared", "en").PermanentLinks;
        Assert.Contains(first.ContentGuid, shared);
        Assert.Contains(second.ContentGuid, shared);
    }

    [Fact]
    public void Execute_recreates_a_tag_that_content_still_uses()
    {
        var content = AddContent(nameof(TestContent.ArticleTags), "alpha,deleted-in-admin");
        _repository.Add("alpha", "ArticleTags", content.ContentGuid);

        _job.Execute();

        Assert.Contains(content.ContentGuid, _repository.Find("deleted-in-admin", "ArticleTags").PermanentLinks);
    }

    [Fact]
    public void Execute_reads_tags_from_all_language_versions()
    {
        var contentGuid = Guid.NewGuid();
        var english = new TestContent(contentGuid).With(nameof(TestContent.LocalizedTags), "english");
        var swedish = new TestContent(contentGuid, "sv").With(nameof(TestContent.LocalizedTags), "svenska");
        english.ExistingLanguages = new[] { english.Language, swedish.Language };
        _contents[contentGuid] = english;
        _contentLoader.TryGet(contentGuid, swedish.Language, out Arg.Any<IContent>()).Returns(call =>
        {
            call[2] = swedish;
            return true;
        });
        _repository.Add("english", "en", contentGuid);
        _repository.Add("svenska", "sv", contentGuid);

        _job.Execute();

        Assert.Contains(contentGuid, _repository.Find("english", "en").PermanentLinks);
        Assert.Contains(contentGuid, _repository.Find("svenska", "sv").PermanentLinks);
    }
}
