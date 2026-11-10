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
        /// <summary>Gets whether the native view and input services are available.</summary>
        bool IsReady { get; }
        /// <summary>Gets the mouse position in the game's world coordinates.</summary>
        Vector2Int MouseWorld { get; }
        /// <summary>Gets the mouse position in UI coordinates.</summary>
        Vector2Int MouseUi { get; }
        /// <summary>Gets whether the native interact action was pressed.</summary>
        bool InteractPressed { get; }
        /// <summary>Gets whether the native interact action was released.</summary>
        bool InteractReleased { get; }
        /// <summary>Gets whether the native cancel action was pressed.</summary>
        bool CancelPressed { get; }
        /// <summary>Gets whether game input is enabled.</summary>
        bool GameInputEnabled { get; }
        /// <summary>Gets the game's input action asset.</summary>
        InputActionAsset NativeActionAsset { get; }
        /// <summary>Gets the game's player input component.</summary>
        GamePlayerInput NativeInput { get; }
        /// <summary>Gets the native world camera.</summary>
        WorldFlowCamera NativeCamera { get; }

        /// <summary>Gets the current native camera zoom level.</summary>
        int CameraZoom { get; }
        /// <summary>Gets the maximum native camera zoom level.</summary>
        int MaximumCameraZoom { get; }
        /// <summary>Gets the current camera zoom scale.</summary>
        float ZoomScale { get; }
        /// <summary>Requests a specific native camera zoom level.</summary>
        /// <param name="level">Requested zoom level.</param>
        void SetCameraZoom(int level);
        /// <summary>Adjusts the native camera zoom level.</summary>
        /// <param name="change">Relative zoom adjustment.</param>
        void ChangeCameraZoom(int change);
        /// <summary>Pans the camera using the native camera service.</summary>
        /// <param name="deltaX">Horizontal pan delta.</param>
        /// <param name="deltaY">Vertical pan delta.</param>
        void PanCamera(float deltaX, float deltaY);
        /// <summary>Requests recalculation of native camera bounds.</summary>
        void RecalculateCameraBounds();
    }
}
