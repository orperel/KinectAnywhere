using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Kinect;
using System.Globalization;

namespace KinectAnywhere
{
    /// <summary>
    /// A class for replaying skeleton state in each frame from local camera files, in real time.
    /// Records can be used, for example, to feed a neural network with training data.
    /// </summary>
    public class SkelReplay
    {
        // The maximum threshold of milliseconds between 2 recordings for them to count as the same frame.
        // This number is chosen as a heuristic considering the Kinect's rate of skeleton capturing
        // (30 FPS ~ 33 ms)
        private const int FRAME_TIME_THRESHOLD = 24; // milliseconds
        
        /// <summary>
        /// Time of the beginning of the recording session.
        /// </summary>
        private DateTime _sessionTimestamp;

        /// <summary>
        /// Recorded skeleton joint states, by cameraId.
        /// Each cameraId data is sorted in chronological order.
        /// </summary>
        private Dictionary<int, LinkedList<SkelJointsData>> _camRecordedFrames;

        /// <summary>
        /// A handler for handling each frame data (the skeleton information as captured by each camera).
        /// </summary>
        /// <param name="frameData"> The skeleton information as captured by each camera </param>
        public delegate void ReplayFrameHandler(Dictionary<SkelJointsData, int> frameData);

        /// <summary>
        /// Reads and parses the next skeleton stored in the cameraFile.
        /// </summary>
        /// <param name="cameraFile"> Camera file to read from </param>
        /// <param name="cameraId"> Camera id of the camera whose file is parsed </param>
        /// <returns></returns>
        private SkelJointsData fetchNextSkeletonFromCameraFile(BinaryReader cameraFile, int cameraId)
        {
            // The format of each skeleton recording file:
            // <cameraId>
            // <timestamp offset> <skelTrackingId>
            // <skel joint id> <joint x,y,z> <skel joint id> <joint x,y,z> ...
            // <timestamp offset> <skelTrackingId>
            // <skel joint id> <joint x,y,z> <skel joint id> <joint x,y,z> ...
            // ...

            uint frameOffset = cameraFile.ReadUInt32();
            int skelId = cameraFile.ReadInt32();

            SkelJointsData jointsData = new SkelJointsData(cameraId, skelId, frameOffset);

            for (int i = 0; i < SkelJointsData.numOfJoints; i++)
            {
                byte parsedjointType = cameraFile.ReadByte();
                float posX = cameraFile.ReadSingle();
                float posY = cameraFile.ReadSingle();
                float posZ = cameraFile.ReadSingle();

                SkeletonPoint jointPos = new SkeletonPoint();
                jointPos.X = posX;
                jointPos.Y = posY;
                jointPos.Z = posZ;
                jointsData.updateJoint(parsedjointType, jointPos);
            }

            return jointsData;
        }

        /// <summary>
        /// Load the entire data of a single camera.
        /// </summary>
        /// <param name="cameraId"> Camera id of the camera whose file will be loaded </param>
        private void loadCameraFile(int cameraId)
        {
            string filename = null;
            BinaryReader reader = null;

            try
            {
                filename = SkelRecorder.getCameraFilename(_sessionTimestamp, cameraId);
                Console.Write("Loading camera file " + filename + "...");
                reader = new BinaryReader(File.Open(filename, FileMode.Open));

                int parsedCameraId = reader.ReadInt32();
                if (cameraId != parsedCameraId)
                    throw new InvalidOperationException("Camera file #" + cameraId +
                                                        " contains an illegal header: " + parsedCameraId);

                // Read entire camera file to memory
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    SkelJointsData jointsData =
                        fetchNextSkeletonFromCameraFile(reader, cameraId);
                    _camRecordedFrames[cameraId].AddLast(jointsData);
                }

                reader.Close();
                reader.Dispose();
            }
            catch (FileNotFoundException e)
            {
                Console.Error.Write("Error: Camera file " + filename + " not found");
                Console.Error.Write("Details: " + e);
                Environment.Exit(1);
            }
            catch (IOException e)
            {
                Console.Error.Write("Error: IO Exception during openning of camera file " + filename);
                Console.Error.Write("Details: " + e);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Constructs a new Skeleton Capture Replay object.
        /// </summary>
        /// <param name="sessionTimestamp"> The time of the beginning of the recorded session. </param>
        /// <param name="numOfCameras"> The number of cameras involved in the recording session. </param>
        public SkelReplay(DateTime sessionTimestamp, int numOfCameras)
        {
            Console.Write("Loading camera files for session " + sessionTimestamp);
            _sessionTimestamp = sessionTimestamp;
            _camRecordedFrames = new Dictionary<int, LinkedList<SkelJointsData>>();

            // Load each camera data to memory.
            // Note: A single camera information recording that lasts for 1 hour is expected to
            // be as big as 28 MBs of memory.
            for (int cameraIndex = 0; cameraIndex < numOfCameras; cameraIndex++)
            {
                _camRecordedFrames[cameraIndex] = new LinkedList<SkelJointsData>();
                loadCameraFile(cameraIndex);
            }

            Console.Write("Skeleton capture session replay loaded successfully.");
        }

        /// <summary>
        /// Replays a single frame of the recording session.
        /// Cameras will be fowarded until a synced frame is found for all cameras involved in the recording
        /// session. Other recordings will be ignored.
        /// The handler will be invoked for the replayed frame.
        /// </summary>
        /// <param name="handler"> A handler to respond to the recorded frame data fetched. </param>
        /// <returns> True if a frame has been replayed, false if the session recording has ended. </returns>
        public bool replaySessionFrame(ReplayFrameHandler handler)
        {
            // Sync all cameras on the same frame

            int numOfCameras = _camRecordedFrames.Keys.Count;
            Dictionary<SkelJointsData, int> frameData = new Dictionary<SkelJointsData, int>(numOfCameras);
            bool isSynced = true;

            uint minOffset = UInt32.MaxValue;

            do // Repeat until all cameras are synced on the same frame
            {
                // Find earliest frame recorded for any of the cameras
                for (int cameraId = 0; cameraId < numOfCameras; cameraId++)
                {
                    // Recording of camera has ended
                    if (_camRecordedFrames[cameraId].First == null)
                        return false; // No frame was replayed

                    minOffset = Math.Min(minOffset, _camRecordedFrames[cameraId].First.Value.frameOffset);
                }

                isSynced = true;

                // Check each of the cameras against the earliest frame recording for any of the cameras.
                // See if we can find a common frame for all cameras.
                for (int cameraId = 0; cameraId < numOfCameras; cameraId++)
                {
                    SkelJointsData camFrameInfo = _camRecordedFrames[cameraId].First.Value;
                    uint timeOffset = camFrameInfo.frameOffset;

                    // Skel recordings must occur within the threshold distance to count as the same frame.
                    bool isInMinFrame = Math.Abs(timeOffset - minOffset) < FRAME_TIME_THRESHOLD;
                    isSynced = isSynced && isInMinFrame;

                    if (isInMinFrame)
                    { // Capture frame data for current camera and eliminate from recordings list
                        frameData.Add(camFrameInfo, cameraId);
                        _camRecordedFrames[cameraId].RemoveFirst();
                    }
                }

            } while (!isSynced); // Repeat until synced

            // Invoke handler on the common frame data for all cameras.
            handler.Invoke(frameData);

            // A frame was replayed
            return true;
        }
    }
}
