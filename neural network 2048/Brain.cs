﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace neural_network_2048
{
    public class Brain
    {
        public static Random r;
        public static int NextID;
        //brain with 1 hidden layer
        public Brain(int input=18, int hidden1=18, int hidden2 = 8, int output=4)
        {
            Input = new Matrix(input, 1);
            Hidden1 = new Matrix(hidden1, 1);
            Hidden2 = new Matrix(hidden2, 1);
            Output = new Matrix(output, 1);
            

            ID = NextID;
            NextID++;
        }
        public void Initnew()
        {
            Mutability = r.NextDouble()*0.1;

            Weight1 = new Matrix(Hidden1.Rows, Input.Rows);
            Weight2 = new Matrix(Hidden2.Rows, Hidden1.Rows);
            Weight3 = new Matrix(Output.Rows, Hidden2.Rows);
            for (int i = 1; i <= Weight1.Rows; i++)
            {
                for (int j = 1; j <= Weight1.Columns; j++)
                {
                    Weight1.Set(i, j, r.NextDouble() * 4 - 2);
                }
            }
            for (int i = 1; i <= Weight2.Rows ; i++)
            {
                for (int j = 1; j <= Weight2.Columns; j++)
                {
                    Weight2.Set(i, j, r.NextDouble() * 4 - 2);
                }
            }
            for (int i = 1; i <= Weight3.Rows; i++)
            {
                for (int j = 1; j <= Weight3.Columns; j++)
                {
                    Weight3.Set(i, j, r.NextDouble() * 4 - 2);
                }
            }

            Bias1 = r.NextDouble() * 2 - 1;
            Bias2 = r.NextDouble() * 2 - 1;
            Bias3 = r.NextDouble() * 2 - 1;
        }
        Matrix Input;
        Matrix Hidden1;
        Matrix Hidden2;
        Matrix Output;

        Matrix Weight1;
        Matrix Weight2;
        Matrix Weight3;

        double Bias1;
        double Bias2;
        double Bias3;

        public void CalculateMove(Puzzle P, bool forceMove = false)
        {
            // give data to input matrix
            for (int x=0;x<P.Width;x++)
            {
                for (int y = 0; y < P.Height; y++)
                {
                    double v = P.Grid[x, y] == 0 ? -1 : Math.Log(P.Grid[x, y], 2);
                    double m = Math.Log(P.HighestOnGrid(), 2);
                    Input.Set(x * P.Height + y + 1, 1, v / m);
                }
            }
            //ActivationFunction(Input);
            Input.Set(17, 1, P.BadMove());
            Input.Set(18, 1, P.EmptyPercent());

            if (forceMove && P.EmptyPercent() > 0.7) // force moves at start
            {
                Output.Set(1, 1,0);//x1
                Output.Set(2, 1,1);//y1
                Output.Set(3, 1,1);//x2
                Output.Set(4, 1,0);//y2
            }
            else
            {
                // calculate output
                Hidden1.FromMultiply(Weight1, Input, Bias1);
                ActivationFunction(Hidden1);
                Hidden2.FromMultiply(Weight2, Hidden1, Bias2);
                ActivationFunction(Hidden1);
                Output.FromMultiply(Weight3, Hidden2, Bias3);
                ActivationFunction(Output);
            }

            
        }
        public void MakeMove(Puzzle P)
        {
            double x, y;
            if (P.DoForceMove)
            {
                x = Output.Get(3, 1);
                y = Output.Get(4, 1);
            } 
            else
            {
                x = Output.Get(1, 1);
                y = Output.Get(2, 1);
            }
            

            if (x*x > y*y)
            { // x bigger
                if (x>0)
                { // positive  
                    P.Move(Puzzle.MoveDirection.right);
                }
                else
                {
                    P.Move(Puzzle.MoveDirection.left);
                }
            }
            else
            { // y bigger
                if (y > 0)
                { // positive  
                    P.Move(Puzzle.MoveDirection.down);
                }
                else
                {
                    P.Move(Puzzle.MoveDirection.up);
                }
            }

        }

        public void ActivationFunction(Matrix A)
        {
            for (int i = 1; i <= A.Rows; i++)
            {
                for (int j = 1; j <= A.Columns; j++)
                {
                    A.Set(i, j, Math.Tanh(A.Get(i, j)));
                }
            }
        }

        public double Fitness;
        public int ID;
        public int ParentID;
        public int Generation = 0;

        public double Mutability;
        private double MaxMuta = 0.2;
        private double MinMuta = 0.001;
        /// <summary>
        /// how much less mutability will change compated to other vars.
        /// </summary>
        private double DeltaMuta = 4;

        public Brain Evolve()
        {
            Brain child = new Brain(Input.Rows, Hidden1.Rows, Hidden2.Rows, Output.Rows);
            child.Weight1 = MatrixEvolve(Weight1);
            child.Weight2 = MatrixEvolve(Weight2);
            child.Weight3 = MatrixEvolve(Weight3);
            child.Bias1 = Bias1 * Math.Pow(1.05, r.NextDouble() - 0.5);
            child.Bias2 = Bias2 * Math.Pow(1.05, r.NextDouble() - 0.5);
            child.Bias3 = Bias3 * Math.Pow(1.05, r.NextDouble() - 0.5);
            //child.Mutability = Mutability + (r.NextDouble() - 0.5) * Mutability / DeltaMuta;
            if (child.Mutability > MaxMuta) { child.Mutability = MaxMuta; }
            if (child.Mutability < MinMuta) { child.Mutability = MinMuta; }

            child.Generation = Generation + 1;
            child.ParentID = ID;
            return child;
        }
        private Matrix MatrixEvolve(Matrix A)
        {
            Matrix R = A.Clone();
            for (int i = 1; i <= R.Rows; i++)
            {
                for (int j = 1; j <= R.Columns; j++)
                {
                    R.Set(i, j, R.Get(i, j) * Math.Pow(1.05, r.NextDouble() - 0.5));
                }
            }

            return R;
        }
    }
}
