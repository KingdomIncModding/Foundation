using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Api
{
    /// <summary>One registry for ordinary mod windows and type-specific machine windows.</summary>
    public interface IFoundationPanelRegistry
    {
        IReadOnlyList<IFoundationPanelRegistration> Registrations { get; }
        event Action Changed;
        IFoundationPanelRegistration Register(FoundationPanelDefinition definition);
        bool TryGet(string modId, string id, out FoundationPanelDefinition panel);
        bool TryResolveMachine(Type nativeMachineType, out FoundationPanelDefinition panel);
        bool Open(string modId, string id);
        /// <summary>Performs the open machine operation.</summary>
        bool OpenMachine(FlowElement machine);
        void Close();
        bool IsOpen { get; }
        int RemoveOwner(string modId);
        IFoundationPanelModContext ForMod(string modId);
    }

    public interface IFoundationPanelRegistration : IDisposable
    {
        /// <summary>Gets or sets the mod id value.</summary>
        string ModId { get; }
        string Id { get; }
        bool IsRegistered { get; }
        FoundationPanelDefinition Definition { get; }
    }

    /// <summary>Tracks a consumer's registrations, preventing orphan panels on mod unload.</summary>
    public interface IFoundationPanelModContext : IDisposable
    {
        string ModId { get; }
        IFoundationPanelRegistration Register(string id, string title, Action<IFoundationPanelBuilder> build,
            Type machineType = null, int priority = 0, bool allowDrag = true);
        bool Open(string id);
    }
}
