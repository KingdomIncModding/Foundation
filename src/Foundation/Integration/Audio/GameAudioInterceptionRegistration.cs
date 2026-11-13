using System;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameAudioInterceptionRegistration : IDisposable
    {
        internal string ModId;
        internal Func<AudioSource, float, float> WriteEffect;
        internal Func<AudioSource, float, float> WriteMusic;
        internal Func<AudioSource, float, float> ReadMusic;
        internal Func<GameAudioActivity, IDisposable> ActivityScope;
        internal bool Active = true;
        private readonly GameAudioInterceptionService owner;
        internal GameAudioInterceptionRegistration(GameAudioInterceptionService owner) { this.owner = owner; }
        public void Dispose() { if (!Active) return; Active = false; owner.Remove(this); }
    }
}
