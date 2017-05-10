package kinectAnywhere;

import android.graphics.Point;
import kinectAnywhere.SkelJointsData.SkeletonPoint;

/**
 * Maps Kinect IR camera 3d coordinates to 2d screen coordinates for display.
 * Uses Analytical projection to calculate the 2d coordinates.
 * Results may differ slightly from Microsoft SDK's CoordinateMapper which
 * uses a lookup table.
 */
public class AnalyticCoordinatesMapper {
	
	/** Kinect 1 IR camera uses a 640x480 depth map */
	private static final float DEFAULT_ASPECT = 1.33f;
	
	/** Kinect 1 IR camera should have ~46.8 degrees fov according to
	 *  manufacturer definition (this may vary slightly) */
	private static final float DEFAULT_FOVY = 46.8f;
	
	/** Near / far clipping planes*/
	private static final float DEFAULT_NEAR = 1;
	private static final float DEFAULT_FAR = 100;
	
	/** User device canvas coordinates*/
	private int _canvasWidth;
	private int _canvasHeight;
	
	/** Kinect parameters to construct projection matrix */
	private float _aspect;
	private float _fovY;
	private float _near;
	private float _far;
	
	/** Kinect 1 calculated Projection matrix (Camera to 2d screen space). */
	private float[][] _projection;
	
	/**
	 * Calculates the Frustum according to 6 defining planes.
	 * @return Projection matrix defined by given params
	 */
    private static float[][] setFrustum(float l, float r, float b, float t, float n, float f)
    {
        float[][] mat = new float[4][4];
        mat[0][0] = 2 * n / (r - l);
        mat[1][1] = 2 * n / (t - b);
        mat[0][2] = (r + l) / (r - l);
        mat[1][2] = (t + b) / (t - b);
        mat[2][2] = -(f + n) / (f - n);
        mat[3][2] = -1;
        mat[2][3] = -(2 * f * n) / (f - n);
        mat[3][3] = 0;
        return mat;
    }

    /**
     * Gives the projection matrix that matches the given parameters
     * @param fovY Kinect IR camera vertical field of view (in degrees)
     * @param aspect Kinect camera aspect ratio
     * @param front Near plane
     * @param back Far plane
     * @return Projection matrix for the Kinect IR camera for mapping 3d to 2d
     */
    private static float[][] setFrustum(float fovY, float aspect, float front, float back)
    {
    	// tangent of half fovY (deg to rad)
        float tangent = (float)(Math.tan(fovY / 2 * (Math.PI / 180)));
        float height = front * tangent; // Half height of near plane
        float width = height * aspect;  // Half width of near plane

        // Parameters are: left, right, bottom, top, near, far
        return setFrustum(-width, width, -height, height, front, back);
    }
	
    /**
     * Creates a coordinate-mapper with full control over the parameters.
     * This overload is used for pesky Kinect sensors that might not align with the
     * manufacturer technical spec.
     * @param canvasWidth User canvas width.
     * @param canvasHeight User canvas height.
     * @param aspect Aspect ratio of the Kinect camera depth images (W / H)
     * @param fovY Vertical field of view of the Kinect IR camera
     * @param near Front clipping plane (to define frustum)
     * @param far Far clipping plane (to define frustum)
     */
	public AnalyticCoordinatesMapper(int canvasWidth, int canvasHeight, float aspect,
									 float fovY, float near, float far)
	{
		_canvasWidth = canvasWidth;
		_canvasHeight = canvasHeight;
		_aspect = aspect;
		_fovY = fovY;
		_near = near;
		_far = far;
		
		_projection = setFrustum(fovY, aspect, near, far);
	}
	
	/**
     * Creates a coordinate-mapper with default Kinect sensor parameters.
     * This overload should match the majority of Kinect sensors 
     * that behave according to the manufacturer's technical spec.
     * @param canvasWidth User canvas width.
     * @param canvasHeight User canvas height.
     */
	public AnalyticCoordinatesMapper(int canvasWidth, int canvasHeight)
	{
		this(canvasWidth, canvasHeight,
			 DEFAULT_ASPECT, DEFAULT_FOVY, DEFAULT_NEAR, DEFAULT_FAR);
	}
	
	/**
	 * Maps a skeleton joint position from the 3d IR camera space to the
	 * device 2d screen space (the original Microsoft Kinect SDK terms these
	 * as "DepthPoints".
	 * @param point SkeletonPoint in 3d IR camera space.
	 * @return SkeletonPoint projected to 2d.
	 */
    public Point MapSkeletonPointToDepthPoint(SkeletonPoint point)
    {
        float xi = point.x;
        float yi = point.y;
        float zi = point.z;
        float wi = 1; // Position vector

        // Camera to NDC
        float xt = xi * _projection[0][0] + zi * _projection[0][2];
        float yt = yi * _projection[1][1] + zi * _projection[1][2];
        float zt = zi * _projection[2][2] + wi * _projection[2][3];
        float wt = zi * _projection[3][2] + wi * _projection[3][3];

        // Normalized device coordinates to viewport
        float xo = ((xt / zt) + 1) * _canvasWidth - (_canvasWidth * 0.5f);
        float yo = ((yt / zt) + 1) * _canvasHeight - (_canvasHeight * 0.5f);

        return new Point(xo, yo);
    }
}
