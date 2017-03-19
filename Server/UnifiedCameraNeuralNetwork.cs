using System;

namespace KinectAnywhere
{
    class UnifiedCameraNeuralNetwork
    {
        private const int RANDOM_SEED = 555; // For debug purposes, reproduces the same random numbers every session

        private Matrix.MatrixPerElementOperation _nonLinearityFunc;
        private Matrix.MatrixPerElementOperation _nonLinearityFuncDeriv;
        private Matrix.MatrixPerElementProduct _errorFunction;
        private Matrix.MatrixPerElementProduct _errorFunctionDerv; // x is expected, y is prediction
        private Random _random;
        private int _inputLayerSize; // Amount of neurons in input layer
        private int _hiddenLayerSize; // Amount of neurons in hidden layer
        private int _outputLayerSize; // Amount of neurons in output layer

        private float _learningRate;
        private float _momentum;
        private Matrix _input;
        private Matrix _expected;
        private Matrix _hiddenLayerWeights;
        private Matrix _outputLayerWeights;
        private Matrix _prevHiddenLayerWeightsDelta;
        private Matrix _prevOutputLayerWeightsDelta;

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

        public UnifiedCameraNeuralNetwork(int inputLayerSize, int hiddenLayerSize, int outputLayerSize,
                                          float learningRate, float momentum)
        {
            // Network functional parameters
            _nonLinearityFunc = sigmoid;
            _nonLinearityFuncDeriv = sigmoidDerivative;
            _errorFunction = meanSquareError;
            _errorFunctionDerv = meanSquareErrorDerivative;

            // Network initialization
            _learningRate = -learningRate; // Keep negation to save on minus sign when training
            _momentum = momentum;
            _inputLayerSize = inputLayerSize;
            _hiddenLayerSize = hiddenLayerSize;
            _outputLayerSize = outputLayerSize;
            _input = new Matrix(inputLayerSize, 1); // Vector
            _hiddenLayerWeights = new Matrix(_hiddenLayerSize, _inputLayerSize);
            _outputLayerWeights = new Matrix(_outputLayerSize, _hiddenLayerSize);
            _prevHiddenLayerWeightsDelta = new Matrix(_hiddenLayerSize, _inputLayerSize);
            _prevOutputLayerWeightsDelta = new Matrix(_outputLayerSize, _hiddenLayerSize);

            _random = new Random(RANDOM_SEED);
            randomizeWeights();
        }

        private float relu(float w)
        {
            return Math.Max(0, w);
        }

        private float reluDerivative(float x)
        {
            return (x > 0) ? 1 : 0;
        }

        private float sigmoid(float w)
        {
            return 1 / (1 + (float)Math.Exp(-w));
        }

        private float sigmoidDerivative(float x)
        {
            return x*(1-x);
        }

        private float diffError(float x, float y)
        {
            return x - y;
        }

        private float meanSquareError(float x, float y)
        {
            return 0.5f * (float)Math.Pow(x - y, 2);
        }

        private float meanSquareErrorDerivative(float x, float y)
        {
            return y - x; // - (x - y)
        }

        public Matrix feedForward(float[] input)
        {
            // Feed forward -
            // Propagate input through hidden & output layers to get a prediction
            Matrix hiddenLayerVals = _hiddenLayerWeights * _input;
            Matrix prediction = _outputLayerWeights * hiddenLayerVals.invoke(_nonLinearityFunc);
            prediction = prediction.invoke(_nonLinearityFunc);

            return prediction;
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

            // Next calculate the new weights by applying backprapagation, first to the output layer, then to hidden.
            // Algorithm description is available at: https://codesachin.wordpress.com/2015/12/06/backpropagation-for-dummies/

            // Output layer delta - Calculated by Leibniz's chain rule:
            //      dE          dE     dai     dIni
            //      --     =    --  *  ---  *  ----
            //      dwi,j       dai    dIni    dwi,j
            //
            // Where:
            // E - the error function between prediction and expected values (result is a column vector)
            // ai - The output of an output neuron after activation function is applied
            // Ini - The weighted output of an output neuron, before activation function is applied.
            // wi,j - Weights of output weights matrix between hidden neuron j and output neuron i.
            Matrix output_dE_dai = Matrix.invoke(_errorFunctionDerv, expected, prediction);
            Matrix output_dai_dIni = prediction.invoke(_nonLinearityFuncDeriv);
            Matrix output_dE_dIni = output_dai_dIni.dot(output_dE_dai); // Chain rule applied
            Matrix output_dE_dwij = output_dE_dIni.mul(hiddenLayerVals.transpose());

            // Hidden layer delta - Calculated by Leibniz's chain rule as well.
            // The only term different here is dE / dai which is calculated by:
            //      dE          wi,k     dak     dE
            //      --     =          *  ---  *  ----
            //      dai                  dInk    dak
            // Where wi,k is the weight between hidden neuron i and output neuron k.
            // (ai is the output of hidden neuron i and ak is the output of hidden neuron k)
            Matrix hidden_dE_dai = (output_dE_dIni.transpose().mul(_outputLayerWeights)).transpose();
            Matrix hidden_dai_dIni = hiddenLayerVals.invoke(_nonLinearityFuncDeriv);
            Matrix hidden_dE_dIni = hidden_dai_dIni.dot(hidden_dE_dai); // Chain rule applied
            Matrix hidden_dE_dwij = hidden_dE_dIni.mul(_input.transpose());

            // Finally update the weights with the calculated delta weighted with a negated learning rate.
            // We also take into consideration the previous updates with a momentum factor.
            // Note: Learning rate is already negated with minus.
            Matrix outputDelta = output_dE_dwij * _learningRate + _prevOutputLayerWeightsDelta * _momentum;
            Matrix hiddenDelta = hidden_dE_dwij * _learningRate + _prevHiddenLayerWeightsDelta * _momentum;
            _outputLayerWeights += outputDelta;
            _hiddenLayerWeights += hiddenDelta;
            _prevOutputLayerWeightsDelta = outputDelta;
            _prevHiddenLayerWeightsDelta = hiddenDelta;
        }
    }
}
