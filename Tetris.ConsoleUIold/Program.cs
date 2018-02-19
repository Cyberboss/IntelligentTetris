using System;
using System.Threading;
using Tetris.GameEngine;
using System.Timers;
using Cyberboss.IntelligentInvaders;

namespace TetrisConsoleUI
{
    class TetrisConsoleUI : IGameOrchestrator
    {
        private static Game _game;
		private static TetrisAI tetrisAI;
        private static  ConsoleDrawing _drawer;
        private static  System.Timers.Timer _gameTimer;
        private static int _timerCounter = 0;
        private static readonly int _timerStep = 10;

		static void Main(string[] args) => new TetrisConsoleUI().Run();

		void Run()
        {
            //preparing Console
            Console.Clear();
            Console.CursorVisible = false;

            _drawer = new ConsoleDrawing();

            ConsoleDrawing.ShowControls();

			tetrisAI = new TetrisAI(this, (x) => Console.WriteLine("Generation: " + x), (x) => Console.WriteLine("Top Fitness: " + x));
			tetrisAI.StartTraining();

            while (KeyPressedHandler(Console.ReadKey(true)))
            {

            }

            Console.ResetColor();
            Console.CursorVisible = true;
        }

        private static bool KeyPressedHandler(ConsoleKeyInfo input_key)
        {
            switch (input_key.Key)
            {
                case ConsoleKey.Spacebar:
					tetrisAI.RunDemo();
					break;
                case ConsoleKey.N:
                    _game.NextPieceMode = !_game.NextPieceMode;
                    break;
                case ConsoleKey.Escape:
					return false;
                default:
                    break;
            }
			return true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (_game.Status != Game.GameStatus.Finished)
            {
                if (_game.Status != Game.GameStatus.Paused)
                {
                    _timerCounter += _timerStep;
                    _game.MoveDown();
                    if (_game.Status == Game.GameStatus.Finished)
                    {
                        _gameTimer.Stop();
                    }
                    else
                    {
                        _drawer.DrawScene(_game);
                        if ( _timerCounter >= ( 1000 - (_game.Lines * 10) ) )
                        {
                            _gameTimer.Interval -= 50;
                            _timerCounter = 0;
                        }
                    }
                }
            }
        }

		public IGameInstance CreateGameInstance(bool demoMode)
		{
			var res = new Game();
			if (demoMode)
				res.OnMoveFinished = () =>
				{
					_drawer.DrawScene(res);
					Thread.Sleep(800);
				};
			res.Start();
			return res;
		}
	}
}