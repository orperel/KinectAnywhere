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
            int numOfJoints = Enum.GetNames(typeof(JointType)).Length;
            joints = new SkeletonPoint[numOfJoints];
        }

        /// <summary>
        /// Updates the position ofa single joint.
        /// </summary>
        /// <param name="jointType"> The joint type to update </param>
        /// <param name="pos"> The new position of the joint </param>
        public void updateJoint(byte jointType, SkeletonPoint pos)
        {
            joints[jointType] = pos;
        }
    }
}
