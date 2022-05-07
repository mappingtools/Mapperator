using Mapping_Tools_Core.MathUtil;

namespace Mapperator {
    public static class Helpers {
        public static double Mod(double x, double m) {
            var r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Calculates the difference in angle from a1 to a2 and normalizes it to the range [-pi, pi]
        /// </summary>
        /// <param name="a1">The first angle</param>
        /// <param name="a2">The second angle</param>
        /// <returns>The difference in angles</returns>
        public static double AngleDifference(double a1, double a2) {
            return Mod(a2 - a1 + MathHelper.Pi, MathHelper.TwoPi) - MathHelper.Pi;
        }
    }
}
