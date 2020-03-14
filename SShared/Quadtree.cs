
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

        private byte[] ByteForm;


        public int NumberOfChoices;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(this.NumberOfChoices);
            writer.PutBytesWithLength(this.ByteForm, 0, this.ByteForm.Length);
        }

        public void Deserialize(NetDataReader reader)
        {
            this.NumberOfChoices = reader.GetInt();
            this.ByteForm = reader.GetBytesWithLength();
            this.QuadrantList = PathString.ByteArrayToQuadrantList(this.ByteForm, this.NumberOfChoices);

        }

        public PathString()
        {
            this.NumberOfChoices = 0;
            this.QuadrantList = new List<Quadrant>();
        }

        public PathString(List<Quadrant> choiceList)
        {
            this.QuadrantList = choiceList;
            this.NumberOfChoices = QuadrantList.Count();
            this.ByteForm = this.ToByteArray();
        }

        public void AddChoice(Quadrant quadrant)
        {
            this.NumberOfChoices += 1;
            this.QuadrantList.Add(quadrant);
        }

        public byte[] ToByteArray()
        {
            List<Byte> ByteArray = new List<Byte>();
            string bitstring = this.ToString();

            for (int i = 0; i < NumberOfChoices; i += 4)
            {
                string bits = bitstring.Substring(i * 2, 8).PadRight(8, '0');
                ByteArray.Add(Convert.ToByte(bits, 2));
            }

            return ByteArray.ToArray();
        }

        public static List<Quadrant> ByteArrayToQuadrantList(byte[] byteArray, int numChoices)
        {
            List<Quadrant> quadrantList = new List<Quadrant>();
            for (int i = 0; (i * 4) < numChoices; i++)
            {
                string bitstring = Convert.ToString(byteArray[i], 2);
                for (int j = 0; j < 4; j++)
                {
                    string bits = bitstring.Substring(j * 2, 2);
                    switch (bits)
                    {
                        case "00":
                            quadrantList.Add(Quadrant.NW);
                            break;
                        case "01":
                            quadrantList.Add(Quadrant.NE);
                            break;
                        case "10":
                            quadrantList.Add(Quadrant.SW);
                            break;
                        case "11":
                            quadrantList.Add(Quadrant.SE);
                            break;
                        default:
                            Console.WriteLine("Error: Invalid Input in PathString.ByteArrayToQuadrantList");
                            break;
                    }
                }
            }
            return quadrantList;


        }

        public override string ToString()
        {
            string bitstring = "";
            foreach (Quadrant choice in this.QuadrantList)
            {
                if ((int)choice < 2)
                {
                    bitstring += Convert.ToString(0, 2);
                }
                bitstring += Convert.ToString((int)choice, 2);
            }


            return bitstring;
        }


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

        public QuadTreeNode<T> SmallestNodeWhichContainsShip(Spaceship ship)
        {
            QuadTreeNode<T> node = this;
            QuadTreeNode<T> child = null;
            QuadTreeNode<T> validChild = node;
            if (!node.Bounds.ContainsQuad(ship.Bounds))
            {
                return null;
            }

            do
            {
                node = validChild;
                validChild = null;
                for (int j = 0; j < 4; j++)
                {
                    if ((child = node._children[j]) != null && child.Bounds.ContainsQuad(ship.Bounds))
                    {
                        validChild = child;
                    }
                }
            } while (validChild != null);

            return node;

        }



    }
}


