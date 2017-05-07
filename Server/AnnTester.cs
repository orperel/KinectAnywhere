using KinectAnywhere;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KinectAnywhere
{
    class AnnTester
    {
        static int inputLayerSize = 1;
        static int outputLayerSize = 1;

        // Configurable ANN parameters
        static int hiddenLayerSize;
        static float learningRate;
        static bool isStochastic = true;
        static float momentum;
        static int learning_rate_decay; // Epochs

        private static UnifiedCameraNeuralNetwork ann;
        private static int numOfSamples = 1000;
        private static int numOfTests = 5000;
        private static float targetSuccess = 0.98f;
        private static float[] samples = new float[numOfSamples];
        private static float[] tests = new float[numOfTests];
        private static float successRate = 0;
        private static int epochNum = 1;

        private static readonly Brush groundTruthBrush = Brushes.Blue;
        private static readonly Brush predictionBrush = Brushes.Red;
        private static float minRange;
        private static float maxRange;
        private static float minFunc;
        private static float maxFunc;
        private delegate float ApproxFunc(float x);
        private static ApproxFunc approximationFunc;

        private static void initApproxFunc()
        {
            //approximationFunc = ((x) => { return x; });
            //minRange = 0;
            //maxRange = 480f;
            //minFunc = 0;
            //maxFunc = 480;
            //hiddenLayerSize = 10;
            //learningRate = 0.001f;
            //momentum = 0.01f;
            //learning_rate_decay = 10; // Epochs

            //approximationFunc = ((x) => { return (float)Math.Pow(x, 3) - 2 * x; });
            //minRange = 0;
            //maxRange = 5.0f;
            //minFunc = -5;
            //maxFunc = 125;
            //hiddenLayerSize = 22;
            //learningRate = 0.01f;
            //momentum = 0.01f;
            //learning_rate_decay = 12; // Epochs

            approximationFunc = ((x) => { return (float)Math.Sin(x); });
            minRange = 0;
            maxRange = (float)Math.PI * 2.0f;
            minFunc = -1;
            maxFunc = 1;
            hiddenLayerSize = 22;
            learningRate = 0.01f;
            momentum = 0.01f;
            learning_rate_decay = 10; // Epochs
        }

        private static float performEpoch(UnifiedCameraNeuralNetwork ann,
                                         int numOfSamples, float[] samples, int numOfTests, float[] tests,
                                         DrawingGroup drawingGroup)
        {
            int failedTests = 0;

            using (DrawingContext dc = drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                float RenderWidth = 640f;
                float RenderHeight = 480f;
                dc.DrawRectangle(Brushes.Black, null, new System.Windows.Rect(0.0, 0.0, RenderWidth, RenderHeight));
                drawingGroup.ClipGeometry = new RectangleGeometry(new System.Windows.Rect(0.0, 0.0, RenderWidth, RenderHeight));

                for (int i = 0; i < numOfSamples; i++)
                {
                    float x = samples[i];
                    float[] input = new float[1];
                    float[] expected = new float[1];
                    input[0] = (float)x;
                    expected[0] = normalizeFunc(x); // Normalize
                    ann.train(input, expected);

                    if (i % 10000 == 9999)
                        Console.WriteLine((i + 1) + " samples trained..");
                }

                if (ann.Stochastic)
                    ann.forceWeightUpdates();

                for (int i = 0; i < numOfTests; i++)
                {
                    float x = tests[i];
                    float[] input = new float[1];
                    input[0] = (float)x;
                    float result = approximationFunc(x);
                    float prediction = ann.feedForward(input)[0, 0];

                    if (float.IsNaN(prediction))
                        throw new InvalidOperationException("ANN returned a NaN output.");

                    prediction = denormalizeFunc(prediction); // Denormalize

                    // Draw
                    int brushSize = 2;
                    float xPlot = (x / (maxRange - minRange))* RenderWidth;
                    float funcPlot = RenderHeight - normalizeFunc(x) * RenderHeight;
                    float predictPlot = RenderHeight - normalize(prediction) * RenderHeight;
                    dc.DrawEllipse(groundTruthBrush, null, new System.Windows.Point(xPlot, funcPlot), brushSize, brushSize);
                    dc.DrawEllipse(predictionBrush, null, new System.Windows.Point(xPlot, predictPlot), brushSize, brushSize);

                    float diff = Math.Abs(result - prediction);

                    if (diff > 0.0001)
                        failedTests++;
                }
            }

            Console.WriteLine("Total failed tests: " + failedTests);
            float successRate = 1.0f - (float)((float)failedTests / (float)numOfTests);
            Console.WriteLine("Success rate: " + successRate);

            return successRate;
        }

        public static void initNeuralNetworkTest()
        {
            initApproxFunc();

            ann = new UnifiedCameraNeuralNetwork(inputLayerSize, hiddenLayerSize, outputLayerSize,
                                                                            learningRate, isStochastic, momentum);

            Random random = new Random(123);

            for (int i = 0; i < numOfSamples; i++)
                samples[i] = (float)random.NextDouble() * (maxRange - minRange) + minRange;

            for (int i = 0; i < numOfTests; i++)
                tests[i] = (float)random.NextDouble() * (maxRange - minRange) + minRange;
        }

        public static void runNeuralNetworkTest(DrawingGroup drawingGroup)
        {
            if (successRate >= targetSuccess)
            {
                Console.WriteLine("Training complete, " + epochNum + " epoches were needed for " + targetSuccess + " success rate.");
                return;
            }

            Console.WriteLine("Starting epoch #" + epochNum);
            successRate = performEpoch(ann, numOfSamples, samples, numOfTests, tests, drawingGroup);
            epochNum++;

            if (epochNum % learning_rate_decay == learning_rate_decay - 1)
                ann.LearningRate = ann.LearningRate * 0.5f; // Reduce learning rate after every few epochs
        }

        private static float normalize(float x)
        {
            return (x - minFunc) / (maxFunc - minFunc);

            //return (float)(x + 1) / 2;
        }

        private static float normalizeFunc(float x)
        {
            return normalize(approximationFunc(x));

            // return (float)((Math.Sin(x) + 1) / 2);
        }

        private static float denormalizeFunc(float x)
        {
            return x * (maxFunc - minFunc) + minFunc;

            // return (x * 2) - 1;
        }

        private static float testMatrixPerElementOperation(float x)
        {
            return x + 1;
        }

        private static float testMatrixProduct(float x, float y)
        {
            return x / y;
        }

        public static void runMatrixTest()
        {
            Matrix mat1 = new Matrix(3, 3);
            mat1[0, 0] = 1;
            mat1[0, 1] = 0;
            mat1[0, 2] = 0;
            mat1[1, 0] = 0;
            mat1[1, 1] = 1;
            mat1[1, 2] = 0;
            mat1[2, 0] = 0;
            mat1[2, 1] = 0;
            mat1[2, 2] = 1;

            Matrix mat2 = new Matrix(3, 3);
            mat2[0, 0] = 1;
            mat2[0, 1] = 0;
            mat2[0, 2] = 0;
            mat2[1, 0] = 0;
            mat2[1, 1] = 1;
            mat2[1, 2] = 0;
            mat2[2, 0] = 0;
            mat2[2, 1] = 0;
            mat2[2, 2] = 1;

            if (!mat1.Equals(mat2))
            {
                Console.WriteLine("Equal test #1 failed");
                return;
            }

            mat2[0, 0] = 0;

            if (mat1.Equals(mat2))
            {
                Console.WriteLine("Equal test #2 failed");
                return;
            }

            if (!mat1.Equals(Matrix.identity(3,3)) || mat2.Equals(Matrix.identity(3,3)))
            {
                Console.WriteLine("Equal test #3 failed");
                return;
            }

            float[] vals = new float[9];
            for (int i = 0; i < 9; i++)
                vals[i] = i;

            Matrix matVals = new Matrix(vals);
            Matrix matClone = new Matrix(matVals);

            if (!matVals.Equals(matClone))
            {
                Console.WriteLine("Copy constructor failed");
                return;
            }

            Matrix mul = new Matrix(1, 9);
            mul = mul.transpose();
            mul.init(vals);
            if (!mul.Equals(matClone))
            {
                Console.WriteLine("Init failed");
                return;
            }

            matVals = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    matVals[i,j] = i * 3 + j;

            matClone = new Matrix(matVals);
            mul = matVals.mul(mat1);

            if (!mul.Equals(matClone))
            {
                Console.WriteLine("Mul with identity failed");
                return;
            }

            mul = matVals.mul(matClone);

            if (mul.Equals(matClone))
            {
                Console.WriteLine("Mul with non-identity failed");
                return;
            }

            // Debug window zone
            Matrix add = matClone + matVals;
            Matrix sub = matClone - matVals;
            Matrix transpose = matClone.transpose();
            Matrix mulScalar = matClone * 0.5f;
            Matrix dot = matClone.dot(matVals);

            Matrix.MatrixPerElementOperation testfunc1 = testMatrixPerElementOperation;
            Matrix.MatrixPerElementProduct testfunc2 = testMatrixProduct;
            Matrix invoke1 = matClone.invoke(testfunc1);
            Matrix invoke2 = matClone.invoke(testfunc2, matVals);

            Console.WriteLine("Test ended");
        }
    }
}
