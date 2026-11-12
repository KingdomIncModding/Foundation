using System;
using System.Collections.Generic;
using System.IO;

namespace KingdomFoundation.Api
{
    /// <summary>Owns a mod's registrations and subscriptions; dispose during BepInEx OnDestroy.</summary>
    public sealed class FoundationModContext : IDisposable
    {
        readonly List<IDisposable> registrations = new List<IDisposable>();
        bool disposed;
        public string ModId { get; }

        /// <summary>Directory containing this mod's DLL and mod.json.</summary>
        public string ModDirectory { get; }

        /// <summary>Directory containing this mod's approved runtime Assets/ folder.</summary>
        public string AssetsDirectory => Metadata?.AssetsDirectory ?? (!string.IsNullOrEmpty(ModDirectory) ? Path.Combine(ModDirectory, "Assets") : null);

        /// <summary>Metadata parsed from this mod's mod.json file, if present.</summary>
        public ModMetadata Metadata { get; }

        public FoundationModContext(string modId)
        {
            SignalPropertyDefinition.ValidateId(modId, nameof(modId));
            ModId = modId;

            // Resolve mod directory from BepInEx Chainloader or Paths
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(modId, out var info) && !string.IsNullOrEmpty(info?.Location))
            {
                ModDirectory = Path.GetDirectoryName(info.Location);
            }
            else
            {
                string plugins = BepInEx.Paths.PluginPath;
                if (!string.IsNullOrEmpty(plugins))
                {
                    string candidate = Path.Combine(plugins, modId);
                    if (Directory.Exists(candidate)) ModDirectory = candidate;
                }
            }

            if (!string.IsNullOrEmpty(ModDirectory))
            {
                Metadata = ModMetadata.LoadFromDirectory(ModDirectory);
            }
        }

        /// <summary>Returns the absolute path to a file in Assets/{category}/{fileName}.</summary>
        public string GetAssetPath(string category, string fileName)
        {
            if (string.IsNullOrEmpty(AssetsDirectory)) return null;
            return Path.Combine(AssetsDirectory, category, fileName);
        }

        /// <summary>Loads a sprite directly from this mod's Assets/{category}/{fileName}.</summary>
        public IFoundationAsset LoadAssetSprite(string category, string fileName, float pixelsPerUnit = 100f)
        {
            string path = GetAssetPath(category, fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException($"Asset not found: Assets/{category}/{fileName}", path);
            string assetId = $"{category}/{fileName}";
            return LoadSprite(assetId, path, pixelsPerUnit);
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
        public IFoundationAsset LoadSprite(string id, string absolutePngPath, float pixelsPerUnit = 100f)
            => Track(FoundationApi.Assets.LoadSprite(ModId, id, absolutePngPath, pixelsPerUnit));
        public IFoundationAsset LoadBundle(string id, string absoluteBundlePath)
            => Track(FoundationApi.Assets.LoadAssetBundle(ModId, id, absoluteBundlePath));
        public IDisposable Persist(int schemaVersion, Func<LightJson.JsonValue> save, Action<int, LightJson.JsonValue> load)
            => Track(FoundationApi.ModSave.Register(ModId, schemaVersion, save, load));

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

        public ITechNode Register(ITechNodeDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!(definition is IModTechNodeDefinition owner) || owner.ModId != ModId)
                throw new ArgumentException("A context-owned technology requires an explicitly matching ModId.", nameof(definition));
            var node = FoundationApi.CreateTechNode(definition);
            return Track(node);
        }
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
        /// <summary>Registers a top-level Options tab, owned and disposed by this mod context.</summary>
        public IOptionsTabRegistration Register(IOptionsTab tab)
        {
            EnsureActive();
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            if (tab.ModId != ModId) throw new ArgumentException("Tab owner does not match the mod context.", nameof(tab));
            return Track(FoundationApi.Options.Register(tab));
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
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
