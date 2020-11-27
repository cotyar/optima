using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDatasetEntry : IActor
    {
        Task<Result> SetDataAsync(DatasetInfo data);
        Task<DatasetInfo> GetDataAsync();
        Task<Result> DeleteDataAsync();
        // Task RegisterReminder();
        // Task UnregisterReminder();
        // Task RegisterTimer();
        // Task UnregisterTimer();
    }
}
