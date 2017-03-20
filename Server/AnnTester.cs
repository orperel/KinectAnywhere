using KinectAnywhere;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.SkeletonServer
{
    class AnnTester
    {
        public static void runTest()
        {
            int inputLayerSize = 1;
            int outputLayerSize = 1;

            // Configurable ANN parameters
            int hiddenLayerSize = 20;
            float learningRate = 0.15f;
            float momentum = 0f;

            UnifiedCameraNeuralNetwork ann = new UnifiedCameraNeuralNetwork(inputLayerSize, hiddenLayerSize, outputLayerSize,
                                                                    learningRate, momentum);

            Random random = new Random();

            for (int i = 0; i < 1000000; i++)
            {
                double x = random.NextDouble();
                float[] input = new float[1];
                float[] expected = new float[1];
                input[0] = (float)x;
                expected[0] = (float)((Math.Sin(x) + 1) / 2); // Normalize
                ann.train(input, expected);
            }

            int failedTests = 0;

            for (int i = 0; i < 7000; i++)
            {
                double x = random.NextDouble();
                float[] input = new float[1];
                input[0] = (float)x;
                float result = (float)Math.Sin(x);
                float prediction = ann.feedForward(input)[0,0];
                prediction = (prediction * 2) - 1; // Denormalize
                float diff = Math.Abs(result - prediction);

                if (diff > 0.000001)
                    failedTests++;
            }

            Console.WriteLine("Total failed tests: " + failedTests);
        }
    }
}
