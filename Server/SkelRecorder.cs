using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Kinect;
using System.Globalization;

namespace KinectAnywhere
{
    /// <summary>
    /// A class for recording skeleton state in each frame to a local file, in real time.
    /// Records can be used later on to feed neural network with training data.
    /// </summary>
    public class SkelRecorder
    {
        private const string REC_FILE_PREFIX = "_cam";
        private const string REC_FILE_SUFFIX = ".rec";

        /// <summary>
        /// The session timestamp is used for name files of different cameras in the same session.
        /// The timestamp is also used as a baseline for tiestamp offset as saved per each frame record.
        /// </summary>
        private DateTime _sessionTimestamp;

        /// <summary>
        /// A hash-map for holding file pointers for each camera present in the session.
        /// </summary>
        private Dictionary<int, BinaryWriter> _cameraFiles;

        /// <summary>
        /// Constructs a new skeleton recorder instance, with the current time label.
        /// </summary>
        public SkelRecorder()
        {
            _sessionTimestamp = DateTime.Now;
            _cameraFiles = new Dictionary<int, BinaryWriter>();
        }

        public static string getCameraFilename(DateTime sessionTimestamp, int cameraId)
        {
            string timestamp = sessionTimestamp.ToString("HH_mm_ss_fff",
                                                          CultureInfo.InvariantCulture);
            string fileName = timestamp + REC_FILE_PREFIX + cameraId + REC_FILE_SUFFIX;

            return fileName;
        }

        /// <summary>
        /// Creates a new empty camera file, with the timestamp of the beginning of the session.
        /// </summary>
        /// <param name="cameraId"></param>
        public void createFile(int cameraId)
        {
            if (_cameraFiles.ContainsKey(cameraId))
                throw new InvalidOperationException("SkelRecorder already has a file for camera #" + cameraId);

            string fileName = null;
            BinaryWriter writer = null;

            try
            {
                fileName = getCameraFilename(_sessionTimestamp, cameraId);
                writer = new BinaryWriter(File.Open(fileName, FileMode.Create));

                _cameraFiles.Add(cameraId, writer);
                writer.Write(cameraId); // Write header
            }
            catch (IOException e)
            {
                Console.Error.Write("Error: IO Exception during creation of camera file " + fileName + "...");
                Console.Error.Write("Details: " + e);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Writes the data of a single skeleton of a camera, in a given frame, to the camera file.
        /// </summary>
        /// <param name="skel"> Skeleton joints data to save </param>
        /// <param name="cameraId"> Camera id of the camera who tracks the skeleton </param>
        /// <param name="timestamp"> timestamp of the current frame </param>
        public void recordSkelFrame(Skeleton skel, int cameraId, DateTime timestamp)
        {
            // The format of each skeleton recording file:
            // <cameraId>
            // <timestamp offset> <skelTrackingId>
            // <skel joint id> <joint x,y,z> <skel joint id> <joint x,y,z> ...
            // <timestamp offset> <skelTrackingId>
            // <skel joint id> <joint x,y,z> <skel joint id> <joint x,y,z> ...
            // ...

            // Note: camera is expected to exist by the time this method is called.
            // The process is expected to be executed in real time, so we try to make this function call
            // as efficient as possible
            // (a low level optimization: avoid redundant if mostly not taken in each frame).
            BinaryWriter writer = _cameraFiles[cameraId];

            // Record each of the skeleton's joints.
            // Only tracked skeletons and items are recorded.
            if (skel.TrackingState == SkeletonTrackingState.Tracked)
            {
                // Write skel id
                int skelTrackingId = skel.TrackingId;
                writer.Write(skelTrackingId);

                // Write time span since session beginning, in milliseconds.
                // This saves on some bytes since we don't have to encode the whole date format
                // which mostly doesn't change within the session.
                TimeSpan span = timestamp - _sessionTimestamp;
                uint spanMs = (uint)span.TotalMilliseconds;
                writer.Write(spanMs);

                foreach (Joint joint in skel.Joints)
                {
                    byte jointType = (byte)joint.JointType; // Enum
                    float jointX = 0f;
                    float jointY = 0f;
                    float jointZ = 0f;

                    if ((joint.TrackingState == JointTrackingState.Tracked) ||
                        (joint.TrackingState == JointTrackingState.Inferred))
                    {
                        jointX = joint.Position.X;
                        jointY = joint.Position.Y;
                        jointZ = joint.Position.Z;
                    }
                    else // Not tracked
                    {
                        // MinValue of float is used as a heuristic for representing the non-tracked
                        // case. Although technically this is a legal position value for joints, it
                        // is very unlikely to find a joint tracked at these coordinates (in such case,
                        // the joint tracking is possibly distorted anyway). 
                        jointX = SkelJointsData.UNTRACKED_POSITION_VALUE;
                        jointY = SkelJointsData.UNTRACKED_POSITION_VALUE;
                        jointZ = SkelJointsData.UNTRACKED_POSITION_VALUE;
                    }

                    writer.Write(jointType);
                    writer.Write(jointX);
                    writer.Write(jointY);
                    writer.Write(jointZ);
                }
            }
            else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
            {
                // Ignore for now, we deal only with sensor tracked joints of skeletons
            }
        }

        /// <summary>
        /// Close each of the camera files, flushes and clears resources
        /// </summary>
        public void closeFiles()
        {
            foreach (KeyValuePair<int, BinaryWriter> cameraEntry in _cameraFiles)
            {
                BinaryWriter file = cameraEntry.Value;
                file.Close();
                file.Dispose();
            }

            _cameraFiles = null;
        }
    }
}
