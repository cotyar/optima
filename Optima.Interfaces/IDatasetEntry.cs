using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDatasetEntry : IActor
    {
        Task<string> SetDataAsync(DatasetInfo data);
        Task<DatasetInfo> GetDataAsync();
        Task RegisterReminder();
        Task UnregisterReminder();
        Task RegisterTimer();
        Task UnregisterTimer();
    }
}
