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
    };

    abstract class SGameQuadTreeNode : QuadTreeNode<Spaceship>
    {
        public SGameQuadTreeNode(QuadTreeNode<Spaceship> parent, Quadrant quadrant, uint depth) : base(parent, quadrant, depth) { }

        public SGameQuadTreeNode(Quad bounds, uint depth) : base(bounds, depth) { }

        /// <summary>
        /// Returns a (area gain for shooter, list of struck ships) pair.
        /// </summary>
        public abstract Task<ScanShootResults> ScanShootRecur(Messages.ScanShoot msg);
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

        public override async Task<ScanShootResults> ScanShootRecur(Messages.ScanShoot msg)
        {
            ScanShootResults results = new ScanShootResults();

            // 1) Search ships locally (but only if affected by the scan)
            bool affected = MathUtils.DoesQuadIntersectCircleSector(this.Bounds, msg);
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

            // 2) Search all siblings (always)
            if (Parent != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var sibling = (SGameQuadTreeNode)Parent.Child((Quadrant)i);
                    if (sibling == null || sibling == this) continue;

                    var resultsHere = await sibling.ScanShootRecur(msg);
                    results.Merge(resultsHere);
                }
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
        public static readonly TimeSpan REPLYTIMEOUT = new TimeSpan(1500);

        public RemoteQuadTreeNode(SGameQuadTreeNode parent, Quadrant quadrant, uint depth, NetNode bus, LiteNetLib.NetPeer nodePeer)
            : base(parent, quadrant, depth)
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
}
