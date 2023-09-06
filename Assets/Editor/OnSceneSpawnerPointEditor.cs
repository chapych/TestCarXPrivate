using BaseClasses.OnScenePoints;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(OnSceneSpawnerPoint))]
    public class OnSceneSpawnerPointEditor : OnScenePointEditor
    {
        private static Color color = Color.blue;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Active | GizmoType.Pickable)]
        public static void RenderCustomGizmo(OnSceneSpawnerPoint instance, GizmoType gizmoType) =>
            CircleGizmo(instance.transform,
                0.6f,
                color);
    }
}