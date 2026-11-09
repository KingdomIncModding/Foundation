using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed partial class SignalRegistry
    {
        sealed class Property : ISignalProperty
        {
            readonly Source source;
            readonly SignalPropertyDefinition def;
            object last;
            bool known;
            public string Id => def.Id;
            public string Name => def.Name;
            public Type ValueType => def.ValueType;
            public bool CanWrite => def.Setter != null;
            public event Action<SignalPropertyChanged> Changed;
            internal Property(Source source, SignalPropertyDefinition def) { this.source = source; this.def = def; }
            public object Read()
            {
                if (!source.IsRegistered) throw new ObjectDisposedException(nameof(ISignalSource));
                var value = def.Getter();
                if (value != null && !def.ValueType.IsInstanceOfType(value))
                    throw new InvalidOperationException("Signal getter returned wrong type: " + source.Id + "/" + Id);
                return value;
            }
            public void Write(object value)
            {
                if (!CanWrite) throw new InvalidOperationException("Signal property is read-only: " + Id);
                if (!source.IsRegistered) throw new ObjectDisposedException(nameof(ISignalSource));
                if (value == null && def.ValueType != typeof(string)) throw new ArgumentNullException(nameof(value));
                if (value != null && !def.ValueType.IsInstanceOfType(value)) throw new ArgumentException("Value must be " + def.ValueType.Name, nameof(value));
                var before = Read();
                def.Setter(value);
                var after = Read();
                known = true; last = after;
                if (!Equals(before, after)) Notify(before, after);
            }
            internal void Poll()
            {
                object current;
                try { current = Read(); }
                catch (Exception ex) { FoundationApi.Report(new InvalidOperationException("Signal getter failed: " + source.Id + "/" + Id, ex)); return; }
                if (!known) { known = true; last = current; return; }
                if (Equals(last, current)) return;
                var before = last; last = current; Notify(before, current);
            }
            void Notify(object before, object after)
            {
                var ev = new SignalPropertyChanged(source.Id, Id, before, after);
                FoundationApi.Events.Publish(ev);
                if (Changed == null) return;
                foreach (Action<SignalPropertyChanged> handler in Changed.GetInvocationList())
                    try { handler(ev); } catch (Exception ex) { FoundationApi.Report(ex); }
            }
        }
    }

}
