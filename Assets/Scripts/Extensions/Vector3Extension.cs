using UnityEngine;

namespace Extensions
{
    public static class Vector3Extension
    {
        public static float DoubleMagnitude(this Vector3 vector)
            => Vector3.Dot(vector, vector);

        public static Vector3 WithY(this Vector3 vector, float yComponent)
            => new Vector3(vector.x, yComponent, vector.z);
    }
}