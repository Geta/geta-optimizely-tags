using EPiServer.Enterprise.Transfer;

namespace Geta.Optimizely.Tags.Core.Export
{
    /// <summary>
    /// Called for every content item that is exported.
    /// </summary>
    public class TagsExporter
    {
        /// <summary>
        /// Does nothing by default: tags are not added to export packages. Importing content publishes it, and publishing
        /// links the content to its tags and creates the tags that are missing. Exporting the stored tags as well would
        /// add a copy of every tag on each import, linked to the content of the exporting site.
        /// Override this method to add data of your own to the export package.
        /// </summary>
        public virtual void AddTagsToExport(ITransferContext transferContext)
        {
        }
    }
}
