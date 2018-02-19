using System;
using System.Threading;
using Tetris.GameEngine;
using System.Timers;
using Cyberboss.IntelligentInvaders;
using SharpNeat.Network;
using SharpNeat.View.Graph;
using SharpNeat.View;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace TetrisConsoleUI
{
    class TetrisConsoleUI : IGameOrchestrator
    {
		private static TetrisAI tetrisAI;
        private static  ConsoleDrawing _drawer;
		private static NetworkGraphFactory graphFactory = new NetworkGraphFactory();

		private static IOGraphViewportPainter graphViewportPainter;
		private static GraphControl graphControl;
		private static Form form;
		private static int saveCount = 0;

		static void Main(string[] args) => new TetrisConsoleUI().Run();

		void Run()
        {
            //preparing Console
            Console.Clear();
            Console.CursorVisible = false;

            _drawer = new ConsoleDrawing();

			ConsoleDrawing.ShowControls();

			form = new Form();
			form.Width = 1920;
			form.Height = 1080;
			graphViewportPainter = new IOGraphViewportPainter(new IOGraphPainter());
			graphControl = new GraphControl();
			graphControl.ViewportPainter = graphViewportPainter;
			graphControl.Width = form.Width;
			graphControl.Height = form.Height;
			form.Controls.Add(graphControl);
			form.Show();
			form.Visible = false;

			tetrisAI = new TetrisAI(this, (x) =>
			{
				if (++saveCount >= 20)
				{
					saveCount = 0;
					Task.Run(() => form.Invoke((MethodInvoker)delegate {
						tetrisAI.Save();
						Console.WriteLine("Network saved!");
					}));
				}
			}, HandleNewBestNetwork, File.Exists("SavedProgress.xml"));
			tetrisAI.StartTraining();

			Task.Run(() =>
			{
				while (KeyPressedHandler(Console.ReadKey())) ;
				form.Invoke((MethodInvoker)delegate { form.Close(); });
			});

			form.ShowDialog();

			tetrisAI.StopTraining();
			tetrisAI.Save();

            Console.ResetColor();
            Console.CursorVisible = true;
        }

		static void HandleNewBestNetwork(INetworkDefinition networkDefinition)
		{
			graphViewportPainter.IOGraph = graphFactory.CreateGraph(networkDefinition);
			form.Invoke((MethodInvoker)delegate {
				graphControl.RefreshImage();
			});
		}

        private static bool KeyPressedHandler(ConsoleKeyInfo input_key)
        {
            switch (input_key.Key)
            {
                case ConsoleKey.Spacebar:
					tetrisAI.RunDemo();
                    break;
				case ConsoleKey.S:
					tetrisAI.Save();
					Console.WriteLine("Network saved!");
					break;
				case ConsoleKey.P:
					tetrisAI.PauseTraining();
					Console.WriteLine("Learning paused!");
					break;
				case ConsoleKey.R:
					tetrisAI.StartTraining();
					Console.WriteLine("Learning resumed!");
					break;
				case ConsoleKey.Escape:
					return false;
                default:
                    break;
            }
			return true;
        }

		public IGameInstance CreateGameInstance(bool demoMode)
		{
			var res = new Game();
			if (demoMode)
			{
				Console.Clear();
				res.OnMoveFinished = () =>
				{
					_drawer.DrawScene(res);
					Thread.Sleep(800);
				};
			}
			res.Start();
			return res;
		}
	}
}