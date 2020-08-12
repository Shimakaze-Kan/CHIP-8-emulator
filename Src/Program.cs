using System;
using System.IO;
using GameWindow;

namespace CHIP_8_Emulator
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("Please enter the file name as an argument");
            else if (!File.Exists(args[0]))
                Console.WriteLine("File '" + args[0] + "' doesn't exist");
            else
                using (var game = new App(args[0]))
                    game.Run();
        }
    }
}
