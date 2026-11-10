using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Dungeon;
using Assets.Behaviour.UI.TechTree;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Behaviour.Flow;
using Assets.Source.Player;

namespace KingdomFoundation
{
    /// <summary>Provides the tech tree tabs API.</summary>
    public sealed partial class TechTreeTabs
    {
        sealed class CameraState
        {
            internal Vector3 Position;
            internal int Zoom;
            internal float Size;
        }
    }

}
