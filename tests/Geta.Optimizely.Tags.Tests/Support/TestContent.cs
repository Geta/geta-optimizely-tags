using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Tags.Core.Attributes;

namespace Geta.Optimizely.Tags.Tests.Support;

/// <summary>
/// Content with one tag property of each kind. Property values live in the property collection, like CMS content.
/// </summary>
public class TestContent : ContentData, IContent, ILocalizable
{
    public const int TypeId = 42;

    public TestContent(Guid contentGuid, string language = "en")
    {
        ContentGuid = contentGuid;
        ContentLink = new ContentReference(Math.Abs(contentGuid.GetHashCode() % 10000) + 1);
        ContentTypeID = TypeId;
        Language = CultureInfo.GetCultureInfo(language);
        MasterLanguage = CultureInfo.GetCultureInfo("en");
        ExistingLanguages = new[] { Language };

        foreach (var name in new[] { nameof(Tags), nameof(ArticleTags), nameof(StoryTags), nameof(LocalizedTags) })
        {
            Property.Add(name, new PropertyString());
        }
    }

    public string Name { get; set; }
    public ContentReference ContentLink { get; set; }
    public ContentReference ParentLink { get; set; }
    public Guid ContentGuid { get; set; }
    public new int ContentTypeID { get; set; }
    public bool IsDeleted { get; set; }
    public IEnumerable<CultureInfo> ExistingLanguages { get; set; }
    public CultureInfo MasterLanguage { get; set; }
    public CultureInfo Language { get; set; }

    // Tag properties: values are read through the property collection (content["Name"]).
    [UIHint("Tags")]
    public virtual string Tags { get; set; }

    [UIHint("Tags")]
    [TagsGroupKey("ArticleTags")]
    public virtual string ArticleTags { get; set; }

    [UIHint("Tags")]
    [TagsGroupKey("StoryTags")]
    public virtual string StoryTags { get; set; }

    [UIHint("Tags")]
    [CultureSpecific]
    public virtual string LocalizedTags { get; set; }

    public TestContent With(string propertyName, string tags)
    {
        this[propertyName] = tags;
        return this;
    }

    public static ContentType CreateContentType()
    {
        var contentType = new ContentType { ID = TypeId, ModelType = typeof(TestContent) };
        foreach (var name in new[] { nameof(Tags), nameof(ArticleTags), nameof(StoryTags), nameof(LocalizedTags) })
        {
            contentType.PropertyDefinitions.Add(new PropertyDefinition { Name = name, TemplateHint = "Tags" });
        }

        return contentType;
    }
}
