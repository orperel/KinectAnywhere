using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace KinectAnywhere
{
    /// <summary>
    /// Displays skeletons on the screen
    /// </summary>
    public class SkelDisplay
    {
        private const float RenderWidth = 640.0f;   // Width of the output drawing
        private const float RenderHeight = 480.0f;  // Height of the output drawing
        private const double JointThickness = 3;    // Thickness of drawn joint lines
        private const double BodyCenterThickness = 10;  // Thickness of body center ellipse
        private const double ClipBoundsThickness = 10;  // Thickness of clip edge rectangles       
        private readonly Brush centerPointBrush = Brushes.Blue; // Brush used to draw skeleton center point
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));   // Brush used for drawing joints that are currently tracked     
        private readonly Brush inferredJointBrush = Brushes.Yellow; // Brush used for drawing joints that are currently inferred
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);    // Pen used for drawing bones that are currently tracked
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);    // Pen used for drawing bones that are currently inferred
        private DrawingGroup drawingGroup;  // Drawing group for skeleton rendering output
        public DrawingImage imageSource;   // Drawing image that we will display

        private CoordinateMapper coordinateMapper;  // Active coordinate mapper

        /// <summary>
        /// Constructs a new skeleton display instance
        /// </summary>
        public SkelDisplay(KinectSensor sensor)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            if (sensor != null)
            {
                this.coordinateMapper = sensor.CoordinateMapper;
            }
            else
            {
                byte[] coordMapperParams = File.ReadAllBytes("..\\..\\Coord_Mapper_Params.txt");
                this.coordinateMapper = new CoordinateMapper(coordMapperParams);
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, RenderWidth, ClipBoundsThickness));

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, ClipBoundsThickness, RenderHeight));

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Joint[] joints, DrawingContext drawingContext, JointType jointType0, JointType jointType1, Boolean skelPointsOnly = false)
        {
            Joint joint0 = joints[(int)jointType0];
            Joint joint1 = joints[(int)jointType1];

            Pen drawPen = this.inferredBonePen;

            if (!skelPointsOnly)
            {
                // If we can't find either of these joints, exit
                if (joint0.TrackingState == JointTrackingState.NotTracked || joint1.TrackingState == JointTrackingState.NotTracked)
                    return;

                // Don't draw if both points are inferred
                if (joint0.TrackingState == JointTrackingState.Inferred && joint1.TrackingState == JointTrackingState.Inferred)
                    return;

                // We assume all drawn bones are inferred unless BOTH joints are tracked
                if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
                    drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space 
            // We are not using depth directly, but we do want the points in our 640x480 output resolution
            DepthImagePoint depthPoint = this.coordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Joint[] joints, DrawingContext drawingContext, Boolean skelPointsOnly = false)
        {
            // Render Torso
            this.DrawBone(joints, drawingContext, JointType.Head, JointType.ShoulderCenter, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.ShoulderCenter, JointType.Spine, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.Spine, JointType.HipCenter, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.HipCenter, JointType.HipLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.HipCenter, JointType.HipRight, skelPointsOnly);

            // Left Arm
            this.DrawBone(joints, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.ElbowLeft, JointType.WristLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.WristLeft, JointType.HandLeft, skelPointsOnly);

            // Right Arm
            this.DrawBone(joints, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.ElbowRight, JointType.WristRight, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.WristRight, JointType.HandRight, skelPointsOnly);

            // Left Leg
            this.DrawBone(joints, drawingContext, JointType.HipLeft, JointType.KneeLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.KneeLeft, JointType.AnkleLeft, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.AnkleLeft, JointType.FootLeft, skelPointsOnly);

            // Right Leg
            this.DrawBone(joints, drawingContext, JointType.HipRight, JointType.KneeRight, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.KneeRight, JointType.AnkleRight, skelPointsOnly);
            this.DrawBone(joints, drawingContext, JointType.AnkleRight, JointType.FootRight, skelPointsOnly);

            // Render Joints
            foreach (Joint joint in joints)
            {
                Brush drawBrush = null;

                if (!skelPointsOnly)
                {
                    if (joint.TrackingState == JointTrackingState.Tracked)
                        drawBrush = this.trackedJointBrush;
                    else if (joint.TrackingState == JointTrackingState.Inferred)
                        drawBrush = this.inferredJointBrush;
                }
                else
                {
                    drawBrush = this.trackedJointBrush;
                }
                
                if (drawBrush != null)
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
            }
        }

        private Joint[] convertJointCollToJointArr(JointCollection jointColl)
        {
            Joint[] jointArr = new Joint[jointColl.Count];
            int i = 0;
            foreach (Joint joint in jointColl)
            {
                jointArr[i] = joint;
                i++;
            }
            return jointArr;
        }

        public void drawSkeletons(List<Skeleton> skeletons)
        {
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                for (int i = 0; i < skeletons.Count; i++)
                {
                    RenderClippedEdges(skeletons[i], dc);

                    // Draws the skeleton
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                        this.DrawBonesAndJoints(convertJointCollToJointArr(skeletons[i].Joints), dc);
                    else if (skeletons[i].TrackingState == SkeletonTrackingState.PositionOnly)
                        dc.DrawEllipse(this.centerPointBrush, null, this.SkeletonPointToScreen(skeletons[i].Position), BodyCenterThickness, BodyCenterThickness);
                    Skeleton sk = skeletons[i];
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        public void drawSkeletons(SkelJointsData[] frameData)
        {
            Joint[] joints = new Joint[frameData[0].joints.Length];
            for (int i = 0; i < frameData[0].joints.Length; i++)
            {
                joints[i] = new Joint();
                joints[i].Position = frameData[0].joints[i];
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (joints.Length != 0)
                    this.DrawBonesAndJoints(joints, dc, true);

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }
    }
}