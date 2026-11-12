using EPiServer.Enterprise.Transfer;

namespace Geta.Optimizely.Tags.Core.Export
{
    /// <summary>
    /// Called for every content item that is exported.
    /// </summary>
    public class TagsExporter
    {
        /// <summary>
        /// Does nothing by default. The tags of exported content are exported and imported by
        /// <see cref="TagsTransferHandler"/>. Override this method to add data of your own to the export package.
        /// </summary>
        public virtual void AddTagsToExport(ITransferContext transferContext)
        {
        }
    }
}
