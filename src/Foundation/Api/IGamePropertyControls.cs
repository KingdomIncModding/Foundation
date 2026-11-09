using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace KingdomFoundation.Api
{
    public interface IGamePropertyControls
    {
        int Build(IModConfigSection section, ISignalSource source, PropertyEditorOptions options = null);
    }
}
