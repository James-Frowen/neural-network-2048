using System;
using System.Collections.Generic;

namespace NeuralNetwork2048_v2
{
    public class Brain
    {
        private float[] Input;
        private float[] Output;
        private readonly List<int> _hiddenSizes;

        private int layerCount;
        private int weightCount;
        private List<float[]> Layers;
        private Matrix[] Weights;

        public static Random r;
        public static int NextID;

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

        public Brain(int input = 32, List<int> hidden = null, int output = 4)
        {
            if (hidden == null)
            {
                hidden = new List<int>()
                {
                    //18,12,8
                    60
                    //24,20,16,12,8
                };
                _hiddenSizes = hidden;
            }

            Input = new float[input];
            Output = new float[output];

            layerCount = hidden.Count + 2;
            weightCount = layerCount - 1;
            Layers = new List<float[]>(layerCount);
            Layers.Add(Input);
            foreach (var size in hidden)
            {
                Layers.Add(new float[size]);
            }
            Layers.Add(Output);

            ID = NextID;
            NextID++;

            r.NextBytes(randoms);
        }
        public void Initnew()
        {
            Weights = new Matrix[weightCount];
            for (var i = 0; i < weightCount; i++)
            {
                var weight = new Matrix(Layers[i + 1].Length, Layers[i].Length);
                weight.InitRandom(r);

                Weights[i] = weight;
            }
        }

        public void CalculateMove(Puzzle P, bool forceMove = false)
        {
            // give data to input matrix
            var m = Math.Log(2048, 2);

            for (var i = 16; i < 32; i++)
                Input[i] = 0;// clear

            for (var x = 0; x < P.Width; x++)
            {
                for (var y = 0; y < P.Height; y++)
                {
                    //var v = P.Grid[x, y] == 0 
                    //    ? -1 
                    //    : Math.Log(P.Grid[x, y], 2);
                    //Input[(x * P.Height) + y] = (float)(v / m);

                    var value = P.Grid[x, y];

                    if (value == 0)
                    {
                        Input[(x * P.Height) + y] = -1;
                        Input[16]++;
                    }
                    else
                    {
                        Input[(x * P.Height) + y] = (float)(value / m);

                        // set 2nd half of input to the counts of each tile
                        var i = (int)Math.Log(value, 2);
                        Input[16 + i]++;
                    }
                }
            }


            //ActivationFunction(Input);
            //Input[16] = 0;
            //Input[17] = P.EmptyPercent() * 100;

            //if (forceMove && P.EmptyCount >15) // force moves at start
            //{
            //    Output[0] = 0;//x1
            //    Output[1] = 1;//y1
            //    Output[2] = 1;//x2
            //    Output[3] = 0;//y2
            //}
            //else
            //{
            // calculate output
            for (var i = 0; i < weightCount; i++)
            {
                Matrix.ActivationFromMultiply(Layers[i + 1], Weights[i], Layers[i]);
            }
            //}
        }

        private (float priority, Puzzle.MoveDirection direction)[] moves = new (float priority, Puzzle.MoveDirection direction)[4];
        public void MakeMove(Puzzle P)
        {
            for (var i = 0; i < 4; i++)
            {
                moves[i] = (Output[i], (Puzzle.MoveDirection)i);
            }

            //var ordered = moves.OrderByDescending(x => x.priority).Select(x => x.direction).ToList();
            for (var i = 0; i < 4; i++)
            {
                for (var j = i + 1; j < 4; j++)
                {
                    if (moves[i].priority < moves[j].priority)
                    {
                        // Swap the elements to sort in descending order
                        var temp = moves[i];
                        moves[i] = moves[j];
                        moves[j] = temp;
                    }
                }
            }

            for (var i = 0; i < 4; i++)
            {
                if (P.Move(moves[i].direction))
                    return;
            }

            //P.TryMoves(ordered);
        }


        public Brain Evolve(float mutate = 0.01f, float sign = 0.001f)
        {
            var child = EmptyChild();
            child.Weights = new Matrix[weightCount];
            for (var i = 0; i < weightCount; i++)
            {
                child.Weights[i] = MatrixEvolveNew(Weights[i], mutate, sign);
            }

            return child;
        }

        private static void MutateValue(ref float value, float mutate = 0.1f, float sign = 0.001f)
        {
            // only mutate at 10% chance
            if (r.NextDouble() < mutate)
                value *= (float)Math.Pow(1.05, r.NextDouble() - 0.5);

            // only mutate (flip sign) at 0.01%/value
            // examples:
            // if value = 0.01 then there is 10% chance of fliping sign
            // if value is 0.1 then there is a 1% chance of flipping
            if (r.NextDouble() < sign / value)
                value *= -1;
        }
        private static float MutateValue(float value, float mutate = 0.1f, float sign = 0.001f)
        {
            MutateValue(ref value, mutate, sign);
            return value;
        }

        private static Matrix MatrixEvolveNew(Matrix A, float mutate = 0.1f, float sign = 0.001f)
        {
            var R = A.Clone();
            for (var i = 0; i < R.Data.Length; i++)
            {
                MutateValue(ref R.Data[i], mutate, sign);
            }

            R.Bias = MutateValue(A.Bias, mutate, sign);
            return R;
        }
        private static void MatrixEvolve(Matrix A, float mutate = 0.1f, float sign = 0.001f)
        {
            for (var i = 0; i < A.Data.Length; i++)
            {
                MutateValue(ref A.Data[i], mutate, sign);
            }

            A.Bias = MutateValue(A.Bias, mutate, sign);
        }

        public Brain EmptyChild()
        {
            var child = new Brain(Input.Length, _hiddenSizes, Output.Length);

            return child;
        }

        private static byte[] randoms = new byte[223];
        private static int randomIndex;

        private static void MateValue(float p, float q, ref float c1, ref float c2)
        {
            randomIndex = (randomIndex + 1) % 223;
            if (randoms[randomIndex] > 127)
            {
                c1 = p;
                c2 = q;
            }
            else
            {
                c1 = q;
                c2 = p;
            }
        }
        private static void MateMatrix(Matrix p, Matrix q, Matrix c1, Matrix c2)
        {
            for (var i = 0; i < p.Data.Length; i++)
            {
                MateValue(p.Data[i], q.Data[i], ref c1.Data[i], ref c2.Data[i]);
            }

            MateValue(p.Bias, q.Bias, ref c1.Bias, ref c2.Bias);
        }
        public static void Mate(Brain p, Brain q, out Brain c1, out Brain c2)
        {
            c1 = p.EmptyChild();
            c2 = p.EmptyChild();

            c1.Weights = new Matrix[p.weightCount];
            c2.Weights = new Matrix[p.weightCount];

            for (var i = 0; i < p.weightCount; i++)
            {
                c1.Weights[i] = p.Weights[i].EmptyClone();
                c2.Weights[i] = p.Weights[i].EmptyClone();

                MateMatrix(p.Weights[i], q.Weights[i], c1.Weights[i], c2.Weights[i]);

                MatrixEvolve(c1.Weights[i], 0.02f, 0.0001f);
                MatrixEvolve(c2.Weights[i], 0.02f, 0.0001f);
            }
        }
    }
}
