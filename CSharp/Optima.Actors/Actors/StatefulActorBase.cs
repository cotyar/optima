using System;
using System.Threading.Tasks;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;

namespace Optima.Actors.Actors
{
    public abstract class StatefulActorBase<TState>: Actor
    {
        private readonly string _stateName;
        private readonly Func<TState> _emptyStateFactory;
        protected TState State;

        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        /// <param name="stateName"></param>
        /// <param name="emptyStateFactory"></param>
        protected StatefulActorBase(ActorService actorService, ActorId actorId,
            string stateName, Func<TState> emptyStateFactory)
            : base(actorService, actorId)
        {
            _stateName = stateName;
            _emptyStateFactory = emptyStateFactory;
        }
        
        private async Task<TState> GetStateAsync()
        {
            var state = await StateManager.TryGetStateAsync<TState>(_stateName);
            var ret = state.HasValue
                ? state.Value
                : _emptyStateFactory();
            State = ret;
            return ret;
        }

        protected Task SetStateAsync() => StateManager.SetStateAsync(_stateName, State);
        
        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            State = await GetStateAsync();
            Logger.LogDebug($"Loaded state: {State}");
        }
    }
}