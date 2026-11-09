using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace KingdomFoundation.Api
{
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

        public ModConfigInput AddInput(string label, string initialValue, Action<string> onChanged,
            Func<string> getter = null, int maxLength = 256)
        {
            var value = new ModConfigInput(label, initialValue, onChanged, getter, maxLength);
            AddElement(value);
            return value;
        }

        public ModConfigNumber AddNumber(string label, double minimum, double maximum, double value,
            double step, Action<double> onChanged, Func<double> getter = null)
        {
            var number = new ModConfigNumber(label, minimum, maximum, value, step, onChanged, getter);
            AddElement(number);
            return number;
        }

        public ModConfigSearch AddSearch(string label, IReadOnlyList<string> options,
            int initialIndex, Action<int> onChanged, Func<int> getter = null)
        {
            var search = new ModConfigSearch(label, options, initialIndex, onChanged, getter);
            AddElement(search);
            return search;
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

        public IModConfigColorSwatches AddColorSwatches(string label, IReadOnlyList<UnityEngine.Color> colors,
            UnityEngine.Color initialValue, Action<UnityEngine.Color> onValueChanged, Func<UnityEngine.Color> getter = null)
        {
            var control = new ModConfigColorSwatches(label, colors, initialValue, onValueChanged, getter);
            AddElement(control);
            return control;
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

        // IReadOnlyList<T> does not necessarily implement IList<T>. Do not cast
        // caller-owned read-only collections when resolving a BepInEx value.
        private static int IndexOfOption(IReadOnlyList<string> options, string value)
        {
            if (options == null) return -1;
            for (int index = 0; index < options.Count; index++)
                if (string.Equals(options[index], value, StringComparison.Ordinal)) return index;
            return -1;
        }

        public IModConfigDropdown AddDropdown(ConfigEntry<string> entry, IReadOnlyList<string> options, string customLabel = null)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string label = customLabel ?? entry.Definition.Key;
            int initialIdx = Math.Max(0, IndexOfOption(options, entry.Value));
            var el = new ModConfigDropdown(label, options, initialIdx, idx => {
                if (options != null && idx >= 0 && idx < options.Count)
                {
                    entry.Value = options[idx];
                    entry.ConfigFile?.Save();
                }
            }, () => Math.Max(0, IndexOfOption(options, entry.Value)));
            AddElement(el);
            return el;
        }

        // Called once when the native Options panel opens. Switching mods/tabs only
        // rebuilds views; it must never reset another mod's uncommitted controls.
        internal void BeginEdit()
        {
            foreach (var el in elements)
            {
                if (!(el is IModConfigEditSession staged)) continue;
                try { staged.BeginEdit(); }
                catch (Exception error) { FoundationApi.Report(error); }
            }
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
            if (Changed == null) return;
            foreach (Action subscriber in Changed.GetInvocationList())
                try { subscriber(); } catch (Exception error) { FoundationApi.Report(error); }
            registry.Notify();
        }
    }
}
