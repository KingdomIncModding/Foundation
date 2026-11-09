using System;
using System.Collections.Generic;
using UnityEngine;
namespace KingdomFoundation.Api
{
    /// <summary>Binary-compatible extension: does not add methods to the existing IModConfigSection contract.</summary>
    public static class ModConfigColorSwatchesExtensions
    {
        public static IModConfigColorSwatches AddColorSwatches(this IModConfigSection section,
            string label, System.Collections.Generic.IReadOnlyList<UnityEngine.Color> colors,
            UnityEngine.Color initialValue, System.Action<UnityEngine.Color> onValueChanged,
            System.Func<UnityEngine.Color> getter = null)
        {
            if (section == null) throw new System.ArgumentNullException(nameof(section));
            var control = new ModConfigColorSwatches(label, colors, initialValue, onValueChanged, getter);
            section.AddElement(control);
            return control;
        }
    }
}
