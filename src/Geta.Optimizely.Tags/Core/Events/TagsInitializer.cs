using System;
using EPiServer;
using EPiServer.Core;
using EPiServer.Enterprise;
using EPiServer.Enterprise.Transfer;
using Geta.Optimizely.Tags.Core.Export;

namespace Geta.Optimizely.Tags.Core.Events
{
    public class TagsInitializer
    {
        private readonly IContentEvents _contentEvents;
        private readonly Func<ITagService> _tagServiceFactory;
        private readonly IDataExportEvents _dataExportEvents;
        private readonly Func<TagsExporter> _tagsExporterFactory;

        public TagsInitializer(
            IContentEvents contentEvents,
            Func<ITagService> tagServiceFactory,
            IDataExportEvents dataExportEvents,
            Func<TagsExporter> tagsExporterFactory)
        {
            _contentEvents = contentEvents;
            _tagServiceFactory = tagServiceFactory;
            _dataExportEvents = dataExportEvents;
            _tagsExporterFactory = tagsExporterFactory;
        }

        public void Initialize()
        {
            _contentEvents.PublishedContent += OnPublishedContent;
            _dataExportEvents.ContentExporting += OnContentExporting;
        }

        private void OnContentExporting(ITransferContext transferContext, ContentExportingEventArgs e)
        {
            var tagsExporter = _tagsExporterFactory();
            tagsExporter.AddTagsToExport(transferContext);
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            var tagService = _tagServiceFactory();
            tagService.UpdateContentTags(e.Content);
        }
    }
}
