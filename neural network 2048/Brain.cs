using System;
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
        public Brain(int input = 18, int hidden1 = 18, int hidden2 = 12, int hidden3 = 8, int output = 4)
        {
            Input = new Matrix(input, 1);
            Hidden1 = new Matrix(hidden1, 1);
            Hidden2 = new Matrix(hidden2, 1);
            Hidden3 = new Matrix(hidden3, 1);
            Output = new Matrix(output, 1);


            ID = NextID;
            NextID++;
        }
        public void Initnew()
        {
            //Mutability = r.NextDouble() * 0.1;

            Weight1 = new Matrix(Hidden1.Rows, Input.Rows);
            Weight2 = new Matrix(Hidden2.Rows, Hidden1.Rows);
            Weight3 = new Matrix(Hidden3.Rows, Hidden2.Rows);
            Weight4 = new Matrix(Output.Rows, Hidden3.Rows);
            for (int i = 0; i < Weight1.Rows; i++)
            {
                for (int j = 0; j < Weight1.Columns; j++)
                {
                    Weight1.Data[i][j] = r.NextDouble() * 4 - 2;
                }
            }
            for (int i = 0; i < Weight2.Rows; i++)
            {
                for (int j = 0; j < Weight2.Columns; j++)
                {
                    Weight2.Data[i][j] = r.NextDouble() * 4 - 2;
                }
            }
            for (int i = 0; i < Weight3.Rows; i++)
            {
                for (int j = 0; j < Weight3.Columns; j++)
                {
                    Weight3.Data[i][j] = r.NextDouble() * 4 - 2;
                }
            }
            for (int i = 0; i < Weight4.Rows; i++)
            {
                for (int j = 0; j < Weight4.Columns; j++)
                {
                    Weight4.Data[i][j] = r.NextDouble() * 4 - 2;
                }
            }
            

            Bias1 = r.NextDouble() * 2 - 1;
            Bias2 = r.NextDouble() * 2 - 1;
            Bias3 = r.NextDouble() * 2 - 1;
            Bias4 = r.NextDouble() * 2 - 1;
        }
        Matrix Input;
        Matrix Hidden1;
        Matrix Hidden2;
        Matrix Hidden3;
        Matrix Output;

        Matrix Weight1;
        Matrix Weight2;
        Matrix Weight3;
        Matrix Weight4;

        double Bias1;
        double Bias2;
        double Bias3;
        double Bias4;

        public void CalculateMove(Puzzle P, bool forceMove = false)
        {
            // give data to input matrix
            double m = Math.Log(P.HighestOnGrid(), 2);
            for (int x = 0; x < P.Width; x++)
            {
                for (int y = 0; y < P.Height; y++)
                {
                    double v = P.Grid[x, y] == 0 ? -1 : Math.Log(P.Grid[x, y], 2);
                    Input.Data[x * P.Height + y][0] = v / m;
                }
            }
            //ActivationFunction(Input);
            Input.Data[16][0] = P.BadMove();
            Input.Data[17][0] = P.EmptyPercent();

            if (forceMove && P.EmptyPercent() > 0.7) // force moves at start
            {
                Output.Data[0][0] = 0;//x1
                Output.Data[1][0] = 1;//y1
                Output.Data[2][0] = 1;//x2
                Output.Data[3][0] = 0;//y2
            }
            else
            {
                // calculate output
                Hidden1.ActivationFromMultiply(Weight1, Input, Bias1);
                Hidden2.ActivationFromMultiply(Weight2, Hidden1, Bias2);
                Hidden3.ActivationFromMultiply(Weight3, Hidden2, Bias3);
                Output.ActivationFromMultiply(Weight4, Hidden3, Bias4);
            }


        }
        public void MakeMove(Puzzle P)
        {
            double x, y;
            if (P.DoForceMove)
            {
                x = Output.Data[2][0];
                y = Output.Data[3][0];
            }
            else
            {
                x = Output.Data[0][0];
                y = Output.Data[1][0];
            }


            if (x * x > y * y)
            { // x bigger
                if (x > 0)
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
            for (int i = 0; i < A.Rows; i++)
            {
                for (int j = 0; j < A.Columns; j++)
                {
                    A.Data[i][j] = Math.Tanh(A.Data[i][j]);
                }
            }
        }

        public double Fitness;
        public int ID;
        //public int ParentID;
        //public int Generation = 0;

        //public double Mutability;
        //private double MaxMuta = 0.2;
        //private double MinMuta = 0.001;
        /// <summary>
        /// how much less mutability will change compated to other vars.
        /// </summary>
        //private double DeltaMuta = 4;

        public Brain Evolve()
        {
            Brain child = new Brain(Input.Rows, Hidden1.Rows, Hidden2.Rows, Hidden3.Rows, Output.Rows);
            child.Weight1 = MatrixEvolve(Weight1);
            child.Weight2 = MatrixEvolve(Weight2);
            child.Weight3 = MatrixEvolve(Weight3);
            child.Weight4 = MatrixEvolve(Weight4);
            child.Bias1 = Bias1 * Math.Pow(1.05, r.NextDouble() - 0.5);
            if (Math.Abs(child.Bias1) < 0.01 * r.NextDouble())
            { child.Bias1 *= -1; }
            child.Bias2 = Bias2 * Math.Pow(1.05, r.NextDouble() - 0.5);
            if (Math.Abs(child.Bias2) < 0.01 * r.NextDouble())
            { child.Bias2 *= -1; }
            child.Bias3 = Bias3 * Math.Pow(1.05, r.NextDouble() - 0.5);
            if (Math.Abs(child.Bias3) < 0.01 * r.NextDouble())
            { child.Bias3 *= -1; }
            child.Bias4 = Bias4 * Math.Pow(1.05, r.NextDouble() - 0.5);
            if (Math.Abs(child.Bias4) < 0.01 * r.NextDouble())
            { child.Bias4 *= -1; }
            //child.Mutability = Mutability + (r.NextDouble() - 0.5) * Mutability / DeltaMuta;
            //if (child.Mutability > MaxMuta) { child.Mutability = MaxMuta; }
            //if (child.Mutability < MinMuta) { child.Mutability = MinMuta; }

            //child.Generation = Generation + 1;
            //child.ParentID = ID;
            return child;
        }
        private Matrix MatrixEvolve(Matrix A)
        {
            Matrix R = A.Clone();
            for (int i = 0; i < R.Rows; i++)
            {
                for (int j = 0; j < R.Columns; j++)
                {
                    // only mutate at 10% chance
                    if (r.NextDouble() < 0.1) 
                    {
                        R.Data[i][j] = R.Data[i][j] * Math.Pow(1.05, r.NextDouble() - 0.5);
                    }
                    // only mutate (flip sign) at 0.01%/value
                    // examples:
                    // if value = 0.01 then there is 10% chance of fliping sign
                    // if value is 0.1 then there is a 1% chance of flipping
                    if (r.NextDouble() < 0.001/ R.Data[i][j]) 
                    { R.Data[i][j] *= -1; }
                }
            }

            return R;
        }

        public Brain Evolve(double mutate, double sign)
        {
            Brain child = new Brain(Input.Rows, Hidden1.Rows, Hidden2.Rows, Hidden3.Rows, Output.Rows);
            child.Weight1 = MatrixEvolve(Weight1, mutate, sign);
            child.Weight2 = MatrixEvolve(Weight2, mutate, sign);
            child.Weight3 = MatrixEvolve(Weight3, mutate, sign);
            child.Weight4 = MatrixEvolve(Weight4, mutate, sign);

            if (r.NextDouble() < mutate)
            { child.Bias1 = Bias1 * Math.Pow(1.05, r.NextDouble() - 0.5); }
            if (r.NextDouble() < sign / Bias1)
            { child.Bias1 *= -1; }

            if (r.NextDouble() < mutate)
            { child.Bias2 = Bias2 * Math.Pow(1.05, r.NextDouble() - 0.5); }
            if (r.NextDouble() < sign / Bias2)
            { child.Bias2 *= -1; }

            if (r.NextDouble() < mutate)
            { child.Bias3 = Bias3 * Math.Pow(1.05, r.NextDouble() - 0.5); }
            if (r.NextDouble() < sign / Bias3)
            { child.Bias3 *= -1; }

            if (r.NextDouble() < mutate)
            { child.Bias4 = Bias4 * Math.Pow(1.05, r.NextDouble() - 0.5); }
            if (r.NextDouble() < sign / Bias4)
            { child.Bias4 *= -1; }

            return child;
        }
        private Matrix MatrixEvolve(Matrix A, double mutate, double sign)
        {
            Matrix R = A.Clone();
            for (int i = 0; i < R.Rows; i++)
            {
                for (int j = 0; j < R.Columns; j++)
                {
                    // only mutate at 10% chance
                    if (r.NextDouble() < mutate)
                    {
                        R.Data[i][j] = R.Data[i][j] * Math.Pow(1.05, r.NextDouble() - 0.5);
                    }
                    // only mutate (flip sign) at 0.01%/value
                    // examples:
                    // if value = 0.01 then there is 10% chance of fliping sign
                    // if value is 0.1 then there is a 1% chance of flipping
                    if (r.NextDouble() < sign / R.Data[i][j])
                    { R.Data[i][j] *= -1; }
                }
            }

            return R;
        }

        public Brain EmptyChild()
        {
            Brain child = new Brain(Input.Rows, Hidden1.Rows, Hidden2.Rows, Hidden3.Rows, Output.Rows);
            
            return child;
        }

        public static void Mate(Brain p, Brain q, out Brain c1, out Brain c2)
        {
            c1 = p.EmptyChild();
            c2 = p.EmptyChild();

            c1.Weight1 = new Matrix(c1.Hidden1.Rows, c1.Input.Rows);
            c1.Weight2 = new Matrix(c1.Hidden2.Rows, c1.Hidden1.Rows);
            c1.Weight3 = new Matrix(c1.Hidden3.Rows, c1.Hidden2.Rows);
            c1.Weight4 = new Matrix(c1.Output.Rows, c1.Hidden3.Rows);
            c2.Weight1 = new Matrix(c2.Hidden1.Rows, c2.Input.Rows);
            c2.Weight2 = new Matrix(c2.Hidden2.Rows, c2.Hidden1.Rows);
            c2.Weight3 = new Matrix(c2.Hidden3.Rows, c2.Hidden2.Rows);
            c2.Weight4 = new Matrix(c2.Output.Rows, c2.Hidden3.Rows);

            #region swap genes
            if (r.NextDouble() > 0.5) {
                c1.Bias1 = p.Bias1;
                c2.Bias1 = q.Bias1;
            } else {
                c1.Bias1 = q.Bias1;
                c2.Bias1 = p.Bias1;
            }
            if (r.NextDouble() > 0.5) {
                c1.Bias2 = p.Bias2;
                c2.Bias2 = q.Bias2;
            } else {
                c1.Bias2 = q.Bias2;
                c2.Bias2 = p.Bias2;
            }
            if (r.NextDouble() > 0.5) {
                c1.Bias3 = p.Bias3;
                c2.Bias3 = q.Bias3;
            } else {
                c1.Bias3 = q.Bias3;
                c2.Bias3 = p.Bias3;
            }
            if (r.NextDouble() > 0.5) {
                c1.Bias4 = p.Bias4;
                c2.Bias4 = q.Bias4;
            } else {
                c1.Bias4 = q.Bias4;
                c2.Bias4 = p.Bias4;
            }

            for (int i = 0; i < p.Weight1.Rows; i++) {
                for (int j = 0; j < p.Weight1.Columns; j++) {
                    if (r.NextDouble() > 0.5) {
                        c1.Weight1.Data[i][j] = p.Weight1.Data[i][j];
                        c2.Weight1.Data[i][j] = q.Weight1.Data[i][j];
                    } else {
                        c2.Weight1.Data[i][j] = q.Weight1.Data[i][j];
                        c1.Weight1.Data[i][j] = p.Weight1.Data[i][j];
            }   }   }
            for (int i = 0; i < p.Weight2.Rows; i++) {
                for (int j = 0; j < p.Weight2.Columns; j++) {
                    if (r.NextDouble() > 0.5) {
                        c1.Weight2.Data[i][j] = p.Weight2.Data[i][j];
                        c2.Weight2.Data[i][j] = q.Weight2.Data[i][j];
                    } else {
                        c2.Weight2.Data[i][j] = q.Weight2.Data[i][j];
                        c1.Weight2.Data[i][j] = p.Weight2.Data[i][j];
            }   }   }
            for (int i = 0; i < p.Weight3.Rows; i++) {
                for (int j = 0; j < p.Weight3.Columns; j++) {
                    if (r.NextDouble() > 0.5) {
                        c1.Weight3.Data[i][j] = p.Weight3.Data[i][j];
                        c2.Weight3.Data[i][j] = q.Weight3.Data[i][j];
                    } else {
                        c2.Weight3.Data[i][j] = q.Weight3.Data[i][j];
                        c1.Weight3.Data[i][j] = p.Weight3.Data[i][j];
            }   }   }
            for (int i = 0; i < p.Weight4.Rows; i++) {
                for (int j = 0; j < p.Weight4.Columns; j++) {
                    if (r.NextDouble() > 0.5) {
                        c1.Weight4.Data[i][j] = p.Weight4.Data[i][j];
                        c2.Weight4.Data[i][j] = q.Weight4.Data[i][j];
                    } else {
                        c2.Weight4.Data[i][j] = q.Weight4.Data[i][j];
                        c1.Weight4.Data[i][j] = p.Weight4.Data[i][j];
            }   }   }
            #endregion


            c1 = c1.Evolve(0.01, 0.0001);
            c2 = c2.Evolve(0.01, 0.0001);
        }

    }
}
