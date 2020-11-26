using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
using Optima.Domain.Ownership;
using Optima.Domain.Security;

// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.OwnershipService)]
    public class OwnershipServiceActor: StatefulActorBase<OwnershipServiceActor.ServiceState>, IOwnershipService
    {
        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public OwnershipServiceActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, "ownership_service", 
                () => new ServiceState
                {
                    CurrentOwners = new Dictionary<DatasetId, DatasetOwner>(),
                    OwnershipHistory = new Dictionary<DatasetId, DatasetOwnerHistory>(),
                    RequestHistory = new Dictionary<DatasetId, HashSet<OwnerAuthorizationRequest>>(),
                    ResponseHistory = new Dictionary<DatasetId, HashSet<OwnerAuthorizationResponse>>()
                })
        {
        }
        
        public class ServiceState
        {
            public Dictionary<DatasetId, DatasetOwner> CurrentOwners { get; set; } 
            public Dictionary<DatasetId, DatasetOwnerHistory> OwnershipHistory { get; set; } 
            public Dictionary<DatasetId, HashSet<OwnerAuthorizationRequest>> RequestHistory { get; set; } 
            public Dictionary<DatasetId, HashSet<OwnerAuthorizationResponse>> ResponseHistory { get; set; } 
        }

        public Task<DatasetOwner[]> GetOwners() => 
            Task.FromResult(State.CurrentOwners.Values.Distinct().ToArray());

        public Task<DatasetOwner> GetOwner(DatasetId datasetId) =>
            Task.FromResult(State.CurrentOwners.TryGetValue(datasetId, out var owner) ? owner : null);

        public Task<DatasetOwnerHistory> GetOwnerHistory(DatasetId datasetId) => 
            Task.FromResult(State.OwnershipHistory.TryGetValue(datasetId, out var history) ? history : null);

        public Task<OwnerAuthorizationRequest[]> GetRequestHistory(DatasetId datasetId) => 
            Task.FromResult(State.RequestHistory.TryGetValue(datasetId, out var history) 
                ? history.ToArray() 
                : new OwnerAuthorizationRequest[0]);

        public async Task<OwnerAuthorizationRequest[]> GetPrincipalRequestHistory(DatasetId datasetId, UUID principal)
        {
            var datasets = State.CurrentOwners.Values.Where(o => o.Owner.Id.Equals(principal)).Select(o => o.Dataset).ToArray();
            return (await GetRequestHistory(datasetId)).Where(r => datasets.Contains(r.DatasetId)).ToArray();
        }

        public async Task<OwnerAuthorizationRequest[]> GetActiveRequests(DatasetId datasetId) =>
            (await GetRequestHistory(datasetId)).Where(r => !r.IsResponded).ToArray();

        public async Task<OwnerAuthorizationRequest[]> GetPrincipalActiveRequests(DatasetId datasetId, UUID principal) =>
            (await GetPrincipalRequestHistory(datasetId, principal)).Where(r => !r.IsResponded).ToArray();

        public Task<OwnerAuthorizationResponse[]> GetResponseHistory(DatasetId datasetId) => 
            Task.FromResult(State.ResponseHistory.TryGetValue(datasetId, out var history) 
                ? history.ToArray() 
                : new OwnerAuthorizationResponse[0]);

        public async Task<OwnerAuthorizationResponse[]> GetPrincipalResponseHistory(DatasetId datasetId, UUID principal)
        {
            var datasets = State.CurrentOwners.Values.Where(o => o.Owner.Id.Equals(principal)).Select(o => o.Dataset).ToArray();
            return (await GetResponseHistory(datasetId)).Where(r => datasets.Contains(r.Request.DatasetId)).ToArray();
        }

        public Task<OwnerAuthorizationRequest> GetRequest(UUID requestId) => 
            Task.FromResult(State.RequestHistory.Values.SelectMany(rh => rh).FirstOrDefault(r => r.Id.Equals(requestId)));

        public Task<OwnerAuthorizationResponse[]> GetResponses(UUID requestId)=> 
            Task.FromResult(State.ResponseHistory.Values.SelectMany(rh => rh).Where(r => r.Request.Id.Equals(requestId)).ToArray());


        public async Task<Result> Request(OwnerAuthorizationRequest request)
        {
            // TODO: Validate requestor's identity

            if (!State.RequestHistory.TryGetValue(request.DatasetId, out var requests))
            {
                requests = new [] { request }.ToHashSet();
                State.RequestHistory[request.DatasetId] = requests;
            }

            if (!requests.Any(r => r.Id.Equals(request.Id)))
            {
                requests.Add(request);
            }

            await SetStateAsync();
            
            // TODO: Update createdAt fields
            // TODO: Implement Owner notification
            return Result.SUCCESS;
        }

        public async Task<Result> Respond(OwnerAuthorizationResponse response)
        {
            // TODO: Validate owner's identity
            
            if (!State.ResponseHistory.TryGetValue(response.Request.DatasetId, out var responses))
            {
                responses = new [] { response }.ToHashSet();
                State.ResponseHistory[response.Request.DatasetId] = responses;
            }

            responses.Add(response);

            await SetStateAsync();
            
            // TODO: Update createdAt fields
            // TODO: Implement requestor and requestee notifications
            return Result.SUCCESS;
        }

        public async Task<Result> SetOwner(DatasetId datasetId, UUID principalId)
        {
            // TODO: Validate caller's identity

            var principal = await SecurityRegistryActorProxy.GetPrincipal(principalId);
            if (principal == null)
            {
                var reason = $"Principal with Id: '{principalId}' wasn't found";
                Logger.LogInformation(reason);
                return Result.FAILURE(reason);
            }

            if (State.CurrentOwners.TryGetValue(datasetId, out var currentOwner))
            {
                if (!currentOwner.Owner.Id.Equals(principalId))
                {
                    currentOwner.WithdrawnAt = Timestamp.FromDateTimeOffset(DateTimeOffset.Now);
                    if (State.OwnershipHistory.TryGetValue(datasetId, out var history))
                    {
                        history.History.Add(currentOwner);
                    }
                    else
                    {
                        State.OwnershipHistory[datasetId] = new DatasetOwnerHistory { History = { currentOwner } };
                    }
                    State.CurrentOwners[datasetId] = new DatasetOwner { Dataset = datasetId, Owner = principal, AssignedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.Now) };
                    await SetStateAsync();
                }
            }
            else
            {
                State.CurrentOwners[datasetId] = new DatasetOwner { Dataset = datasetId, Owner = principal, AssignedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.Now) };
                State.CurrentOwners[datasetId] = currentOwner;
                await SetStateAsync();
            }
            return Result.SUCCESS;
        }
        
        private static ISecurityRegistry SecurityRegistryActorProxy => 
            ActorProxy.Create<ISecurityRegistry>(new ActorId("default"), ActorTypes.SecurityRegistry);
    }
}