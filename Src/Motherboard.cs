using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace CHIP_8_Emulator
{
    public class Motherboard
    {
        #region constants
        public const byte ScreenHeight = 32;
        public const byte ScreenWidth = 64;
        #endregion

        #region fields
        private byte[] _memory;
        private byte[] _V; //registers
        private ushort _I = 0; //register used to store memory addresses
        private Stack<ushort> _stack;
        private byte _delayTimer = 0;
        private byte _soundTimer = 0;
        private ushort _currentKey = 0;
        private byte[] _screen;
        private ushort _PC = 0;
        private int _programLength = 0;
        public bool pause = false;
        private int _cpuClocking = 700;
        private int _timerSpeed = 60;
        #endregion

        #region objects
        private Random _random;
        private Stopwatch _stopwatch = new Stopwatch();
        private Stopwatch _stopwatch2 = new Stopwatch();
        KeyboardMapper _keyboardMapper;
        #endregion

        #region events
        public event EventHandler EndOfExecution;
        public event EventHandler WaitingForKey;
        public AutoResetEvent autoEvent;
        #endregion

        public Motherboard(string path)
        {
            _memory = new byte[4096];
            _screen = new byte[ScreenHeight * ScreenWidth];
            _V = new byte[16];

            _random = new Random();
            _keyboardMapper = new KeyboardMapper(path);
            _stack = new Stack<ushort>();
            autoEvent = new AutoResetEvent(false);

            LoadFont();
            LoadProgramTo_Memory(path);
        }

        /// <summary> 
        /// Returns a byte array representing the screen 
        /// </summary>
        public byte[] GetScreenData => _screen;

        /// <summary>
        /// Returns current cpu clocking
        /// </summary>
        public int GetCpuClocking => _cpuClocking;

        public int GetTimerSpeed => _timerSpeed;

        public byte GetSoundTimer => _soundTimer;

        /// <summary>
        /// Forwards a pressed key to the emulator
        /// </summary>
        public void SetCurrentKey(ushort key) => _currentKey = key;

        /// <summary>
        /// It loads the program code to the address 512 of the memory and sets the program counter also to 512
        /// </summary>
        /// <param name="path"> File path </param>
        private void LoadProgramTo_Memory(string path)
        {
            long fileSize = (new FileInfo(path)).Length;

            if (fileSize > 4096 - 512 || fileSize == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Size of file: {0} bytes, acceptable file size 1-3584 bytes", fileSize);
                Console.ResetColor();

                Environment.Exit(-1);
            }

            var buffer = File.ReadAllBytes(path);
            _programLength = buffer.Length;
            Array.Copy(buffer, 0, _memory, 512, buffer.Length);
            _PC = 512;
        }

        /// <summary>
        /// Copies the font data to memory address 0
        /// </summary>
        private void LoadFont()
        {
            byte[] hexadecimalFont = CHIP_8_Emulator.Font.GetHexadecimalFont();
            Array.Copy(hexadecimalFont, 0, _memory, 0, hexadecimalFont.Length);
        }

        /// <summary>
        /// The main emulator loop, executes instructions until the counter 
        /// program is within memory range or is stopped by signal
        /// </summary>
        public void StartExecuting(CancellationToken ct)
        {
            while (_PC < _programLength + 512)
            {
                Thread.Sleep(1000/_cpuClocking);
                if (!_stopwatch.IsRunning) _stopwatch.Start();
                
                ExecuteNext();

                if (_stopwatch.ElapsedMilliseconds > 1000 / _timerSpeed) //default 60Hz
                {
                    _delayTimer--;
                    _soundTimer--;
                    _stopwatch.Restart();
                }

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (pause)
                    autoEvent.WaitOne();
            }

            EndOfExecution?.Invoke(this, EventArgs.Empty);

        }

        /// <summary>
        /// It takes from _memory the instruction indicated by the program counter and executes it
        /// </summary>
        private void ExecuteNext()
        {
            ushort opcode = (ushort)(_memory[_PC & 0xFFF] * 0x100 + _memory[(_PC + 1) & 0xFFF]);
            _PC += 2;

            //Console.WriteLine("Opcode: " + opcode.ToString("X4"));

            byte u = (byte)((opcode >> 12) & 0xF);
            byte p = (byte)((opcode >> 0) & 0xF);
            byte y = (byte)((opcode >> 4) & 0xF);
            byte x = (byte)((opcode >> 8) & 0xF);
            byte kk = (byte)((opcode >> 0) & 0xFF);
            ushort nnn = (ushort)((opcode >> 0) & 0xFFF);

            switch (u)
            {
                case 0:
                    if (opcode == 0x0E0)
                        Array.Clear(_screen, 0, _screen.Length);
                    else if (opcode == 0x0EE)
                        _PC = _stack.Pop();
                    else
                    {
                        Console.WriteLine( new UnsupportedInstructionException(opcode.ToString("X4")).Message); //temporary solution
                        Environment.Exit(-1);
                    }
                    break;
                case 1:
                    _PC = nnn;
                    break;
                case 2:
                    _stack.Push(_PC);
                    _PC = nnn;
                    break;
                case 3:
                    if (_V[x] == kk)
                        _PC += 2;
                    break;
                case 4:
                    if (_V[x] != kk)
                        _PC += 2;
                    break;
                case 5:
                    if (_V[x] == _V[y])
                        _PC += 2;
                    break;
                case 6:
                    _V[x] = kk;
                    break;
                case 7:
                    _V[x] += kk;
                    break;
                case 8:
                    switch (p)
                    {
                        case 0:
                            _V[x] = _V[y];
                            break;
                        case 1:
                            _V[x] |= _V[y];
                            break;
                        case 2:
                            _V[x] &= _V[y];
                            break;
                        case 3:
                            _V[x] ^= _V[y];
                            break;
                        case 4:
                            _V[0xF] = (byte)(_V[x] + _V[y] > byte.MaxValue ? 1 : 0);
                            _V[x] += _V[y];
                            break;
                        case 5:
                            _V[0xF] = (byte)(_V[x] > _V[y] ? 1 : 0);
                            _V[x] -= _V[y];
                            break;
                        case 6:
                            _V[0xF] = (byte)(_V[x] & 0x0001);
                            _V[x] >>= 1;
                            break;
                        case 7:
                            _V[0xF] = (byte)(_V[y] > _V[x] ? 1 : 0);
                            _V[x] = (byte)(_V[y] - _V[x]);
                            break;
                        case 0xE:
                            _V[0xF] = (byte)((_V[x] & 0x80) == 0x80 ? 1 : 0);
                            _V[x] <<= 1;
                            break;

                        default:
                            Console.WriteLine( new UnsupportedInstructionException(opcode.ToString("X4")).Message);
                            Environment.Exit(-1);
                            break;
                    }
                    break;
                case 9:
                    if (_V[x] != _V[y]) _PC += 2;
                    break;
                case 0xA:
                    _I = nnn;
                    break;
                case 0xB:
                    _PC = (ushort)(nnn + _V[0]);
                    break;
                case 0xC:
                    _V[x] = (byte)(_random.Next(byte.MaxValue + 1) & kk);
                    break;
                case 0xD:
                    int x1 = _V[x];
                    int y1 = _V[y];

                    _V[15] = 0;

                    for (int i = 0; i < p; i++)
                    {
                        byte mem = _memory[_I + i];

                        for (int j = 0; j < 8; j++)
                        {
                            byte pixel = (byte)((mem >> (7 - j)) & 0x01);
                            int index = x1 + j + (y1 + i) * ScreenWidth;

                            index %= 2048;

                            if (pixel == 1 && _screen[index] != 0) _V[15] = 1;

                            _screen[index] =(byte)(_screen[index] ^ pixel);
                        }
                    }
                    break;
                case 0xE:

                    if (kk == 0x9E)
                    {
                        if ((_currentKey == _keyboardMapper.Convert(_V[x])))
                            _PC += 2;
                    }
                    else if (kk == 0xA1)
                    {
                        if ((_currentKey != _keyboardMapper.Convert(_V[x])))
                            _PC += 2;
                    }
                    else
                    {
                        Console.WriteLine( new UnsupportedInstructionException(opcode.ToString("X4")).Message);
                        Environment.Exit(-1);
                    }
                    break;
                case 0xF:
                    switch (kk)
                    {
                        case 0x07:
                            _V[x] = _delayTimer;
                            break;
                        case 0x0A:
                            while (_currentKey != _keyboardMapper.Convert(_V[x]))
                            {
                                WaitingForKey?.Invoke(this, EventArgs.Empty);
                                autoEvent.WaitOne();
                            }
                            break;
                        case 0x15:
                            _delayTimer = _V[x];
                            break;
                        case 0x18:
                            _soundTimer = _V[x];
                            break;
                        case 0x1E:
                            _I += _V[x];
                            break;
                        case 0x29:
                            _I = (ushort)(_V[x] * 5);
                            break;
                        case 0x33:
                            _memory[_I] = (byte)(_V[x] / 100);
                            _memory[_I + 1] = (byte)((_V[x] % 100) / 10);
                            _memory[_I + 2] = (byte)(_V[x] % 10);
                            break;
                        case 0x55:
                            Array.Copy(_V, 0, _memory, _I, x + 1);
                            break;
                        case 0x65:
                            Array.Copy(_memory, _I, _V, 0, x + 1);
                            break;

                        default:
                            Console.WriteLine( new UnsupportedInstructionException(opcode.ToString("X4")).Message);
                            Environment.Exit(-1);
                            break;
                    }
                    break;

                default:
                    Console.WriteLine( new UnsupportedInstructionException(opcode.ToString("X4")).Message);
                    Environment.Exit(-1);
                    break;             
            }
        }

        public void IncreaseCpuClocking()
        {
            if (_cpuClocking < 1000)
                _cpuClocking+=100;
        }

        public void DecreaseCpuClocking()
        {
            if (_cpuClocking > 200)
                _cpuClocking-=100;
        }

        public void IncreaseTimerSpeed()
        {
            if (_timerSpeed < 80)
                _timerSpeed += 10;
        }

        public void DecreaseTimerSpeed()
        {
            if (_timerSpeed > 20)
                _timerSpeed -= 20;
        }
    }
}
