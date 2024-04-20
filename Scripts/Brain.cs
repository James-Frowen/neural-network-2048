using System;
using System.Collections.Generic;

namespace NeuralNetwork2048_v2
{
    public class Brain
    {
        private BFloat[] Input;
        private BFloat[] Output;
        private readonly List<int> _hiddenSizes;

        private int layerCount;
        private int weightCount;
        private List<BFloat[]> Layers;
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

        public Brain(List<int> hidden = null, int output = 4)
        {
            hidden ??= new List<int>()
            {
                //18,12,8
                //64
                //32 // (lower is faster, and seems to be enough because game simple?)
                64,
                //input*input
                //24,20,16,12,8
            };
            _hiddenSizes = hidden;

            Input = new BFloat[176]; // CreateInputs creates 65 inputs
            Output = new BFloat[output];

            layerCount = hidden.Count + 2;
            weightCount = layerCount - 1;
            Layers = new List<BFloat[]>(layerCount);
            Layers.Add(Input);
            foreach (var size in hidden)
            {
                Layers.Add(new BFloat[size]);
            }
            Layers.Add(Output);

            ID = NextID;
            NextID++;

            r.NextBytes(randoms);
        }
        public void InitNew()
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
            CreateInputs(P, Input);

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

        private static void CreateInputs(Puzzle P, BFloat[] input)
        {
            for (var i = 16; i < 32; i++)
                input[i] = BFloat.Zero;// clear

            var emptyCount = 0;
            var offset = 0;
            // +224
            for (var x = 0; x < P.Width; x++)
            {
                for (var y = 0; y < P.Height; y++)
                {
                    var value = P.Grid[x, y];

                    for (var i = 0; i < Puzzle.MAX; i++)
                    {
                        if (value == i)
                        {
                            input[offset] = 1f.ToByte();
                        }
                        else
                        {
                            input[offset] = 0f.ToByte();
                        }
                        offset++;
                    }

                    //BFloat inputValue;
                    //if (value == 0)
                    //{
                    //    inputValue = (-1f).ToByte();
                    //    emptyCount++;
                    //}
                    //else
                    //{
                    //    inputValue = (value / (float)Puzzle.MAX).ToByte();
                    //}
                    //input[offset] = inputValue;
                    //offset++;


                }
            }

            // +48
            //for (var x = 0; x < P.Width; x++)
            //{
            //    for (var y = 0; y < P.Height; y++)
            //    {
            //        var value = P.Grid[x, y];
            //        var valueUp = TryGet(x, y + 1);
            //        var valueDown = TryGet(x, y - 1);
            //        var valueRight = TryGet(x + 1, y);
            //        var valueLeft = TryGet(x - 1, y);

            //        Set(value, valueUp);
            //        Set(value, valueDown);
            //        Set(value, valueRight);
            //        Set(value, valueLeft);

            //        int? TryGet(int x, int y)
            //        {
            //            if (x < 0 || x >= P.Width)
            //                return null;
            //            if (y < 0 || y >= P.Height)
            //                return null;

            //            return P.Grid[x, y];
            //        }

            //        void Set(int value, int? other)
            //        {
            //            if (other.HasValue)
            //            {
            //                input[offset] = Compare(value, other.Value);
            //                offset++;
            //            }
            //        }

            //        BFloat Compare(int value, int other)
            //        {
            //            if (value == other)
            //            {
            //                return 1f.ToByte();
            //            }
            //            else if (value == 0 || other == 0)
            //            {
            //                return 0f.ToByte();
            //            }
            //            else
            //            {
            //                return (-1f).ToByte();
            //            }
            //        }
            //    }
            //}

            // +1
            var size = P.Width * P.Height;
            //input[offset] = (emptyCount / size).ToByte();
            offset++;
        }

        public (float priority, Puzzle.MoveDirection direction)[] moves = new (float priority, Puzzle.MoveDirection direction)[4];
        private Puzzle.MoveDirection[] orderedMoves = new Puzzle.MoveDirection[4];

        public void MakeMove(Puzzle P)
        {
            for (var i = 0; i < 4; i++)
            {
                moves[i] = (Output[i].ToFloat(), (Puzzle.MoveDirection)i);
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
                orderedMoves[i] = moves[i].direction;
            }
            P.TryMoves(orderedMoves);
        }


        public Brain Evolve(float factor = 1)
        {
            var child = EmptyChild();
            child.Weights = new Matrix[weightCount];
            var mutator = new Mutator();
            mutator.factor = factor;
            for (var i = 0; i < weightCount; i++)
            {
                child.Weights[i] = mutator.MatrixEvolveNew(Weights[i]);
            }

            return child;
        }


        public Brain EmptyChild()
        {
            var child = new Brain(_hiddenSizes, Output.Length);

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
        private static void MateValue(BFloat p, BFloat q, ref BFloat c1, ref BFloat c2)
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

                var mutator = new Mutator();
                mutator.factor = 2;
                mutator.MatrixEvolve(c1.Weights[i]);
                mutator.MatrixEvolve(c2.Weights[i]);
            }
        }


        private class Mutator
        {
            public float factor = 1;

            public void MutateValue(ref BFloat value)
            {
                var f = value.ToFloat();
                MutateValue(ref f);
                value = f.ToByte();
            }
            public void MutateValue(ref float value)
            {
                var rate = 0.10 * factor;
                if (r.NextDouble() > rate)
                {
                    var rnd = r.NextDouble();
                    var shift = rnd * rnd * 0.2f * factor;

                    if (r.NextDouble() > 0.5f)
                        shift *= -1;

                    value += (float)shift;
                }
            }
            public BFloat MutateValue(BFloat value)
            {
                var f = value.ToFloat();
                MutateValue(ref f);
                return f.ToByte();
            }
            public float MutateValue(float value)
            {
                MutateValue(ref value);
                return value;
            }

            public Matrix MatrixEvolveNew(Matrix A)
            {
                var R = A.Clone();
                for (var i = 0; i < R.Data.Length; i++)
                {
                    MutateValue(ref R.Data[i]);
                }

                R.Bias = MutateValue(A.Bias);
                return R;
            }

            public void MatrixEvolve(Matrix A)
            {
                for (var i = 0; i < A.Data.Length; i++)
                {
                    MutateValue(ref A.Data[i]);
                }

                A.Bias = MutateValue(A.Bias);
            }
        }
    }
}
