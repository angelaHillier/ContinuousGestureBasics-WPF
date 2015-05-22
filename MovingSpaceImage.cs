//------------------------------------------------------------------------------
// <copyright file="MovingSpaceImage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ContinuousGestureBasics
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Represents an image that can move within the bounds of the Space visual
    /// </summary>
    public sealed class MovingSpaceImage
    {
        /// <summary> TransformGroup associated with the image </summary>
        private TransformGroup transformGroup = null;

        /// <summary> Random number generator, used to set new translation/rotation values when an object goes out of bounds </summary>
        private Random rand = null;

        /// <summary>
        /// Initializes a new instance of the MovingSpaceImage class
        /// </summary>
        /// <param name="imageSource">ImageSource associated with the moving space image</param>
        /// <param name="imageWidth">Starting width to display image at</param>
        /// <param name="imageHeight">Starting height to display image at</param>
        /// <param name="speed">Initial speed of the moving space image</param>
        public MovingSpaceImage(ImageSource imageSource, double imageWidth, double imageHeight, double speed)
        {
            if (imageSource == null)
            {
                throw new ArgumentNullException("imageSource");
            }

            this.Image = new Image();
            this.Image.Source = imageSource;
            this.Image.Width = imageWidth;
            this.Image.Height = imageHeight;
            this.Image.RenderTransformOrigin = new Point(0.5, 0.5);
            this.Rotation = new RotateTransform();
            this.Translation = new TranslateTransform();
            this.Scale = new ScaleTransform();
            this.transformGroup = new TransformGroup();
            this.transformGroup.Children.Add(this.Rotation);
            this.transformGroup.Children.Add(this.Translation);
            this.transformGroup.Children.Add(this.Scale);
            this.Image.RenderTransform = this.transformGroup;
            this.Speed = speed;
            this.rand = new Random();
        }

        /// <summary> Gets the image to visualize and move in the UI </summary>
        public Image Image { get; private set; }

        /// <summary> Gets or sets a value which represents the image's rotation </summary>
        public RotateTransform Rotation { get; set; }

        /// <summary> Gets or sets a value which represents the image's scale </summary>
        public ScaleTransform Scale { get; set; }

        /// <summary> Gets or sets a value which represents the speed of the moving image </summary>
        public double Speed { get; set; }

        /// <summary> Gets or sets a value which represents the image's translation </summary>
        public TranslateTransform Translation { get; set; }

        /// <summary>
        /// Updates the position (rotation and translation) of an object in space
        /// </summary>
        /// <param name="space">Image of space, which determines the bounding area that the MovingSpaceImage can move within</param>
        /// <param name="useRandomPoint">True, if random values can be used for rotation/translation when the object moves outside of the space boundary</param>
        public void UpdatePosition(Image space, bool useRandomPoint)
        {
            if (space == null)
            {
                throw new ArgumentNullException("space");
            }

            var oldPosition = new Point(this.Translation.X, this.Translation.Y);
            double radians = this.Rotation.Angle * (Math.PI / 180);

            // The object's rotation is based off of the Y axis, so switch sin and cos when calculating new X/Y values
            double x = oldPosition.X + (Math.Sin(radians) * this.Speed);
            double y = oldPosition.Y - (Math.Cos(radians) * this.Speed);
            var newPosition = new Point(x, y);

            // Verify that the new point falls within the bounds of the space image, 
            // if the point is not in bounds, calculate a new one
            this.AdjustPositionToFitSpaceBounds(space, oldPosition, ref newPosition, useRandomPoint);
            this.Translation.X = newPosition.X;
            this.Translation.Y = newPosition.Y;
        }

        /// <summary>
        /// Adjusts an object's translation coordinates to keep the object within the bounds of the space image
        /// Note: The object originates at the center of the space image, so we can use this point to
        /// move between the two coordinate systems.
        /// ***************************************************************
        ///  Space Image Coordinates    Object Translation Coordinates
        ///    .--------->                 -x, -y | +x, -y
        ///    |                           _______|________
        ///    | +x, +y                           | 
        ///    |                           -x, +y | +x, +y
        ///    v
        /// ***************************************************************
        /// </summary>
        /// <param name="space">Image of space, which defines the bounding box of the moving object</param>
        /// <param name="oldTranslationPoint">The object's current TranslateTransform point</param>
        /// <param name="newTranslationPoint">The next TranslateTransform point that the object will move to</param>
        /// <param name="useRandomPoint">True, if using random values for translation/rotation is acceptable</param>
        private void AdjustPositionToFitSpaceBounds(Image space, Point oldTranslationPoint, ref Point newTranslationPoint, bool useRandomPoint)
        {            
            // Update the bounding box created by the space image
            var spaceTopLeftPoint = space.PointToScreen(new Point(0, 0));
            var spaceSize = new Size(space.ActualWidth, space.ActualHeight);
            var spaceRect = new Rect(spaceTopLeftPoint, spaceSize);
            var spaceCenterPoint = new Point(spaceTopLeftPoint.X + (space.ActualWidth / 2), spaceTopLeftPoint.Y + (space.ActualHeight / 2));

            // The object originates at spaceCenterPoint, so we can get the object into space coordinates by adding the center point to the translation values
            var oldPointInSpace = new Point(oldTranslationPoint.X + spaceCenterPoint.X, oldTranslationPoint.Y + spaceCenterPoint.Y);
            var newPointInSpace = new Point(newTranslationPoint.X + spaceCenterPoint.X, newTranslationPoint.Y + spaceCenterPoint.Y);

            // verify that the object falls within the space boundaries, 
            // if not, calculate a new point on the opposite side of the space image
            if (!spaceRect.Contains(newPointInSpace))
            {
                if (useRandomPoint)
                {
                    // when an asteroid goes out of bounds, set it to a new rotation/position within space
                    this.ResetToRandomPointOnOppositeEdge(spaceRect, ref newPointInSpace);
                    this.Rotation.Angle = this.rand.NextDouble() * 360;
                }
                else
                { 
                    // when the ship goes out of bounds, set the image to the opposite side of space
                    // find the opposite point on the line so it can continue on the same path as before
                    this.ResetToOppositePointOnLine(spaceRect, oldPointInSpace, ref newPointInSpace);
                }

                // remove spaceCenterPoint to get back to the object's coordinate system
                newTranslationPoint.X = newPointInSpace.X - spaceCenterPoint.X;
                newTranslationPoint.Y = newPointInSpace.Y - spaceCenterPoint.Y;
            }
        }

        /// <summary>
        /// Resets the given point to a random location on the opposite edge of space
        /// </summary>
        /// <param name="spaceRect">Bounding rectangle created by the space image</param>
        /// <param name="pointInSpace">A point which falls outside of the space boundary and needs to be reset</param>
        private void ResetToRandomPointOnOppositeEdge(Rect spaceRect, ref Point pointInSpace)
        {
            int top = Convert.ToInt32(spaceRect.Top);
            int bottom = Convert.ToInt32(spaceRect.Bottom);
            int left = Convert.ToInt32(spaceRect.Left);
            int right = Convert.ToInt32(spaceRect.Right);

            if (pointInSpace.X < spaceRect.Left || pointInSpace.X > spaceRect.Right)
            {
                if (pointInSpace.X < spaceRect.Left)
                {
                    pointInSpace.X = spaceRect.Right;
                }
                else
                {
                    pointInSpace.X = spaceRect.Left;
                }

                pointInSpace.Y = this.rand.Next(top, bottom);
            }
            else if (pointInSpace.Y < spaceRect.Top || pointInSpace.Y > spaceRect.Bottom)
            {
                if (pointInSpace.Y < spaceRect.Top)
                {
                    pointInSpace.Y = spaceRect.Bottom;
                }
                else
                {
                    pointInSpace.Y = spaceRect.Top;
                }

                pointInSpace.X = this.rand.Next(left, right);
            }
        }

        /// <summary>
        /// Resets the new point to another point on the line, which is on the opposite side of space
        /// </summary>
        /// <param name="spaceRect">Bounding rectangle created by the space image</param>
        /// <param name="oldPointInSpace">Current position which lies within the space bounds</param>
        /// <param name="newPointInSpace">New position which falls outside of the space bounds and needs to be reset</param>
        private void ResetToOppositePointOnLine(Rect spaceRect, Point oldPointInSpace, ref Point newPointInSpace)
        {
            // Note: Because the object's Y coordinate decreases when the object is moving up, and increases when the object is moving down, 
            // the equation of our line is slightly different: y = mx - b
            double slope = 0; // the 'm' value in our slope-intercept equation
            double intercept = 0; // the 'b' value in our slope-intercept equation

            if (newPointInSpace.X - oldPointInSpace.X != 0)
            {
                slope = (newPointInSpace.Y - oldPointInSpace.Y) / (newPointInSpace.X - oldPointInSpace.X);
            }

            intercept = (slope * oldPointInSpace.X) - oldPointInSpace.Y;

            // adjust the X coordinate of the object, if it does not fall within the left/right bounds of the space image
            if (newPointInSpace.X > spaceRect.Right || newPointInSpace.X < spaceRect.Left)
            {
                if (newPointInSpace.X > spaceRect.Right)
                {
                    newPointInSpace.X = spaceRect.Left;
                }
                else if (newPointInSpace.X < spaceRect.Left)
                {
                    newPointInSpace.X = spaceRect.Right;
                }

                newPointInSpace.Y = (slope * newPointInSpace.X) - intercept;

                // verify that the new Y coordinate falls within the top/bottom bounds of space, if not, we need to adjust it
                if (newPointInSpace.Y < spaceRect.Top || newPointInSpace.Y > spaceRect.Bottom)
                {
                    if (newPointInSpace.Y < spaceRect.Top)
                    {
                        newPointInSpace.Y = spaceRect.Top;
                    }
                    else
                    {
                        newPointInSpace.Y = spaceRect.Bottom;
                    }

                    if (slope != 0)
                    {
                        newPointInSpace.X = (newPointInSpace.Y + intercept) / slope;
                    }
                }
            }

            // adjust the Y coordinate of the object, if it does not fall within the top/bottom bounds of the space image
            if (newPointInSpace.Y < spaceRect.Top || newPointInSpace.Y > spaceRect.Bottom)
            {
                if (newPointInSpace.Y < spaceRect.Top)
                {
                    newPointInSpace.Y = spaceRect.Bottom;
                }
                else
                {
                    newPointInSpace.Y = spaceRect.Top;
                }

                if (slope != 0)
                {
                    newPointInSpace.X = (newPointInSpace.Y + intercept) / slope;
                }

                // verify that the new X coordinate falls within the left/right bounds of space, if not, we need to adjust it
                if (newPointInSpace.X < spaceRect.Left || newPointInSpace.X > spaceRect.Right)
                {
                    if (newPointInSpace.X < spaceRect.Left)
                    {
                        newPointInSpace.X = spaceRect.Left;
                    }
                    else
                    {
                        newPointInSpace.X = spaceRect.Right;
                    }

                    newPointInSpace.Y = (slope * newPointInSpace.X) - intercept;
                }
            }
        }
    }
}
