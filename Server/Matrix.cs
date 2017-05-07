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
        public delegate float MatrixPerElementProduct(float x, float y);

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
                for (int j = 0; j < cols; j++)
                {
                    _mat[i, j] = copy[i, j];
                }
            }
        }

        public Matrix(float[] vals): this(vals.Length, 1)
        {
            for (int i = 0; i < vals.Length; i++)
                _mat[i, 0] = vals[i];
        }

        public static Matrix identity(int rows, int cols)
        {
            Matrix idenMat = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    idenMat[i, j] = (i == j) ? 1 : 0;
                }
            }

            return idenMat;
        }

        /// <summary>
        /// Creates a column vector of size (Length(vals), 1), initialized with vals' values.
        /// If vals is shorter than the current vector, the first vals.length values are filled.
        /// </summary>
        /// <param name="vals"> Initialization values for the vector </param>
        public void init(float[] vals)
        {
            if ((cols != 1) || (vals.Length > rows))
            {
                throw new InvalidOperationException("Matrix values initialization failed due to" +
                                                    "non-matching dimensions.");
            }

            for (int i = 0; i < vals.Length; i++)
                _mat[i, 0] = vals[i];
        }

        public Matrix transpose()
        {
            Matrix newMat = new Matrix(cols, rows);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newMat[j, i] = _mat[i, j];
                }
            }

            return newMat;
        }

        public Matrix resize(int newrow, int newcol, float padValue)
        {
            Matrix newMat = new Matrix(newrow, newcol);

            for (int i = 0; i < newrow; i++)
            {
                for (int j = 0; j < newcol; j++)
                {
                    newMat[i, j] = ((i < rows) && (j < cols)) ? _mat[i, j] : padValue;
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
                for (int j = 0; j < cols; j++)
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
                for (int j = 0; j < cols; j++)
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

        public Matrix mul(float x)
        {
            Matrix result = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = this[i, j] * x;
                }
            }

            return result;
        }

        public Matrix dot(Matrix m2)
        {
            if ((cols != m2.cols) || (rows != m2.rows))
                throw new InvalidOperationException("Trying to calc dot product for matrices with non-matching dimensions");

            Matrix result = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = this[i, j] * m2[i, j];
                }
            }

            return result;
        }

        public Matrix invoke(MatrixPerElementOperation oper)
        {
            Matrix result = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = oper.Invoke(_mat[i, j]);
                }
            }

            return result;
        }

        public Matrix invoke(MatrixPerElementProduct oper, Matrix m2)
        {
            if ((cols != m2.cols) || (rows != m2.rows))
                throw new InvalidOperationException("Trying to invoke element-wise operation on matrices" +
                                                    "with non-matching dimensions");

            Matrix result = new Matrix(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = oper.Invoke(_mat[i, j], m2[i, j]);
                }
            }

            return result;
        }

        public static Matrix invoke(MatrixPerElementProduct oper, Matrix m1, Matrix m2)
        {
            return m1.invoke(oper, m2);
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

        public static Matrix operator *(Matrix a, float x)
        {
            return a.mul(x);
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

        public override bool Equals(System.Object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Matrix return false.
            // Also quit if dimensions don't agree
            Matrix other = obj as Matrix;
            if (((System.Object)other == null) || (other.rows != this.rows) || (other.cols != this.cols))
            {
                return false;
            }

            // Return true if the values match
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (_mat[i, j] != other._mat[i, j])
                        return false;
                }
            }

            return true;
        }

        public bool Equals(Matrix other)
        {
            // If parameter is null or dimensions don't agree return false
            if (((Object)other == null) || (other.rows != this.rows) || (other.cols != this.cols))
            {
                return false;
            }

            // Return true if the values match
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (_mat[i, j] != other._mat[i, j])
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 17;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                    hash = hash * 23 + _mat[i, j].GetHashCode();
            }

            return hash;
        }
    }
}
