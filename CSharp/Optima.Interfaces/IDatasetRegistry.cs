using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDatasetRegistry : IActor
    {
        Task<string> RegisterDataset(DatasetInfo datasetInfo);
        Task<DatasetInfo> GetDataset(DatasetId id);
        Task<ImmutableArray<DatasetId>> GetDatasetIds();
        Task<ImmutableArray<DatasetId>> GetDatasetIdsForName(string name);
        Task<ImmutableArray<DatasetInfo>> GetDatasetsForName(string name);
        Task<ImmutableArray<string>> GetDatasetNames();
        Task<ImmutableArray<DatasetInfo>> GetDatasets();
        Task DeleteDataset(DatasetId id);
    }
}
