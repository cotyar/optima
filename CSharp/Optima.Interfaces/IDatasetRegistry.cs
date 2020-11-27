using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDatasetRegistry : IActor
    {
        Task<Result> RegisterDataset(DatasetInfo datasetInfo);
        Task<DatasetInfo> GetDataset(DatasetId id);
        Task<DatasetId[]> GetDatasetIds();
        Task<DatasetId[]> GetDatasetIdsForName(string name);
        Task<DatasetInfo[]> GetDatasetsForName(string name);
        Task<string[]> GetDatasetNames();
        Task<DatasetInfo[]> GetDatasets();
        Task DeleteDataset(DatasetId id);
    }
}
