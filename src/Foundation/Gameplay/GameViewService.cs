using System;
using Assets.Behaviour.Flow;
using GamePlayerInput = Assets.Behaviour.Input.PlayerInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomFoundation.Api
{
    internal sealed class GameViewService : IGameViewService
    {
        static WorldFlowCamera Camera => WorldFlowCamera.Instance
            ? WorldFlowCamera.Instance : throw new InvalidOperationException("The native flow camera is not active.");
        public bool IsReady => WorldFlowCamera.Instance && GamePlayerInput.Instance;
        public Vector2Int MouseWorld => GamePlayerInput.MouseWorld;
        public Vector2Int MouseUi => GamePlayerInput.MouseUI;
        public bool InteractPressed => Mouse.current != null && GamePlayerInput.InteractPressed;
        public bool InteractReleased => Mouse.current != null && GamePlayerInput.InteractReleased;
        public bool CancelPressed => GamePlayerInput.Instance && GamePlayerInput.CancelPressed;
        public bool GameInputEnabled => GamePlayerInput.Enabled;
        public InputActionAsset NativeActionAsset => GamePlayerInput.Instance ? GamePlayerInput.Asset : null;
        public GamePlayerInput NativeInput => GamePlayerInput.Instance;
        public WorldFlowCamera NativeCamera => WorldFlowCamera.Instance;

        public int CameraZoom => Camera.ZoomLevel;
        public int MaximumCameraZoom => Camera.MaxZoom;
        public float ZoomScale => Camera.ZoomScale;
        public void SetCameraZoom(int level)
        {
            var cam = Camera;
            if (level < 0 || level > cam.MaxZoom) throw new ArgumentOutOfRangeException(nameof(level));
            cam.SetZoomLevel(level);
        }
        public void ChangeCameraZoom(int change)
        {
            var cam = Camera;
            // Native ChangeZoomLevel is intentionally one step per call.
            if (change == 0) return;
            int count = Math.Min(4, Math.Abs(change));
            for (int i = 0; i < count; i++) cam.ChangeZoomLevel(Math.Sign(change));
        }
        public void PanCamera(float deltaX, float deltaY)
        {
            if (float.IsNaN(deltaX) || float.IsInfinity(deltaX) ||
                float.IsNaN(deltaY) || float.IsInfinity(deltaY))
                throw new ArgumentOutOfRangeException(nameof(deltaX));
            Camera.Move(deltaX, deltaY);
        }
        public void RecalculateCameraBounds() => Camera.RecalculateBounds();
    }
}
