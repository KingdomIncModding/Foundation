using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    /// <summary>Provides the mod bar button API.</summary>
    public sealed class ModBarButton : ModBarElement
    {
        public string Text { get; set; }
        public bool Interactable { get; set; } = true;
        public event Action Clicked;
        public ModBarButton(string modId, string id, string text, Action clicked = null, float width = 160, int order = 0, ModBarAlignment alignment = ModBarAlignment.Left)
            : base(modId, id, width, order, alignment)
        { Text = text; if (clicked != null) Clicked += clicked; }
        /// <summary>Creates view.</summary>
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
}
