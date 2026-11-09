using System;
using System.Text;
using UnityEngine;

namespace KingdomFoundation
{
    /// <summary>Client-only panel layout preferences, isolated by mod GUID and panel ID.</summary>
    internal static class UiPanelState
    {
        private static string Key(string owner, string panel, string control)
        {
            var raw = owner + ":" + panel + ":" + control;
            return "Foundation.Ui." + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }
        internal static int ReadInt(string owner, string panel, string control, int fallback)
            => PlayerPrefs.GetInt(Key(owner, panel, control), fallback);
        internal static void WriteInt(string owner, string panel, string control, int value)
        { PlayerPrefs.SetInt(Key(owner, panel, control), value); PlayerPrefs.Save(); }
        internal static float ReadFloat(string owner, string panel, string control, float fallback)
            => PlayerPrefs.GetFloat(Key(owner, panel, control), fallback);
        internal static void WriteFloat(string owner, string panel, string control, float value)
        { PlayerPrefs.SetFloat(Key(owner, panel, control), value); PlayerPrefs.Save(); }
    }
}
