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
        bool OpenMachine(FlowElement machine);
        void Close();
        bool IsOpen { get; }

        int RemoveOwner(string modId);
        IFoundationPanelModContext ForMod(string modId);
    }
}
