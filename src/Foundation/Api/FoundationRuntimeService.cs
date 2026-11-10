using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace KingdomFoundation.Api
{
    internal sealed partial class FoundationRuntimeService : IFoundationRuntimeService
    {

        readonly List<Registration> callbacks = new List<Registration>();
        /// <summary>Gets or sets the registration counts value.</summary>
        public IReadOnlyDictionary<string, int> RegistrationCounts => callbacks.Where(x => x.Active)
            .GroupBy(x => x.ModId).ToDictionary(x => x.Key, x => x.Count());
        static void Validate(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) throw new ArgumentException("Mod GUID required.", nameof(modId));
        }
        public IDisposable EachFrame(string modId, Action<float> callback)
        {
            Validate(modId);
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            var reg = new Registration { Owner = this, ModId = modId, Frame = callback };
            callbacks.Add(reg); return reg;
        }
        /// <summary>Performs the key pressed operation.</summary>
        public IDisposable KeyPressed(string modId, Key key, Action callback, bool allowWhileTyping = false)
        {
            Validate(modId);
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (key == Key.None) throw new ArgumentOutOfRangeException(nameof(key));
            var reg = new Registration { Owner = this, ModId = modId, Key = key,
                Shortcut = callback, AllowWhileTyping = allowWhileTyping };
            callbacks.Add(reg); return reg;
        }
        void Remove(Registration reg)
        {
            reg.Active = false;
            callbacks.Remove(reg);
        }
        static bool Typing()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (!selected) return false;
            return selected.GetComponent<UnityEngine.UI.InputField>() != null ||
                   selected.GetComponent<TMPro.TMP_InputField>() != null;
        }
        internal void Tick(float delta)
        {
            var keyboard = Keyboard.current;
            var typing = keyboard != null && Typing();
            foreach (var reg in callbacks.ToArray())
            {
                if (!reg.Active) continue;
                try
                {
                    if (reg.Frame != null) reg.Frame(delta);
                    if (reg.Shortcut != null && keyboard != null && (reg.AllowWhileTyping || !typing) &&
                        keyboard[reg.Key].wasPressedThisFrame) reg.Shortcut();
                }
                catch (Exception ex) { FoundationApi.Report(new InvalidOperationException("Runtime callback failed for mod " + reg.ModId, ex)); }
            }
        }
    }
}
