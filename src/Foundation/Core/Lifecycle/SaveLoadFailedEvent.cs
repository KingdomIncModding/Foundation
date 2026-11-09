using System;

namespace KingdomFoundation.Core
{
    public sealed class SaveLoadFailedEvent
    {
        public Exception Error { get; }
        public SaveLoadFailedEvent(Exception error) { Error = error; }
    }
}
