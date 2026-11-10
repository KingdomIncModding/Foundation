using System;
using Assets.Source.Player;

namespace KingdomFoundation.Api
{
    internal sealed class FoundationLifecycleService : IFoundationLifecycleService
    {
        /// <summary>Gets or sets the last phase value.</summary>
        public FoundationLifecyclePhase? LastPhase { get; private set; }
        public event Action<FoundationLifecycleEvent> Changed;
        internal void Raise(FoundationLifecyclePhase phase, GamePlayer player)
        {
            LastPhase = phase;
            var notice = new FoundationLifecycleEvent(phase, player);
            FoundationApi.Events.Publish(notice);
            if (Changed == null) return;
            foreach (Action<FoundationLifecycleEvent> callback in Changed.GetInvocationList())
                try { callback(notice); } catch (Exception ex) { FoundationApi.Report(ex); }
        }
    }
}
