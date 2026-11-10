using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace KingdomFoundation.Api
{
    internal sealed class TechNodeRegistration : INativeTechNode, IModTechNode
    {
        readonly TechNodeRegistry owner;
        /// <summary>Gets or sets the id value.</summary>
        public string Id { get; }
        public ITechMod Mod { get; }
        public ITechNodeDefinition Definition { get; private set; }
        public bool IsRegistered { get; private set; } = true;
        public Assets.Source.Player.TechNode NativeNode { get; internal set; }
        public event Action<ITechNode> Clicked;
        internal TechNodeRegistration(TechNodeRegistry owner, TechNodeDefinition definition, ITechMod mod)
        { this.owner = owner; Id = definition.Id; Definition = definition; Mod = mod; }

        /// <summary>Performs the update operation.</summary>
        public void Update(ITechNodeDefinition definition)
        {
            if (!IsRegistered) throw new ObjectDisposedException(nameof(ITechNode));
            var snapshot = TechNodeDefinition.Snapshot(definition);
            if (snapshot.Id != Id) throw new ArgumentException("Updating a node cannot change its identifier.", nameof(definition));
            if (snapshot.ModId != null && (snapshot.ModId != Mod.Id || snapshot.ModName != Mod.Name)) throw new ArgumentException("Updating a node cannot change its owning mod.", nameof(definition));
            if (NativeNode != null) NativeResearch.Apply(this, snapshot);
            Definition = snapshot; owner.Notify();
        }
        /// <summary>Performs the dispose operation.</summary>
        public void Dispose()
        {
            if (!IsRegistered) return;
            IsRegistered = false; Clicked = null; owner.Remove(this);
            if (NativeNode != null) NativeResearch.Deactivate(this);
        }
        internal void Click()
        {
            if (!IsRegistered || Clicked == null) return;
            foreach (Action<ITechNode> subscriber in Clicked.GetInvocationList())
                try { subscriber(this); } catch (Exception error) { FoundationApi.Report(error); }
        }
    }
}
