
using System;
using System.Diagnostics;


namespace SShared
{
    /// <summary>
    /// World space rectangle
    /// </summary>
    public struct Rectangle
    {

        public Rectangle(float centrex, float centrey, float radius)
        {
            CentreX = centrex;
            CentreY = centrey;
            Radius = radius;

            X = centrex - radius;
            Y = centrey - radius;
            X2 = centrex + radius;
            Y2 = centrey + radius;
        }

        /// Properties

        /// <summary>
        /// centre of rectangle on x-axis.
        /// </summary>
        public float CentreX { get; private set; }


        /// <summary>
        /// centre of rectangle on y-axis.
        /// </summary>
        public float CentreY { get; private set; }


        /// <summary>
        /// radius of rectangle
        /// </summary>
        public float Radius { get; private set; }

        /// <summary>
        /// left corner x coord
        /// </summary>
        public float X { get; private set; }

        /// <summary>
        /// right corner x coord   
        /// </summary>
        public float X2 { get; private set; }

        /// <summary>
        /// top corner y coord
        /// </summary>
        public float Y { get; private set; }

        /// <summary>
        /// bottom corner y coord
        /// </summary>
        public float Y2 { get; private set; }


        /// methods

        /// <summary>
        /// Checks the rectangle contains another rectangle
        /// returns true if it does other wise false
        /// </summary>
        public bool Contains(Rectangle otherrect)
        {
            return otherrect.X >= X && otherrect.Y >= Y &&
                    otherrect.X2 <= X2 && otherrect.Y2 <= Y2;
        }


        /// <summary>
        /// Checks if the rectangle interesects with an other rectangle
        /// returns true if it does other wise false
        /// </summary>
        public bool Intersects(Rectangle otherrect)
        {
            return !(otherrect.X >= X2 || otherrect.X2 <= X || otherrect.Y >= Y2 || otherrect.Y2 <= Y);
        }
    }
}