using Mapperator.Resources;
using System;

namespace Mapperator {
    public class Program {
        static void Main(string[] args) {
            ConfigManager.LoadConfig();
            Console.WriteLine(Strings.Test);
        }
    }
}
