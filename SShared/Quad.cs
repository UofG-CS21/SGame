using System;
using System.Diagnostics;

namespace SShared
{
    /// <summary>
    /// A quadrant in a quadtree node.
    /// </summary>
    public enum Quadrant : int
    {
        NW = 0,
        NE = 1,
        SW = 2,
        SE = 3,
    }

    /// <summary>
    /// World space quad.
    /// </summary>
    public struct Quad
    {

        public Quad(double centrex, double centrey, double radius)
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
        public double CentreX { get; private set; }


        /// <summary>
        /// centre of rectangle on y-axis.
        /// </summary>
        public double CentreY { get; private set; }


        /// <summary>
        /// radius of rectangle
        /// </summary>
        public double Radius { get; private set; }

        /// <summary>
        /// left corner x coord
        /// </summary>
        public double X { get; private set; }

        /// <summary>
        /// right corner x coord   
        /// </summary>
        public double X2 { get; private set; }

        /// <summary>
        /// top corner y coord
        /// </summary>
        public double Y { get; private set; }

        /// <summary>
        /// bottom corner y coord
        /// </summary>
        public double Y2 { get; private set; }


        /// methods

        /// <summary>
        /// Checks the rectangle contains another rectangle
        /// returns true if it does other wise false
        /// </summary>
        public bool Contains(Quad other)
        {
            return other.X >= X && other.Y >= Y &&
                    other.X2 <= X2 && other.Y2 <= Y2;
        }

        /// <summary>
        /// Checks if the rectangle interesects with an other rectangle
        /// returns true if it does other wise false
        /// </summary>
        public bool Intersects(Quad other)
        {
            return !(other.X >= X2 || other.X2 <= X || other.Y >= Y2 || other.Y2 <= Y);
        }

        /// <summary>
        /// Return the bounds of a certain quadrant of this quad.
        /// </summary>
        public Quad QuadrantBounds(Quadrant quadrant)
        {
            double radius = Radius * 0.5, halfRadius = radius * 0.5;
            switch (quadrant)
            {
                case Quadrant.NW:
                    return new Quad(CentreX - halfRadius, CentreY + halfRadius, radius);
                case Quadrant.NE:
                    return new Quad(CentreX + halfRadius, CentreY + halfRadius, radius);
                case Quadrant.SW:
                    return new Quad(CentreX - halfRadius, CentreY - halfRadius, radius);
                default: // Quadrant.SE
                    return new Quad(CentreX + halfRadius, CentreY - halfRadius, radius);
            }
        }
    }
}
