
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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
    /// Something bounded by a worldspace quad.
    /// </summary>
    public interface IQuadBounded
    {
        /// <summary>
        /// The world-space bounding box of this item.
        /// </summary>
        public Quad Bounds { get; }
    }

    /// <summary>
    /// The base class of all quadtree nodes.
    /// The values stored in the nodes will be of type `T`.
    /// </summary>
    public abstract class QuadTreeNode<T> : IQuadBounded where T : IQuadBounded
    {
        /// <summary>
        /// The 4 children of this node (each might be null).
        /// </summary>
        private QuadTreeNode<T>[] _children;

        /// <summary>
        /// The maximum number of quadtree subdivisions (= the maximum depth of a leaf node).
        /// </summary>
        public const uint MaxDepth = 15;

        /// <summary>
        /// The parent node of this quadtree node (root has parent=null)
        /// </summary>
        public QuadTreeNode<T> Parent { get; private set; }

        /// <summary>
        /// The depth of this quadtree node (root has Depth=0).
        /// </summary>
        public uint Depth { get; private set; }

        private Quad _bounds;

        /// <summary>
        /// The bounds of this quadtree node.
        /// </summary>
        public Quad Bounds { get { return _bounds; } }

        public QuadTreeNode(QuadTreeNode<T> parent, Quad bounds, uint depth)
        {
            this.Parent = parent;
            this._bounds = bounds;
            this.Depth = depth;
            this._children = new QuadTreeNode<T>[4] { null, null, null, null };
        }

        /// <summary>
        /// Returns the child of this node at the given position (or null if not present).
        /// </summary>
        public QuadTreeNode<T> Child(Quadrant pos)
        {
            return _children[(int)pos];
        }

        /// <summary>
        /// Sets the child of this node at the given position (replacing any previously-present one).
        /// (Automatically modifies the child's bounds, depth and parent as needed).
        /// Throws if the maximum subdivision depth was reached
        /// </summary>
        public void SetChild(Quadrant pos, QuadTreeNode<T> child)
        {
            if (Depth == MaxDepth)
            {
                throw new InvalidOperationException("Maximum quadtree depth reached");
            }

            if (child != null)
            {
                double halfRadius = Bounds.Radius * 0.5;
                switch (pos)
                {
                    case Quadrant.NW:
                        child._bounds = new Quad(Bounds.CentreX - halfRadius, Bounds.CentreY + halfRadius, halfRadius);
                        break;
                    case Quadrant.NE:
                        child._bounds = new Quad(Bounds.CentreX + halfRadius, Bounds.CentreY + halfRadius, halfRadius);
                        break;
                    case Quadrant.SW:
                        child._bounds = new Quad(Bounds.CentreX - halfRadius, Bounds.CentreY - halfRadius, halfRadius);
                        break;
                    case Quadrant.SE:
                        child._bounds = new Quad(Bounds.CentreX + halfRadius, Bounds.CentreY - halfRadius, halfRadius);
                        break;
                }
                child.Parent = this;
                child.Depth = Depth + 1;
            }
            _children[(int)pos] = child;
        }

        /// <summary>
        /// Recursively count all nodes that are children of this node.
        /// </summary>
        public uint ChildCountRecur()
        {
            uint count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (_children[i] != null)
                {
                    count += _children[i].ChildCountRecur();
                }
            }
            return count;
        }

        /// <summary>
        /// Checks a range in this quad (and NOT its children) for items intersecting it.
        /// </summary>
        public abstract Task<List<T>> CheckRangeLocal(Quad range);

        /// <summary>
        /// Checks a range in this quad (and all of its children) for items intersecting it.
        /// </summary>
        public async Task<List<T>> CheckRangeRecur(Quad range)
        {
            List<T> found = new List<T>();

            // abort if the range does not intersect this quad
            if (_bounds.Intersects(range))
            {
                // checking at the current quad level
                found.AddRange(await CheckRangeLocal(range).ConfigureAwait(false));

                // checking recursively all children
                foreach (var child in _children)
                {
                    if (child != null)
                    {
                        found.AddRange(await child.CheckRangeRecur(range).ConfigureAwait(false));
                    }
                }
            }

            return found;
        }
    }
}
