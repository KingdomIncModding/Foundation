using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class ModBarControlView : IModBarView
    {
        Action refresh, dispose;
        internal ModBarControlView(Action refresh, Action dispose = null) { this.refresh = refresh; this.dispose = dispose; }
        public void Refresh() => refresh?.Invoke();
        public void Dispose() { refresh = null; var release = dispose; dispose = null; release?.Invoke(); }
    }
}
