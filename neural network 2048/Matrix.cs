using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace neural_network_2048
{
    public class Matrix
    {
        public Matrix(int rows, int columns)
        {
            Data = new double[rows, columns];
            Rows = rows;
            Columns = columns;
        }
        public int Rows, Columns;
        public double[,] Data;


        public void SetData(double[,] data)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] = data[i, j];
                }
            }
        }

        //public double Get(int i, int j)
        //{
        //    return Data[i - 1, j - 1];
        //}
        //public void Set(int i, int j, double value)
        //{
        //    Data[i - 1, j - 1] = value;
        //}

        public void FromMultiply(Matrix A, Matrix B, double bias = 0)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    double sum = bias;
                    for (int n = 0; n < A.Columns; n++)
                    {
                        sum += A.Data[i, n] * B.Data[n, j];
                    }
                    Data[i, j] = sum;
                }
            }
        }
        public void ActivationFromMultiply(Matrix A, Matrix B, double bias = 0)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    double sum = bias;
                    for (int n = 0; n < A.Columns; n++)
                    {
                        sum += A.Data[i, n] * B.Data[n, j];
                    }
                    Data[i, j] = Math.Tanh(sum);
                }
            }
        }

        public static Matrix Multiply(Matrix A, Matrix B, double bias = 0)
        {
            Matrix R = new Matrix(A.Rows, B.Columns);
            for (int i = 0; i < R.Rows; i++)
            {
                for (int j = 0; j < R.Columns; j++)
                {
                    double sum = bias;
                    for (int n = 0; n < A.Columns; n++)
                    {
                        sum += A.Data[i, n] * B.Data[n, j];
                    }
                    R.Data[i, j] = sum;
                }
            }

            return R;
        }

        public Matrix Clone()
        {
            Matrix R = new Matrix(Rows, Columns);
            R.SetData(Data);
            return R;
        }
    }
}
