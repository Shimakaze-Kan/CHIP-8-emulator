using System;
using System.IO;

namespace CHIP_8_Emulator
{
    class KeyboardMapper
    {
        private int[] _map;
        private bool _treatAsAscii = false;

        public KeyboardMapper(string path)
        {
            _map = new int[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 113, 119, 101, 114, 116, 121 };
            ReadKeyboardLayout(path);
        }

        /// <summary>
        /// Looks for a file with a keyboard layout in the same folder as the file to be executed by the emulator
        /// </summary>
        /// <exception> Thrown when a file does not exist or is invalid </exception>
        private void ReadKeyboardLayout(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path + ".kb");
                foreach (var line in lines)
                {
                    if (line.Contains("TREATASASCII"))
                    {
                        _treatAsAscii = true;
                        break;
                    }
                    var separate = line.Split(" ");
                    _map[Int32.Parse(separate[0])] = Int32.Parse(separate[1]);
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("The file '{0}' with the keyboard layout does not exist or is invalid'\n Default keyboard layout is set", path + ".kb");
                Console.ResetColor();

                _map = new int[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 113, 119, 101, 114, 116, 121 };
            }
        }

        /// <summary>
        /// Converts the character to the corresponding one in the file with the keyboard layout
        /// </summary>
        public int Convert(ushort key) => _treatAsAscii == true ? key : _map[key];
    }
}
