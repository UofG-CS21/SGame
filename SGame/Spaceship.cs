using System.Numerics;

namespace SGame{

    class Spaceship{

        private int energy;
        private double area;
        private Vector2 pos;
        private int id;

        public Spaceship(int id)
        {
            this.id = id;
            this.area = 1;
            this.energy = 10;
            this.pos = new Vector2(0,0);
        }
    }

}