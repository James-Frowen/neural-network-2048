using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NeuralNetwork2048_v2
{
    public class Matrix
    {
        public readonly int Rows;
        public readonly int Columns;
        public readonly float[] Data;
        public float Bias;

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Data = new float[rows * columns];
        }

        public Matrix Clone()
        {
            var R = new Matrix(Rows, Columns);
            Buffer.BlockCopy(Data, 0, R.Data, 0, Data.Length * 4);
            R.Bias = Bias;
            return R;
        }
        public Matrix EmptyClone()
        {
            return new Matrix(Rows, Columns);
        }

        public void ActivationFromMultiply(Matrix A, Matrix B, float bias = 0)
        {
            for (var i = 0; i < Rows; i++)
            {
                var this_I = i * Columns;
                var A_I = i * A.Columns;

                for (var j = 0; j < Columns; j++)
                {
                    var sum = bias;
                    for (var n = 0; n < A.Columns; n++)
                    {
                        sum += A.Data[A_I + n] * B.Data[(n * B.Columns) + j];
                    }
                    Data[this_I + j] = (float)Math.Tanh(sum);
                }
            }
        }

        public unsafe void TransposeTo(float* outPtr)
        {
            for (var i = 0; i < Rows; i++)
            {
                var I = i * Columns;
                for (var j = 0; j < Columns; j++)
                {
                    outPtr[(j * Rows) + i] = Data[I + j];
                }
            }
        }

        public static long ticks1;
        public static long ticks2;
        public static long count;

        // normal 50us
        // unrolled 45us
        // transposed 150us
        // transposed 142us
        public static unsafe void ActivationFromMultiply(float[] outLayer, Matrix weightMatrix, float[] inLayer)
        {
            var start = Stopwatch.GetTimestamp();
            var inLength = inLayer.Length;
            var outLength = outLayer.Length;
            var weights = weightMatrix.Data;
            var bias = weightMatrix.Bias;

            var transposed = stackalloc float[weightMatrix.Data.Length];
            weightMatrix.TransposeTo(transposed);

            for (var i = 0; i < outLength; i++)
            {
                var weight_I = i * inLength;
                var sum = bias;

                // normal
                {
                    //for (var n = 0; n < inLength; n++)
                    //{
                    //    sum += weights[weight_I + n] * inLayer[n];
                    //}
                }

                // unrolled
                {
                    // Unrolled loop 4 times
                    var n = 0;
                    var inLengthMinus3 = inLength - 3;

                    for (; n < inLengthMinus3; n += 4)
                    {
                        sum += weights[weight_I + n] * inLayer[n];
                        sum += weights[weight_I + n + 1] * inLayer[n + 1];
                        sum += weights[weight_I + n + 2] * inLayer[n + 2];
                        sum += weights[weight_I + n + 3] * inLayer[n + 3];
                    }

                    // Handle the remaining elements if inLength is not a multiple of 4
                    for (; n < inLength; n++)
                    {
                        sum += weights[weight_I + n] * inLayer[n];
                    }
                }

                // transposed
                {
                    //for (var n = 0; n < inLength; n++)
                    //{
                    //    sum += transposed[i + (n * outLength)] * inLayer[n];
                    //}
                }
                // transposed unrolled
                {
                    //var n = 0;
                    //var inLengthMinus3 = inLength - 3;

                    //for (; n < inLengthMinus3; n += 4)
                    //{
                    //    sum += transposed[i + (n * outLength)] * inLayer[n];
                    //    sum += transposed[i + ((n + 1) * outLength)] * inLayer[n + 1];
                    //    sum += transposed[i + ((n + 2) * outLength)] * inLayer[n + 2];
                    //    sum += transposed[i + ((n + 3) * outLength)] * inLayer[n + 3];
                    //}

                    //// Handle the remaining elements if inLength is not a multiple of 4
                    //for (; n < inLength; n++)
                    //{
                    //    sum += transposed[i + (n * outLength)] * inLayer[n];
                    //}
                }

                //var activation = (float)Selu(sum);
                //var activation = (float)Math.Tanh(sum);

                const float alpha = 1.6733f;
                const float scale = 1.0507f;
                //return scale * (x < 0 ? ((alpha * (float)Math.Exp(x)) - alpha) : x);
                if (sum > 0)
                {
                    outLayer[i] = scale * sum;
                }
                else
                {
                    outLayer[i] = scale * ((alpha * (float)Math.Exp(sum)) - alpha);
                }
            }
            var end = Stopwatch.GetTimestamp();
            Interlocked.Add(ref ticks, end - start);
            Interlocked.Increment(ref count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Selu(float x)
        {
            const float alpha = 1.6733f;
            const float scale = 1.0507f;
            return scale * (x < 0 ? ((alpha * (float)Math.Exp(x)) - alpha) : x);
        }
    }

    public static class MatrixExtensions
    {
        public static void InitRandom(this Matrix matrix, Random r)
        {
            matrix.Data.InitRandom(r);
            matrix.Bias = .2f;// (float)(r.NextDouble() - 0.5) * 2;
        }

        public static void InitRandom(this float[] data, Random r)
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (float)(r.NextDouble() - 0.5) * 4;
            }
        }
    }
}
