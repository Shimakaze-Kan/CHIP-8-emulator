using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CHIP_8_Emulator;
using System.Threading.Tasks;
using System.Threading;

namespace GameWindow
{
    public class App : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Motherboard _motherboard;
        private Texture2D _rect;
        private Color _pixelColor;
        private byte[] _screen;
        private bool _pause = false;
        private bool _waitForKey = false;
        private string _path;
        private TimeSpan _lastPause;
        private TimeSpan _lastCpuClockingChange;
        private TimeSpan _lastTimerSpeedChange;
        private TimeSpan _lastReset;
        private SpriteFont _font12;
        private SpriteFont _font20;
        private SpriteFont _font25;
        private Task _emulatorTask;
        private CancellationTokenSource _tokenSource2;


        public App(string path)
        {
            _path = path;
            _motherboard = new Motherboard(_path);
            _pixelColor = Color.White;//pixelColor;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferHeight = 350;
            _graphics.PreferredBackBufferWidth = 860;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            InitializeEmulator();
        }

        protected override void Initialize()
        {
            _font12 = Content.Load<SpriteFont>("font12");
            _font20 = Content.Load<SpriteFont>("font20");
            _font25 = Content.Load<SpriteFont>("font25");

            if (_rect is null)
            {
                _rect = new Texture2D(_graphics.GraphicsDevice, 1, 1);
                _rect.SetData(new[] { Color.White });
            }

            if (_screen is null)
            {
                _screen = new byte[Motherboard.ScreenHeight * Motherboard.ScreenWidth];
            }

            base.Initialize();
        }

        protected void InitializeEmulator()
        {
            _motherboard.EndOfExecution += new EventHandler(delegate (Object o, EventArgs a) { _pause = true; });
            _motherboard.WaitingForKey += new EventHandler(delegate (Object o, EventArgs a) { _waitForKey = true; });

            _tokenSource2 = new CancellationTokenSource();
            CancellationToken ct = _tokenSource2.Token;

            
            _emulatorTask = Task.Run(() => _motherboard.StartExecuting(ct), ct);

            // try
            // {
            //     _emulatorTask.Wait();
            // }
            // catch(AggregateException ae)
            // {
            //     foreach(var ex in ae.InnerExceptions)
            //     {
            //         if(ex is UnsupportedInstructionException)
            //         {
            //             Console.WriteLine(ex.Message);
            //             Environment.Exit(-1);
            //         }
            //         else
            //             throw;
            //     }
            // }
        }

        protected void PauseEmulatorTask() => _motherboard.pause = true;

        protected void ResumeEmulatorTask()
        {
            _motherboard.pause = false;
            _motherboard.autoEvent.Set();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            _tokenSource2.Cancel();
            base.OnExiting(sender, args);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            var keyboard = Keyboard.GetState();
            var keys = keyboard.GetPressedKeys();

            if (keys.Length > 0)
            {
                _motherboard.SetCurrentKey((ushort)(keys[0]));

                if (_waitForKey)
                {
                    ResumeEmulatorTask();
                    _waitForKey = false;
                }
            }
            else
                _motherboard.SetCurrentKey(0);

            _screen = _motherboard.GetScreenData;

            if ((gameTime.TotalGameTime - _lastPause).Milliseconds > 200 && keyboard.IsKeyDown(Keys.Space))
            {
                if (_pause)
                {
                    ResumeEmulatorTask();
                }
                else
                {
                    PauseEmulatorTask();
                }
                _pause = !_pause;
                _lastPause = gameTime.TotalGameTime;
            }

            if ((gameTime.TotalGameTime - _lastCpuClockingChange).Milliseconds > 200)
            {
                _lastCpuClockingChange = gameTime.TotalGameTime;

                if (keyboard.IsKeyDown(Keys.F2))
                    _motherboard.IncreaseCpuClocking();
                else if (keyboard.IsKeyDown(Keys.F1))
                    _motherboard.DecreaseCpuClocking();
            }

            if ((gameTime.TotalGameTime - _lastReset).Milliseconds > 200 && keyboard.IsKeyDown(Keys.F5))
            {
                _lastReset = gameTime.TotalGameTime;
                _tokenSource2.Cancel();
                _motherboard = new Motherboard(_path);
                InitializeEmulator();
                PauseEmulatorTask();
            }

            if ((gameTime.TotalGameTime - _lastTimerSpeedChange).Milliseconds > 200)
            {
                _lastTimerSpeedChange = gameTime.TotalGameTime;

                if (keyboard.IsKeyDown(Keys.F3))
                    _motherboard.DecreaseTimerSpeed();
                else if (keyboard.IsKeyDown(Keys.F4))
                    _motherboard.IncreaseTimerSpeed();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();


            for (int y = 0; y < Motherboard.ScreenHeight; y++)
            {
                for (int x = 0; x < Motherboard.ScreenWidth; x++)
                {
                    if (_screen[y * Motherboard.ScreenWidth + x] != 0)
                        _spriteBatch.Draw(_rect, new Rectangle(x * 10, y * 10, 10, 10), _pixelColor);
                }
            }

            _spriteBatch.Draw(_rect, new Rectangle(0, 10 * (Motherboard.ScreenHeight + 1), 10 * (Motherboard.ScreenWidth + 1), 1), Color.White * 0.2f);
            _spriteBatch.Draw(_rect, new Rectangle(10 * (Motherboard.ScreenWidth + 1), 0, 1, 10 * (Motherboard.ScreenHeight + 1)), Color.White * 0.2f);

            if (_pause)
            {
                _spriteBatch.Draw(_rect, new Rectangle(0, 5 * Motherboard.ScreenHeight - 10, Motherboard.ScreenWidth * 10, 50), Color.Black * 0.7f);
                _spriteBatch.DrawString(_font25, "Pause", new Vector2(5 * Motherboard.ScreenWidth - 50, Motherboard.ScreenHeight * 5), Color.White);
            }

            var cpuClocking = _motherboard.GetCpuClocking;
            string cpuClockingFormated = cpuClocking.ToString();

            if(cpuClocking==1000)
            {
                cpuClockingFormated=cpuClockingFormated[0]+"."+cpuClockingFormated[1]+"KHz";
            }
            else
            {
                cpuClockingFormated+="Hz";
            }

            _spriteBatch.DrawString(_font20, "Cpu Clocking\n" + cpuClockingFormated, new Vector2(10 * Motherboard.ScreenWidth + 20, 50), Color.White);
            _spriteBatch.DrawString(_font20, "Timer Speed\n" + _motherboard.GetTimerSpeed + "Hz", new Vector2(10 * Motherboard.ScreenWidth + 20, 150), Color.White);
            _spriteBatch.DrawString(_font12, "File: " + _path, new Vector2(0, 10 * (Motherboard.ScreenHeight + 1) + 5), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}