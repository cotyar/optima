using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDataProviderManager : IActor
    {
        Task<uint> RegisterProvider(DatasetId requesterId); // Returns allocated port (TODO: Return NetworkAddress?)
        Task UnRegisterProvider(DatasetId requesterId);
        Task<Dictionary<DatasetId, uint>> ActiveDatasetProviders();
        
        Task<string> GeneratedCodeFolder(); // TODO: Change to URI?
    }
}
