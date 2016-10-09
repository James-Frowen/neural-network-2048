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
            if (data.GetLength(0) != Rows || data.GetLength(1) != Columns) { throw new Exception("data wrong size"); }
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] = data[i, j];
                }
            }
        }

        public double Get(int i, int j)
        {
            return Data[i - 1, j - 1];
        }
        public void Set(int i, int j, double value)
        {
            Data[i - 1, j - 1] = value;
        }

        public void FromMultiply(Matrix A, Matrix B, double bias = 0)
        {
            if (A.Columns != B.Rows) { throw new Exception("matrix Multiply failed, bad size"); }
            if (A.Rows != Rows || B.Columns != Columns) { throw new Exception("matrix Multiply failed, result matrix wrong size"); }

            for (int i = 1; i <= Rows; i++)
            {
                for (int j = 1; j <= Columns; j++)
                {
                    double sum = bias;
                    for (int n = 1; n <= A.Columns; n++)
                    {
                        sum += A.Get(i, n) * B.Get(n, j);
                    }
                    Set(i, j, sum);
                }
            }
        }

        public static Matrix Multiply(Matrix A, Matrix B, double bias = 0)
        {
            if (A.Columns != B.Rows) { throw new Exception("matrix Multiply failed, bad size"); }
            Matrix R = new Matrix(A.Rows, B.Columns);
            for (int i = 1; i <= R.Rows; i++)
            {
                for (int j = 1; j <= R.Columns; j++)
                {
                    double sum = bias;
                    for (int n = 1; n <= A.Columns; n++)
                    {
                        sum += A.Get(i, n) * B.Get(n, j);
                    }
                    R.Set(i, j, sum);
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
