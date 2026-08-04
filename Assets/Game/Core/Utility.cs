namespace TrickcalRevive.Core
{
    public static class Utility
    {
        public static string Format(long value) => value.ToString("N0");

        public static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;

        public static int Clamp(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;
    }
}
