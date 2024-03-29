﻿// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Linq;
using Geta.Optimizely.Tags.Core;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Tags.Autocomplete
{
    [ApiController]
    public class GetaTagsController : Controller
    {
        private readonly ITagService _tagService;

        public GetaTagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        [Route("/getatags")]
        public ActionResult Index(string term, string groupKey = null)
        {
            var normalized = Normalize(term);
            var tags = _tagService.GetAllTags();

            if (IsNotEmpty(normalized))
            {
                tags = tags.Where(t => t.Name.ToLower().StartsWith(normalized.ToLower()));

                if (IsNotEmpty(groupKey))
                {
                    tags = tags.Where(t => t.GroupKey.Equals(groupKey));
                }
            }

            var items = tags.OrderBy(t => t.Name)
                .Select(t => t.Name)
                .Take(10)
                .ToList();

            return Ok(items);
        }

        private static string Normalize(string term)
        {
            return string.IsNullOrWhiteSpace(term) ? string.Empty : term;
        }

        private static bool IsNotEmpty(string name)
        {
            return !string.IsNullOrEmpty(name);
        }
    }
}
