namespace Mapperator {
    public static class Helpers {
        public static double Mod(double x, double m) {
            var r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
