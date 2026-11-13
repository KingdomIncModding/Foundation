using System;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Primitive native volume interception and exception-safe operation scopes. Mixers and channel policy belong to mods.</summary>
    public interface IGameAudioInterception
    {
        IDisposable Subscribe(string modId, Func<AudioSource, float, float> writeEffect = null,
            Func<AudioSource, float, float> writeMusic = null, Func<AudioSource, float, float> readMusic = null,
            Func<GameAudioActivity, IDisposable> activityScope = null);
    }
}
