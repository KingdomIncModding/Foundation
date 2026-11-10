using System;

namespace KingdomFoundation.Core
{
    /// <summary>Provides the save load failed event API.</summary>
    public sealed class SaveLoadFailedEvent
    {
        public Exception Error { get; }
        public SaveLoadFailedEvent(Exception error) { Error = error; }
    }
}
