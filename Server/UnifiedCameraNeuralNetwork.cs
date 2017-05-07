using System;

namespace KinectAnywhere
{
    class UnifiedCameraNeuralNetwork
    {
        private const int RANDOM_SEED = 555; // For debug purposes, reproduces the same random numbers every session
        private const int DEFAULT_BIAS = 1; // Set bias to 1 for all layers

        private Matrix.MatrixPerElementOperation _nonLinearityFunc;
        private Matrix.MatrixPerElementOperation _nonLinearityFuncDeriv;
        private Matrix.MatrixPerElementProduct _errorFunction;
        private Matrix.MatrixPerElementProduct _errorFunctionDerv; // x is expected, y is prediction
        private Random _random;
        private int _inputLayerSize; // Amount of neurons in input layer
        private int _hiddenLayerSize; // Amount of neurons in hidden layer
        private int _outputLayerSize; // Amount of neurons in output layer

        private bool _isStochastic;
        private float _learningRate;
        private float _momentum;
        private Matrix _input;
        private Matrix _expected;
        private Matrix _hiddenLayerWeights;
        private Matrix _outputLayerWeights;
        private Matrix _prevHiddenLayerWeightsDelta;
        private Matrix _prevOutputLayerWeightsDelta;

        private Matrix _outputWeightUpdates; // Accumulated in stochastic mode
        private Matrix _hiddenWeightUpdates; // Accumulated in stochastic mode
        private int _weightUpdatesCount; // Counted in stochastic mode

        public float LearningRate
        {
            get { return _learningRate; }
            set { _learningRate = -value; /* Keep negation to save on minus sign when training */ }
        }

        public bool Stochastic
        {
            get { return _isStochastic; }
        }

        private float generateRandomGauss(float mean, float var)
        {
            // We generate a random number using gaussian distribution with mean 0 and variance 1,
            // Then transform it.
            // Gaussian distribution is simulated by a fast Box-Muller Transformation,
            // generated from 2 uniform distributed numbers.
            // See: http://mathworld.wolfram.com/Box-MullerTransformation.html
            double uni1 = 1.0 - _random.NextDouble();
            double uni2 = 1.0 - _random.NextDouble();
            double gaussRand = Math.Sqrt(-2.0 * Math.Log(uni1)) *
                                Math.Sin(2.0 * Math.PI * uni2);

            return (float)(mean + Math.Sqrt(var) * gaussRand);
        }

        private float generateRandomGauss()
        {
            return generateRandomGauss(0, 1);
        }

        private void randomizeWeights()
        {
            float mean = 0.0f;
            float var = 2.1f;

            for (int i = 0; i < _hiddenLayerWeights.rows; i++)
                for (int j = 0; j < _hiddenLayerWeights.cols; j++)
                    _hiddenLayerWeights[i, j] = generateRandomGauss(mean, var);

            for (int i = 0; i < _outputLayerWeights.rows; i++)
                for (int j = 0; j < _outputLayerWeights.cols; j++)
                    _outputLayerWeights[i, j] = generateRandomGauss(mean, var);
        }

        public UnifiedCameraNeuralNetwork(int inputLayerSize, int hiddenLayerSize, int outputLayerSize,
                                          float learningRate,
                                          bool isStochastic = true, float momentum = 0)
        {
            // Network functional parameters
            _nonLinearityFunc = sigmoid;
            _nonLinearityFuncDeriv = sigmoidDerivative;
            _errorFunction = meanSquareError;
            _errorFunctionDerv = meanSquareErrorDerivative;

            // Network initialization
            this.LearningRate = learningRate;
            _momentum = momentum;
            _inputLayerSize = inputLayerSize;
            _hiddenLayerSize = hiddenLayerSize;
            _outputLayerSize = outputLayerSize;
            _input = new Matrix(inputLayerSize + 1, 1); // Vector (+ 1 for bias)
            _expected = new Matrix(outputLayerSize, 1); // Vector

            // Add 1 for bias
            _hiddenLayerWeights = new Matrix(_hiddenLayerSize, _inputLayerSize + 1);
            _outputLayerWeights = new Matrix(_outputLayerSize, _hiddenLayerSize + 1);
            _prevHiddenLayerWeightsDelta = new Matrix(_hiddenLayerSize, _inputLayerSize + 1);
            _prevOutputLayerWeightsDelta = new Matrix(_outputLayerSize, _hiddenLayerSize + 1);

            _isStochastic = isStochastic;
            _hiddenWeightUpdates = new Matrix(_hiddenLayerSize, _inputLayerSize + 1);
            _outputWeightUpdates = new Matrix(_outputLayerSize, _hiddenLayerSize + 1);
            _weightUpdatesCount = 0;

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

            _input.init(input);
            _input[input.Length, 0] = DEFAULT_BIAS; // Always set the bias as last neuron in layer
            Matrix hiddenLayerVals = _hiddenLayerWeights * _input;

            // Extend hidden layer to include bias
            Matrix hiddenLayerValsWithBias =
                hiddenLayerVals.resize(hiddenLayerVals.rows + 1, hiddenLayerVals.cols, DEFAULT_BIAS);
            Matrix prediction = _outputLayerWeights * hiddenLayerValsWithBias.invoke(_nonLinearityFunc);
            prediction = prediction.invoke(_nonLinearityFunc);

            return prediction;
        }

        public void train(float[] input, float[] expectedVals)
        {
            _expected.init(expectedVals);
            _input.init(input);
            _input[input.Length, 0] = DEFAULT_BIAS; // Always set the bias as last neuron in layer

            // Feed forward -
            // Propagate input through hidden & output layers to get a prediction
            Matrix hiddenLayerVals = _hiddenLayerWeights * _input;
            Matrix hiddenLayerValsWithBias =
                hiddenLayerVals.resize(hiddenLayerVals.rows + 1, hiddenLayerVals.cols, DEFAULT_BIAS);
            Matrix prediction = _outputLayerWeights * hiddenLayerValsWithBias.invoke(_nonLinearityFunc);
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
            Matrix output_dE_dai = Matrix.invoke(_errorFunctionDerv, _expected, prediction);
            Matrix output_dai_dIni = prediction.invoke(_nonLinearityFuncDeriv);
            Matrix output_dE_dIni = output_dai_dIni.dot(output_dE_dai); // Chain rule applied
            Matrix output_dE_dwij = output_dE_dIni.mul(hiddenLayerValsWithBias.transpose());

            // Hidden layer delta - Calculated by Leibniz's chain rule as well.
            // The only term different here is dE / dai which is calculated by:
            //      dE          wi,k     dak     dE
            //      --     =          *  ---  *  ----
            //      dai                  dInk    dak
            // Where wi,k is the weight between hidden neuron i and output neuron k.
            // (ai is the output of hidden neuron i and ak is the output of hidden neuron k)

            // Ignore bias of output layer when backpropagating hidden layer (there is no real neuron to update)
            Matrix _outputLayerWeights_WithoutBias =
                _outputLayerWeights.resize(_outputLayerWeights.rows, _outputLayerWeights.cols - 1, 0);

            Matrix hidden_dE_dai = (output_dE_dIni.transpose().mul(_outputLayerWeights_WithoutBias)).transpose();
            Matrix hidden_dai_dIni = hiddenLayerVals.invoke(_nonLinearityFuncDeriv);
            Matrix hidden_dE_dIni = hidden_dai_dIni.dot(hidden_dE_dai); // Chain rule applied
            Matrix hidden_dE_dwij = hidden_dE_dIni.mul(_input.transpose());


            if (!_isStochastic)
            {   // Online mode

                // Finally update the weights with the calculated delta weighted with a negated learning rate.
                // We also take into consideration the previous updates with a momentum factor.
                // Note: Learning rate is already negated with minus.
                Matrix outputDelta = output_dE_dwij * LearningRate + _prevOutputLayerWeightsDelta * _momentum;
                Matrix hiddenDelta = hidden_dE_dwij * LearningRate + _prevHiddenLayerWeightsDelta * _momentum;
                _outputLayerWeights += outputDelta;
                _hiddenLayerWeights += hiddenDelta;
                _prevOutputLayerWeightsDelta = outputDelta;
                _prevHiddenLayerWeightsDelta = hiddenDelta;
            }
            else
            {   // Stochastic mode
                // Just accumulate the change, until weight-updating stage
                _outputWeightUpdates += output_dE_dwij;
                _hiddenWeightUpdates += hidden_dE_dwij;
                _weightUpdatesCount++;
            }
        }

        public void forceWeightUpdates()
        {
            if (!_isStochastic)
            {
                throw new InvalidOperationException("Matrix is in non-stochastic mode but operatred as stochastic.");
            }

            float avgFactor = 1.0f / _weightUpdatesCount;
            Matrix outputDelta = _outputWeightUpdates * (avgFactor * LearningRate) + _prevOutputLayerWeightsDelta * _momentum;
            Matrix hiddenDelta = _hiddenWeightUpdates * (avgFactor * LearningRate) + _prevHiddenLayerWeightsDelta * _momentum;
            _outputLayerWeights += outputDelta;
            _hiddenLayerWeights += hiddenDelta;

            _prevOutputLayerWeightsDelta = outputDelta;
            _prevHiddenLayerWeightsDelta = hiddenDelta;
            _hiddenWeightUpdates = new Matrix(_hiddenLayerSize, _inputLayerSize + 1);
            _outputWeightUpdates = new Matrix(_outputLayerSize, _hiddenLayerSize + 1);
            _weightUpdatesCount = 0;
        }
    }
}
