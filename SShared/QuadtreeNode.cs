
using System;
using System.Diagnostics;


namespace SShared
{
    /// <summary>
    /// Quadtree node class
    /// </summary>
    public class QuadtreeNode
    {
        private int MAX_DEPTH = 16;
        public int X { get; set; }
        public int Y { get; set; }
        public int Depth { get; set; }
        public QuadtreeNode Parent { get; set; }
        public QuadtreeNode[,] Children = new QuadtreeNode[2, 2];

        /// <summary>
        /// Get bounds method
        /// </summary>


        /// <summary>
        /// adds a child
        /// </summary>
        public void addChild(int x, int y)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public QuadtreeNode()
        {

        }
    }


}