// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Geta.Optimizely.Tags.Core
{
    public class TagRepository : ITagRepository
    {
        private static DynamicDataStore TagStore => typeof(Tag).GetOrCreateStore();

        public Tag GetTagById(Identity id)
        {
            return TagStore.Load<Tag>(id);
        }

        public Tag GetTagByName(string name)
        {
            return TagStore.Find<Tag>("Name", name).FirstOrDefault();
        }

        public Tag GetTagByNameAndGroup(string name, string groupKey)
        {
            return TagStore
                .Find<Tag>(new Dictionary<string, object>
                {
                    { "Name", name }, { "GroupKey", groupKey }
                })
                .FirstOrDefault();
        }

        public IEnumerable<Tag> GetTagsByName(string name)
        {
            return TagStore.Find<Tag>("Name", name);
        }

        public IEnumerable<Tag> GetTagsByContent(Guid contentGuid)
        {
            return GetAllTags().Where(t => t.PermanentLinks.Contains(contentGuid));
        }

        public IQueryable<Tag> GetAllTags()
        {
            return TagStore.Items<Tag>();
        }

        public Identity Save(Tag tag)
        {
            if (string.IsNullOrEmpty(tag?.Name))
            {
                return null;
            }

            // A tag loaded from the store is saved to its own row. A new tag is merged into the stored tag with the same
            // name and group key, if there is one.
            var id = tag.Id ?? GetTagByNameAndGroup(tag.Name, tag.GroupKey)?.GetIdentity();
            return id != null
                ? TagStore.Save(tag, id)
                : TagStore.Save(tag);
        }

        public void Delete(Tag tag)
        {
            TagStore.Delete(tag);
        }
    }
}
