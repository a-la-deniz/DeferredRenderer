using System;
using System.Threading;
using System.Globalization;

namespace DeferredLighting
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            using (Game1 game = new Game1())
            {
                game.Run();
            }
        }
    }
#endif
}

