using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    /// <summary>Owns a mod's registrations and subscriptions; dispose during BepInEx OnDestroy.</summary>
    public sealed class FoundationModContext : IDisposable
    {
        readonly List<IDisposable> registrations = new List<IDisposable>();
        bool disposed;
        public string ModId { get; }
        public FoundationModContext(string modId)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            ModId = modId;
        }
        T Track<T>(T registration) where T : IDisposable
        {
            if (disposed) { registration.Dispose(); throw new ObjectDisposedException(nameof(FoundationModContext)); }
            registrations.Add(registration); return registration;
        }
        public IDisposable Listen<T>(Action<T> handler) => Track(FoundationApi.Events.Subscribe(ModId, handler));
        /// <summary>Run an owner-scoped action each Unity frame.</summary>
        public IDisposable EachFrame(Action<float> callback) => Track(FoundationApi.Runtime.EachFrame(ModId, callback));
        /// <summary>Register a shortcut that is automatically unbound when the context is disposed.</summary>
        public IDisposable BindKey(UnityEngine.InputSystem.Key key, Action handler, bool allowWhileTyping = false)
            => Track(FoundationApi.Runtime.KeyPressed(ModId, key, handler, allowWhileTyping));
        public bool GetFlag(string key, bool defaultValue = false) => FoundationApi.Flags.HasFlag(ModId, key, defaultValue);
        public void SetFlag(string key, bool value = true) => FoundationApi.Flags.SetFlag(ModId, key, value);
        public int GetCounter(string key, int defaultValue = 0) => FoundationApi.Flags.GetCounter(ModId, key, defaultValue);
        public int AddCounter(string key, int amount = 1) => FoundationApi.Flags.AddCounter(ModId, key, amount);
        public string GetString(string key, string defaultValue = null) => FoundationApi.Flags.GetString(ModId, key, defaultValue);
        public void SetString(string key, string value) => FoundationApi.Flags.SetString(ModId, key, value);

        /// <summary>Owner-scoped logical binding. Disposed with the mod and never alters native pipes.</summary>
        public IFoundationSignalLink Link(SignalSelection source, SignalSelection destination)
            => Track(FoundationApi.SignalLinks.Connect(ModId, source, destination));
        /// <summary>Use Foundation's native-themed panel registry; not a separate UI implementation.</summary>
        public IFoundationPanelRegistration RegisterPanel(FoundationPanelDefinition definition)
        {
            EnsureActive();
            if (definition == null || definition.ModId != ModId)
                throw new ArgumentException("Panel must have this context's ModId.", nameof(definition));
            return Track(FoundationApi.Panels.Register(definition));
        }
        /// <summary>Manually own an externally-created disposable token or UI registration.</summary>
        public T Own<T>(T resource) where T : IDisposable => Track(resource);
        public ISignalSource ExposeNative(string id, string label, Type nativeType,
            Func<object> target, IEnumerable<string> members, bool writable = false)
            => Track(FoundationApi.NativeSignals.Expose(ModId, id, label, nativeType, target, members, writable));
        public ISignalSource Expose(SignalSourceDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.ModId != ModId) throw new ArgumentException("Source ownership does not match the mod context.", nameof(definition));
            return Track(FoundationApi.Signals.Register(definition));
        }
        /// <summary>Loads sprite.</summary>
        public IFoundationAsset LoadSprite(string id, string absolutePngPath, float pixelsPerUnit = 100f)
            => Track(FoundationApi.Assets.LoadSprite(ModId, id, absolutePngPath, pixelsPerUnit));
        public IFoundationAsset LoadBundle(string id, string absoluteBundlePath)
            => Track(FoundationApi.Assets.LoadAssetBundle(ModId, id, absoluteBundlePath));
        public IDisposable Persist(int schemaVersion, Func<LightJson.JsonValue> save, Action<int, LightJson.JsonValue> load)
            => Track(FoundationApi.ModSave.Register(ModId, schemaVersion, save, load));
        // Native catalog registration is intentionally process-lifetime: Kingdom Inc saves
        // use native IDs/indices, so removing a registered item or quest during the same
        // session would corrupt deserialization. These convenience methods are NOT Track()ed.
        /// <summary>Registers item.</summary>
        public Assets.Source.Items.ItemType RegisterItem(Assets.Source.Items.ItemType item)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterItem(ModId, item);
        }
        public Assets.Source.Items.CraftingRecipe RegisterRecipe(Assets.Source.Items.CraftingRecipe recipe)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterRecipe(ModId, recipe);
        }
        /// <summary>Registers quest.</summary>
        public Assets.Source.Adventure.Quest RegisterQuest(Assets.Source.Adventure.Quest quest)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterQuest(ModId, quest);
        }
        public Assets.Source.Adventure.DungeonType RegisterDungeon(Assets.Source.Adventure.DungeonType dungeon)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterDungeon(ModId, dungeon);
        }
        /// <summary>Registers war.</summary>
        public Assets.Source.Adventure.WarType RegisterWar(Assets.Source.Adventure.WarType war)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterWar(ModId, war);
        }
        public Assets.Source.Player.PrestigeNode RegisterPrestige(Assets.Source.Player.PrestigeNode node)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterPrestige(ModId, node);
        }
        /// <summary>Registers enhancement.</summary>
        public Assets.Source.Items.ItemEnhancement RegisterEnhancement(Assets.Source.Items.ItemEnhancement enhancement)
        {
            EnsureActive();
            return FoundationApi.Content.RegisterEnhancement(ModId, enhancement);
        }
        public void RegisterUpgrade(string typeId, Func<Assets.Source.Flow.FlowUpgrade> factory)
        {
            EnsureActive();
            FoundationApi.Upgrades.Register(ModId, typeId, factory);
        }
        /// <summary>Registers sprite.</summary>
        public void RegisterSprite(string localName, UnityEngine.Sprite sprite)
        {
            EnsureActive();
            FoundationApi.Presentation.RegisterSprite(ModId, localName, sprite);
        }
        public void RegisterText(string localKey, string locale, string text)
        {
            EnsureActive();
            FoundationApi.Presentation.RegisterText(ModId, localKey, locale, text);
        }
        void EnsureActive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(FoundationModContext));
        }

        /// <summary>Performs the register operation.</summary>
        public ITechNode Register(ITechNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!(definition is IModTechNodeDefinition owner) || owner.ModId != ModId)
                throw new ArgumentException("A context-owned technology requires an explicitly matching ModId.", nameof(definition));
            var node = FoundationApi.CreateTechNode(definition);
            return Track(node);
        }
        /// <summary>Performs the register operation.</summary>
        public IWorkspaceNodeType Register(IWorkspaceNodeDefinition definition) => Track(FoundationApi.RegisterWorkspaceNode(definition));
        public IModBarRegistration Register(IModBarElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.ModId != ModId) throw new ArgumentException("Bar element owner does not match the mod context.", nameof(element));
            return Track(FoundationApi.ModBar.Register(element));
        }
        public IModConfigSection Settings(string title = null, string description = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(FoundationModContext));
            return FoundationApi.ModConfig.GetOrCreateSection(ModId, title, description);
        }
        /// <summary>Performs the dispose operation.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Keep a local list of the exact owned asset handles: releasing by
            // ModId would also release resources owned by other contexts using
            // the same ID. Retry them after all dependent registrations unload.
            var ownedAssets = new List<IFoundationAsset>();
            foreach (var token in registrations)
                if (token is IFoundationAsset asset) ownedAssets.Add(asset);
            for (int i = registrations.Count - 1; i >= 0; i--)
            {
                try { registrations[i].Dispose(); }
                catch (Exception ex) { FoundationApi.Report(ex); }
            }
            foreach (var asset in ownedAssets)
                if (asset.IsLoaded)
                    try { asset.Dispose(); }
                    catch (Exception ex) { FoundationApi.Report(ex); }
            registrations.Clear();
            FoundationApi.Signals.RemoveOwner(ModId);
            FoundationApi.Events.RemoveOwner(ModId);
            FoundationApi.ModConfig.RemoveSection(ModId);
        }
    }
}
