using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Optima.Domain.Core;
using Optima.Domain.Security;

// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.SecurityRegistry)]
    public class SecurityRegistryActor: Actor, ISecurityRegistry
    {
        private const string StateName = "security_registry";
        private RegistryState _state;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public SecurityRegistryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }
        
        private async Task<RegistryState> GetStateAsync()
        {
            var state = await StateManager.TryGetStateAsync<RegistryState>(StateName);
            return state.HasValue
                ? state.Value
                : new RegistryState
                    {
                        Permissions = new Dictionary<UUID, PrincipalPermissions>()
                    };
        }
        
        private Task SaveStateAsync() => StateManager.SetStateAsync(StateName, _state);

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            // Provides opportunity to perform some optional setup.
            Console.WriteLine($"Activating actor id: {Id}");
            _state = await GetStateAsync();
            Console.WriteLine($"Loaded state: {_state}");
        }

        /// <summary>
        /// This method is called whenever an actor is deactivated after a period of inactivity.
        /// </summary>
        protected override Task OnDeactivateAsync()
        {
            // Provides Opportunity to perform optional cleanup.
            Console.WriteLine($"Deactivating actor id: {Id}");
            return Task.CompletedTask;
        }
        
        public async Task<PrincipalPermissions> AddPrincipal(Principal principal)
        {
            if (_state.Permissions.TryGetValue(principal.Id, out var permissions))
                return permissions;

            permissions = new PrincipalPermissions {Principal = principal};
            _state.Permissions[principal.Id] = permissions;
            await SaveStateAsync();
            return permissions;
        }

        public async Task<PrincipalPermissions> AddPrincipalPermissions(PrincipalPermissions principalPermissions)
        {
            if (_state.Permissions.TryGetValue(principalPermissions.Principal.Id, out var permissions))
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

            _state.Permissions[principalPermissions.Principal.Id] = permissions;
            await SaveStateAsync();
            return permissions;
        }

        public async Task<PrincipalPermissions> RevokePrincipalPermissions(PrincipalPermissions principalPermissions)
        {
            if (!_state.Permissions.TryGetValue(principalPermissions.Principal.Id, out var permissions))
                return new PrincipalPermissions { Principal = principalPermissions.Principal };

            foreach (var access in principalPermissions.Allowed)
            {
                if (permissions.Allowed.Contains(access))
                {
                    permissions.Allowed.Remove(access);
                }
            }
            
            _state.Permissions[principalPermissions.Principal.Id] = permissions;
            await SaveStateAsync();
            return permissions;
        }

        public Task<PrincipalPermissions> GetPrincipalPermissions(UUID principalId) => 
            _state.Permissions.TryGetValue(principalId, out var permissions) 
                ? Task.FromResult(permissions) 
                : Task.FromResult(new PrincipalPermissions());

        public Task<Permissions> GetPermissions() =>
            Task.FromResult(new Permissions
            {
                Permissions_ = { _state.Permissions.Values }
            });

        public async Task<Principal> GetPrincipal(UUID principalId) => 
            (await GetPrincipalPermissions(principalId)).Principal;

        public Task<Principal[]> GetPrincipals() =>
            Task.FromResult(_state.Permissions.Values.Select(p => p.Principal).ToArray());
        
        private class RegistryState
        {
            public Dictionary<UUID, PrincipalPermissions> Permissions { get; set; } 
        }
    }
}