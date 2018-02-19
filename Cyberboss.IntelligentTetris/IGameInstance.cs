namespace Cyberboss.IntelligentInvaders
{
	public interface IGameInstance
	{
		bool GameOver { get; }

		int Score { get; }

		BlockType CurrentBlock { get; }

		BlockType NextBlock { get; }

		int CurrentBlockRotation { get; }

		bool IsBoardCoordinateOccupied(int x, int y);

		void MoveLeft();

		void MoveRight();

		void Rotate();

		void FinishMove();
	}
}
