using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data;
using EPiServer.DataAccess;
using EPiServer.Security;
using Geta.Optimizely.Tags.Core;
using Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags.Components.Pager;
using Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;

namespace Geta.Optimizely.Tags.Pages.Geta.Optimizely.Tags
{
    [Authorize(Constants.PolicyName)]
    public class Index : PageModel
    {
        private readonly ITagRepository _tagRepository;
        private readonly IContentRepository _contentRepository;
        private readonly ITagEngine _tagEngine;

        public Index(ITagRepository tagRepository, IContentRepository contentRepository, ITagEngine tagEngine)
        {
            _tagRepository = tagRepository;
            _contentRepository = contentRepository;
            _tagEngine = tagEngine;
        }

        public IPagedList<Tag> Items { get; set; } = Enumerable.Empty<Tag>().ToPagedList();

        public string EditItemId { get; set; }

        public PagerViewModel Pager { get; set; }

        [BindProperty]
        public TagEditModel Tag { get; set; }

        [BindProperty(SupportsGet = true)]
        public Paging Paging { get; set; }

        [BindProperty(SupportsGet = true, Name = "q")]
        public string Query { get; set; }

        public bool HasQuery => !string.IsNullOrEmpty(Query);

        public void OnGet()
        {
            Load();
        }

        public IActionResult OnPostEdit(string id)
        {
            var tag = _tagRepository.GetTagById(Identity.Parse(id));
            if (tag == null)
            {
                return RedirectToCurrentPage();
            }

            EditItemId = id;
            Tag = ToEditModel(tag);
            Load();
            return Page();
        }

        public IActionResult OnPostUpdate(string id)
        {
            if (!ModelState.IsValid)
            {
                Load();
                return Page();
            }

            var existingTag = _tagRepository.GetTagById(Identity.Parse(id));

            if (existingTag == null)
            {
                return RedirectToCurrentPage();
            }

            if (Tag.CheckedEditContentTags)
            {
                EditTagsInContentRepository(existingTag, Tag);
            }

            existingTag.Name = Tag.Name;
            // An empty field is bound as null, but publishing stores and looks up tags without a group key under "", and
            // the tag store does not match null with "". Keep a group key that is already null, though: saving it as ""
            // would overwrite a tag with the same name that is stored under "".
            if (Tag.GroupKey != null || existingTag.GroupKey != null)
            {
                existingTag.GroupKey = Tag.GroupKey ?? string.Empty;
            }

            _tagRepository.Save(existingTag);

            return RedirectToCurrentPage();
        }

        public IActionResult OnPostDelete(string id)
        {
            var existingTag = _tagRepository.GetTagById(Identity.Parse(id));

            if (existingTag != null)
            {
                _tagRepository.Delete(existingTag);
            }

            return RedirectToCurrentPage();
        }

        private IActionResult RedirectToCurrentPage()
        {
            // Keep the pager and search state, so editing on page 2 or in search results returns to the same list.
            return RedirectToPage(new { pageNumber = Paging.PageNumber, q = Query });
        }

        private void Load()
        {
            var tags = FindTags().ToList();

            // Stay on a page that exists, for example after deleting the only tag on the last page.
            var pageCount = Math.Max(1, (tags.Count + Paging.PageSize - 1) / Paging.PageSize);
            Paging.PageNumber = Math.Clamp(Paging.PageNumber, 1, pageCount);

            Items = tags.ToPagedList(Paging.PageNumber, Paging.PageSize);
        }

        private IEnumerable<Tag> FindTags()
        {
            var allTags = _tagRepository.GetAllTags().ToList();
            return HasQuery ? allTags.Where(x => x.Name.Contains(Query, StringComparison.InvariantCultureIgnoreCase)) : allTags;
        }

        private void EditTagsInContentRepository(Tag tagFromTagRepository, TagEditModel tagFromUser)
        {
            var existingTagName = tagFromTagRepository.Name;
            var contentReferencesFromTag = _tagEngine.GetContentReferencesByTags(existingTagName);

            foreach (var item in contentReferencesFromTag)
            {
                var pageFromRepository = (ContentData)_contentRepository.Get<IContent>(item);

                var clone = pageFromRepository.CreateWritableClone();

                var tagAttributes = clone.GetType().GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(UIHintAttribute)) &&
                            prop.PropertyType == typeof(string) &&
                            Attribute.GetCustomAttributes(prop, typeof(UIHintAttribute))
                                .Any(x => ((UIHintAttribute)x).UIHint == "Tags"));

                foreach (var tagAttribute in tagAttributes)
                {
                    var tags = tagAttribute.GetValue(clone) as string;
                    if (string.IsNullOrEmpty(tags)) continue;

                    var pageTagList = tags.Split(',').ToList();
                    var indexTagToReplace = pageTagList.IndexOf(existingTagName);

                    if (indexTagToReplace == -1) continue;
                    pageTagList[indexTagToReplace] = tagFromUser.Name;

                    var tagsCommaSeperated = string.Join(",", pageTagList);

                    tagAttribute.SetValue(clone, tagsCommaSeperated);
                }

                _contentRepository.Save((IContent)clone, SaveAction.Publish, AccessLevel.NoAccess);
            }

            _tagRepository.Delete(tagFromTagRepository);
        }

        private static TagEditModel ToEditModel(Tag tag)
        {
            return new TagEditModel { Id = tag.Id.ToString(), Name = tag.Name, GroupKey = tag.GroupKey };
        }

        public bool IsEditing(string id)
        {
            return id == EditItemId;
        }
    }
}
