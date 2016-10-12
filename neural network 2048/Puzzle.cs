using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace neural_network_2048
{
    public class Puzzle
    {
        Random r = new Random(0);
        public Puzzle(int width = 4, int height = 4, int max = 2048)
        {
            Width = width;
            Height = height;
            Max = max;

            Grid = new int[Width, Height];
            BadMovesTillLose = BadMovesTillLoseMax;

            int n1 = Convert.ToInt32(r.NextDouble() * Width * Height - 0.5);
            int n2 = Convert.ToInt32(r.NextDouble() * Width * Height - 0.5);
            while (n2 == n1)
            {
                n2 = Convert.ToInt32(r.NextDouble() * Width * Height - 0.5);
            }

            Grid[n1 % Width, n1 / Width] = r.NextDouble() > 0.9 ? 4 : 2;
            Grid[n2 % Width, n2 / Width] = r.NextDouble() > 0.9 ? 4 : 2;

            EmptyCount = Width * Height - 2;
        }

        public int Width;
        public int Height;
        public int Max;
        public int[,] Grid;
        public double Score = 0;

        public int HighestOnGrid() {
            int high = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Grid[x,y] > high) { high = Grid[x, y]; }
                }
            }
            return high;
        }

        public int BadMove() { return ChangedThisMove ? 1 : 0; }
        public bool DoForceMove = false;
        private bool ChangedThisMove = false;
        private int BadMovesTillLose;
        private int BadMovesTillLoseMax = 10;
        private List<Point> EmptyTiles;
        private int EmptyCount;
        public double EmptyPercent()
        {
            return EmptyCount*1.0/(Width*Height);
        }

        public bool hasWon = false;
        public bool hasLost = false;

        public MoveDirection LastMove;

        public void Move(MoveDirection dir)
        {
            LastMove = dir;
            ChangedThisMove = false;
            switch (dir)
            {
                case MoveDirection.up:
                    for (int y = 1; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            MoveTile(x, y, 0, -y);
                        }
                    } 
                    break;
                case MoveDirection.down:
                    for (int y = Height-2; y >= 0; y--)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            MoveTile(x, y, 0, (Height-1) - y);
                        }
                    }
                    break;
                case MoveDirection.left:
                    for (int x = 1; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            MoveTile(x, y, -x, 0);
                        }
                    }
                    break;
                case MoveDirection.right:
                    for (int x = Width-2; x >= 0; x--)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            MoveTile(x, y, (Width - 1) - x, 0);
                        }
                    }
                    break;
            }

            
            if (DoForceMove && !ChangedThisMove)
            {
                ForceMove(dir);
            }

            if (ChangedThisMove)
            {
                BadMovesTillLose = BadMovesTillLoseMax;
                DoForceMove = false;
                EmptyTiles = new List<Point>();
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (Grid[x, y] == 0)
                        {
                            EmptyTiles.Add(new Point(x, y));
                        }
                    }
                }

                int n = Convert.ToInt32(r.NextDouble() * EmptyTiles.Count-0.5);
                Grid[EmptyTiles[n].X, EmptyTiles[n].Y] = r.NextDouble() > 0.9 ? 4 : 2;
                EmptyCount = EmptyTiles.Count - 1;
            }
            else
            {
                BadMovesTillLose--;
                DoForceMove = true;
                if (BadMovesTillLose == 0)
                {
                    Lose();
                }
            }
        }

        

        private void MoveTile(int x, int y, int dx, int dy)
        {
            int value = Grid[x, y];
            #region dx
            while (dx != 0)
            {
                //x +- in driection of dx
                int nextvalue = Grid[x + Math.Sign(dx), y];
                if (nextvalue == 0) 
                { // if grid is empty, move into this square
                    Grid[x, y] = 0;
                    x += Math.Sign(dx);
                    Grid[x, y] = value;
                    dx -= Math.Sign(dx);
                    ChangedThisMove = true;
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
                        ChangedThisMove = true;
                    }
                    // else dont.
                    //set dx to 0 to stop other moves.
                    dx = 0;
                }
            }
            #endregion
            #region dy
            while (dy !=0)
            {
                //x +- in driection of dx
                int nextvalue = Grid[x, y + Math.Sign(dy)];
                if (nextvalue == 0)
                { // if grid is empty, move into this square
                    Grid[x, y] = 0;
                    y += Math.Sign(dy);
                    Grid[x, y] = value;
                    dy -= Math.Sign(dy);
                    ChangedThisMove = true;
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
                        ChangedThisMove = true;
                    }
                    // else dont.
                    //set dx to 0 to stop other moves.
                    dy = 0;
                }
            }
            #endregion
        }
        private void MoveTile(int x, int y, int dx, int dy, bool changedbefore ,out bool changed)
        {
            changed = changedbefore;
            int value = Grid[x, y];
            #region dx
            while (dx != 0)
            {
                //x +- in driection of dx
                int nextvalue = Grid[x + Math.Sign(dx), y];
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
                int nextvalue = Grid[x, y + Math.Sign(dy)];
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
        }
        private void Lose()
        {
            hasLost = true;
        }

        private void ForceMove(MoveDirection dir)
        {
            List<MoveDirection> otherdir = new List<MoveDirection>();
            if (dir != MoveDirection.up) { otherdir.Add(MoveDirection.up); }
            if (dir != MoveDirection.down) { otherdir.Add(MoveDirection.down); }
            if (dir != MoveDirection.left) { otherdir.Add(MoveDirection.left); }
            if (dir != MoveDirection.right) { otherdir.Add(MoveDirection.right); }
            bool forcechanged;

            

            for (int n = 0;n<3;n++)
            {
                int a = Convert.ToInt32(r.NextDouble() * otherdir.Count - .5);
                forcechanged = false;
                switch (otherdir[a])
                {
                    case MoveDirection.up:
                        for (int y = 1; y < Height; y++)
                        {
                            for (int x = 0; x < Width; x++)
                            {
                                MoveTile(x, y, 0, -y, forcechanged, out forcechanged);
                            }
                        }
                        break;
                    case MoveDirection.down:
                        for (int y = Height - 2; y >= 0; y--)
                        {
                            for (int x = 0; x < Width; x++)
                            {
                                MoveTile(x, y, 0, (Height - 1) - y, forcechanged, out forcechanged);
                            }
                        }
                        break;
                    case MoveDirection.left:
                        for (int x = 1; x < Width; x++)
                        {
                            for (int y = 0; y < Height; y++)
                            {
                                MoveTile(x, y, -x, 0, forcechanged, out forcechanged);
                            }
                        }
                        break;
                    case MoveDirection.right:
                        for (int x = Width - 2; x >= 0; x--)
                        {
                            for (int y = 0; y < Height; y++)
                            {
                                MoveTile(x, y, (Width - 1) - x, 0, forcechanged, out forcechanged);
                            }
                        }
                        break;
                }
                if (forcechanged)
                {
                    ChangedThisMove = true;
                    return;
                }
                otherdir.RemoveAt(a);
            }
        }


        public enum MoveDirection
        {
            up, down, left, right
        }
    }
    
}
