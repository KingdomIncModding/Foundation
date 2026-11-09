using System;
using System.Collections.Generic;
using System.Linq;
using LightJson;

namespace KingdomFoundation.Api
{
    internal sealed partial class ModSaveRegistry
    {
        sealed class Stored
        {
            internal int Version;
            internal JsonValue Data;
        }
    }

}
