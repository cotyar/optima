using System.Threading.Tasks;
using Dapr.Actors;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
using Optima.Domain.Ownership;

namespace Optima.Interfaces
{
    public interface IOwnershipService : IActor
    {
        Task<DatasetOwner[]> GetOwners();
        Task<DatasetOwner> GetOwner(DatasetId datasetId);
        Task<DatasetOwnerHistory> GetOwnerHistory(DatasetId datasetId);
        Task<OwnerAuthorizationRequest[]> GetRequestHistory(DatasetId datasetId);
        Task<OwnerAuthorizationRequest[]> GetPrincipalRequestHistory(DatasetId datasetId, UUID principal);
        Task<OwnerAuthorizationRequest[]> GetActiveRequests(DatasetId datasetId);
        Task<OwnerAuthorizationRequest[]> GetPrincipalActiveRequests(DatasetId datasetId, UUID principal);
        Task<OwnerAuthorizationResponse[]> GetResponseHistory(DatasetId datasetId);
        Task<OwnerAuthorizationResponse[]> GetPrincipalResponseHistory(DatasetId datasetId, UUID principal);

        Task<OwnerAuthorizationRequest> GetRequest(UUID requestId);
        Task<OwnerAuthorizationResponse[]> GetResponses(UUID requestId);
        
        Task<Result> Request(OwnerAuthorizationRequest request);
        Task<Result> Respond(OwnerAuthorizationResponse response);
        
        Task<Result> SetOwner(DatasetId datasetId, UUID principalId);
    }
}
