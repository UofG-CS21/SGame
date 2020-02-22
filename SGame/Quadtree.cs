using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SShared;
using BusMsgs = SShared.BusMsgs;

namespace SGame
{
    abstract class SGameQuadTreeNode : QuadTreeNode<Spaceship>
    {
        public SGameQuadTreeNode(Quad bounds, uint depth) : base(bounds, depth)
        {
        }

        public abstract Task<List<Spaceship>> ScanShootRecur(BusMsgs.ScanShoot msg);
    }

    class LocalQuadTreeNode : SGameQuadTreeNode
    {
        public LocalQuadTreeNode(Quad bounds, uint depth) : base(bounds, depth)
        {
            this.ShipsByToken = new Dictionary<string, Spaceship>();
        }

        public Dictionary<string, Spaceship> ShipsByToken { get; private set; }

        public override Task<List<Spaceship>> CheckRangeLocal(Quad range)
        {
            return new Task<List<Spaceship>>(() =>
                ShipsByToken.Values.Where((ship) => ship.Bounds.Intersects(range)).ToList()
            );
        }

        public override Task<List<Spaceship>> ScanShootRecur(BusMsgs.ScanShoot msg)
        {
            throw new System.NotImplementedException();
        }
    }
}
