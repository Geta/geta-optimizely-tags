// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.Optimizely.Tags.Core;

namespace Geta.Optimizely.Tags
{
    [ScheduledPlugIn(DisplayName = "Geta Tags maintenance", DefaultEnabled = true)]
    public class TagsScheduledJob : ScheduledJobBase
    {
        private bool _stop;

        private readonly ITagService _tagService;
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentLoader _contentLoader;

        public TagsScheduledJob(
            IContentTypeRepository contentTypeRepository,
            ITagService tagService,
            IContentLoader contentLoader)
        {
            _contentTypeRepository = contentTypeRepository;
            _tagService = tagService;
            _contentLoader = contentLoader;
            IsStoppable = true;
        }

        public override string Execute()
        {
            var tags = _tagService.GetAllTags().ToList();
            var contentGuids = GetTaggedContentGuids(tags);

            foreach (var contentGuid in contentGuids)
            {
                if (_stop)
                {
                    return "Geta Tags maintenance was stopped";
                }

                IContent content = null;

                try
                {
                    var contentReference = TagsHelper.GetContentReference(contentGuid);

                    if (!ContentReference.IsNullOrEmpty(contentReference))
                    {
                        content = _contentLoader.Get<IContent>(contentReference);
                    }
                }
                catch (ContentNotFoundException)
                {
                    // Ignore if not found
                }

                if (content == null || (content is PageData data && data.IsDeleted))
                {
                    RemoveFromTags(contentGuid, tags);
                    continue;
                }

                CheckContentProperties(content, tags);
            }

            return "Geta Tags maintenance completed successfully";
        }

        private void CheckContentProperties(IContent content, IList<Tag> tags)
        {
            var contentType = _contentTypeRepository.Load(content.ContentTypeID);

            var tagProperties = contentType.PropertyDefinitions
                .Where(TagsHelper.IsTagProperty);

            var contentTagNames = tagProperties
                .Select(propertyDefinition => GetTagNames(content, propertyDefinition))
                .Where(tagNames => !string.IsNullOrEmpty(tagNames));

            var contentTags = contentTagNames
                .SelectMany(ParseTags)
                .Distinct()
                .ToList();

            // make sure the tags it has added has the ContentReference
            ValidateTags(content.ContentGuid, contentTags);

            // make sure there's no ContentReference to this ContentReference in the rest of the tags
            RemoveFromTags(content.ContentGuid, tags.Except(contentTags));
        }

        private string GetTagNames(IContent content, PropertyDefinition propertyDefinition)
        {
            if (content is ILocalizable)
            {
                return GetAllLanguageTagNames(content, propertyDefinition);
            }

            return ((ContentData)content)[propertyDefinition.Name] as string;
        }

        private string GetAllLanguageTagNames(IContent localizableContent, PropertyDefinition tagPropertyDefinition)
        {
            var localizable = (ILocalizable)localizableContent;
            var tags = localizable
                .ExistingLanguages
                .Select(language => _contentLoader.Get<IContent>(localizableContent.ContentGuid, language))
                .Select(x => ((ContentData)x)[tagPropertyDefinition.Name] as string);
            return string.Join(",", tags);
        }

        private static IEnumerable<Guid> GetTaggedContentGuids(IEnumerable<Tag> tags)
        {
            return tags.Where(x => x?.PermanentLinks != null)
                .SelectMany(x => x.PermanentLinks)
                .ToList();
        }

        private IEnumerable<Tag> ParseTags(string tagNames)
        {
            return tagNames.Split(',')
                .SelectMany(_tagService.GetTagsByName)
                .Where(tag => tag != null)
                .ToList();
        }

        private void ValidateTags(Guid contentGuid, IEnumerable<Tag> addedTags)
        {
            foreach (var addedTag in addedTags)
            {
                if (addedTag.PermanentLinks.Contains(contentGuid)) continue;

                addedTag.PermanentLinks.Add(contentGuid);

                _tagService.Save(addedTag);
            }
        }

        private void RemoveFromTags(Guid guid, IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                if (tag.PermanentLinks == null || !tag.PermanentLinks.Contains(guid)) continue;

                tag.PermanentLinks.Remove(guid);

                _tagService.Save(tag);
            }
        }

        public override void Stop()
        {
            _stop = true;
        }
    }
}
