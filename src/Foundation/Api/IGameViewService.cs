using System;
using Assets.Behaviour.Flow;
using GamePlayerInput = Assets.Behaviour.Input.PlayerInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomFoundation.Api
{
    /// <summary>
    /// Mouse position, native input actions and workflow camera, without duplicating
    /// the game's input dispatch or manipulating its private camera transform.
    /// Access on the Unity main thread after the game scene has loaded.
    /// </summary>
    public interface IGameViewService
    {
        bool IsReady { get; }
        Vector2Int MouseWorld { get; }
        Vector2Int MouseUi { get; }
        bool InteractPressed { get; }
        bool InteractReleased { get; }
        bool CancelPressed { get; }
        bool GameInputEnabled { get; }
        InputActionAsset NativeActionAsset { get; }
        GamePlayerInput NativeInput { get; }
        WorldFlowCamera NativeCamera { get; }

        int CameraZoom { get; }
        int MaximumCameraZoom { get; }
        float ZoomScale { get; }
        void SetCameraZoom(int level);
        void ChangeCameraZoom(int change);
        void PanCamera(float deltaX, float deltaY);
        void RecalculateCameraBounds();
    }
}
