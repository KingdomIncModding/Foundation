using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    /// <summary>Tracks a consumer's registrations, preventing orphan panels on mod unload.</summary>
    public interface IFoundationPanelModContext : IDisposable
    {
        string ModId { get; }

        IFoundationPanelRegistration Register(string id, string title, Action<IFoundationPanelBuilder> build, Type machineType = null, int priority = 0, bool allowDrag = true);
        bool Open(string id);
    }
}
