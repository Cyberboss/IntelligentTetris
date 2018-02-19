using System;
using Cyberboss.IntelligentInvaders;

namespace Tetris.GameEngine
{
    public class Piece: ICloneable
    {
		#region Private Fields

		public BlockType BlockType { get; private set; }

        private int[,] _piece;
        private int _initPosX;
        private int _initPosY;

		int rotationCounter;

        #endregion

        #region Constructors

        public Piece(int[,] p, BlockType blockType)
        {
            if (p == null)
            {
                throw new ArgumentNullException();
            }
            _piece = (int[,])p.Clone();
            _initPosY = (p.GetUpperBound(0) + 1) * -1;
            _initPosX = 0;
			BlockType = blockType;
        }

        #endregion

        #region Public Properties

        public int Height
        {
            get
            {
                return _piece.GetUpperBound(0) + 1;
            }
        }

        public int Width
        {
            get
            {
                return _piece.GetUpperBound(1) + 1;
            }
        }

        public int InitPosX
        {
            get 
            { 
                return _initPosX; 
            }
        }

        public int InitPosY
        {
            get 
            { 
                return _initPosY; 
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Rotates a Piece clockwise
        /// </summary>
        /// <returns>rotated Piece</returns>
        public Piece RotateRight()
        {
            int[,] rotated = new int[this.Width, this.Height];
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < Width; j++)
                {
                    rotated[j, Height - i - 1] = _piece[i, j];
                }
            }
            var p = new Piece( rotated , BlockType);
			p.rotationCounter = rotationCounter++;
			return p;
        }

		public int Rotation => rotationCounter % 4;

        public void MakeItShadow()
        {
            for (int i = 0; i < this.Height; i++)
            {
                for (int j = 0; j < this.Width; j++)
                {
                    if (this[i, j] != 0)
                    {
                        _piece[i, j] = 8;
                    }
                }
            }
        }

        public int[,] ToArray()
        {
            return _piece;
        }

        #endregion

        #region Public Indexers

        public int this[int h, int w]
        {
            get 
            {
                if ( ( h < 0 ) || ( h >= this.Height ) || ( w < 0 ) || ( w >= this.Width ) )
                {
                    throw new IndexOutOfRangeException("Index is out of range!");
                }
                return _piece[h, w];
            }
        }

        #endregion

        #region ICloneable Implementation

        public object Clone()
        {
            return new Piece(this._piece, BlockType);
        }

        #endregion
    }
}