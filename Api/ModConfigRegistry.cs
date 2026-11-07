using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace KingdomFoundation.Api
{
    public class ModConfigRegistry : IModConfigRegistry
    {
        readonly Dictionary<string, ModConfigSection> sections = new Dictionary<string, ModConfigSection>(StringComparer.Ordinal);
        readonly List<ModConfigSection> ordered = new List<ModConfigSection>();

        public IReadOnlyList<IModConfigSection> Sections => ordered.ToArray();
        public event Action Changed;

        public IModConfigSection GetOrCreateSection(string modId, string modName = null, string description = null)
        {
            if (string.IsNullOrEmpty(modId)) throw new ArgumentException("ModId cannot be empty.", nameof(modId));
            if (!sections.TryGetValue(modId, out var section))
            {
                string name = modName;
                if (string.IsNullOrEmpty(name))
                {
                    var detected = TechModIdentity.Detect();
                    name = detected.Item2;
                }
                section = new ModConfigSection(this, modId, name ?? modId, description);
                sections.Add(modId, section);
                ordered.Add(section);
                Notify();
            }
            else
            {
                if (!string.IsNullOrEmpty(modName) && string.IsNullOrEmpty(section.ModName))
                    section.ModName = modName;
                if (!string.IsNullOrEmpty(description))
                    section.Description = description;
            }
            return section;
        }

        public bool TryGetSection(string modId, out IModConfigSection section)
        {
            if (modId != null && sections.TryGetValue(modId, out var found))
            {
                section = found;
                return true;
            }
            section = null;
            return false;
        }

        public void RemoveSection(string modId)
        {
            if (modId != null && sections.TryGetValue(modId, out var found))
            {
                sections.Remove(modId);
                ordered.Remove(found);
                Notify();
            }
        }

        internal void Notify()
        {
            try { Changed?.Invoke(); }
            catch (Exception error) { FoundationApi.Report(error); }
        }
    }

    public class ModConfigSection : IModConfigSection
    {
        readonly ModConfigRegistry registry;
        readonly List<IModConfigElement> elements = new List<IModConfigElement>();

        public string ModId { get; }
        public string ModName { get; internal set; }
        public string Description { get; set; }
        public IReadOnlyList<IModConfigElement> Elements => elements.ToArray();

        public event Action Changed;
        public event Action OnCommit;
        public event Action OnCancel;

        internal ModConfigSection(ModConfigRegistry registry, string modId, string modName, string description)
        {
            this.registry = registry;
            ModId = modId;
            ModName = modName;
            Description = description ?? "";
        }

        public IModConfigSection AddElement(IModConfigElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            elements.Add(element);
            Notify();
            return this;
        }

        public bool RemoveElement(IModConfigElement element)
        {
            if (element != null && elements.Remove(element))
            {
                Notify();
                return true;
            }
            return false;
        }

        public void ClearElements()
        {
            elements.Clear();
            Notify();
        }

        public IModConfigText AddText(string text, bool isHeader = false)
        {
            var el = new ModConfigText(text, isHeader);
            AddElement(el);
            return el;
        }

        public IModConfigButton AddButton(string label, Action onClick, string buttonText = null)
        {
            var el = new ModConfigButton(label, onClick, buttonText);
            AddElement(el);
            return el;
        }

        public IModConfigToggle AddToggle(string label, bool initialValue, Action<bool> onValueChanged, Func<bool> getter = null)
        {
            var el = new ModConfigToggle(label, initialValue, onValueChanged, getter);
            AddElement(el);
            return el;
        }

        public IModConfigSlider AddSlider(string label, float min, float max, float initialValue, Action<float> onValueChanged, float step = 0f, Func<float> getter = null, Func<float, string> formatValue = null)
        {
            var el = new ModConfigSlider(label, min, max, initialValue, onValueChanged, step, getter, formatValue);
            AddElement(el);
            return el;
        }

        public IModConfigDropdown AddDropdown(string label, IReadOnlyList<string> options, int initialIndex, Action<int> onSelectionChanged, Func<int> getter = null)
        {
            var el = new ModConfigDropdown(label, options, initialIndex, onSelectionChanged, getter);
            AddElement(el);
            return el;
        }

        public IModConfigToggle AddToggle(ConfigEntry<bool> entry, string customLabel = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string label = customLabel ?? entry.Definition.Key;
            var el = new ModConfigToggle(label, entry.Value, val => {
                entry.Value = val;
                entry.ConfigFile?.Save();
            }, () => entry.Value);
            AddElement(el);
            return el;
        }

        public IModConfigSlider AddSlider(ConfigEntry<int> entry, int min, int max, string customLabel = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string label = customLabel ?? entry.Definition.Key;
            var el = new ModConfigSlider(label, min, max, entry.Value, val => {
                entry.Value = UnityEngine.Mathf.RoundToInt(val);
                entry.ConfigFile?.Save();
            }, step: 1f, getter: () => entry.Value, formatValue: v => UnityEngine.Mathf.RoundToInt(v).ToString());
            AddElement(el);
            return el;
        }

        public IModConfigSlider AddSlider(ConfigEntry<float> entry, float min, float max, float step = 0.05f, string customLabel = null, Func<float, string> formatValue = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string label = customLabel ?? entry.Definition.Key;
            var el = new ModConfigSlider(label, min, max, entry.Value, val => {
                entry.Value = val;
                entry.ConfigFile?.Save();
            }, step: step, getter: () => entry.Value, formatValue: formatValue ?? (v => v.ToString("0.##")));
            AddElement(el);
            return el;
        }

        public IModConfigDropdown AddDropdown(ConfigEntry<string> entry, IReadOnlyList<string> options, string customLabel = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string label = customLabel ?? entry.Definition.Key;
            int initialIdx = Math.Max(0, options != null ? ((IList<string>)options).IndexOf(entry.Value) : 0);
            var el = new ModConfigDropdown(label, options, initialIdx, idx => {
                if (options != null && idx >= 0 && idx < options.Count)
                {
                    entry.Value = options[idx];
                    entry.ConfigFile?.Save();
                }
            }, () => options != null ? Math.Max(0, ((IList<string>)options).IndexOf(entry.Value)) : 0);
            AddElement(el);
            return el;
        }

        public void Commit()
        {
            foreach (var el in elements)
            {
                try { el.Commit(); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
            try { OnCommit?.Invoke(); }
            catch (Exception error) { FoundationApi.Report(error); }
        }

        public void Cancel()
        {
            foreach (var el in elements)
            {
                try { el.Cancel(); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
            try { OnCancel?.Invoke(); }
            catch (Exception error) { FoundationApi.Report(error); }
        }

        internal void Notify()
        {
            try { Changed?.Invoke(); }
            catch (Exception error) { FoundationApi.Report(error); }
            registry.Notify();
        }
    }
}
