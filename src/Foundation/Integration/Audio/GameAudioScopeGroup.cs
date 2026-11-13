using System;
using System.Collections.Generic;

namespace KingdomFoundation.Api
{
    internal sealed class GameAudioScopeGroup : IDisposable
    {
        private readonly List<IDisposable> scopes;
        internal GameAudioScopeGroup(List<IDisposable> scopes) { this.scopes = scopes; }
        public void Dispose()
        {
            for (int index = scopes.Count - 1; index >= 0; index--)
                try { scopes[index]?.Dispose(); }
                catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
