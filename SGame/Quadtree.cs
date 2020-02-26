using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SShared;
using Messages = SShared.Messages;

namespace SGame
{
    class ScanShootResults
    {
        public List<Spaceship> Struck = new List<Spaceship>();
        public double AreaGain = 0.0;

        public ScanShootResults Merge(ScanShootResults other)
        {
            if (other != null)
            {
                Struck.AddRange(other.Struck);
                AreaGain += other.AreaGain;
            }
            return this;
        }
    };

    abstract class SGameQuadTreeNode : QuadTreeNode<Spaceship>
    {
        public SGameQuadTreeNode(QuadTreeNode<Spaceship> parent, Quad bounds, uint depth) : base(parent, bounds, depth)
        { }

        /// <summary>
        /// Returns a (area gain for shooter, list of struck ships) pair.
        /// </summary>
        public abstract Task<ScanShootResults> ScanShootRecur(Messages.ScanShoot msg);
    }

    class LocalQuadTreeNode : SGameQuadTreeNode
    {
        public LocalQuadTreeNode(QuadTreeNode<Spaceship> parent, Quad bounds, uint depth) : base(parent, bounds, depth)
        {
            this.ShipsByToken = new Dictionary<string, LocalSpaceship>();
        }

        public Dictionary<string, LocalSpaceship> ShipsByToken { get; private set; }

        public override Task<List<Spaceship>> CheckRangeLocal(Quad range)
        {
            return new Task<List<Spaceship>>(() =>
                ShipsByToken.Values
                    .Where((ship) => ship.Bounds.Intersects(range))
                    .Select(localShip => (Spaceship)localShip)
                    .ToList()
            );
        }

        public override async Task<ScanShootResults> ScanShootRecur(Messages.ScanShoot msg)
        {
            ScanShootResults results = new ScanShootResults();

            // 1) Search ships locally (but only if affected by the scan)
            bool affected = true; // TODO!!
            if (affected)
            {
                Vector2 leftPoint = MathUtils.DirVec(msg.Direction + msg.Width) * msg.Radius;
                Vector2 rightPoint = MathUtils.DirVec(msg.Direction - msg.Width) * msg.Radius;

                Console.WriteLine($"Scanning with radius {msg.Radius}, in triangle <{msg.Origin}, {leftPoint}, {rightPoint}>");

                var iscanned = ShipsByToken.Values.Where((ship) =>
                    MathUtils.CircleTriangleIntersection(ship.Pos, ship.Radius(), msg.Origin, leftPoint, rightPoint)
                    || MathUtils.CircleSegmentIntersection(ship.Pos, ship.Radius(), msg.Origin, msg.Radius, msg.Direction, msg.Width)
                );
                results.Struck.AddRange(iscanned);

                // FIXME: Implement local shooting here; change results.AreaGain accordingly
            }

            // 2) Search all siblings (always)
            for (int i = 0; i < 4; i++)
            {
                var sibling = (SGameQuadTreeNode)Parent.Child((Quadrant)i);
                if (sibling == null || sibling == this) continue;

                var resultsHere = await sibling.ScanShootRecur(msg);
                results.Merge(resultsHere);
            }

            // 3) Search all children (but only if the local node was affected by the scan its children could be)
            if (affected)
            {
                for (int i = 0; i < 4; i++)
                {
                    var child = (SGameQuadTreeNode)Child((Quadrant)i);
                    if (child == null) continue;

                    var resultsHere = await child.ScanShootRecur(msg);
                    results.Merge(resultsHere);
                }
            }

            return results;
        }
    }

    class RemoteQuadTreeNode : SGameQuadTreeNode
    {
        public RemoteQuadTreeNode(SGameQuadTreeNode parent, Quad bounds, uint depth, NetNode bus, LiteNetLib.NetPeer nodePeer)
            : base(parent, bounds, depth)
        {
            this.Bus = bus;
            this.NodePeer = nodePeer;
        }

        /// <summary>
        /// The message bus to the other nodes.
        /// </summary>
        public NetNode Bus { get; set; }

        /// <summary>
        /// The network peer managing this quadtree node.
        /// </summary>
        public LiteNetLib.NetPeer NodePeer { get; set; }

        public override Task<List<Spaceship>> CheckRangeLocal(Quad range)
        {
            // TODO Peer.SendBusMessage(new BusMsgs.CheckRangeLocal(thing))
            // TODO Peer.AwaitBusResponse(MyResp)
            throw new NotImplementedException();
        }

        public override async Task<ScanShootResults> ScanShootRecur(Messages.ScanShoot msg)
        {
            bool affected = true; // TODO!!
            if (affected)
            {
                Bus.SendMessage(msg, NodePeer);

                // TODO: Add a timeout, after which to return null if no response received
                var struck = await new MessageWaiter<Messages.Struck>(Bus, NodePeer, (struck) => struck.Originator == msg.Originator).Wait;

                ScanShootResults results = new ScanShootResults();
                foreach (var struckInfo in struck.ShipsInfo)
                {
                    results.AreaGain += struckInfo.AreaGain;
                    results.Struck.Add(struckInfo.Ship);
                }
                return results;
            }
            else
            {
                return null;
            }
        }
    }
}
