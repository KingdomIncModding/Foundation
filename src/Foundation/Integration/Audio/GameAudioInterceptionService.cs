using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameAudioInterceptionService : IGameAudioInterception
    {
        private readonly List<GameAudioInterceptionRegistration> registrations = new List<GameAudioInterceptionRegistration>();
        private GameAudioInterceptionRegistration[] snapshot = Array.Empty<GameAudioInterceptionRegistration>();
        private Harmony harmony;
        public IDisposable Subscribe(string modId, Func<AudioSource, float, float> writeEffect = null,
            Func<AudioSource, float, float> writeMusic = null, Func<AudioSource, float, float> readMusic = null,
            Func<GameAudioActivity, IDisposable> activityScope = null)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("A mod identifier is required.", nameof(modId));
            if (writeEffect == null && writeMusic == null && readMusic == null && activityScope == null) throw new ArgumentException("At least one handler is required.");
            Install();
            var registration = new GameAudioInterceptionRegistration(this) { ModId=modId, WriteEffect=writeEffect, WriteMusic=writeMusic, ReadMusic=readMusic, ActivityScope=activityScope };
            registrations.Add(registration); snapshot=registrations.ToArray();
            return registration;
        }
        private void Install()
        {
            if (harmony != null) return;
            var candidate = new Harmony("urakka.kingdominc.foundation.audio-hooks");
            try
            {
                foreach (var type in new[] { typeof(NativeSoundVolumePatch), typeof(NativeMusicVolumePatch), typeof(ManualCraftContextPatch),
                    typeof(MachineContextPatch), typeof(EconomyContextPatch), typeof(CombatContextPatch) }) candidate.CreateClassProcessor(type).Patch();
                harmony=candidate;
            }
            catch { candidate.UnpatchSelf(); throw; }
        }
        internal void Remove(GameAudioInterceptionRegistration registration) { registrations.Remove(registration); snapshot=registrations.ToArray(); }
        private float Transform(AudioSource source, float volume, int operation)
        {
            foreach (var entry in snapshot)
            {
                if (!entry.Active) continue;
                var handler=operation==0 ? entry.WriteEffect : operation==1 ? entry.WriteMusic : entry.ReadMusic;
                if (handler==null) continue;
                try { volume=handler(source,volume); }
                catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Native audio subscriber failed: "+entry.ModId,error)); }
            }
            return volume;
        }
        internal IDisposable Enter(GameAudioActivity activity)
        {
            IDisposable single=null; List<IDisposable> multiple=null;
            foreach (var entry in snapshot)
            {
                if (!entry.Active || entry.ActivityScope==null) continue;
                IDisposable scope;
                try { scope=entry.ActivityScope(activity); }
                catch (Exception error) { FoundationApi.Report(new InvalidOperationException("Native audio scope failed: "+entry.ModId,error)); continue; }
                if (scope==null) continue;
                if (single==null) single=scope;
                else { if (multiple==null) multiple=new List<IDisposable> {single}; multiple.Add(scope); }
            }
            return multiple==null ? single : new GameAudioScopeGroup(multiple);
        }
        internal static void Exit(IDisposable scope)
        {
            try { scope?.Dispose(); } catch (Exception error) { FoundationApi.Report(error); }
        }
        public static void WriteEffect(AudioSource source,float nativeVolume) { if (source) source.volume=FoundationApi.AudioInterceptionInternal.Transform(source,nativeVolume,0); }
        public static void WriteMusic(AudioSource source,float nativeVolume) { if (source) source.volume=FoundationApi.AudioInterceptionInternal.Transform(source,nativeVolume,1); }
        public static float ReadMusic(AudioSource source) => source ? FoundationApi.AudioInterceptionInternal.Transform(source,source.volume,2) : 0;
        internal void Shutdown() { harmony?.UnpatchSelf(); harmony=null; registrations.Clear(); snapshot=Array.Empty<GameAudioInterceptionRegistration>(); }
    }
}
