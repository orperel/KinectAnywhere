/**
 *  A "plain object" for holding information about a single skeleton tracked,
 *  for a single camera, at a certain frame.
 *
 */
public class SkelJointsData {

	/**
	 * Contains all of the possible joint types identified by the sensor.
	 */
    public enum JointType
    {
        HipCenter(0), 		// The center of the hip.
        Spine(1),			// The bottom of the spine.
        ShoulderCenter(2),  // The center of the shoulders.
        Head(3),	        // The players head.
        ShoulderLeft(4),    // The left shoulder.
        ElbowLeft(5),       // The left elbow.
        WristLeft(6),       // The left wrist.
        HandLeft(7),        // The left hand.
        ShoulderRight(8),   // The right shoulder.
        ElbowRight(9),      // The right elbow.
        WristRight(10),     // The right wrist.
        HandRight(11),      // The right hand.
        HipLeft(12),        // The left hip.
        KneeLeft(13),       // The left knee.
        AnkleLeft(14),      // The left ankle.
        FootLeft(15),       // The left foot.
        HipRight(16),       // The right hip.
        KneeRight(17),      // The right knee.
        AnkleRight(18),     // The right ankle.
        FootRight(19);      // The right foot.
        
    	/** Amount of possible JointType enum values*/
    	private static final int size = JointType.values().length;
    	
        /** Assigned joint id by the Microsoft Kinect SDK */
        public int jointId;
        
        /** Assigns ID to the constructed joint type */
        private JointType(int id) { 
        	jointId = id;
        }
    }
    
    /**
     * Represents the position of a skeleton joint in 3d space.
     */
    public class SkeletonPoint
    {
        /** The X coordinate of the skeleton point. */
        public float x;

        /** The Y coordinate of the skeleton point. */
        public float y;
        
        /** The Z coordinate of the skeleton point. */
        public float z;
    }
    
	/**
	 * A special position value for untracked joints.
     * MinValue of float is used as a heuristic for representing the non-tracked
     * case. Although technically this is a legal position value for joints, it
     * is very unlikely to find a joint tracked at these coordinates (in such case,
     * the joint tracking is possibly distorted anyway). 
	 */
    public final static float UNTRACKED_POSITION_VALUE = Float.MIN_VALUE;


    /** Camera id that tracked the current skeleton. **/
    public int cameraId;

    /** Skeleton id tracked by the camera.
     *  Note: Different cameras may assign different skeleton ids to the same "real" skeleton detected.
     */  
    public int skelId;

    /** Offset in milliseconds of the current frame recorded from the beginning of the calibration session.
     */
    public int frameOffset;

    /** Positions of each of the skeleton joints.
     */
    public SkeletonPoint[] joints;
    
    /**
     * Constructs a new Skeleton Joints Data object.
     * @param aCameraId Camera id of the camera that captured the skeleton
     * @param aSkelId Skeleton id of the skeleton tracked by the camera
     * @param aFrameOffset Frame offset in milliseconds from the beginning of the session
     */
    public SkelJointsData(int aCameraId, int aSkelId, int aFrameOffset)
    {
        cameraId = aCameraId;
        skelId = aSkelId;
        frameOffset = aFrameOffset;
        joints = new SkeletonPoint[JointType.size];
    }
    
    /**
     * Updates the position of a single joint.
     * @param jointType The joint type to update
     * @param pos The new position of the joint
     */
    public void updateJoint(byte jointType, SkeletonPoint pos)
    {
        joints[jointType] = pos;
    }

    /**
     * Updates the position of a single joint.
     * @param jointType The joint type to update
     * @param pos The new position of the joint
     */
    public void updateJoint(byte jointType, float x, float y, float z)
    {
        joints[jointType].x = x;
        joints[jointType].y = y;
        joints[jointType].z = z;
    }
    
    /**
     * Converts the skeleton joints data to a flat floats array format.
     * @return The joints data in a format of [x,y,z,x,y,z,...] float 1d array of floats
     */
    public float[] toArray()
    {
        float[] vals = new float[JointType.size * 3];
        int i = 0;

        for (SkeletonPoint joint: joints)
        {
            vals[i] = joint.x;
            vals[i + 1] = joint.y;
            vals[i + 2] = joint.z;
            i += 3;
        }

        return vals;
    }
}
