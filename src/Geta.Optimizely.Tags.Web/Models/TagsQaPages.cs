using System.ComponentModel.DataAnnotations;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Tags.Core.Attributes;

namespace Geta.Optimizely.Tags.Web.Models;

// Sandbox-only content types used to manually QA the tags property in the CMS UI.
// Each property covers a configuration that has been reported in GitHub issues.

[ContentType(DisplayName = "[Tags QA] Article page", GUID = "6A1E3C1B-4F7B-4C8D-9C1A-2B7E1D3F5A01", GroupName = "Tags QA")]
[AvailableContentTypes(Availability.Specific, Include = new[] { typeof(TagsQaArticlePage), typeof(TagsQaStoryPage) })]
public class TagsQaArticlePage : PageData
{
    [Display(Name = "Tags (no group key)", Order = 10)]
    [UIHint("Tags")]
    public virtual string? Tags { get; set; }

    [Display(Name = "Article tags (group key 'ArticleTags')", Order = 20)]
    [UIHint("Tags")]
    [TagsGroupKey("ArticleTags")]
    [GetaTags(AllowSpaces = true)]
    public virtual string? ArticleTags { get; set; }

    [Display(Name = "Localized tags (culture specific)", Order = 30)]
    [UIHint("Tags")]
    [CultureSpecific]
    public virtual string? LocalizedTags { get; set; }

    [Display(Name = "Header tags (page header tab)", GroupName = SystemTabNames.PageHeader, Order = 10)]
    [UIHint("Tags")]
    public virtual string? HeaderTags { get; set; }

    [Display(Name = "Limited tags (case-insensitive, max 2)", Order = 40)]
    [UIHint("Tags")]
    [GetaTags(CaseSensitive = false, TagLimit = 2)]
    public virtual string? LimitedTags { get; set; }

    [Display(Name = "Tagged block", Order = 50)]
    public virtual TagsQaBlock? Block { get; set; }
}

[ContentType(DisplayName = "[Tags QA] Story page", GUID = "6A1E3C1B-4F7B-4C8D-9C1A-2B7E1D3F5A02", GroupName = "Tags QA")]
public class TagsQaStoryPage : PageData
{
    [Display(Name = "Story tags (group key 'StoryTags')", Order = 10)]
    [UIHint("Tags")]
    [TagsGroupKey("StoryTags")]
    [GetaTags(AllowSpaces = true)]
    public virtual string? Tags { get; set; }
}

[ContentType(DisplayName = "[Tags QA] Block", GUID = "6A1E3C1B-4F7B-4C8D-9C1A-2B7E1D3F5A03", GroupName = "Tags QA")]
public class TagsQaBlock : BlockData
{
    [Display(Name = "Block tags", Order = 10)]
    [UIHint("Tags")]
    public virtual string? Tags { get; set; }
}
