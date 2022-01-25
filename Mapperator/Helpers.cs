namespace Mapperator {
    public static class Helpers {
        public static double Mod(double x, double m) {
            double r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
