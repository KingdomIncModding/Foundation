using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Flow;
using KingdomFoundation.Api;
using KingdomFoundation.Machines;

namespace KingdomFoundation
{
    /// <summary>Provides the foundation panel registry API.</summary>
    public sealed class FoundationPanelRegistry : IFoundationPanelRegistry
    {
        private readonly Dictionary<(string, string), PanelRegistration> panels =
            new Dictionary<(string, string), PanelRegistration>();

        public IReadOnlyList<IFoundationPanelRegistration> Registrations => panels.Values
            .OrderBy(p => p.ModId, StringComparer.Ordinal)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .Cast<IFoundationPanelRegistration>().ToArray();

        /// <summary>Occurs when changed is raised.</summary>
        public event Action Changed;
        public bool IsOpen => FoundationPanelHost.IsShowing;

        public IFoundationPanelRegistration Register(FoundationPanelDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var key = (definition.ModId, definition.Id);
            if (panels.ContainsKey(key)) throw new ArgumentException("Duplicate panel owner + ID: " + definition.ModId + "/" + definition.Id);
            var token = new PanelRegistration(this, definition);
            panels.Add(key, token);
            Notify();
            return token;
        }

        /// <summary>Attempts to get.</summary>
        public bool TryGet(string modId, string id, out FoundationPanelDefinition panel)
        {
            panel = null;
            if (modId == null || id == null || !panels.TryGetValue((modId, id), out var token)) return false;
            panel = token.Definition;
            return true;
        }

        /// <summary>Attempts to resolve machine.</summary>
        public bool TryResolveMachine(Type nativeMachineType, out FoundationPanelDefinition panel)
        {
            if (nativeMachineType == null) throw new ArgumentNullException(nameof(nativeMachineType));
            if (!typeof(FlowElement).IsAssignableFrom(nativeMachineType))
                throw new ArgumentException("Native FlowElement type required.", nameof(nativeMachineType));
            panel = panels.Values.Select(entry => entry.Definition)
                .Where(item => item.MachineType != null && item.MachineType.IsAssignableFrom(nativeMachineType))
                .OrderBy(item => Distance(nativeMachineType, item.MachineType))
                .ThenByDescending(item => item.Priority)
                .ThenBy(item => item.ModId, StringComparer.Ordinal)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            return panel != null;
        }

        private static int Distance(Type actual, Type candidate)
        {
            var depth = 0;
            for (var type = actual; type != null; type = type.BaseType, depth++)
                if (type == candidate) return depth;
            return int.MaxValue;
        }

        /// <summary>Performs the open operation.</summary>
        public bool Open(string modId, string id)
        {
            MachineThread.Verify();
            if (!TryGet(modId, id, out var definition) || definition.MachineType != null) return false;
            return FoundationPanelHost.Open(definition, null);
        }

        public bool OpenMachine(FlowElement machine)
        {
            MachineThread.Verify();
            if (!MachineThread.IsPlaced(machine))
                throw new InvalidOperationException("The native machine must be placed in the active kingdom.");
            if (!TryResolveMachine(machine.GetType(), out var definition)) return false;
            return FoundationPanelHost.Open(definition, machine);
        }

        /// <summary>Performs the close operation.</summary>
        public void Close()
        {
            MachineThread.Verify();
            FoundationPanelHost.CloseCurrent();
        }

        public int RemoveOwner(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return 0;
            var matches = panels.Values.Where(p => p.ModId == modId).ToArray();
            foreach (var match in matches) match.Dispose();
            return matches.Length;
        }

        /// <summary>Performs the for mod operation.</summary>
        public IFoundationPanelModContext ForMod(string modId) => new PanelModContext(this, modId);

        private void Remove(PanelRegistration token)
        {
            var key = (token.ModId, token.Id);
            if (!panels.TryGetValue(key, out var live) || !ReferenceEquals(live, token)) return;
            panels.Remove(key);
            FoundationPanelHost.CloseIfShowing(token.Definition);
            Notify();
        }

        private void Notify()
        {
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception error) { FoundationApi.Report(error); }
        }

        internal void Clear()
        {
            foreach (var value in panels.Values.ToArray()) value.Dispose();
        }

        private sealed class PanelRegistration : IFoundationPanelRegistration
        {
            private FoundationPanelRegistry registry;
            /// <summary>Gets or sets the mod id value.</summary>
            public string ModId => Definition.ModId;
            public string Id => Definition.Id;
            public bool IsRegistered => registry != null;
            public FoundationPanelDefinition Definition { get; }
            internal PanelRegistration(FoundationPanelRegistry owner, FoundationPanelDefinition definition)
            { registry = owner; Definition = definition; }
            public void Dispose()
            {
                var owner = registry;
                if (owner == null) return;
                registry = null;
                owner.Remove(this);
            }
        }

        private sealed class PanelModContext : IFoundationPanelModContext
        {
            private readonly FoundationPanelRegistry registry;
            private readonly List<IFoundationPanelRegistration> owned = new List<IFoundationPanelRegistration>();
            private bool disposed;
            /// <summary>Gets or sets the mod id value.</summary>
            public string ModId { get; }
            public PanelModContext(FoundationPanelRegistry registry, string modId)
            {
                ModBarRegistry.ValidateIdentity(modId, "owner");
                this.registry = registry;
                ModId = modId;
            }
            public IFoundationPanelRegistration Register(string id, string title,
                Action<IFoundationPanelBuilder> build, Type machineType = null, int priority = 0, bool allowDrag = true)
            {
                if (disposed) throw new ObjectDisposedException(nameof(PanelModContext));
                var token = registry.Register(new FoundationPanelDefinition(ModId, id, title, build, machineType, priority, allowDrag));
                owned.Add(token);
                return token;
            }
            /// <summary>Performs the open operation.</summary>
            public bool Open(string id)
            {
                if (disposed) throw new ObjectDisposedException(nameof(PanelModContext));
                return registry.Open(ModId, id);
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                foreach (var token in owned) token.Dispose();
                owned.Clear();
            }
        }
    }
}
