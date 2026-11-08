using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data;
using Geta.Optimizely.Tags.Core;

namespace Geta.Optimizely.Tags.Tests.Support;

/// <summary>
/// Tag repository that behaves like <see cref="TagRepository"/> on the dynamic data store: every load returns new
/// instances, a loaded tag is saved to its own row, and a new tag is merged into the tag with the same name and group
/// key (case-insensitively, like the default SQL collation).
/// </summary>
public class InMemoryTagRepository : ITagRepository
{
    private readonly List<Tag> _tags = new();

    public IReadOnlyList<Tag> Stored => _tags.Select(Copy).ToList();

    public Tag Add(string name, string groupKey, params Guid[] links)
    {
        var tag = new Tag { Name = name, GroupKey = groupKey, PermanentLinks = links.ToList() };
        Save(tag);
        return Copy(tag);
    }

    public Tag Find(string name, string groupKey) => GetTagByNameAndGroup(name, groupKey);

    public Tag GetTagById(Identity id) => _tags.Where(t => t.Id.ExternalId == id.ExternalId).Select(Copy).FirstOrDefault();

    public Tag GetTagByNameAndGroup(string name, string groupKey) =>
        _tags.Where(t => Same(t.Name, name) && Same(t.GroupKey, groupKey)).Select(Copy).FirstOrDefault();

    public IEnumerable<Tag> GetTagsByName(string name) => _tags.Where(t => Same(t.Name, name)).Select(Copy).ToList();

    public IEnumerable<Tag> GetTagsByContent(Guid contentGuid) =>
        _tags.Where(t => t.PermanentLinks?.Contains(contentGuid) == true).Select(Copy).ToList();

    public IQueryable<Tag> GetAllTags() => _tags.Select(Copy).ToList().AsQueryable();

    public Identity Save(Tag tag)
    {
        if (string.IsNullOrEmpty(tag?.Name))
        {
            return null;
        }

        tag.Id ??= _tags.FirstOrDefault(t => Same(t.Name, tag.Name) && Same(t.GroupKey, tag.GroupKey))?.Id
                   ?? Identity.NewIdentity(Guid.NewGuid());
        _tags.RemoveAll(t => t.Id.ExternalId == tag.Id.ExternalId);
        _tags.Add(Copy(tag));
        return tag.Id;
    }

    public void Delete(Tag tag) => _tags.RemoveAll(t => t.Id.ExternalId == tag.Id.ExternalId);

    private static bool Same(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static Tag Copy(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        GroupKey = tag.GroupKey,
        Slug = tag.Slug,
        Description = tag.Description,
        PermanentLinks = tag.PermanentLinks?.ToList()
    };
}
