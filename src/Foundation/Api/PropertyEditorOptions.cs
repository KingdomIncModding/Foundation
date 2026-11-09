using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Options for rendering exposed signal properties into mod configuration UI.</summary>
    public sealed class PropertyEditorOptions
    {
        public float NumberMinimum = 0f;
        public float NumberMaximum = 100f;
        public float NumberStep = 0.1f;
        public bool IncludeReadOnly = true;
        public IReadOnlyList<Color> Colors = new [] { Color.white, Color.red, new Color(1f, .65f, .1f),
            Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta, Color.gray, Color.black };
    }
}
