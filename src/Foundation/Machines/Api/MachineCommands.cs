using System;
using System.Collections.Generic;
using Assets.Source.Flow;
namespace KingdomFoundation.Machines
{
    /// <summary>A consumer-owned action routed to existing game-native methods by the registering mod.</summary>
    public sealed class MachineCommandDefinition
    {
        public string Id { get; }
        public Type MachineType { get; }
        public Func<FlowElement,object,MachineOperationResult> InvokeNative { get; }
        public MachineCommandDefinition(string id,Type machineType,Func<FlowElement,object,MachineOperationResult> invokeNative)
        {
            if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Command ID required",nameof(id));
            if(machineType==null || !typeof(FlowElement).IsAssignableFrom(machineType)) throw new ArgumentException("Native FlowElement-derived type required",nameof(machineType));
            Id=id;MachineType=machineType;InvokeNative=invokeNative??throw new ArgumentNullException(nameof(invokeNative));
        }
    }
    /// <summary>Owner-tagged extension commands; never overrides the native game update loop.</summary>
    public sealed class MachineCommands
    {
        private sealed class Entry { public string Owner;public MachineCommandDefinition Definition; }
        private sealed class Token : IDisposable
        {
            private readonly MachineCommands _registry;
            private readonly string _id;
            private readonly Entry _entry;
            private bool _disposed;
            public Token(MachineCommands registry,string id,Entry entry) { _registry=registry;_id=id;_entry=entry; }
            /// <summary>Performs the dispose operation.</summary>
            public void Dispose()
            {
                MachineThread.Verify();
                if(_disposed) return;_disposed=true;
                if(_registry._commands.TryGetValue(_id,out Entry current) && ReferenceEquals(current,_entry)) _registry._commands.Remove(_id);
            }
        }
        private readonly Dictionary<string,Entry> _commands=new Dictionary<string,Entry>(StringComparer.Ordinal);
        /// <summary>Performs the register operation.</summary>
        public IDisposable Register(string modGuid, MachineCommandDefinition command)
        {
            MachineThread.Verify();
            if(string.IsNullOrWhiteSpace(modGuid) || modGuid.Contains(":")) throw new ArgumentException("Valid mod GUID required",nameof(modGuid));
            if(command==null) throw new ArgumentNullException(nameof(command));
            string id=modGuid+":"+command.Id;
            if(_commands.ContainsKey(id)) throw new InvalidOperationException("Duplicate extension command: "+id);
            var entry=new Entry { Owner=modGuid,Definition=command };
            _commands.Add(id,entry);
            return new Token(this,id,entry);
        }
        /// <summary>Performs the for machine operation.</summary>
        public IReadOnlyList<string> ForMachine(FlowElement native)
        {
            MachineThread.Verify();
            if(!MachineThread.IsPlaced(native)) throw new InvalidOperationException("Native machine must be placed in active kingdom.");
            var list=new List<string>();
            foreach(var pair in _commands)
                if(pair.Value.Definition.MachineType.IsInstanceOfType(native)) list.Add(pair.Key);
            return list.AsReadOnly();
        }
        /// <summary>Performs the execute operation.</summary>
        public MachineOperationResult Execute(string qualifiedCommandId,FlowElement native,object argument)
        {
            MachineThread.Verify();
            if(native==null || !MachineThread.IsPlaced(native)) return MachineOperationResult.Reject("Native machine is absent or not placed.");
            if(string.IsNullOrWhiteSpace(qualifiedCommandId) || !_commands.TryGetValue(qualifiedCommandId,out Entry entry))
                return MachineOperationResult.Reject("Unknown owner-qualified extension command.");
            if(!entry.Definition.MachineType.IsInstanceOfType(native)) return MachineOperationResult.Reject("Command not compatible with native machine type.");
            try {return entry.Definition.InvokeNative(native,argument) ?? MachineOperationResult.Reject("Extension did not return an operation result.");}
            catch(Exception ex) {MachineRuntime.ReportCallbackError("Extension command "+qualifiedCommandId,ex);return MachineOperationResult.Reject(ex.Message);}
        }
        internal void Clear()=>_commands.Clear();
    }
}
