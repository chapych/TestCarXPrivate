using BaseClasses.OnScenePoints;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(OnSceneTowerPoint))]
    public class OnSceneTowerPointEditor : OnScenePointEditor
    {
        private static Color color = Color.red;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable)]
        public static void RenderCustomGizmo(OnSceneTowerPoint instance, GizmoType gizmoType) =>
            CircleGizmo(instance.transform,
                0.6f,
                color);
    }
}