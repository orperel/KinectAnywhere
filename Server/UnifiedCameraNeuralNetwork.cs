using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectAnywhere
{
    class UnifiedCameraNeuralNetwork
    {
        private const int RANDOM_SEED = 555; // For debug purposes, reproduces the same random numbers every session

        private delegate float ErrorFunction(float x, float y);

        private Matrix.MatrixPerElementOperation _nonLinearityFunc;
        private Matrix.MatrixPerElementOperation _nonLinearityFuncDeriv;
        private ErrorFunction _errorFunction;
        private Random _random;
        private int _inputLayerSize; // Amount of neurons in input layer
        private int _hiddenLayerSize; // Amount of neurons in hidden layer
        private int _outputLayerSize; // Amount of neurons in output layer

        private Matrix _input;
        private Matrix _expected;
        private Matrix _hiddenLayerWeights;
        private Matrix _outputLayerWeights;

        private float generateRandomGauss()
        {
            // We generate a random number using gaussian distribution with mean 0 and variance 1.
            // Gaussian distribution is simulated by a fast Box-Muller Transformation,
            // generated from 2 uniform distributed numbers.
            // See: http://mathworld.wolfram.com/Box-MullerTransformation.html
            double uni1 = 1.0 - _random.NextDouble();
            double uni2 = 1.0 - _random.NextDouble();
            double gaussRand = Math.Sqrt(-2.0 * Math.Log(uni1)) *
                                Math.Sin(2.0 * Math.PI * uni2);

            return (float)gaussRand;
        }

        private void randomizeWeights()
        {
            for (int i = 0; i < _hiddenLayerWeights.rows; i++)
                for (int j = 0; j < _hiddenLayerWeights.cols; j++)
                    _hiddenLayerWeights[i, j] = generateRandomGauss();

            for (int i = 0; i < _outputLayerWeights.rows; i++)
                for (int j = 0; j < _outputLayerWeights.cols; j++)
                    _outputLayerWeights[i, j] = generateRandomGauss();
        }

        public UnifiedCameraNeuralNetwork(int inputLayerSize, int hiddenLayerSize, int outputLayerSize)
        {
            // Network functional parameters
            _nonLinearityFunc = sigmoid;
            _nonLinearityFuncDeriv = sigmoidDerivation;
            _errorFunction = diffError;

            // Network initialization
            _inputLayerSize = inputLayerSize;
            _hiddenLayerSize = hiddenLayerSize;
            _outputLayerSize = outputLayerSize;
            _input = new Matrix(inputLayerSize, 1); // Vector
            _hiddenLayerWeights = new Matrix(_hiddenLayerSize, _inputLayerSize);
            _outputLayerWeights = new Matrix(_outputLayerSize, _hiddenLayerSize);

            _random = new Random(RANDOM_SEED);
            randomizeWeights();
        }

        private float relu(float w)
        {
            return Math.Max(0, w);
        }

        private float reluDerivation(float x)
        {
            return (x > 0) ? 1 : 0;
        }

        private float sigmoid(float w)
        {
            return 1 / (1 + (float)Math.Exp(-w));
        }

        private float sigmoidDerivation(float x)
        {
            return x*(1-x);
        }

        private float diffError(float x, float y)
        {
            return x - y;
        }

        private float meanSquareError(float x, float y)
        {
            return (float)Math.Pow(x - y, 2);
        }

        public Matrix feedForward(float[] input)
        {
            return null;
        }

        public void train(float[] input, float[] expectedVals)
        {
            _input.init(input);
            Matrix expected = new Matrix(expectedVals);

            // Feed forward -
            // Propagate input through hidden & output layers to get a prediction
            Matrix hiddenLayerVals = _hiddenLayerWeights * _input;
            Matrix prediction = _outputLayerWeights * hiddenLayerVals.invoke(_nonLinearityFunc);
            prediction = prediction.invoke(_nonLinearityFunc);

            Matrix outputError = expected - prediction.transpose();
            Matrix outputDelta = outputError * prediction.invoke(_nonLinearityFuncDeriv); ; // Multiply error by weighted prediction

            Matrix hiddenError = outputDelta * (_hiddenLayerWeights.transpose());
            Matrix hiddenDelta = hiddenError * (hiddenLayerVals.invoke(_nonLinearityFuncDeriv)); // Multiply error by weighted prediction

            _outputLayerWeights += prediction * outputDelta;
            _hiddenLayerWeights += hiddenLayerVals * hiddenDelta;
        }
    }
}
