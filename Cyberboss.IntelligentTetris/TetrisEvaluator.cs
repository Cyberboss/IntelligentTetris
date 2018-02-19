using SharpNeat.Core;
using SharpNeat.Phenomes;
using System;
using System.Threading;

namespace Cyberboss.IntelligentInvaders
{
	sealed class TetrisEvaluator : IPhenomeEvaluator<IBlackBox>
	{
		const int GameBoardWidth = 10;
		const int GameBoardHeight = 22;

		const int BaseInputs = 7;
		public const int NumInputs = BaseInputs + GameBoardHeight * GameBoardWidth;
		public const int NumOutputs = 3;

		static int NumBlockTypes = Enum.GetNames(typeof(BlockType)).Length;

		public int MaxFitness { get; private set; }

		public ulong EvaluationCount { get; private set; }

		public bool StopConditionSatisfied => false;

		readonly IGameOrchestrator gameOrchestrator;
		readonly bool demoMode;

		public TetrisEvaluator(IGameOrchestrator gameOrchestrator, bool demoMode)
		{
			this.gameOrchestrator = gameOrchestrator ?? throw new ArgumentNullException(nameof(gameOrchestrator));
			this.demoMode = demoMode;
		}

		static void BlockTypeToInput(ISignalArray array, int offset, BlockType blockType)
		{
			for (var I = 0; I < NumBlockTypes; ++I)
				array[I + offset] = I == (int)blockType ? 1 : -1;
		}

		static void SetInputs(ISignalArray inputs, IGameInstance gameInstance)
		{
			/*
			input
			 0-6 next piece
			 game width * game height
			 
			 */
			BlockTypeToInput(inputs, 0, gameInstance.NextBlock);

			for (var I = BaseInputs; I < BaseInputs + GameBoardWidth * GameBoardHeight; ++I)
			{
				var Imin2 = I - BaseInputs;
				inputs[I] = gameInstance.IsBoardCoordinateOccupied(Imin2 % GameBoardWidth, Imin2 / GameBoardWidth) ? 1 : -1;
			}
		}

		static bool CheckOutputs(ISignalArray output, IGameInstance gameInstance)
		{
			/*
			outputs:
			move < -.5 = left > .5 = right
			 rotate >= .5 
			 drop > .5
			 */
			if (output[0] <= -0.5)
				gameInstance.MoveLeft();
			else if (output[0] >= 0.5)
				gameInstance.MoveRight();

			if (output[1] >= 0.5)
				gameInstance.Rotate();

			if (output[2] > 0.5)
			{
				gameInstance.FinishMove();
				return true;
			}
			return false;
		}

		public FitnessInfo Evaluate(IBlackBox phenome)
		{
			if (phenome == null)
				throw new ArgumentNullException(nameof(phenome));

			if (phenome.InputCount != NumInputs || phenome.OutputCount != NumOutputs)
				throw new ArgumentOutOfRangeException(nameof(phenome), "Invalid input/output count!");

			++EvaluationCount;

			var game = gameOrchestrator.CreateGameInstance(demoMode);

			const int MaxMoves = 20;
			var movesCounter = 0;
			bool turnInProgress;
			bool droppedEveryTime = true;
			do
			{
				if (movesCounter >= MaxMoves)
					//retarded, get out of here
					return new FitnessInfo(0, 0);
				phenome.ResetState();
				SetInputs(phenome.InputSignalArray, game);
				phenome.Activate();
				++movesCounter;
				turnInProgress = !CheckOutputs(phenome.OutputSignalArray, game);
				if (!turnInProgress)
					movesCounter = 0;
				else
					droppedEveryTime = false;
			}
			while (!game.GameOver);

			var fitness = 1;   //they finished a game at least, infinitely better

			//award them somewhat for total board coverage
			for (var I = 0; I < GameBoardWidth * GameBoardHeight; ++I) {
				var row = I / GameBoardWidth;
				if (game.IsBoardCoordinateOccupied(I % GameBoardWidth, row))
					if (row < GameBoardHeight - 1)
						++fitness;
					//a large factor more for bottom row coverage
					else
						fitness += 100;
			}

			if (droppedEveryTime)
				//i fucking hate you
				fitness = 1;

			//award them GENEROUSLY for lines cleared
			fitness += 1000 * game.Score;

			lock (this)
				MaxFitness = Math.Max(MaxFitness, fitness);
			return new FitnessInfo(fitness, fitness);
		}

		public void Reset()
		{
		}
	}
}
