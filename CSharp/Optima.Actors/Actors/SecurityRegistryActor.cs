using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using Optima.Domain.Core;
using Optima.Domain.Security;

// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.SecurityRegistry)]
    public class SecurityRegistryActor: StatefulActorBase<SecurityRegistryActor.RegistryState>, ISecurityRegistry
    {
        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public SecurityRegistryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, "security_registry", 
                () => new SecurityRegistryActor.RegistryState
                    {
                        Permissions = new Dictionary<UUID, PrincipalPermissions>()
                    })
        {
        }
        
        public async Task<PrincipalPermissions> AddPrincipal(Principal principal)
        {
            if (State.Permissions.TryGetValue(principal.Id, out var permissions))
                return permissions;

            permissions = new PrincipalPermissions {Principal = principal};
            State.Permissions[principal.Id] = permissions;
            await SetStateAsync();
            return permissions;
        }

        public async Task<PrincipalPermissions> AddPrincipalPermissions(PrincipalPermissions principalPermissions)
        {
            if (State.Permissions.TryGetValue(principalPermissions.Principal.Id, out var permissions))
            {
                foreach (var access in principalPermissions.Allowed)
                {
                    if (!permissions.Allowed.Contains(access))
                    {
                        permissions.Allowed.Add(access);
                    }
                }
            }
            else
            {
                permissions = principalPermissions;
            }

            State.Permissions[principalPermissions.Principal.Id] = permissions;
            await SetStateAsync();
            return permissions;
        }

        public async Task<PrincipalPermissions> RevokePrincipalPermissions(PrincipalPermissions principalPermissions)
        {
            if (!State.Permissions.TryGetValue(principalPermissions.Principal.Id, out var permissions))
                return new PrincipalPermissions { Principal = principalPermissions.Principal };

            foreach (var access in principalPermissions.Allowed)
            {
                if (permissions.Allowed.Contains(access))
                {
                    permissions.Allowed.Remove(access);
                }
            }
            
            State.Permissions[principalPermissions.Principal.Id] = permissions;
            await SetStateAsync();
            return permissions;
        }

        public Task<PrincipalPermissions> GetPrincipalPermissions(UUID principalId) => 
            State.Permissions.TryGetValue(principalId, out var permissions) 
                ? Task.FromResult(permissions) 
                : Task.FromResult(new PrincipalPermissions());

        public Task<Permissions> GetPermissions() =>
            Task.FromResult(new Permissions
            {
                Permissions_ = { State.Permissions.Values }
            });

        public async Task<Principal> GetPrincipal(UUID principalId) => 
            (await GetPrincipalPermissions(principalId)).Principal;

        public Task<Principal[]> GetPrincipals() =>
            Task.FromResult(State.Permissions.Values.Select(p => p.Principal).ToArray());

        public class RegistryState
        {
            public Dictionary<UUID, PrincipalPermissions> Permissions { get; set; } 
        }
    }
}