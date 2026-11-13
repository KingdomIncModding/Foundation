using System.Collections.Generic;
using UnityEngine;

namespace KingdomFoundation.Api
{
    /// <summary>Primitive access to native camera views without leaking game component types.</summary>
    public interface IGameCameraAccess
    {
        IReadOnlyList<Component> Views { get; }
        Component ActiveView { get; }
        void Move(Component view,float x,float y);
        float ZoomScale(Component view);
        Camera Camera(Component view);
        SpriteRenderer Background(Component view);
        int BackgroundPeriod(Component view);
        int ZoomLevel(Component view);
        int MaximumZoom(Component view);
        void SetZoomLevel(Component view, int level);
        void RefreshFontFiltering(Component view);
        void CleanupFontFiltering();
        void UpdateDecorationZoom(int level);
    }
}
