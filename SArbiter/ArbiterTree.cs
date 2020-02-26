using System;
using SShared;
using LiteNetLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SArbiter
{
    class ArbiterTreeNode
    {
        public ArbiterTreeNode Parent { get; private set; }

        public uint Depth { get; private set; }

        public Quad Bounds { get; private set; }

        public NetPeer SGamePeer { get; private set; }

        public ArbiterTreeNode[] Children = new ArbiterTreeNode[4] { null, null, null, null };

        public ArbiterTreeNode()
        {
            Parent = null;
            Bounds = new Quad(0.0, 0.0, Double.MaxValue);
            Depth = 0;
        }

        public ArbiterTreeNode(ArbiterTreeNode parent, Quadrant quadrant)
        {
            Parent = parent;
            parent.Children[(int)quadrant] = this;
            Bounds = parent.Bounds.QuadrantBounds(quadrant);
            Depth = parent.Depth + 1;
        }

        public ArbiterTreeNode RandomLeafNode()
        {
            Random rand = new Random();

            ArbiterTreeNode node = this;
            ArbiterTreeNode child = null;
            do
            {
                int randQuadrant = rand.Next(0, 4);
                for (int j = 0; j < 4; j++)
                {
                    if ((child = node.Children[(randQuadrant + j) % 4]) != null)
                    {
                        break;
                    }
                }
                node = child ?? node;
            } while (child != null);

            return node;
        }
    }
}
