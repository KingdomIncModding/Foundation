using System;
using System.Linq;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomFoundation
{
    public sealed partial class FoundationPlugin
    {
        sealed class MutableDefinition : ITechNodeDefinition
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public Sprite Icon { get; set; }
            public Vector2 Position { get; set; }
        }
    }

}

