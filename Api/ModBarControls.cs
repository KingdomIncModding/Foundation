using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class ModBarControlView : IModBarView
    {
        Action refresh, dispose;
        internal ModBarControlView(Action refresh, Action dispose = null) { this.refresh = refresh; this.dispose = dispose; }
        public void Refresh() => refresh?.Invoke();
        public void Dispose() { refresh = null; var release = dispose; dispose = null; release?.Invoke(); }
    }
    public sealed class ModBarText : ModBarElement
    {
        public string Text { get; set; }
        public ModBarText(string modId, string id, string text, float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment) { Text = text; }
        public override IModBarView CreateView(IModBarContext context)
        { var text = context.CreateText(Text); return new ModBarControlView(() => text.text = Text ?? ""); }
    }
    public sealed class ModBarButton : ModBarElement
    {
        public string Text { get; set; }
        public bool Interactable { get; set; } = true;
        public event Action Clicked;
        public ModBarButton(string modId, string id, string text, Action clicked = null, float width = 160, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment)
        { Text = text; if (clicked != null) Clicked += clicked; }
        public override IModBarView CreateView(IModBarContext context)
        {
            var button = context.CreateButton(Text, () => Invoke(Clicked));
            var text = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            return new ModBarControlView(() => { text.text = Text ?? ""; button.interactable = Interactable; });
        }
        internal static void Invoke(Action callbacks)
        {
            if (callbacks == null) return;
            foreach (Action callback in callbacks.GetInvocationList()) try { callback(); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
    public sealed class ModBarDropdown : ModBarElement
    {
        public IReadOnlyList<string> Options { get; }
        public int SelectedIndex { get; private set; }
        public bool Interactable { get; set; } = true;
        public event Action<int> SelectionChanged;
        public ModBarDropdown(string modId, string id, IEnumerable<string> options, int selectedIndex = 0, Action<int> selected = null,
            float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment)
        {
            var copy = options?.ToArray() ?? throw new ArgumentNullException(nameof(options));
            if (copy.Length == 0 || copy.Any(o => o == null)) throw new ArgumentException("At least one non-null option is required.", nameof(options));
            Options = Array.AsReadOnly(copy); SetSelectedIndex(selectedIndex); if (selected != null) SelectionChanged += selected;
        }
        /// <summary>Changes state silently. Call registration.Refresh to apply it to the current view.</summary>
        public void SetSelectedIndex(int index)
        { if (index < 0 || index >= Options.Count) throw new ArgumentOutOfRangeException(nameof(index)); SelectedIndex = index; }
        public override IModBarView CreateView(IModBarContext context)
        {
            var dropdown = context.CreateDropdown(Options, SelectedIndex, index => {
                SelectedIndex = index;
                if (SelectionChanged == null) return;
                foreach (Action<int> callback in SelectionChanged.GetInvocationList()) try { callback(index); } catch (Exception error) { FoundationApi.Report(error); }
            });
            return new ModBarControlView(() => { dropdown.SetValueWithoutNotify(SelectedIndex); dropdown.RefreshShownValue(); dropdown.interactable = Interactable; }, dropdown.Hide);
        }
    }
    public sealed class ModBarToggle : ModBarElement
    {
        public string Text { get; set; }
        public bool Value { get; private set; }
        public bool Interactable { get; set; } = true;
        public event Action<bool> ValueChanged;
        public ModBarToggle(string modId, string id, string text, bool value = false, Action<bool> changed = null,
            float width = 180, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment)
        { Text = text; Value = value; if (changed != null) ValueChanged += changed; }
        public void SetValue(bool value) { Value = value; }
        public override IModBarView CreateView(IModBarContext context)
        {
            var toggle = context.CreateToggle(Text, Value, value => {
                Value = value;
                if (ValueChanged == null) return;
                foreach (Action<bool> callback in ValueChanged.GetInvocationList()) try { callback(value); } catch (Exception error) { FoundationApi.Report(error); }
            });
            var text = toggle.GetComponentInChildren<TMPro.TMP_Text>(true);
            return new ModBarControlView(() => { text.text = Text ?? ""; toggle.SetIsOnWithoutNotify(Value); toggle.interactable = Interactable; });
        }
    }
    public sealed class ModBarSeparator : ModBarElement
    {
        public ModBarSeparator(string modId, string id, float width = 16, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment) { }
        public ModBarSeparator(string modId, string id, ModBarAlignment alignment, int order = 0, float width = 16)
            : base(modId, id, width, order, alignment) { }
        public override IModBarView CreateView(IModBarContext context)
        {
            context.CreateSeparator();
            return new ModBarControlView(null);
        }
    }
}
