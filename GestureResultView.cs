//------------------------------------------------------------------------------
// <copyright file="GestureResultView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ContinuousGestureBasics
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;
    using Microsoft.Samples.Kinect.ContinuousGestureBasics.Common;

    /// <summary>
    /// Tracks gesture results coming from the GestureDetector and displays them in the UI.
    /// Updates the SpaceView object with the latest gesture result data from the sensor.
    /// </summary>
    public sealed class GestureResultView : BindableBase
    {
        /// <summary> True, if the user is attempting to turn left (either 'Steer_Left' or 'MaxTurn_Left' is detected) </summary>
        private bool turnLeft = false;

        /// <summary> True, if the user is attempting to turn right (either 'Steer_Right' or 'MaxTurn_Right' is detected) </summary>
        private bool turnRight = false;

        /// <summary> True, if the user is holding the wheel, but not turning it (Closed hands detected) </summary>
        private bool keepStraight = false;

        /// <summary> Current progress value reported by the continuous 'SteerProgress' gesture </summary>
        private float steerProgress = 0.0f;
        
        /// <summary> True, if the body is currently being tracked </summary>
        private bool isTracked = false;

        /// <summary> SpaceView object in UI which has a spaceship that needs to be updated when we get new gesture results from the sensor </summary>
        private SpaceView spaceView = null;

        /// <summary>
        /// Initializes a new instance of the GestureResultView class and sets initial property values
        /// </summary>
        /// <param name="isTracked">True, if the body is currently tracked</param>
        /// <param name="left">True, if the 'Steer_Left' gesture is currently detected</param>
        /// <param name="right">True, if the 'Steer_Right' gesture is currently detected</param>
        /// <param name="straight">True, if the 'SteerStraight' gesture is currently detected</param>
        /// <param name="progress">Progress value of the 'SteerProgress' gesture</param>
        /// <param name="space">SpaceView object in UI which should be updated with latest gesture result data</param>
        public GestureResultView(bool isTracked, bool left, bool right, bool straight, float progress, SpaceView space)
        {
            if (space == null)
            {
                throw new ArgumentNullException("spaceView");
            }

            this.IsTracked = isTracked;
            this.TurnLeft = left;
            this.TurnRight = right;
            this.KeepStraight = straight;
            this.SteerProgress = progress;
            this.spaceView = space;
        }

        /// <summary> 
        /// Gets a value indicating whether or not the body associated with the gesture detector is currently being tracked 
        /// </summary>
        public bool IsTracked
        {
            get
            {
                return this.isTracked;
            }

            private set
            {
                this.SetProperty(ref this.isTracked, value);
            }
        }

        /// <summary> 
        /// Gets a value indicating whether the user is attempting to turn the ship left 
        /// </summary>
        public bool TurnLeft
        {
            get
            {
                return this.turnLeft;
            }

            private set
            {
                this.SetProperty(ref this.turnLeft, value);
            }
        }

        /// <summary> 
        /// Gets a value indicating whether the user is attempting to turn the ship right 
        /// </summary>
        public bool TurnRight
        {
            get
            {
                return this.turnRight;
            }

            private set
            {
                this.SetProperty(ref this.turnRight, value);
            }
        }

        /// <summary> 
        /// Gets a value indicating whether the user is trying to keep the ship straight
        /// </summary>
        public bool KeepStraight
        {
            get
            {
                return this.keepStraight;
            }

            private set
            {
                this.SetProperty(ref this.keepStraight, value);
            }
        }

        /// <summary> 
        /// Gets a value indicating the progress associated with the 'SteerProgress' gesture for the tracked body 
        /// </summary>
        public float SteerProgress
        {
            get
            {
                return this.steerProgress;
            }

            private set
            {
                this.SetProperty(ref this.steerProgress, value);
            }
        }

        /// <summary>
        /// Updates gesture detection result values for display in the UI
        /// </summary>
        /// <param name="isBodyTrackingIdValid">True, if the body associated with the GestureResultView object is still being tracked</param>
        /// <param name="left">True, if detection results indicate that the user is attempting to turn the ship left</param>
        /// <param name="right">True, if detection results indicate that the user is attempting to turn the ship right</param>
        /// <param name="straight">True, if detection results indicate that the user is attempting to keep the ship straight</param>
        /// <param name="progress">The current progress value of the 'SteerProgress' continuous gesture</param>
        public void UpdateGestureResult(bool isBodyTrackingIdValid, bool left, bool right, bool straight, float progress)
        {
            this.IsTracked = isBodyTrackingIdValid;

            if (!this.isTracked)
            {
                this.TurnLeft = false;
                this.TurnRight = false;
                this.KeepStraight = false;
                this.SteerProgress = -1.0f;
            }
            else
            {
                this.TurnLeft = left;
                this.TurnRight = right;
                this.KeepStraight = straight;
                this.SteerProgress = progress;
            }

            // move the ship in space, using the latest gesture detection results
            this.spaceView.UpdateShipPosition(this.KeepStraight, this.SteerProgress);
        }
    }
}
