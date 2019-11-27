using System.Numerics;

namespace SGame
{

    /// <summary>
    /// Class representing a single spaceship on the server side.
    /// </summary>
    class Spaceship
    {
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
        public Vector2 Pos { get; set; }

        /// <summary>
        /// Velocity of the spaceship.
        /// </summary>
        public Vector2 Velocity { get; set; }

        /// <summary>
        /// ID of the spaceship (only assigned at creation).
        /// </summary>
        public int Id { get; }

        public Spaceship(int id)
        {
            this.Id = id;
            this.Area = 1;
            this.Energy = 10;
            this.Pos = new Vector2(0, 0);
            this.Velocity = new Vector2(0, 0);
        }
    }

}