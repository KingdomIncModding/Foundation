using System;
using Assets.Behaviour.Util;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameAudioService : IGameAudioService
    {
        /// <summary>Gets or sets the is ready value.</summary>
        public bool IsReady => UnityEngine.Object.FindAnyObjectByType<MusicManager>() != null;
        public bool IsPlaying => IsReady && MusicManager.IsPlaying;
        public float MusicVolume
        {
            get => MusicManager.Volume;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                MusicManager.Volume = value;
            }
        }
        /// <summary>Performs the play music operation.</summary>
        public void PlayMusic(string nativeTrackId, float fadeSeconds = -1f)
        {
            if (string.IsNullOrWhiteSpace(nativeTrackId)) throw new ArgumentException("Track ID is required.", nameof(nativeTrackId));
            if (float.IsNaN(fadeSeconds) || float.IsInfinity(fadeSeconds) || fadeSeconds < -1)
                throw new ArgumentOutOfRangeException(nameof(fadeSeconds));
            MusicManager.Play(nativeTrackId, fadeSeconds);
        }
        public void QueueMusic(string nativeTrackId, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(nativeTrackId)) throw new ArgumentException("Track ID is required.", nameof(nativeTrackId));
            if (!IsReady) throw new InvalidOperationException("The native music manager has not initialized.");
            MusicManager.Queue(nativeTrackId, priority);
        }
        /// <summary>Performs the preview music volume operation.</summary>
        public void PreviewMusicVolume(float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0 || volume > 1)
                throw new ArgumentOutOfRangeException(nameof(volume));
            MusicManager.PreviewVolume(volume);
        }
    }
}
