// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.DataAbstraction;

namespace Geta.Optimizely.Tags.Core
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;
        private readonly IContentTypeRepository _contentTypeRepository;

        public TagService(ITagRepository tagRepository, IContentTypeRepository contentTypeRepository)
        {
            _tagRepository = tagRepository;
            _contentTypeRepository = contentTypeRepository;
        }

        public Tag GetTagById(Identity id)
        {
            return _tagRepository.GetTagById(id);
        }

        public IEnumerable<Tag> GetTagsByContent(Guid contentGuid)
        {
            return _tagRepository.GetTagsByContent(contentGuid);
        }

        public Tag GetTagByNameAndGroup(string name, string groupKey)
        {
            return _tagRepository.GetTagByNameAndGroup(name, groupKey);
        }

        public IEnumerable<Tag> GetTagsByName(string name)
        {
            return _tagRepository.GetTagsByName(name);
        }

        public IQueryable<Tag> GetAllTags()
        {
            return _tagRepository.GetAllTags();
        }

        public Identity Save(Tag tag)
        {
            return _tagRepository.Save(tag);
        }

        public Tag Save(Guid contentGuid, string name, string groupKey = "")
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var tag = GetTagByNameAndGroup(name, groupKey)
                      ?? new Tag
                      {
                          Name = name,
                          GroupKey = groupKey
                      };

            if (tag.PermanentLinks == null)
            {
                tag.PermanentLinks = new List<Guid> { contentGuid };
            }
            else
            {
                if (!tag.PermanentLinks.Contains(contentGuid))
                {
                    tag.PermanentLinks.Add(contentGuid);
                }
            }

            Save(tag);

            return tag;
        }

        public void Save(Guid contentGuid, IEnumerable<string> names, string groupKey)
        {
            foreach (var name in names)
            {
                Save(contentGuid, name, groupKey);
            }
        }

        public void Delete(string name)
        {
            var tags = _tagRepository.GetTagsByName(name);
            foreach (var tag in tags)
            {
                _tagRepository.Delete(tag);
            }
        }

        public void Delete(Identity id)
        {
            var tag = _tagRepository.GetTagById(id);

            if (tag == null)
            {
                return;
            }

            _tagRepository.Delete(tag);
        }

        public void UpdateContentTags(IContent content)
        {
            var contentType = _contentTypeRepository.Load(content.ContentTypeID);
            var tagsByGroupKey = ContentTags.GetTagsByGroupKey(
                content,
                contentType,
                cultureSpecificOnly: !ContentTags.IsMasterLanguageVersion(content));

            if (tagsByGroupKey.Count == 0)
            {
                // No tag properties for this version, so there are no tags to link or unlink.
                return;
            }

            RemoveOldContentTags(content.ContentGuid, tagsByGroupKey);

            foreach (var (groupKey, names) in tagsByGroupKey)
            {
                Save(content.ContentGuid, names, groupKey);
            }
        }

        private void RemoveOldContentTags(Guid contentGuid, IReadOnlyDictionary<string, HashSet<string>> tagsByGroupKey)
        {
            // Only unlink tags in the group keys this content version writes to. Tags in other groups, such as the
            // culture-specific groups of other languages, are left alone.
            foreach (var tag in GetTagsByContent(contentGuid).ToList())
            {
                if (tag.PermanentLinks == null
                    || !tagsByGroupKey.TryGetValue(tag.GroupKey ?? string.Empty, out var names)
                    || names.Contains(tag.Name))
                {
                    continue;
                }

                // Save writes to the stored tag found by name and group key. Remove the link from that tag, so a
                // duplicate of the tag never overwrites the links of the tag it duplicates.
                var storedTag = GetTagByNameAndGroup(tag.Name, tag.GroupKey);
                if (storedTag?.PermanentLinks == null || !storedTag.PermanentLinks.Contains(contentGuid))
                {
                    continue;
                }

                storedTag.PermanentLinks = storedTag.PermanentLinks.Where(link => link != contentGuid).ToList();
                Save(storedTag);
            }
        }
    }
}
