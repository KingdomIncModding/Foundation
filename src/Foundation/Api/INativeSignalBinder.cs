using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Explicitly expose real game object properties to the standard Foundation signal
    /// source/property chooser. Never guesses an object's state or replaces item pipes.
    /// </summary>
    public interface INativeSignalBinder
    {
        ISignalSource Expose(string modId, string id, string name, Type nativeType,
            Func<object> resolveTarget, IEnumerable<string> publicMembers, bool writable = false);
    }
}
