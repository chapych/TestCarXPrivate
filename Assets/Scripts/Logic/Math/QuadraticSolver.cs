using System;
using UnityEngine;

namespace Logic.Math
{
    public static class QuadraticSolver
    {
        public static float[] SolveWithParameters(float a, float b, float c)
        {
            float determinant  = b * b - 4 * a * c;
            Debug.Log(determinant);
            if (determinant  < 0) return Array.Empty<float>();
            if (determinant  == 0) return new[] {-b / 2 * a};

            float sqrtD = Mathf.Sqrt(determinant);
            float x1 = (-b + sqrtD) / (2 * a);
            float x2 = (-b - sqrtD) / (2 * a);

            return new[] {x1, x2};
        }
    }
}