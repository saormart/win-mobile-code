using System;
#region CN50 XYZ vectors
/*
              (N)
              +Y
               |           -Z
               |          /
               |         /
               |       .'
         +-----------+/
         | +-------+ |
         | |       | |
         | |       | |
         | |       | |
(E)''''''| |  /    | |''''''  (W)
+X       | |.'     | |     -X
         | /-------+ |
         |/          |
        .'           |
       / +-----------+
     .'        |
    /          |
  +Z           |
              -Y
              (S)

    //calc the acceleration or the longest vector
    double accel = Math.Sqrt(x * x + y * y + z * z);
    //calc the X angle, should be about 180 if device is upright
    double degrees = Math.Acos(x / b) * 360.0 / Math.PI;

 * when device is faceup on table:          x=0     y=0     z=1
 * when device is faceup on table:          x=0     y=0     z=-1
 * when device is upright scan to roof:     x=0     y=-1    z=0
 * when device is upright scanner to down:  x=0     y=1     z=0
 * when scanner lays on left side:          x=-1    y=0     z=0
 * when scanner lays on right side:         x=1     y=0     z=0
*/

#endregion
namespace Movedetection
{
        public struct GMVector
        {
            public GMVector(double x, double y, double z)
            {
                myX = x;
                myY = y;
                myZ = z;
#if MAGNETIC_SENSOR
                myHeading = 0;
                myStrength = 0;
#endif
                myTicks = (ulong)(DateTime.Now.Ticks / 10000); //there are 10000 ticks in a millisecond
                //http://msdn.microsoft.com/en-us/library/system.datetime.ticks.aspx
                
                myDirection = GetDirection(myX, myY, myZ);
                myScreenOrientation = GetScreenorientation(myX, myY, myZ);
                moveState = MoveState.idle;
            }
            //private static GMVector lastGMVector=new GMVector();
            public enum MoveState : int
            {
                idle=0,
                move=1
            }
            MoveState moveState;
            public MoveState _moveState
            {
                get { return moveState; }
                set { moveState = value; }
            }

            ScreenOrientation myScreenOrientation;
            public ScreenOrientation _ScreenOrientation{
                get { return myScreenOrientation; }
            }

            Direction myDirection;
            public Direction _Direction
            {
                get { return myDirection; }
            }

            ulong myTicks;
            /// <summary>
            /// changed to have milliseconds instead of 100ns
            /// </summary>
            public ulong Ticks
            {
                get { return myTicks; }
            }
            double myX;
            public double X
            {
                get { return myX; }
                //set { myX = value; }
            }
            double myY;
            public double Y
            {
                get { return myY; }
                //set { myY = value; }
            }
            double myZ;
            public double Z
            {
                get { return myZ; }
                //set { myZ = value; }
            }

            #region Magnetic Sensor
#if MAGNETIC_SENSOR
            double myHeading;
            /// <summary>
            /// Compass heading in degrees, for use with magnetic sensors
            /// </summary>
            public double Heading
            {
                get { return myHeading; }
                set { myHeading = value; }
            }
            double myStrength;
            /// <summary>
            /// the magnetic strength, for use with magnetic sensors
            /// </summary>
            public double Strength
            {
                get { return myStrength; }
                set { myStrength = value; }
            }
#endif
            #endregion
            public GMVector Normalize()
            {
                return Scale(1 / Length);
            }

            public GMVector Scale(double scale)
            {
                GMVector ret = this;
                ret.myX *= scale;
                ret.myY *= scale;
                ret.myZ *= scale;
                return ret;
            }
			/// <summary>
			/// Convert a radian to degree 
			/// </summary>
			/// <param name="d">
			/// </param>
			/// <returns>
			/// </returns>
			private double RadianToDegree(double d){
				double dRes;
				dRes= d*180/Math.PI;
				return dRes;
			}
            /*
                pitch = arctan(Ax/sqrt(Ay^2+Az^2))
                roll = arctan(Ay/sqrt(Ax^2+Az^2))
                theta = arctan(sqrt(Ax^2+Az^2)/Az)
             * //http://cache.freescale.com/files/sensors/doc/app_note/AN3461.pdf
             * Pitch (phi) is defined as the angle of the X-axis relative to ground. 
             * pitch = arctan(x/sqrt(y*y+z*z))
             * Roll (rho) is defined as the angle of the Y-axis relative to the ground.
             * roll = arctan(Y/sqrt(x*x+z*z))
             * Tilt (theta) is the angle of the Z axis relative to gravity.
             * theta = sqrt(x*x + y*y)/z
            */
            public double Pitch{
				get{
					GMVector gv1=this;
                    double p = Math.Atan(gv1.X / Math.Sqrt(Math.Pow(gv1.Y,2) / Math.Pow(gv1.Z,2)));
					return this.RadianToDegree(p);
				}
			}
			public double Roll{
				get{
					GMVector gv1=this;
                    //double p = Math.Atan(gv1.Z / gv1.X);
					double p=Math.Atan(gv1.Y / Math.Sqrt(Math.Pow(gv1.X,2) + Math.Pow(gv1.Z,2)));
					return this.RadianToDegree(p);
				}
			}
			public double Tilt{	//was called theta
				get{
                    GMVector gv1 = this;
                    double p = Math.Sqrt(Math.Pow(gv1.X, 2) + Math.Pow(gv1.Y, 2)) / gv1.Z;
                    return this.RadianToDegree(p);
                }
			}
		#region segmented direction
            /*
			http://en.wikipedia.org/wiki/Boxing_the_compass
            attention: this here is based on +Y/+X (-1/0) equal 0 degree equal North
            */
			public enum Direction:int{
                None=-1,
			W=0,
				WSW,
				SW,
				SSW,
			S,
				SSE,
				SE,
				ESE,
			E,
				ENE,
				NE,
				NNE,
			N,
				NNW,
				NW,
				WNW,
			}
            public Direction direction
            { //http://stackoverflow.com/questions/1437790/how-to-snap-a-directional-2d-vector-to-a-compass-n-ne-e-se-s-sw-w-nw
				get{
					GMVector gv=this;
					int segmentCount=16;
					int compassSegment = (((int) Math.Round(Math.Atan2(gv.Y, gv.X) / (2 * Math.PI / segmentCount))) + segmentCount) % segmentCount;
                    return (Direction)compassSegment;
				}
			}
            private static Direction GetDirection(double x, double y, double z)
            { //http://stackoverflow.com/questions/1437790/how-to-snap-a-directional-2d-vector-to-a-compass-n-ne-e-se-s-sw-w-nw
					int segmentCount=16;
					int compassSegment = (((int) Math.Round(Math.Atan2(y, x) / (2 * Math.PI / segmentCount))) + segmentCount) % segmentCount;
                    return (Direction)compassSegment;
			}

		#endregion
			/// <summary>
            /// the X angle of the vector
            /// </summary>
            public int angleX
            {
                get
                {
                    return (int)(Math.Acos(myX / Length) * 360.0 / Math.PI);
                }
            }
            /// <summary>
            /// the Y angle of the vector
            /// </summary>
            public int angleY
            {
                get
                {
                    return (int)(Math.Acos(myY / Length) * 360.0 / Math.PI);
                }
            }
            /// <summary>
            /// the Z angle of the vector
            /// </summary>
            public int angleZ
            {
                get
                {
                    return (int)(Math.Acos(myZ / Length) * 360.0 / Math.PI);
                }
            }

            /// <summary>
            /// although this is called length it is the max. acceleration force
            /// </summary>
            public double Length
            {
                get
                {
                    return Math.Sqrt(myX * myX + myY * myY + myZ * myZ);
                }
            }
            
            /// <summary>
            /// calc the distance between the end of two vectors
            /// </summary>
            /// <param name="gvOld">the vector to compare to</param>
            /// <returns></returns>
            public double EuclideanDistance(GMVector gvOld)
            {
                return Math.Sqrt(Math.Pow(this.X - gvOld.X, 2) + Math.Pow(this.Y - gvOld.Y, 2) + Math.Pow(this.Z - gvOld.Z, 2));
            }
            public ulong TicksDiff(GMVector gvNew)
            {
                GMVector gvOld=this;
                ulong diff = gvNew.Ticks - gvOld.Ticks;
                //one tick is 100 nano seconds, scaled with 10000 to milliseconds
                return diff;
            }
            public override string ToString()
            {
                return string.Format("X={0} Y={1} Z={2} Accel={3} angleX={4} angleX={5} angleX={6} orientation={7} direction={8} ticks={9} moving={10}", 
                    Math.Round(myX, 4), Math.Round(myY, 4), Math.Round(myZ, 4),
                    Length,
                    angleX, angleY,angleZ,
                    GetScreenorientation(myX, myY, myZ),
                    GetDirection(myX, myY, myZ),
                    Ticks,
                    moveState);
            }

            //changed to match CN50 XYZ directions
            private static ScreenOrientation GetScreenorientation(double X, double Y, double Z)
            {
                if (Math.Abs(X) > Math.Abs(Y))
                {
                    if (Math.Abs(X) > Math.Abs(Z))
                    {
                        if (X > 0)
                            return ScreenOrientation.ReverseLandscape;     //changed from Landscape
                        return ScreenOrientation.Landscape;                //changed from ReverseLandscape
                    }
                }
                else if (Math.Abs(Y) > Math.Abs(Z))
                {
                    if (Y > 0)
                        return ScreenOrientation.ReversePortrait;       //changed from Portrait
                    return ScreenOrientation.Portrait;                  // changed from ReversePortrait
                }

                if (Z > 0)
                    return ScreenOrientation.FaceUp;    //this is different to HTC
                return ScreenOrientation.FaceDown;      //this is different to HTC
            }
        }
}

