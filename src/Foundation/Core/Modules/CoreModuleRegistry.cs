using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Source.Player;

namespace KingdomFoundation.Core
{
    internal sealed class CoreModuleRegistry : ICoreModules
    {
        readonly object gate = new object();
        readonly Dictionary<string, ModRegistration> modules = new Dictionary<string, ModRegistration>(StringComparer.Ordinal);
        readonly CoreDiagnostics diagnostics;
        readonly CoreServiceRegistry services;
        readonly CoreContentCatalog content;
        readonly CoreDispatcher dispatcher;
        readonly CoreEventBus events;
        readonly CorePatchManager patches;
        readonly ModuleDependencyResolver resolver;
        long sequence;
        GamePlayer currentWorld;
        bool stopped;
        internal CoreModuleRegistry(CoreDiagnostics diagnostics, CoreServiceRegistry services,
            CoreEventBus events, CorePatchManager patches, CoreContentCatalog content, CoreDispatcher dispatcher, ICorePluginCatalog plugins)
        {
            this.diagnostics = diagnostics;
            this.services = services;
            this.content = content;
            this.dispatcher = dispatcher;
            this.events = events;
            this.patches = patches;
            resolver = new ModuleDependencyResolver(plugins);
        }
        public IReadOnlyList<ModSnapshot> Registered
        {
            get { lock (gate) return Array.AsReadOnly(modules.Values.Select(x => x.Snapshot()).OrderBy(x => x.Descriptor.Id, StringComparer.Ordinal).ToArray()); }
        }
        /// <summary>Performs the register operation.</summary>
        public IModRegistration Register(IFoundationModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            var descriptor = module.Descriptor ?? throw new ArgumentException("Module descriptor required.", nameof(module));
            if (descriptor.MinimumCoreApiVersion != null &&
                !CoreApiVersion.Supports(descriptor.MinimumCoreApiVersion))
                throw new NotSupportedException("Module requires incompatible Core API: " + descriptor.MinimumCoreApiVersion);
            lock (gate)
            {
                if (stopped) throw new ObjectDisposedException(nameof(CoreModuleRegistry));
                if (modules.ContainsKey(descriptor.Id))
                    throw new InvalidOperationException("Module ID already registered: " + descriptor.Id);
                var registration = new ModRegistration(this, module, descriptor);
                modules.Add(descriptor.Id, registration);
                return registration;
            }
        }
        /// <summary>Attempts to get.</summary>
        public bool TryGet(string modId, out ModSnapshot snapshot)
        {
            lock (gate)
            {
                if (modId != null && modules.TryGetValue(modId, out var module))
                { snapshot = module.Snapshot(); return true; }
            }
            snapshot = null;
            return false;
        }
        internal void Tick(GamePlayer world)
        {
            if (stopped) return;
            // Native world changes must be observed BEFORE late module activation:
            // otherwise a newly initialized module briefly receives callbacks
            // for an already-unloaded GamePlayer from the previous tick.
            if (!ReferenceEquals(currentWorld, world)) ChangeWorld(world);
            StartEligible();
        }
        void StartEligible()
        {
            // Revisit pending entries after each successful activation; this provides
            // deterministic topological order and supports late registrations.
            for (;;)
            {
                ModRegistration[] pending;
                Dictionary<string, ModRegistration> snapshot;
                lock (gate)
                {
                    pending = modules.Values.Where(x => x.State == ModState.Registered ||
                        x.State == ModState.WaitingForDependencies).OrderBy(x => x.Descriptor.Id, StringComparer.Ordinal).ToArray();
                    snapshot = new Dictionary<string, ModRegistration>(modules, StringComparer.Ordinal);
                }
                bool activated = false;
                foreach (var item in pending)
                {
                    if (item.State == ModState.Disposed) continue;
                    var problem = resolver.DetectCycle(item.Descriptor.Id, snapshot) ?? resolver.PendingReason(item.Descriptor, snapshot);
                    if (problem != null) { ChangeState(item, ModState.WaitingForDependencies, problem); continue; }
                    Start(item);
                    activated = true;
                    break;
                }
                if (!activated) return;
            }
        }
        void Start(ModRegistration entry)
        {
            ChangeState(entry, ModState.Starting, "Initializing.");
            var scope = new ModScope(entry.Descriptor.Id, diagnostics);
            entry.Scope = scope;
            entry.Context = new ModContext(entry.Descriptor.Id, services, events, patches, content, dispatcher, diagnostics, scope);
            content.ActivateOwner(entry.Descriptor.Id);
            try
            {
                entry.Module.Initialize(entry.Context);
                if (entry.State != ModState.Starting) return; // module disposed itself during Initialize
                entry.ActivationSequence = ++sequence;
                ChangeState(entry, ModState.Active, "Active.");
                if (currentWorld != null) LoadWorld(entry, currentWorld);
            }
            catch (Exception ex)
            {
                diagnostics.Record(CoreSeverity.Error, entry.Descriptor.Id, "initialize", "Module initialization failed.", ex);
                Release(entry);
                ChangeState(entry, ModState.Failed, ex.Message);
            }
        }
        void ChangeWorld(GamePlayer next)
        {
            var previous = currentWorld;
            var ordered = ActiveReverse();
            if (previous != null)
            {
                foreach (var item in ordered) UnloadWorld(item, previous);
                services.ReleaseWorld();
                content.ReleaseWorld();
            }
            currentWorld = next;
            events.Publish(new WorldChangedEvent(previous, next));
            if (next != null)
                foreach (var item in ActiveForward()) LoadWorld(item, next);
        }
        void LoadWorld(ModRegistration item, GamePlayer world)
        {
            if (item.State != ModState.Active || item.HasWorld) return;
            item.HasWorld = true;
            if (!(item.Module is IFoundationWorldModule callback)) return;
            try { callback.WorldLoaded(world); }
            catch (Exception ex) { diagnostics.Record(CoreSeverity.Error, item.Descriptor.Id, "world-load", "Module world-load callback failed.", ex); }
        }
        void UnloadWorld(ModRegistration item, GamePlayer world)
        {
            if (!item.HasWorld) return;
            item.HasWorld = false;
            if (!(item.Module is IFoundationWorldModule callback)) return;
            try { callback.WorldUnloading(world); }
            catch (Exception ex) { diagnostics.Record(CoreSeverity.Error, item.Descriptor.Id, "world-unload", "Module world-unload callback failed.", ex); }
        }
        ModRegistration[] ActiveForward()
        {
            lock (gate) return modules.Values.Where(x => x.State == ModState.Active)
                .OrderBy(x => x.ActivationSequence).ToArray();
        }
        ModRegistration[] ActiveReverse() => ActiveForward().AsEnumerable().Reverse().ToArray();
        void ChangeState(ModRegistration entry, ModState state, string reason)
        {
            if (entry.State == state && entry.Message == reason) return;
            entry.SetState(state, reason);
            events.Publish(new ModuleStateChangedEvent(entry.Snapshot()));
        }
        /// <summary>Performs the unregister operation.</summary>
        public void Unregister(string modId)
        {
            ModRegistration item;
            lock (gate)
            {
                if (!modules.TryGetValue(modId, out item)) return;
            }
            Unregister(item);
        }
        internal void Unregister(ModRegistration item)
        {
            var modId = item.Descriptor.Id;
            lock (gate)
            {
                if (!modules.TryGetValue(modId, out var current) || !ReferenceEquals(item, current) ||
                    item.State == ModState.Stopping || item.State == ModState.Disposed) return;
            }
            // Dependents must stop in reverse activation order, but remain
            // REGISTERED so a provider re-registration can restart them.
            // A dependency can be transitive (A -> B -> C); walk the closure.
            var affected = DependentClosure(modId);
            foreach (var dependent in ActiveReverse())
                if (affected.Contains(dependent.Descriptor.Id))
                {
                    Stop(dependent);
                    ChangeState(dependent, ModState.WaitingForDependencies,
                        "Required Foundation module '" + modId + "' was unregistered.");
                }
            Stop(item);
            lock (gate)
                if (modules.TryGetValue(modId, out var stillRegistered) && ReferenceEquals(item, stillRegistered))
                    modules.Remove(modId);
            ChangeState(item, ModState.Disposed, "Unregistered.");
        }
        HashSet<string> DependentClosure(string providerId)
        {
            ModRegistration[] snapshot;
            lock (gate) snapshot = modules.Values.ToArray();
            var affected = new HashSet<string>(StringComparer.Ordinal) { providerId };
            bool changed;
            do
            {
                changed = false;
                foreach (var entry in snapshot)
                {
                    if (affected.Contains(entry.Descriptor.Id)) continue;
                    if (entry.Descriptor.Dependencies.Any(d =>
                        !d.Optional && d.Kind == DependencyKind.FoundationModule && affected.Contains(d.Id)))
                        changed |= affected.Add(entry.Descriptor.Id);
                }
            } while (changed);
            affected.Remove(providerId);
            return affected;
        }
        void Stop(ModRegistration entry)
        {
            if (entry.State == ModState.Active || entry.State == ModState.Starting)
            {
                if (currentWorld != null) UnloadWorld(entry, currentWorld);
                ChangeState(entry, ModState.Stopping, "Stopping.");
                try { entry.Module.Shutdown(); }
                catch (Exception ex) { diagnostics.Record(CoreSeverity.Error, entry.Descriptor.Id, "shutdown", "Module shutdown callback failed.", ex); }
            }
            Release(entry);
            ChangeState(entry, ModState.Stopped, "Stopped.");
        }
        void Release(ModRegistration entry)
        {
            try { entry.Scope?.Dispose(); }
            finally
            {
                services.ReleaseOwner(entry.Descriptor.Id);
                content.ReleaseOwner(entry.Descriptor.Id);
                events.ReleaseOwner(entry.Descriptor.Id);
                dispatcher.CancelOwner(entry.Descriptor.Id);
                patches.ReleaseOwner(entry.Descriptor.Id);
            }
        }
        internal void StopAll()
        {
            if (stopped) return;
            stopped = true;
            if (currentWorld != null) ChangeWorld(null);
            ModRegistration[] snapshot;
            lock (gate) snapshot = modules.Values.OrderByDescending(x => x.ActivationSequence).ToArray();
            foreach (var entry in snapshot) Stop(entry);
            lock (gate) modules.Clear();
        }
    }
}
