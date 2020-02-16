
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SShared
{
    /// <summary>
    /// Interface used in quadtree
    /// </summary>
    public interface INeedsRectangle
    {
        Rectangle Rectangle { get; }
    }

    /// <summary>
    /// Quad tree
    /// param T must be rectangle
    /// </summary>
    public class QuadTree<T> where T : INeedsRectangle
    {

        // attributes

        /// <summary>The child nodes</summary>
        private QuadTree<T>[] _children;

        /// <summary>The stored ships</summary>
        private List<T> _ships;

        /// <summary>The bounding box</summary>
        private readonly Rectangle _rect;

        /// <summary>The size of the smallest area</summary>
        private readonly double _maximumDepth;

        // Constructor
        public QuadTree(Rectangle rectangle, int capacity, double maximumDepth)
        {
            _rect = rectangle;
            _maximumDepth = maximumDepth;
            _ships = new List<T>(capacity);
            _children = null;
        }

        // properties



        /// <summary>
        /// gets the bounding area 
        /// </summary>
        public Rectangle Rectangle { get { return _rect; } }



        // methods

        /// <summary>
        /// counts all nodes in this node
        /// </summary>
        public double Count()
        {

            double count = 0;
            if (null != _children)
                foreach (QuadTree<T> child in _children)
                    count += child.Count();
            return (count += _ships.Count);

        }

        /// <summary>
        /// Inserts a ship
        /// returns true if successfully inserted otherwise false
        /// </summary>
        public bool Insert(T ship)
        {
            // ignore objects that do not belong in this quad tree
            if (!_rect.Contains(ship.Rectangle))
                return false;

            // use subdivise method to add to any accepting node
            if (null == _children)
                Subdivide();

            foreach (QuadTree<T> child in _children)
                if (child.Rectangle.Contains(ship.Rectangle))
                {
                    child.Insert(ship);
                    return true;
                }
            // if no subnodes contain the ship or we are at the smallest subnode size then add the ship as node data
            _ships.Add(ship);
            return true;
        }


        /// <summary>
        /// creates 4 children that divides the quad into 4 quads (of equa length)
        /// </summary>
        public void Subdivide()
        {

            // the smallest subnode has one area
            if (!(_maximumDepth > 4 * _rect.Radius * _rect.Radius))
            {

                _children = new QuadTree<T>[4];

                double halfRadius = 0.5f * _rect.Radius;

                _children[0] = new QuadTree<T>(new Rectangle(_rect.CentreX - halfRadius, _rect.CentreY - halfRadius, halfRadius), _ships.Capacity, _maximumDepth);
                _children[1] = new QuadTree<T>(new Rectangle(_rect.CentreX + halfRadius, _rect.CentreY - halfRadius, halfRadius), _ships.Capacity, _maximumDepth);
                _children[2] = new QuadTree<T>(new Rectangle(_rect.CentreX - halfRadius, _rect.CentreY + halfRadius, halfRadius), _ships.Capacity, _maximumDepth);
                _children[3] = new QuadTree<T>(new Rectangle(_rect.CentreX + halfRadius, _rect.CentreY + halfRadius, halfRadius), _ships.Capacity, _maximumDepth);
            }
        }

        /// <summary>
        /// checks range to find any ships that appear in it 
        /// </summary>
        public List<T> CheckRange(Rectangle range)
        {
            List<T> found = new List<T>();

            // abort if the range does not intersect this quad
            if (_rect.Intersects(range))
            {
                // checking at the current quad level
                foreach (T ship in _ships)
                    if (range.Contains(ship.Rectangle))
                        found.Add(ship);

                if (null != _children)
                    foreach (QuadTree<T> child in _children)
                        // add ship from child
                        found.AddRange(child.CheckRange(range));
            }
            return found;
        }
    }
}