using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GamePropertyControls : IGamePropertyControls
    {
        public int Build(IModConfigSection section, ISignalSource source, PropertyEditorOptions options = null)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (source == null) throw new ArgumentNullException(nameof(source));
            options = options ?? new PropertyEditorOptions();
            if (options.NumberMaximum <= options.NumberMinimum) throw new ArgumentException("NumberMaximum must exceed NumberMinimum.");
            int created = 0;
            foreach (var prop in source.Properties)
            {
                if (!prop.CanWrite)
                {
                    if (options.IncludeReadOnly) { section.AddText(prop.Name + " (read-only): " + SafeRead(prop)); created++; }
                    continue;
                }
                try
                {
                    var type = prop.ValueType;
                    if (type == typeof(bool))
                    {
                        section.AddToggle(prop.Name, (bool)prop.Read(), v => prop.Write(v), () => (bool)prop.Read());
                    }
                    else if (type.IsEnum)
                    {
                        var choices = Enum.GetNames(type);
                        Func<int> selected = () => Math.Max(0, Array.IndexOf(choices, Enum.GetName(type, prop.Read())));
                        section.AddDropdown(prop.Name, choices, selected(), v => prop.Write(Enum.Parse(type, choices[v])), selected);
                    }
                    else if (type == typeof(UnityEngine.Color))
                    {
                        section.AddColorSwatches(prop.Name, options.Colors, (Color)prop.Read(), c => prop.Write(c), () => (Color)prop.Read());
                    }
                    else if (type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double))
                    {
                        Func<float> getter = () => Convert.ToSingle(prop.Read(), CultureInfo.InvariantCulture);
                        Action<float> write = v =>
                        {
                            if (type == typeof(int)) prop.Write(Mathf.RoundToInt(v));
                            else if (type == typeof(long)) prop.Write(Convert.ToInt64(Math.Round(v)));
                            else if (type == typeof(double)) prop.Write((double)v);
                            else prop.Write(v);
                        };
                        float step = type == typeof(int) || type == typeof(long) ? 1 : options.NumberStep;
                        section.AddSlider(prop.Name, options.NumberMinimum, options.NumberMaximum, getter(), write, step, getter);
                    }
                    else if (options.IncludeReadOnly) section.AddText(prop.Name + ": " + SafeRead(prop));
                    created++;
                }
                catch (Exception error)
                {
                    FoundationApi.Report(new InvalidOperationException("Unable to build editor for " + source.Id + "." + prop.Id, error));
                }
            }
            return created;
        }
        static string SafeRead(ISignalProperty property)
        { try { return property.Read()?.ToString() ?? "(none)"; } catch { return "(unavailable)"; } }
    }
}
