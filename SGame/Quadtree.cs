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
        public List<Messages.Struck.ShipInfo> Struck = new List<Messages.Struck.ShipInfo>();
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
    }

    abstract class SGameQuadTreeNode : QuadTreeNode<Spaceship>
    {
        public SGameQuadTreeNode(QuadTreeNode<Spaceship> parent, Quadrant quadrant, uint depth) : base(parent, quadrant, depth) { }

        public SGameQuadTreeNode(Quad bounds, uint depth = 0) : base(bounds, depth) { }

        /// <summary>
        /// Returns a (area gain for shooter, list of struck ships) pair.
        /// </summary>
        public abstract ScanShootResults ScanShootLocal(Messages.ScanShoot msg);
    }

    class LocalQuadTreeNode : SGameQuadTreeNode
    {
        public LocalQuadTreeNode(QuadTreeNode<Spaceship> parent, Quadrant quadrant, uint depth) : base(parent, quadrant, depth)
        {
            this.ShipsByToken = new Dictionary<string, LocalSpaceship>();
        }

        public LocalQuadTreeNode(Quad bounds, uint depth) : base(bounds, depth)
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

        /// <summary>
        /// Minimum ship area, below which it is considered dead.
        /// </summary>
        private const double MINIMUM_AREA = 0.75;

        public override ScanShootResults ScanShootLocal(Messages.ScanShoot msg)
        {
            ScanShootResults results = new ScanShootResults();

            // 1) Search ships locally (but only if affected by the scan)
            //bool affected = MathUtils.DoesQuadIntersectCircleSector(this.Bounds, msg);
            bool affected = true; // FIXME - This is here to make sure scans always go through; ideally, though, ships would always be in the SGame node that manages them...
            if (affected)
            {
                Vector2 leftPoint = MathUtils.DirVec(msg.Direction + msg.Width) * msg.Radius;
                Vector2 rightPoint = MathUtils.DirVec(msg.Direction - msg.Width) * msg.Radius;

                Console.WriteLine($"Scanning with radius {msg.Radius}, in triangle <{msg.Origin}, {leftPoint}, {rightPoint}>");

                var iscanned = ShipsByToken.Values
                    .Where((ship) =>
                        ship.Token != msg.Originator
                        && (MathUtils.CircleTriangleIntersection(ship.Pos, ship.Radius(), msg.Origin, leftPoint, rightPoint)
                            || MathUtils.CircleSegmentIntersection(ship.Pos, ship.Radius(), msg.Origin, msg.Radius, msg.Direction, msg.Width))
                    )
                    .Select((ship) => new Messages.Struck.ShipInfo() { Ship = ship });

                results.Struck.AddRange(iscanned);

                if (msg.ScaledShotEnergy > 0.0)
                {
                    foreach (var struck in results.Struck)
                    {
                        var ourShip = (LocalSpaceship)struck.Ship;

                        double shipDistance = (ourShip.Pos - msg.Origin).Length();
                        double damage = MathUtils.ShotDamage(msg.ScaledShotEnergy, msg.Width, shipDistance);
                        double shielding = MathUtils.ShieldingAmount(ourShip, msg.Origin, msg.Direction, msg.Width, msg.Radius);
                        if (shielding > 0.0)
                        {
                            Console.WriteLine($"{ourShip.PublicId} shielded itself for {shielding * 100.0}% of {msg.Originator}'s shot (= {damage * shielding} damage)");
                        }
                        damage *= (1.0 - shielding);

                        // We have killed a ship, gain it's kill reward, and move struck ship to the graveyard
                        if (ourShip.Area - damage < MINIMUM_AREA)
                        {
                            results.AreaGain += ourShip.KillReward;
                            struck.AreaGain = -damage;
                        }
                        else // Struck ship survived - note that it's in combat
                        {
                            if (ourShip.LastUpdate - ourShip.LastCombat > LocalSpaceship.COMBAT_COOLDOWN)
                            {
                                // Reset kill reward when hit ship was not in combat
                                ourShip.KillReward = ourShip.Area;
                            }
                            ourShip.LastCombat = ourShip.LastUpdate;
                            ourShip.Area -= damage;
                            struck.AreaGain = damage;
                        }
                    }
                }
            }
            return results;
        }
    }

    class RemoteQuadTreeNode : SGameQuadTreeNode
    {
        public static readonly TimeSpan REPLYTIMEOUT = new TimeSpan(1500);

        /// <summary>
        /// The message bus to the other nodes.
        /// </summary>
        public NetNode Bus { get; set; }

        /// <summary>
        /// The network peer managing this quadtree node.
        /// </summary>
        public LiteNetLib.NetPeer NodePeer { get; set; }

        /// <summary>
        /// The REST API URL of the remote SGame node.
        /// </summary>
        /// <value></value>
        public string ApiUrl { get; set; }

        public RemoteQuadTreeNode(Quad bounds, NetNode bus, LiteNetLib.NetPeer nodePeer, string apiUrl)
            : base(bounds)
        {
            this.Bus = bus;
            this.NodePeer = nodePeer;
            this.ApiUrl = ApiUrl;
        }

        public RemoteQuadTreeNode(SGameQuadTreeNode parent, Quadrant quadrant, uint depth, NetNode bus, LiteNetLib.NetPeer nodePeer, string apiUrl)
            : base(parent, quadrant, depth)
        {
            this.Bus = bus;
            this.NodePeer = nodePeer;
            this.ApiUrl = ApiUrl;
        }

        public override Task<List<Spaceship>> CheckRangeLocal(Quad range)
        {
            // TODO Peer.SendBusMessage(new BusMsgs.CheckRangeLocal(thing))
            // TODO Peer.AwaitBusResponse(MyResp)
            throw new NotImplementedException();
        }

        public override ScanShootResults ScanShootLocal(Messages.ScanShoot msg)
        {
            bool affected = MathUtils.DoesQuadIntersectCircleSector(this.Bounds, msg);
            if (affected)
            {
                Bus.SendMessage(msg, NodePeer);

                var struckTask = new MessageWaiter<Messages.Struck>(Bus, NodePeer, (struck) => struck.Originator == msg.Originator).Wait;
                ScanShootResults results = new ScanShootResults();
                if (Task.WaitAll(new Task[] { struckTask }, REPLYTIMEOUT))
                {
                    foreach (var struckInfo in struckTask.Result.ShipsInfo)
                    {
                        results.AreaGain += struckInfo.AreaGain;
                        results.Struck.Add(struckInfo);
                    }
                }
                return results;
            }
            else
            {
                return null;
            }
        }
    }

    class DummyQuadTreeNode : SGameQuadTreeNode
    {
        public DummyQuadTreeNode()
            : base(new Quad(0, 0, 0))
        {
        }

        public DummyQuadTreeNode(SGameQuadTreeNode parent, Quadrant quadrant, uint depth)
            : base(parent, quadrant, depth)
        {
        }

        public override Task<List<Spaceship>> CheckRangeLocal(Quad range)
        {
            return new Task<List<Spaceship>>(() => new List<Spaceship>());
        }

        public override ScanShootResults ScanShootLocal(Messages.ScanShoot msg)
        {
            return new ScanShootResults();
        }
    }
}