using System;
using System.Threading.Tasks;
using Optima.Interfaces;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Optima.Domain.Core;
using Optima.Domain.DatasetDefinition;
// ReSharper disable ClassNeverInstantiated.Global

namespace Optima.Actors.Actors
{
    [Actor(TypeName = ActorTypes.DatasetEntry)]
    public class DatasetEntryActor: StatefulActorBase<DatasetInfo>, IDatasetEntry //, IRemindable
    {
        /// <summary>
        /// Initializes a new instance of MyActor
        /// </summary>
        /// <param name="actorService">The Dapr.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Dapr.Actors.ActorId for this actor instance.</param>
        public DatasetEntryActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId, "dataset_info", () => null)
        {
        }
        
        /// <summary>
        /// Set MyData into actor's private state store
        /// </summary>
        /// <param name="data">the user-defined MyData which will be stored into state store as "my_data" state</param>
        public async Task<Result> SetDataAsync(DatasetInfo data)
        {
            State = data;
            await SetStateAsync();
            
            return Result.SUCCESS;
        }
        
        /// <summary>
        /// Set MyData into actor's private state store
        /// </summary>
        /// <param name="data">the user-defined MyData which will be stored into state store as "my_data" state</param>
        public async Task<Result> DeleteDataAsync()
        {
            // Data is saved to configured state store implicitly after each method execution by Actor's runtime.
            // Data can also be saved explicitly by calling this.StateManager.SaveStateAsync();
            // State to be saved must be DataContract serializable.
            await StateManager.RemoveStateAsync(StateName);  // state name
            
            State = null;

            return Result.SUCCESS;
        }

        /// <summary>
        /// Get MyData from actor's private state store
        /// </summary>
        /// <return>the user-defined MyData which is stored into state store as "my_data" state</return>
        public Task<DatasetInfo> GetDataAsync() => GetStateAsync();

        // /// <summary>
        // /// Register MyReminder reminder with the actor
        // /// </summary>
        // public async Task RegisterReminder()
        // {
        //     await RegisterReminderAsync(
        //         "MyReminder",              // The name of the reminder
        //         null,                      // User state passed to IRemindable.ReceiveReminderAsync()
        //         TimeSpan.FromSeconds(5),   // Time to delay before invoking the reminder for the first time
        //         TimeSpan.FromSeconds(5));  // Time interval between reminder invocations after the first invocation
        // }
        //
        // /// <summary>
        // /// Unregister MyReminder reminder with the actor
        // /// </summary>
        // public Task UnregisterReminder()
        // {
        //     Logger.LogInformation("Unregistering MyReminder...");
        //     return UnregisterReminderAsync("MyReminder");
        // }
        //
        // // <summary>
        // // Implement IRemindeable.ReceiveReminderAsync() which is call back invoked when an actor reminder is triggered.
        // // </summary>
        // public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        // {
        //     Logger.LogInformation("ReceiveReminderAsync is called!");
        //     return Task.CompletedTask;
        // }
        //
        // /// <summary>
        // /// Register MyTimer timer with the actor
        // /// </summary>
        // public Task RegisterTimer()
        // {
        //     return RegisterTimerAsync(
        //         "MyTimer",                  // The name of the timer
        //         OnTimerCallBack,       // Timer callback
        //         null,                       // User state passed to OnTimerCallback()
        //         TimeSpan.FromSeconds(5),    // Time to delay before the async callback is first invoked
        //         TimeSpan.FromSeconds(5));   // Time interval between invocations of the async callback
        // }
        //
        // /// <summary>
        // /// Unregister MyTimer timer with the actor
        // /// </summary>
        // public Task UnregisterTimer()
        // {
        //     Logger.LogInformation("Unregistering MyTimer...");
        //     return UnregisterTimerAsync("MyTimer");
        // }
        //
        // /// <summary>
        // /// Timer callback once timer is expired
        // /// </summary>
        // private Task OnTimerCallBack(object data)
        // {
        //     Logger.LogInformation("OnTimerCallBack is called!");
        //     return Task.CompletedTask;
        // }
    }
}