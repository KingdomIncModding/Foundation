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
        /// <summary>Gets whether the native music player is available.</summary>
        bool IsReady { get; }
        /// <summary>Gets whether the native music player is currently playing.</summary>
        bool IsPlaying { get; }
        /// <summary>Gets or sets the game's music volume preference.</summary>
        float MusicVolume { get; set; }
        /// <summary>Requests playback of a native music track.</summary>
        /// <param name="nativeTrackId">Identifier understood by the game's music player.</param>
        /// <param name="fadeSeconds">Fade duration in seconds; -1 requests the native default.</param>
        void PlayMusic(string nativeTrackId, float fadeSeconds = -1f);
        /// <summary>Queues a native music track using the game's priority rules.</summary>
        /// <param name="nativeTrackId">Identifier understood by the game's music player.</param>
        /// <param name="priority">Priority passed to the native queue.</param>
        void QueueMusic(string nativeTrackId, int priority = 0);
        /// <summary>Previews a music volume value through the native player.</summary>
        /// <param name="volume">Volume value passed to the game.</param>
        void PreviewMusicVolume(float volume);
    }
}
