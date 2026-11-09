using System;
using Assets.Behaviour.Util;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Uses the game's own music player and volume preference. Native track identifiers
    /// and timing rules remain authoritative; no parallel audio mixer is introduced.
    /// </summary>
    public interface IGameAudioService
    {
        bool IsReady { get; }
        bool IsPlaying { get; }
        float MusicVolume { get; set; }
        void PlayMusic(string nativeTrackId, float fadeSeconds = -1f);
        void QueueMusic(string nativeTrackId, int priority = 0);
        void PreviewMusicVolume(float volume);
    }
}
