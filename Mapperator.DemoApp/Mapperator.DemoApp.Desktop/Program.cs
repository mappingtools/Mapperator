using osu.Framework.Platform;
using osu.Framework;
using Mapperator.DemoApp.Game;

namespace Mapperator.DemoApp.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"Mapperator.DemoApp"))
            using (osu.Framework.Game game = new DemoAppGame())
                host.Run(game);
        }
    }
}
