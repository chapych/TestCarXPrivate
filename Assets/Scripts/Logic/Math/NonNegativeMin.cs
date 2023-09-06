namespace Logic.Math
{
    public static class NonNegativeMin
    {
        public static bool TryNonNegativeMin(float[] numbers, out float min)
        {
            bool minExists = false;
            min = float.MaxValue;

            if (numbers.Length == 0) return false;
            foreach (float number in numbers)
            {
                if(number < 0) continue;
                if (number < min)
                {
                    min = number;
                    minExists = true;
                }
            }
            return minExists;
        }
    }
}