using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectAnywhere
{
    class Matrix
    {
        public delegate float MatrixPerElementOperation(float x);

        public int rows { get; private set; }
        public int cols { get; private set; }

        private float[,] _mat;

        public Matrix(int rows, int cols)
        {
            this.rows = rows;
            this.cols = cols;
            _mat = new float[rows, cols]; // Initialized to 0 by default (C# spec)
        }

        public Matrix(Matrix copy): this(copy.rows, copy.cols)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j <= cols; j++)
                {
                    _mat[i, j] = copy[i, j];
                }
            }
        }

        public Matrix(float[] vals): this(vals.Length, 1)
        {
            for (int i = 0; i < vals.Length; i++)
                _mat[i, 1] = vals[i];
        }

        public void init(float[] vals)
        {
            if ((cols != 1) || (vals.Length != rows))
            {
                throw new InvalidOperationException("Matrix values initialization failed due to" +
                                                    "non-matching dimensions.");
            }

            for (int i = 0; i < vals.Length; i++)
                _mat[i, 1] = vals[i];
        }

        public Matrix transpose()
        {
            Matrix newMat = new Matrix(cols, rows);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j <= cols; j++)
                {
                    newMat[j, i] = _mat[i, j];
                }
            }

            return newMat;
        }

        public Matrix add(Matrix m2)
        {
            if ((cols != m2.cols) || (rows != m2.rows))
                throw new InvalidOperationException("Trying to add matrices with non-matching dimensions");

            Matrix result = new Matrix(rows, m2.cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    result[i, j] = _mat[i, j] + m2[i, j];
                }
            }

            return result;
        }

        public Matrix sub(Matrix m2)
        {
            if ((cols != m2.cols) || (rows != m2.rows))
                throw new InvalidOperationException("Trying to add matrices with non-matching dimensions");

            Matrix result = new Matrix(rows, m2.cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    result[i, j] = _mat[i, j] - m2[i, j];
                }
            }

            return result;
        }

        public Matrix mul(Matrix m2)
        {
            if (cols != m2.rows)
                throw new InvalidOperationException("Trying to multiply matrices with non-matching dimensions");

            Matrix result = new Matrix(rows, m2.cols);

            for (int j = 0; j < m2.cols; j++)
            {
                for (int i = 0; i < rows; i++)
                {
                    result[i, j] = 0;

                    for (int k = 0; k < cols; k++)
                    {
                        result[i, j] += this[i, k] * m2[k, j];
                    }
                }
            }

            return result;
        }

        public Matrix invoke(MatrixPerElementOperation oper)
        {
            Matrix result = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    result[i, j] = oper.Invoke(_mat[i, j]);
                }
            }

            return result;
        }

        public static Matrix operator +(Matrix a, Matrix b)
        {
            return a.add(b);
        }

        public static Matrix operator -(Matrix a, Matrix b)
        {
            return a.sub(b);
        }

        public static Matrix operator *(Matrix a, Matrix b)
        {
            return a.mul(b);
        }

        public float this[int i, int j]
        {
            get
            {
                return _mat[i, j];
            }
            set
            {
                _mat[i, j] = value;
            }
        }

        public float[] toArray()
        {

        }
    }
}
