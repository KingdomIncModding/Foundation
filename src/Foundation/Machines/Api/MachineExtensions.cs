using System;
using System.Collections.Generic;
using Assets.Source.Flow;

namespace KingdomFoundation.Machines
{
    /// <summary>Registration can be disposed, which removes only its owner's exposed property.</summary>
    public sealed class MachineExtensions
    {
        private sealed class Entry
        {
            public string Owner;
            public MachinePropertyDefinition Definition;
        }

        private sealed class Registration : IDisposable
        {
            private readonly MachineExtensions _registry;
            private readonly string _id;
            private readonly Entry _entry;
            private bool _disposed;
            public Registration(MachineExtensions registry, string id, Entry entry)
            {
                _registry = registry;
                _id = id;
                _entry = entry;
            }

            public void Dispose()
            {
                MachineThread.Verify();
                if (_disposed)
                    return;
                _disposed = true;
                if (_registry._entries.TryGetValue(_id, out Entry current) && ReferenceEquals(current, _entry))
                    _registry._entries.Remove(_id);
            }
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        public IDisposable Register(string modGuid, MachinePropertyDefinition definition)
        {
            MachineThread.Verify();
            if (string.IsNullOrWhiteSpace(modGuid) || modGuid.Contains(":"))
                throw new ArgumentException("Valid mod owner GUID required", nameof(modGuid));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            string key = modGuid + ":" + definition.Id;
            if (_entries.ContainsKey(key))
                throw new InvalidOperationException("Duplicate property id: " + key);
            var entry = new Entry
            {
                Owner = modGuid,
                Definition = definition
            };
            _entries.Add(key, entry);
            return new Registration(this, key, entry);
        }

        public IReadOnlyDictionary<string, object> ReadProperties(FlowElement machine)
        {
            MachineThread.Verify();
            var props = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var native in FoundationMachineApi.Specializations.Read(machine))
                props["native." + native.Key] = native.Value;
            foreach (var pair in _entries)
            {
                if (!pair.Value.Definition.MachineType.IsInstanceOfType(machine))
                    continue;
                try
                {
                    props[pair.Key] = pair.Value.Definition.Read(machine);
                }
                catch (Exception ex)
                {
                    MachineRuntime.ReportCallbackError("Property read " + pair.Key, ex);
                }
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(props);
        }

        public MachineOperationResult TryWrite(string owner, string id, FlowElement machine, object value)
        {
            MachineThread.Verify();
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(id) || machine == null)
                return MachineOperationResult.Reject("Owner, id and native machine required.");
            if (!MachineThread.IsPlaced(machine))
                return MachineOperationResult.Reject("Machine is not placed in active kingdom.");
            string key = owner + ":" + id;
            if (!_entries.TryGetValue(key, out Entry entry))
                return MachineOperationResult.Reject("Unregistered property: " + key);
            if (!entry.Definition.MachineType.IsInstanceOfType(machine))
                return MachineOperationResult.Reject("Property does not apply to this native machine type.");
            if (entry.Definition.Write == null)
                return MachineOperationResult.Reject("Native property is read-only.");
            try
            {
                entry.Definition.Write(machine, value);
                return MachineOperationResult.Ok();
            }
            catch (Exception ex)
            {
                MachineRuntime.ReportCallbackError("Property write " + key, ex);
                return MachineOperationResult.Reject(ex.Message);
            }
        }

        internal void Clear() => _entries.Clear();
    }
}
