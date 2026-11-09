using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public static class ModConfigSignalSelectorExtensions
    {
        public static ModConfigSignalSelector AddSignalSelector(this IModConfigSection section, string label,
            SignalSelection initial, Action<SignalSelection> onChanged, Func<SignalSelection> getter = null)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            var item = new ModConfigSignalSelector(label, initial, onChanged, getter);
            section.AddElement(item);
            return item;
        }
    }
}
