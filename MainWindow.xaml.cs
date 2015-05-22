//-----------------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <Description>
// This program detects a set of discrete and continuous gestures for a single, tracked person.
// If a person is tracked, the gesture detector will listen for a set of steering gestures (Steer_Left, Steer_Right, SteerProgress, etc).
// If any steering gestures are detected, the position of the space ship will be updated using the Progress value reported by the continuous (SteerProgress) gesture.
// If no person is tracked, the gesture detector will be paused and the space images will stop moving.
// Note: This sample uses polling to get new frames from the Kinect sensor at 60 fps; for event notification, please see the 'DiscreteGestureBasics-WPF' sample.
// </Description>
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ContinuousGestureBasics
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Active Kinect sensor </summary>
        private KinectSensor kinectSensor = null;
        
        /// <summary> Array for the bodies (Kinect can track up to 6 people simultaneously) </summary>
        private Body[] bodies = null;

        /// <summary>  Index of the active body (first tracked person in the body array) </summary>
        private int activeBodyIndex = 0;

        /// <summary> Reader for body frames </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary> Current kinect status text to display </summary>
        private string statusText = null;

        /// <summary> KinectBodyView object which handles drawing the active body to a view box in the UI </summary>
        private KinectBodyView kinectBodyView = null;
        
        /// <summary> Gesture detector which will be tied to the active body (closest skeleton to the sensor) </summary>
        private GestureDetector gestureDetector = null;

        /// <summary> GestureResultView for displaying gesture results associated with the tracked person in the UI </summary>
        private GestureResultView gestureResultView = null;

        /// <summary> SpaceView for displaying spaceship position and rotation, which are related to gesture detection results </summary>
        private SpaceView spaceView = null;

        /// <summary> Timer for updating Kinect frames and space images at 60 fps </summary>
        private DispatcherTimer dispatcherTimer = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            // initialize the MainWindow
            this.InitializeComponent();

            // only one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // open the sensor
            this.kinectSensor.Open();

            // set the initial status text
            this.UpdateKinectStatusText();

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // initialize the BodyViewer object for displaying tracked bodies in the UI
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);
            
            // initialize the SpaceView object
            this.spaceView = new SpaceView(this.spaceGrid, this.spaceImage);

            // initialize the GestureDetector object
            this.gestureResultView = new GestureResultView(false, false, false, false, -1.0f, this.spaceView);
            this.gestureDetector = new GestureDetector(this.kinectSensor, this.gestureResultView);

            // set data context objects for display in UI
            this.DataContext = this;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
            this.gestureResultGrid.DataContext = this.gestureResultView;
            this.spaceGrid.DataContext = this.spaceView;
            this.collisionResultGrid.DataContext = this.spaceView;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the current Kinect sensor status text to display in UI
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            private set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the GestureDetector object
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.gestureDetector != null)
                {
                    this.gestureDetector.Dispose();
                    this.gestureDetector = null;
                }
            }
        }

        /// <summary>
        /// Polls for new Kinect frames and updates moving objects in the spaceView
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateKinectStatusText();
            this.UpdateKinectFrameData();

            if (!this.spaceView.ExplosionInProgress)
            {
                if (this.bodies != null)
                {
                    // only move asteroids when someone is available to drive the ship
                    if (this.bodies[this.activeBodyIndex].IsTracked)
                    {
                        this.spaceView.UpdateTimeSinceCollision(false);
                        this.spaceView.UpdateAsteroids();
                        this.spaceView.CheckForCollision();
                    }
                    else
                    {
                        // pause the collision timer when no bodies are tracked
                        this.spaceView.UpdateTimeSinceCollision(true);
                    }
                }
            }
            else
            {
                this.spaceView.UpdateExplosion();
            }
        }

        /// <summary>
        /// Starts the dispatcher timer to check for new Kinect frames and update objects in space @60fps
        /// Note: We are using a dispatcher timer to demonstrate usage of the VGB polling APIs,
        /// please see the 'DiscreteGestureBasics-WPF' sample for event notification.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            // set the UI to render at 60fps
            CompositionTarget.Rendering += this.DispatcherTimer_Tick;

            // set the game timer to run at 60fps
            this.dispatcherTimer = new DispatcherTimer();
            this.dispatcherTimer.Tick += this.DispatcherTimer_Tick;
            this.dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60);
            this.dispatcherTimer.Start();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            CompositionTarget.Rendering -= this.DispatcherTimer_Tick;

            if (this.dispatcherTimer != null)
            {
                this.dispatcherTimer.Stop();
                this.dispatcherTimer.Tick -= this.DispatcherTimer_Tick;
            }

            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.gestureDetector != null)
            {
                // The GestureDetector contains disposable members (VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader)
                this.gestureDetector.Dispose();
                this.gestureDetector = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Gets the first body in the bodies array that is currently tracked by the Kinect sensor
        /// </summary>
        /// <returns>Index of first tracked body, or -1 if no body is tracked</returns>
        private int GetActiveBodyIndex()
        {
            int activeBodyIndex = -1;
            int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;

            for (int i = 0; i < maxBodies; ++i)
            {
                // find the first tracked body and verify it has hands tracking enabled (by default, Kinect will only track handstate for 2 people)
                if (this.bodies[i].IsTracked && (this.bodies[i].HandRightState != HandState.NotTracked || this.bodies[i].HandLeftState != HandState.NotTracked))
                {
                    activeBodyIndex = i;
                    break;
                }
            }

            return activeBodyIndex;
        }

        /// <summary>
        /// Retrieves the latest body frame data from the sensor and updates the associated gesture detector object
        /// </summary>
        private void UpdateKinectFrameData()
        {
            bool dataReceived = false;

            using (var bodyFrame = this.bodyFrameReader.AcquireLatestFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    if (!this.bodies[this.activeBodyIndex].IsTracked)
                    {
                        // we lost tracking of the active body, so update to the first tracked body in the array
                        int bodyIndex = this.GetActiveBodyIndex();
                        
                        if (bodyIndex > 0)
                        {
                            this.activeBodyIndex = bodyIndex;
                        }
                    }

                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                Body activeBody = this.bodies[this.activeBodyIndex];

                // visualize the new body data
                this.kinectBodyView.UpdateBodyData(activeBody);

                // visualize the new gesture data
                if (activeBody.TrackingId != this.gestureDetector.TrackingId)
                {
                    // if the tracking ID changed, update the detector with the new value
                    this.gestureDetector.TrackingId = activeBody.TrackingId;
                }

                if (this.gestureDetector.TrackingId == 0)
                {
                    // the active body is not tracked, pause the detector and update the UI
                    this.gestureDetector.IsPaused = true;
                    this.gestureDetector.ClosedHandState = false;
                    this.gestureResultView.UpdateGestureResult(false, false, false, false, -1.0f);
                }
                else
                {
                    // the active body is tracked, unpause the detector
                    this.gestureDetector.IsPaused = false;
                    
                    // steering gestures are only valid when the active body's hand state is 'closed'
                    // update the detector with the latest hand state
                    if (activeBody.HandLeftState == HandState.Closed || activeBody.HandRightState == HandState.Closed)
                    {
                        this.gestureDetector.ClosedHandState = true;
                    }
                    else
                    {
                        this.gestureDetector.ClosedHandState = false;
                    }
                    
                    // get the latest gesture frame from the sensor and updates the UI with the results
                    this.gestureDetector.UpdateGestureData();
                }
            }
        }

        /// <summary>
        /// Updates the StatusText with the latest sensor state information
        /// </summary>
        private void UpdateKinectStatusText()
        {
            // reset the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
        }

        /// <summary>
        /// Notifies UI that a property has changed
        /// </summary>
        /// <param name="propertyName">Name of property that has changed</param> 
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
