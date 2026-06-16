using System.Linq;
using EPiServer.Core.Transfer;
using EPiServer.Data;
using EPiServer.Data.Dynamic;
using EPiServer.Enterprise;
using EPiServer.Enterprise.Transfer;

namespace Geta.Optimizely.Tags.Core.Export
{
    public class TagsExporter
    {
        public virtual void AddTagsToExport(ITransferContext transferContext)
        {
            if (transferContext is not ITransferHandlerContext exporter
                || exporter.TransferType != TypeOfTransfer.Exporting)
            {
                return;
            }

            var ddsHandler = exporter
                .TransferHandlers
                .Single(p => p.GetType() == typeof(DynamicDataTransferHandler)) as DynamicDataTransferHandler;

            var store = typeof(Tag).GetStore();
            var externalId = store.GetIdentity().ExternalId;
            var storeName = store.Name;

            ddsHandler?.AddToExport(externalId, storeName);
        }
    }
}
