using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace NeuralNetwork2048_v2
{
    public class Puzzle
    {
        private Random r = new Random(0);
        public Puzzle(int width = 4, int height = 4, int max = 2048)
        {
            Width = width;
            Height = height;
            Max = max;

            Grid = new int[Width, Height];

            var n1 = Convert.ToInt32((r.NextDouble() * Width * Height) - 0.5);
            var n2 = Convert.ToInt32((r.NextDouble() * Width * Height) - 0.5);
            while (n2 == n1)
            {
                n2 = Convert.ToInt32((r.NextDouble() * Width * Height) - 0.5);
            }

            Grid[n1 % Width, n1 / Width] = r.NextDouble() > 0.9 ? 4 : 2;
            Grid[n2 % Width, n2 / Width] = r.NextDouble() > 0.9 ? 4 : 2;

            EmptyCount = (Width * Height) - 2;
        }

        public int Width;
        public int Height;
        public int Max;
        public int[,] Grid;
        public double Score = 0;

        public int HighestOnGrid()
        {
            var high = 0;
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (Grid[x, y] > high) { high = Grid[x, y]; }
                }
            }
            return high;
        }

        private readonly List<Point> EmptyTiles = new List<Point>();
        public int EmptyCount;
        public float EmptyPercent()
        {
            return EmptyCount * 1.0f / (Width * Height);
        }

        public bool hasWon = false;
        public bool hasFinished = false;

        public MoveDirection LastMove;

        /// <summary>
        /// Tries moves in order of preference
        /// </summary>
        /// <param name="moves"></param>
        public void TryMoves(Puzzle.MoveDirection[] moves)
        {
            for (var i = 0; i < moves.Length; i++)
            {
                if (Move(moves[i]))
                    return;
            }
            // no valid moves
            Lose();
        }

        public bool Move(MoveDirection dir)
        {
            LastMove = dir;
            var changed = PlayMove(dir);

            if (changed)
            {
                SpawnNewTile();
            }

            return changed;
        }

        private void SpawnNewTile()
        {
            // spawn next tile
            EmptyTiles.Clear();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (Grid[x, y] == 0)
                    {
                        EmptyTiles.Add(new Point(x, y));
                    }
                }
            }

            var n = Convert.ToInt32((r.NextDouble() * EmptyTiles.Count) - 0.5);
            Grid[EmptyTiles[n].X, EmptyTiles[n].Y] = r.NextDouble() > 0.9 ? 4 : 2;
            EmptyCount = EmptyTiles.Count - 1;
        }

        private bool PlayMove(MoveDirection dir)
        {
            var changed = false;
            switch (dir)
            {
                case MoveDirection.up:
                    for (var y = 1; y < Height; y++)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            MoveTile(x, y, 0, -y, ref changed);
                        }
                    }
                    break;
                case MoveDirection.down:
                    for (var y = Height - 2; y >= 0; y--)
                    {
                        for (var x = 0; x < Width; x++)
                        {
                            MoveTile(x, y, 0, Height - 1 - y, ref changed);
                        }
                    }
                    break;
                case MoveDirection.left:
                    for (var x = 1; x < Width; x++)
                    {
                        for (var y = 0; y < Height; y++)
                        {
                            MoveTile(x, y, -x, 0, ref changed);
                        }
                    }
                    break;
                case MoveDirection.right:
                    for (var x = Width - 2; x >= 0; x--)
                    {
                        for (var y = 0; y < Height; y++)
                        {
                            MoveTile(x, y, Width - 1 - x, 0, ref changed);
                        }
                    }
                    break;
            }
            return changed;
        }

        private void MoveTile(int x, int y, int dx, int dy, ref bool changed)
        {
            var value = Grid[x, y];
            #region dx
            while (dx != 0)
            {
                //x +- in driection of dx
                var nextvalue = Grid[x + Math.Sign(dx), y];
                if (nextvalue == 0)
                { // if grid is empty, move into this square
                    Grid[x, y] = 0;
                    x += Math.Sign(dx);
                    Grid[x, y] = value;
                    dx -= Math.Sign(dx);
                    changed = true;
                }
                else
                { // if grid not empty, ...
                    if (nextvalue == value)
                    { // if same value as current, merge
                        Grid[x, y] = 0;
                        x += Math.Sign(dx);
                        Grid[x, y] = value * 2;
                        Score += value * 2;
                        if (value * 2 == Max)
                        {
                            Win();
                        }
                        dx -= Math.Sign(dx);
                        changed = true;
                    }
                    // else dont.
                    //set dx to 0 to stop other moves.
                    dx = 0;
                }
            }
            #endregion
            #region dy
            while (dy != 0)
            {
                //x +- in driection of dx
                var nextvalue = Grid[x, y + Math.Sign(dy)];
                if (nextvalue == 0)
                { // if grid is empty, move into this square
                    Grid[x, y] = 0;
                    y += Math.Sign(dy);
                    Grid[x, y] = value;
                    dy -= Math.Sign(dy);
                    changed = true;
                }
                else
                { // if grid not empty, ...
                    if (nextvalue == value)
                    { // if same value as current, merge
                        Grid[x, y] = 0;
                        y += Math.Sign(dy);
                        Grid[x, y] = value * 2;
                        Score += value * 2;
                        if (value * 2 == Max)
                        {
                            Win();
                        }
                        dy -= Math.Sign(dy);
                        changed = true;
                    }
                    // else dont.
                    //set dx to 0 to stop other moves.
                    dy = 0;
                }
            }
            #endregion
        }

        private void Win()
        {
            hasWon = true;
            hasFinished = true;
        }
        private void Lose()
        {
            hasWon = false;
            hasFinished = true;
        }

        public enum MoveDirection
        {
            up = 0,
            down = 1,
            left = 2,
            right = 3,
        }
    }
}
