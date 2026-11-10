using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod bar toggle API.</summary>
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
        /// <summary>Sets value.</summary>
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
}
