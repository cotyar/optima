using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.DatasetDefinition;

namespace Optima.Interfaces
{
    public interface IDataProvider : IActor
    {
        Task<DatasetEndpoint> GetGrpcEndpoint();
    }
}
