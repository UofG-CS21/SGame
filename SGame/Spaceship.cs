using System.Numerics;

namespace SGame{

    /// <summary>
    /// Class representing a single spaceship on the server side.
    /// </summary>
    class Spaceship{
        /// <summary>
        /// Energy of the spaceship.
        /// </summary>
        public int Energy { get; set; }

        /// <summary>
        /// Area of the spaceship.
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Position of the spaceship.
        /// </summary>
        public Vector2 pos { get; set; }

        /// <summary>
        /// ID of the spaceship (only assigned at creation).
        /// </summary>
        public int id { get; }
        
        public Spaceship(int id)
        {
            this.id = id;
            this.area = 1;
            this.energy = 10;
            this.pos = new Vector2(0,0);
        }
    }

}