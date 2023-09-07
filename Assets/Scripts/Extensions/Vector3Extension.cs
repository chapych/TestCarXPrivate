using UnityEngine;

namespace Extensions
{
    public static class Vector3Extension
    {
        public static float DoubleMagnitude(this Vector3 vector)
        {
            return Vector3.Dot(vector, vector);
        }
    }
}