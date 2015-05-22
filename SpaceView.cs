//------------------------------------------------------------------------------
// <copyright file="SpaceView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ContinuousGestureBasics
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Samples.Kinect.ContinuousGestureBasics.Common;

    /// <summary>
    /// Creates and maintains a collection of images (ship, asteroids, and explosion) that can move within the bounds of the space image
    /// </summary>
    public sealed class SpaceView : BindableBase
    {
        /// <summary> Total number of asteroids to draw in space  </summary>
        private const int AsteroidCount = 5;

        /// <summary> Maximum degrees of rotation that an asteroid can exhibit </summary>
        private const int MaxRotation = 360;

        /// <summary> Minimum value to use when determining asteroid starting location with random number generator </summary>
        private const int MinStartPosition = -300;

        /// <summary> Maximum value to use when determining asteroid starting location with random number generator </summary>
        private const int MaxStartPosition = 300;

        /// <summary> Speed of motion for asteroids </summary>
        private const double AsteroidSpeed = 0.005;

        /// <summary> Speed of motion for the ship </summary>
        private const double ShipSpeed = 3.5;

        /// <summary> ImageSource for all asteroid objects </summary>
        private readonly ImageSource asteroidImageSource = new BitmapImage(new Uri(@"Images\asteroid.png", UriKind.Relative));

        /// <summary> ImageSource for the ship object </summary>
        private readonly ImageSource shipImageSource = new BitmapImage(new Uri(@"Images\spaceship.png", UriKind.Relative));

        /// <summary> ImageSource for the explosion object </summary>
        private readonly ImageSource explosionImageSource = new BitmapImage(new Uri(@"Images\explosion.png", UriKind.Relative));

        /// <summary> Number of times the ship has collided with an asteroid </summary>
        private int collisionCount = 0;

        /// <summary> Space image, which determines the bounds that the ship can move in on screen </summary>
        private Image space = null;

        /// <summary> Collection of asteroid objects for the ship to avoid </summary>
        private MovingSpaceImage[] asteroids = null;

        /// <summary> Space ship, which is controlled by the user (gesture results determine rotation and translation) </summary>
        private MovingSpaceImage ship = null;

        /// <summary> Explosion, which will appear on screen when the ship collides with an asteroid </summary>
        private MovingSpaceImage explosion = null;

        /// <summary> Random number generator, which is used to set asteroid rotation and translation </summary>
        private Random rand = null;

        /// <summary> Stopwatch for tracking time between collisions </summary>
        private Stopwatch collisionSpanTimer = null;

        /// <summary> Stopwatch for controlling explosion duration</summary>
        private Stopwatch explosionTimer = null;

        /// <summary> Total length of time that an explosion should be rendered on screen </summary>
        private TimeSpan explosionDuration = TimeSpan.FromSeconds(0.5);

        /// <summary> Time since last collision </summary>
        private TimeSpan timeSinceCollision = TimeSpan.FromSeconds(0);

        /// <summary>
        /// Initializes a new instance of the SpaceView class and sets initial property values
        /// </summary>
        /// <param name="spaceGrid">Grid control in the UI that will hold our moving space images </param>
        /// <param name="spaceImage">Space Image control within the grid, determines translation bounds for the moving space images</param>
        public SpaceView(Grid spaceGrid, Image spaceImage)
        {
            if (spaceGrid == null)
            {
                throw new ArgumentNullException("spaceGrid");
            }

            if (spaceImage == null)
            {
                throw new ArgumentNullException("spaceImage");
            }

            this.space = spaceImage;
            this.rand = new Random();
            this.ExplosionInProgress = false;

            // create our asteroid objects
            this.asteroids = new MovingSpaceImage[AsteroidCount];
            for (int i = 0; i < AsteroidCount; ++i)
            {
                this.asteroids[i] = new MovingSpaceImage(this.asteroidImageSource, 20, 20, AsteroidSpeed);
                this.asteroids[i].Rotation.Angle = this.rand.NextDouble() * MaxRotation;
                this.asteroids[i].Translation.X = this.rand.Next(MinStartPosition, MaxStartPosition);
                this.asteroids[i].Translation.Y = this.rand.Next(MinStartPosition, MaxStartPosition);

                spaceGrid.Children.Add(this.asteroids[i].Image);
            }

            // create our ship object
            this.ship = new MovingSpaceImage(this.shipImageSource, 60, 76, ShipSpeed);
            spaceGrid.Children.Add(this.ship.Image);

            // create our explosion object
            this.explosion = new MovingSpaceImage(this.explosionImageSource, 20, 20, 0);
            spaceGrid.Children.Add(this.explosion.Image);
            this.explosion.Image.Visibility = Visibility.Hidden;
            
            // create our stopwatch timers
            this.explosionTimer = new Stopwatch();            
            this.collisionSpanTimer = new Stopwatch();
        }

        /// <summary> 
        /// Gets or sets a value indicating whether an explosion is in progress.
        /// If so, we need to wait for rendering to complete before resetting the moving space objects 
        /// </summary>
        public bool ExplosionInProgress { get; set; }

        /// <summary> 
        /// Gets a value which indicates the amount of time that has passed since the ship collided with an asteroid
        /// </summary>
        public TimeSpan TimeSinceCollision
        {
            get
            {
                return this.timeSinceCollision;
            }

            private set
            {
                this.SetProperty(ref this.timeSinceCollision, value);
            }
        }

        /// <summary> 
        /// Gets a value which indicates the number of times that the ship has collided with an asteroid
        /// </summary>
        public int CollisionCount
        {
            get
            {
                return this.collisionCount;
            }

            private set
            {
                this.SetProperty(ref this.collisionCount, value);
            }
        }
        
        /// <summary>
        /// Checks for collisions between the ship and asteroids
        /// If a collision is detected, creates an explosion and resets the space objects when the explosion is complete
        /// </summary>
        public void CheckForCollision()
        {
            if (!this.ExplosionInProgress)
            {
                foreach (MovingSpaceImage asteroid in this.asteroids)
                {
                    var center = new Point(asteroid.Image.ActualWidth / 2, asteroid.Image.ActualHeight / 2);
                    var impactPoint = asteroid.Image.TranslatePoint(center, this.ship.Image);
                    HitTestResult result = VisualTreeHelper.HitTest(this.ship.Image, impactPoint);
                    if (result != null)
                    {
                        // collision detected, time to create an explosion!
                        this.CollisionCount++;
                        this.collisionSpanTimer.Stop();

                        // set location on screen where the explosion should be rendered
                        this.explosion.Translation.X = this.ship.Translation.X;
                        this.explosion.Translation.Y = this.ship.Translation.Y;

                        // show the explosion image, hide the ship and asteroid images
                        this.explosion.Image.Visibility = Visibility.Visible;
                        this.ship.Image.Visibility = Visibility.Hidden;
                        asteroid.Image.Visibility = Visibility.Hidden;

                        // signal that an explosion is in progress
                        this.ExplosionInProgress = true;
                        this.explosionTimer.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Updates position of asteroids as they float in space
        /// </summary>
        public void UpdateAsteroids()
        {
            foreach (MovingSpaceImage asteroid in this.asteroids)
            {
                asteroid.UpdatePosition(this.space, true);
            }
        }

        /// <summary>
        /// Updates scale/rotation of the explosion image when an explosion is in progress
        /// Stops the explosion if it has reached the expected duration
        /// </summary>
        public void UpdateExplosion()
        {
            if (this.explosionTimer.Elapsed < this.explosionDuration)
            {
                // make the explosion image expand and spin                
                this.explosion.Scale.CenterX = this.explosion.Translation.X;
                this.explosion.Scale.CenterY = this.explosion.Translation.Y;
                this.explosion.Scale.ScaleX += 0.0005;
                this.explosion.Scale.ScaleY += 0.0005;
                this.explosion.Rotation.Angle += 15;
            }
            else
            {
                // signal that the explosion is complete
                this.ExplosionInProgress = false;
                this.explosionTimer.Reset();
                this.collisionSpanTimer.Reset();
                
                // reset space objects to starting position when explosion is over
                this.ResetMovingSpaceImages();
            }
        }

        /// <summary>
        /// Uses the continuous gesture 'SteerProgress' result to rotate and translate the ship in the UI
        /// </summary>
        /// <param name="keepStraight"> True, if the ship should move forward without rotation; false otherwise</param>
        /// <param name="progress"> Continuous gesture progress value which indicates how far the wheel should be turned left or right </param>
        public void UpdateShipPosition(bool keepStraight, float progress)
        {
            // the user is turning the wheel, apply rotation to the ship image
            if (!keepStraight)
            {
                double angle = 0;

                // turn left
                if (progress >= 0 && progress < 0.5)
                {
                    angle = (0.5 - progress) * 10;
                    this.ship.Rotation.Angle -= angle;
                }

                // turn right
                if (progress > 0.5 && progress <= 1)
                {
                    angle = (progress - 0.5) * 10;
                    this.ship.Rotation.Angle += angle;
                }
            }

            // the user is holding the wheel, calculate a new position for the ship to move to
            if (progress >= 0)
            {
                this.ship.UpdatePosition(this.space, false);
            }
        }

        /// <summary>
        /// Updates the time since last explosion for display in UI
        /// </summary>
        /// <param name="pauseTimer">True, if there are no bodies tracked and the timer should be paused</param>
        public void UpdateTimeSinceCollision(bool pauseTimer)
        {
            this.TimeSinceCollision = this.collisionSpanTimer.Elapsed;

            if (this.collisionSpanTimer.IsRunning && pauseTimer)
            {
                this.collisionSpanTimer.Stop();
            }
            else if (!this.collisionSpanTimer.IsRunning && !pauseTimer)
            {
                this.TimeSinceCollision = TimeSpan.FromSeconds(0);
                this.collisionSpanTimer.Start();
            }
        }

        /// <summary>
        /// Resets ship, asteroids, and explosion to new starting positions
        /// </summary>
        private void ResetMovingSpaceImages()
        {
            // hide the explosion and reset to center
            this.explosion.Image.Visibility = Visibility.Hidden;
            this.explosion.Scale.ScaleX = 1;
            this.explosion.Scale.ScaleY = 1;

            // reset ship to center of space
            this.ship.Translation.X = 0;
            this.ship.Translation.Y = 0;
            this.ship.Rotation.Angle = 0;
            this.ship.Image.Visibility = Visibility.Visible;

            // reset each asteroid to random position/rotation in space
            foreach (MovingSpaceImage asteroid in this.asteroids)
            {
                asteroid.Rotation.Angle = this.rand.NextDouble() * MaxRotation;
                asteroid.Translation.X = this.rand.Next(MinStartPosition, MaxStartPosition);
                asteroid.Translation.Y = this.rand.Next(MinStartPosition, MaxStartPosition);
                asteroid.Image.Visibility = Visibility.Visible;
            }
        }
    }
}
