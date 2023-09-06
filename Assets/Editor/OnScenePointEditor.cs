using UnityEngine;

namespace Editor
{
    public class OnScenePointEditor : UnityEditor.Editor
    {
        private protected static void CircleGizmo(Transform instanceTransform, float pointRadius, Color color, float rangeRadius = 0f)
        {
            Gizmos.color = color;
            Vector3 position = instanceTransform.position;
            Gizmos.DrawSphere(position, pointRadius);
            Gizmos.DrawWireSphere(position, rangeRadius);
        }
    }
}