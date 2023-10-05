// # Hidden count 1024
// normal 50us
// unrolled 45us
// transposed 150us
// transposed unrolled 142us
// SIMD 46us
// SIMD unrolled 46us

// # Hidden count 64
// unrolled 2.9us
// SIMD 3.6us
// SIMD unrolled 3.6us

#define UNROLLED
//#define TRANSPOSED
//#define USE_SIMD
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

        public static long ticks;
        public static long count;

        public static unsafe void ActivationFromMultiply(float[] outLayer, Matrix weightMatrix, float[] inLayer)
        {
            var start = Stopwatch.GetTimestamp();
            var inLength = inLayer.Length;
            var outLength = outLayer.Length;
            var weights = weightMatrix.Data;
            var bias = weightMatrix.Bias;

#if TRANSPOSED
            var transposed = stackalloc float[weightMatrix.Data.Length];
            weightMatrix.TransposeTo(transposed);
#endif

            for (var i = 0; i < outLength; i++)
            {
                var weight_I = i * inLength;
                var sum = bias;

#if !USE_SIMD
#if !TRANSPOSED
#if !UNROLLED
                // normal
                for (var n = 0; n < inLength; n++)
                {
                    sum += weights[weight_I + n] * inLayer[n];
                }
#else
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
#endif
#else
#if !UNROLLED
                // transposed
                for (var n = 0; n < inLength; n++)
                {
                    sum += transposed[i + (n * outLength)] * inLayer[n];
                }

#else
                // Unrolled loop 4 times

                var n = 0;
                var inLengthMinus3 = inLength - 3;

                for (; n < inLengthMinus3; n += 4)
                {
                    sum += transposed[i + (n * outLength)] * inLayer[n];
                    sum += transposed[i + ((n + 1) * outLength)] * inLayer[n + 1];
                    sum += transposed[i + ((n + 2) * outLength)] * inLayer[n + 2];
                    sum += transposed[i + ((n + 3) * outLength)] * inLayer[n + 3];
                }

                // Handle the remaining elements if inLength is not a multiple of 4
                for (; n < inLength; n++)
                {
                    sum += transposed[i + (n * outLength)] * inLayer[n];
                }
#endif
#endif
#else // USE_SIMD
#if !UNROLLED
                var n = 0;

                // Use SIMD as long as there are enough elements left
                var vectorSize = Vector<float>.Count;
                var lengthMinusVectorSize = inLength - vectorSize;
                for (; n <= lengthMinusVectorSize; n += vectorSize)
                {
                    var weightsVector = new Vector<float>(weights, weight_I + n);
                    var inLayerVector = new Vector<float>(inLayer, n);
                    sum += Vector.Dot(weightsVector, inLayerVector);
                }

                // Handle the remaining elements
                for (; n < inLength; n++)
                {
                    sum += weights[weight_I + n] * inLayer[n];
                }
#else
                var n = 0;

                // Use SIMD as long as there are enough elements left
                var vectorSize = Vector<float>.Count;
                var lengthMinusVectorSize = inLength - (vectorSize * 2);
                for (; n <= lengthMinusVectorSize; n += vectorSize * 2)
                {
                    {
                        var weightsVector1 = new Vector<float>(weights, weight_I + n);
                        var inLayerVector1 = new Vector<float>(inLayer, n);
                        sum += Vector.Dot(weightsVector1, inLayerVector1);
                    }
                    {
                        var weightsVector2 = new Vector<float>(weights, weight_I + n + vectorSize);
                        var inLayerVector2 = new Vector<float>(inLayer, n + vectorSize);
                        sum += Vector.Dot(weightsVector2, inLayerVector2);
                    }
                }

                // Handle the remaining elements
                for (; n < inLength; n++)
                {
                    sum += weights[weight_I + n] * inLayer[n];
                }
#endif
#endif

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
