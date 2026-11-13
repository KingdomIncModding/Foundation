using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.Shared;
using HarmonyLib;
using UnityEngine;

namespace KingdomFoundation.Api
{
    internal sealed class GameCameraAccess : IGameCameraAccess
    {
        public void Move(Component view,float x,float y)=>view.GetComponent<Assets.Behaviour.Flow.WorldFlowCamera>().Move(x,y);
        public float ZoomScale(Component view)=>view.GetComponent<Assets.Behaviour.Flow.WorldFlowCamera>().ZoomScale;
        private static readonly AccessTools.FieldRef<WorldFlowCamera, Camera> CameraField = AccessTools.FieldRefAccess<WorldFlowCamera, Camera>("_camera");
        private static readonly AccessTools.FieldRef<WorldFlowCamera, SpriteRenderer> BackgroundField = AccessTools.FieldRefAccess<WorldFlowCamera, SpriteRenderer>("_background");
        private static readonly AccessTools.FieldRef<WorldFlowCamera, int> PeriodField = AccessTools.FieldRefAccess<WorldFlowCamera, int>("backgroundSize");
        public IReadOnlyList<Component> Views => Array.AsReadOnly(Resources.FindObjectsOfTypeAll<WorldFlowCamera>().Cast<Component>().ToArray());
        public Component ActiveView => WorldFlowCamera.Instance;
        private static WorldFlowCamera Require(Component view) => view is WorldFlowCamera camera ? camera : throw new ArgumentException("Expected a Foundation camera hook view.", nameof(view));
        public Camera Camera(Component view) => CameraField(Require(view));
        public SpriteRenderer Background(Component view) => BackgroundField(Require(view));
        public int BackgroundPeriod(Component view) => PeriodField(Require(view));
        public int ZoomLevel(Component view) => Require(view).ZoomLevel;
        public int MaximumZoom(Component view) => Require(view).MaxZoom;
        public void SetZoomLevel(Component view, int level) => Require(view).SetZoomLevel(level);
        public void RefreshFontFiltering(Component view) => FontFilteringSwitch.UpdateCameraZoom(Require(view));
        public void CleanupFontFiltering() => FontFilteringSwitch.Cleanup();
        public void UpdateDecorationZoom(int level) => WorldFlowDeco.UpdateZoomLevel(level);
    }
}
