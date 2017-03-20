using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectAnywhere
{
    /// <summary>
    /// This module performs the calibration process of each of the cameras against the main cameras.
    /// Calibration is executed by training a neural network for each pair of camera and "main camera".
    /// After the calibration is done, cameras coordinates can be transformed to absolute coordinates fast
    /// by feeding the camera data into the corresponding neural network.
    /// </summary>
    class Calibration
    {
        private int _numOfCameras; // Number of cameras in the setup
        private SkelReplay _replay; // Replays the recording sessions of each camera
        private UnifiedCameraNeuralNetwork[] _neuralNets; // A network for each pair of camaras (main camera and camera i)
        
        /// <summary>
        /// Creates a new calibration object.
        /// Calibration will occur for the session that was recorded at sessionTimestamp using the numOfCameras defined.
        /// </summary>
        /// <param name="sessionTimestamp"> Timestamp of the session recorded </param>
        /// <param name="numOfCameras"> Number of cameras in the recording </param>
        public Calibration(DateTime sessionTimestamp, int numOfCameras)
        {
            // Non configurable ANN parameters (each network maps a pair of cameras)
            // The input is one of the cameras, the output is the absolute camera.
            int inputLayerSize = SkelJointsData.numOfJoints * 3;
            int outputLayerSize = inputLayerSize;

            // Configurable ANN parameters
            int hiddenLayerSize = inputLayerSize * inputLayerSize;
            float learningRate = 0.15f;
            float momentum = 0.1f;

            // Create an ANN for each pair of cameras
            for (int i = 0; i < numOfCameras - 1; i++)
            {
                _neuralNets[i] = new UnifiedCameraNeuralNetwork(inputLayerSize, hiddenLayerSize, outputLayerSize,
                                                                learningRate, momentum);
            }

            _numOfCameras = numOfCameras;
            _replay = new SkelReplay(sessionTimestamp, numOfCameras);
        }

        /// <summary>
        /// Perform calibration by replaying the recorded session and training a neural network for each pair of cameras.
        /// </summary>
        public void calibrate()
        {
            int i = 0;
            int keep_alive_message_delta = 300;

            // The loop will repeat as long as there are still frames recorded for the session
            while (!_replay.replaySessionFrame(processFrame))
            {
                if (i % keep_alive_message_delta == 0)
                    Console.WriteLine("Calibration executing..");
            }

            Console.WriteLine("Calibration process finished!");
        }

        /// <summary>
        /// This event is prompted for each frame replayed by the skelReplay object.
        /// In response we train the ANN here using the replayed joints data.
        /// </summary>
        /// <param name="frameData"></param>
        private void processFrame(SkelJointsData[] frameData)
        {
            float[] cam0 = frameData[0].toArray(); // Data of the main camera

            // Train each ANN by feeding camera i's data and comparing it with cam0
            for (int i = 1; i < _numOfCameras; i++)
            {
                float[] cami = frameData[i].toArray();
                _neuralNets[i - 1].train(cami, cam0);
            }
        }

        /// <summary>
        /// Converts the coordinates of camera i to the absolute coordinates of camera 0.
        /// Assumption: Calibration process was executed successfully.
        /// </summary>
        /// <param name="cameraId"> Id of the camera whose coordinates will be converted </param>
        /// <param name="skelId"> skeleton id of the skeleton to be converted </param>
        /// <param name="jointsData"> The joints data of the skeleton, in camera i coordinates </param>
        /// <returns></returns>
        public SkelJointsData transform(int cameraId, int skelId, SkelJointsData jointsData)
        {
            // Convert by feeding into corresponding neural network
            SkelJointsData result = new SkelJointsData(cameraId, skelId, 0);
            float[] networkInput = jointsData.toArray();
            Matrix networkOutput = _neuralNets[cameraId - 1].feedForward(networkInput);

            // Fill SkelJointsData with the new coordinates
            for (byte i = 0; i < SkelJointsData.numOfJoints; i++)
            {
                float posX = networkOutput[i, 0];
                float posY = networkOutput[i + 1, 0];
                float posZ = networkOutput[i + 2, 0];

                SkeletonPoint jointPos = new SkeletonPoint();
                byte jointType = i; // Joint types appear sequentially
                jointPos.X = posX;
                jointPos.Y = posY;
                jointPos.Z = posZ;
                result.updateJoint(jointType, jointPos);
            }

            return result;
        }
    }
}
