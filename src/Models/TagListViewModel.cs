﻿using System.Collections.Generic;

namespace Geta.Optimizely.Tags.Models
{
    public class TagListViewModel
    {
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public int TotalItemCount { get; set; }
        public List<Tag> Tags { get; set; }
    }
}