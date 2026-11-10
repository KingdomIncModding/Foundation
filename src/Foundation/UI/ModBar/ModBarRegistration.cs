using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomFoundation.Api
{
    internal sealed class ModBarRegistration : IModBarRegistration
    {
        readonly ModBarRegistry owner;
        /// <summary>Gets or sets the id value.</summary>
        public string Id { get; }
        public string ModId { get; }
        public IModBarElement Element { get; }
        public bool IsRegistered { get; private set; } = true;
        internal ModBarRegistration(ModBarRegistry owner, IModBarElement element)
        { this.owner = owner; Element = element; Id = element.Id; ModId = element.ModId; }
        public void Refresh()
        {
            if (!IsRegistered) throw new ObjectDisposedException(nameof(IModBarRegistration));
            if (Element.Id != Id || Element.ModId != ModId) throw new InvalidOperationException("A registration cannot change its identity.");
            ModBarRegistry.ValidateWidth(Element.Width);
            ModBarRegistry.ValidateAlignment(Element.Alignment);
            owner.Notify();
        }
        /// <summary>Performs the dispose operation.</summary>
        public void Dispose() { if (!IsRegistered) return; IsRegistered = false; owner.Remove(this); }
    }
}
