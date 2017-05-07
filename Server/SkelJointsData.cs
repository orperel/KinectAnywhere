using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectAnywhere
{
    /// <summary>
    /// A "plain object" for holding information about a single skeleton tracked, for a single camera,
    /// at a certain frame.
    /// </summary>
    public class SkelJointsData
    {
        /// <summary>
        /// A special position value for untracked joints.
        /// MinValue of float is used as a heuristic for representing the non-tracked
        /// case. Although technically this is a legal position value for joints, it
        /// is very unlikely to find a joint tracked at these coordinates (in such case,
        /// the joint tracking is possibly distorted anyway). 
        /// </summary>
        public const float UNTRACKED_POSITION_VALUE = float.MinValue;

        public static int numOfJoints = Enum.GetNames(typeof(JointType)).Length;

        /// <summary>
        /// Camera id that tracked the current skeleton.
        /// </summary>
        public int cameraId { get; }

        /// <summary>
        /// Skeleton id tracked by the camera.
        /// Note: Different cameras may assign different skeleton ids to the same "real" skeleton detected.
        /// </summary>
        public int skelId { get; }

        /// <summary>
        /// Offset in milliseconds of the current frame recorded from the beginning of the recording session.
        /// </summary>
        public uint frameOffset { get; }

        /// <summary>
        /// Positions of each of the skeleton joints.
        /// </summary>
        public SkeletonPoint[] joints { get; }

        /// <summary>
        /// Constructs a new Skeleton Joints Data object.
        /// </summary>
        /// <param name="aCameraId"> Camera id of the camera that captured the skeleton </param>
        /// <param name="aSkelId"> Skeleton id of the skeleton tracked by the camera </param>
        /// <param name="aFrameOffset"> Frame offset in milliseconds from the beginning of the session </param>
        public SkelJointsData(int aCameraId, int aSkelId, uint aFrameOffset)
        {
            cameraId = aCameraId;
            skelId = aSkelId;
            frameOffset = aFrameOffset;
            joints = new SkeletonPoint[numOfJoints];
        }

        /// <summary>
        /// Updates the position of a single joint.
        /// </summary>
        /// <param name="jointType"> The joint type to update </param>
        /// <param name="pos"> The new position of the joint </param>
        public void updateJoint(byte jointType, SkeletonPoint pos)
        {
            joints[jointType] = pos;
        }

        /// <summary>
        /// Converts the skeleton joints data to a flat floats array format.
        /// </summary>
        /// <returns> The joints data in a format of [x,y,z,x,y,z,...] float 1d array of floats </returns>
        public float[] toArray()
        {
            float[] vals = new float[numOfJoints * 3];
            int i = 0;

            foreach (SkeletonPoint joint in joints)
            {
                vals[i] = joint.X;
                vals[i + 1] = joint.Y;
                vals[i + 2] = joint.Z;
                i += 3;
            }

            return vals;
        }
    }
}
