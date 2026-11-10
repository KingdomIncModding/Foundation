using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod bar dropdown API.</summary>
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
}
