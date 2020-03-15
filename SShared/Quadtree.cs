
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace SShared
{
    /// <summary>
    /// PathString holds a list of quandrants representing choices when traversing a quadtree.
    /// It can convert this list to a bit string representation where every 2 bits represent a Quadrant.
    /// This bitstring representation can then be converted to a byte array representation to allow effiecient transfer of the data through serialization.
    /// PathString can be initilaized as empty or it can be given a List<Quadrant>.
    /// </summary>
    public class PathString : INetSerializable
    {
        public List<Quadrant> QuadrantList = new List<Quadrant>();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(QuadrantList.Count);
            writer.Put(this.ToByteArray());
        }

        public void Deserialize(NetDataReader reader)
        {
            int numQuadrants = reader.GetInt();
            byte[] bytes = new byte[ByteLength(numQuadrants)];
            reader.GetBytes(bytes, bytes.Length);
            this.QuadrantList = ByteArrayToQuadrantList(bytes, numQuadrants);
        }

        public PathString()
        {
            this.QuadrantList = new List<Quadrant>();
        }

        public PathString(List<Quadrant> choiceList)
        {
            this.QuadrantList = choiceList;
        }

        public static int ByteLength(int numQuadrants)
        {
            return (numQuadrants + 3) / 4;
        }

        public byte[] ToByteArray()
        {
            byte[] byteArray = new byte[ByteLength(QuadrantList.Count)];

            int iBit = 0;
            for (int i = 0; i < QuadrantList.Count; i++)
            {
                int mask = ((int)QuadrantList[i]) << iBit;
                byteArray[i / 4] |= (byte)mask;
                iBit += 2;
                if (iBit == 8) iBit = 0;
            }
            return byteArray;
        }

        public static List<Quadrant> ByteArrayToQuadrantList(byte[] byteArray, int numChoices)
        {
            List<Quadrant> list = new List<Quadrant>();
            int iBit = 0;
            for (int i = 0; i < numChoices; i++)
            {
                int quadrant = (byteArray[i / 4] >> iBit) & 0b11;
                list.Add((Quadrant)quadrant);
                iBit += 2;
                if (iBit == 8) iBit = 0;
            }
            return list;
        }

        public override string ToString() => string.Join(", ", this.QuadrantList);
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
        /// Enum representing which of the possible four child quadrants this node manages
        /// </summary>
        public Quadrant Quadrant { get; private set; }

        /// <summary>
        /// The depth of this quadtree node (root has Depth=0).
        /// </summary>
        public uint Depth { get; private set; }

        private Quad _bounds;

        /// <summary>
        /// The bounds of this quadtree node.
        /// </summary>
        public Quad Bounds { get { return _bounds; } }

        public QuadTreeNode(QuadTreeNode<T> parent, Quadrant quadrant, uint depth)
        {
            this.Parent = parent;
            this._bounds = parent.Bounds.QuadrantBounds(quadrant);
            this.Quadrant = quadrant;
            this.Depth = depth;
            this._children = new QuadTreeNode<T>[4] { null, null, null, null };
        }

        public QuadTreeNode(Quad bounds, uint depth)
        {
            this.Parent = null;
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
                child._bounds = Bounds.QuadrantBounds(pos);
                child.Parent = this;
                child.Quadrant = pos;
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

        /// <summary>
        /// Get a randomly-chosen leaf of this node.
        /// </summary>
        public QuadTreeNode<T> RandomLeafNode()
        {
            Random rand = new Random();

            QuadTreeNode<T> node = this;
            QuadTreeNode<T> child = null;
            do
            {
                int randQuadrant = rand.Next(0, 4);
                for (int j = 0; j < 4; j++)
                {
                    if ((child = node._children[(randQuadrant + j) % 4]) != null)
                    {
                        break;
                    }
                }
                node = child ?? node;
            } while (child != null);

            return node;
        }

        /// <summary>
        /// Returns the smallest child capable of containing `bounds`.  
        /// </summary>
        public QuadTreeNode<T> SmallestNodeWhichContains(Quad bounds)
        {
            QuadTreeNode<T> node = this;
            QuadTreeNode<T> child = null;
            QuadTreeNode<T> validChild = node;
            if (!node.Bounds.ContainsQuad(bounds))
            {
                return null;
            }

            do
            {
                node = validChild;
                validChild = null;
                for (int j = 0; j < 4; j++)
                {
                    if ((child = node._children[j]) != null && child.Bounds.ContainsQuad(bounds))
                    {
                        validChild = child;
                    }
                }
            } while (validChild != null);

            return node;
        }

        /// <summary>
        /// Returns the PathString from the root to this node.
        /// </summary>
        public PathString Path()
        {
            QuadTreeNode<T> node = this;
            List<Quadrant> path = new List<Quadrant>();
            while (node.Parent != null)
            {
                path.Add(node.Quadrant);
                node = node.Parent;
            }
            path.Reverse();
            return new PathString(path);
        }

        /// <summary>
        /// Visit all nodes recursively.
        /// </summary>
        public IEnumerable<QuadTreeNode<T>> Traverse()
        {
            Stack<QuadTreeNode<T>> stack = new Stack<QuadTreeNode<T>>();
            stack.Push(this);
            while (stack.Any())
            {
                var node = stack.Pop();
                yield return node;

                foreach (var child in node._children)
                {
                    if (child != null) stack.Push(child);
                }
            }
        }

        /// <summary>
        /// Set the given child to null if it is present.
        /// </summary>
        public bool EraseChild(QuadTreeNode<T> child)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_children[i] == child)
                {
                    _children[i] = null;
                    return true;
                }
            }
            return false;
        }
    }
}


