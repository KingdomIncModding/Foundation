using System.Collections.Generic;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI.TechTree;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomFoundation
{
    // Resolve actual rendered rectangles, including costs and requirement panels.
    // Move later registrations only; the resolved native positions also drive focus
    // and native branch connectors. Requested definition coordinates are unchanged.
    internal static class ModResearchLayout
    {
        internal static Rect Bounds(UITechNode node, RectTransform parent)
        {
            var corners = new Vector3[4];
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity), max = -min;
            var glow = TechTreeTabs.Field<Image>(node, "_glow");
            var highlight = TechTreeTabs.Field<Image>(node, "_highlight");
            var rects = node.GetComponentsInChildren<Graphic>().Where(g => g.isActiveAndEnabled && g.color.a > 0 && g != glow && g != highlight).Select(g => g.rectTransform).Concat(new[] { (RectTransform)node.transform });
            foreach (var rect in rects)
            {
                rect.GetWorldCorners(corners);
                foreach (var world in corners)
                {
                    Vector2 point = parent.InverseTransformPoint(world);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        internal static void Arrange(TechTreeMenu menu)
        {
            var parent = TechTreeTabs.Field<RectTransform>(menu, "_nodeParent");
            var views = menu.Nodes.OrderBy(n => n.Tech == menu.RootNode ? -1 : n.Tech.Ordinal).ToArray();
            if (views.Length == 0)
                return;
            bool changed = false;
            var recipe = TechTreeTabs.Field<RectTransform>(menu, "_recipeContainer");
            if (recipe)
            {
                var grid = recipe.GetComponent<GridLayoutGroup>();
                if (grid && grid.enabled)
                {
                    grid.enabled = false;
                    changed = true;
                }
            }

            foreach (var node in views)
            {
                node.UpdateNodeStatus();
                node.transform.position = parent.TransformPoint(new Vector3(node.Tech.Position.x, node.Tech.Position.y, 0));
            }

            Canvas.ForceUpdateCanvases();
            var occupied = new List<Rect>();
            const float gap = 32;
            foreach (var view in views)
            {
                var rect = Bounds(view, parent);
                float shift = 0;
                // Moving monotonically right crosses each occupied rectangle at most once.
                while (true)
                {
                    var hit = occupied.Where(r => r.xMin < rect.xMax + gap - .1f && r.xMax + gap > rect.xMin + .1f && r.yMin < rect.yMax + gap - .1f && r.yMax + gap > rect.yMin + .1f).OrderBy(r => r.xMax).FirstOrDefault();
                    if (hit.width == 0)
                        break;
                    float delta = Mathf.Ceil(hit.xMax + gap - rect.xMin);
                    rect.x += delta;
                    shift += delta;
                }

                if (shift > 0)
                {
                    var point = view.Tech.Position + new Vector2Int((int)shift, 0);
                    view.Tech.RelativePosition = Vector2Int.zero;
                    view.Tech.AbsolutePosition = point;
                    view.transform.position = parent.TransformPoint(new Vector3(point.x, point.y, 0));
                    changed = true;
                }

                occupied.Add(rect);
            }

            if (changed)
            {
                foreach (var old in parent.GetComponentsInChildren<LineRenderer>())
                {
                    old.gameObject.SetActive(false);
                    global::UnityEngine.Object.Destroy(old.gameObject);
                }

                var prefab = TechTreeTabs.Field<LineRenderer>(menu, "_linePrefab");
                foreach (var view in views)
                {
                    var previous = view.Tech.Previous ?? view.Tech.Root;
                    if (view.Tech == menu.RootNode || previous == null || !views.Any(n => n.Tech == previous))
                        continue;
                    var line = global::UnityEngine.Object.Instantiate(prefab, parent);
                    line.transform.localPosition = Vector3.zero;
                    line.positionCount = 2;
                    line.SetPosition(0, new Vector3(view.Tech.Position.x, view.Tech.Position.y));
                    line.SetPosition(1, new Vector3(previous.Position.x, previous.Position.y));
                }

                if (WorldFlowCamera.Instance)
                    WorldFlowCamera.Instance.RecalculateBounds();
            }
        }
    }
}
