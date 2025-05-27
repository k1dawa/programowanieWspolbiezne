//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.Data
{
    internal class Ball : IBall
    {
        private static int GlobalId = 0;
        public int Id { get; }

        public Ball(Vector initialPosition, Vector initialVelocity, double diameter = 20, double mass = 1)
        {
            Id = Interlocked.Increment(ref GlobalId);
            Position = initialPosition;
            Velocity = initialVelocity;
            Diameter = diameter;
            Mass = mass;
            Radius = diameter / 2;
        }

        public event EventHandler<IVector>? NewPositionNotification;

        public IVector Velocity { get; set; }
        public IVector Position { get; private set; }

        public double Diameter { get; }
        public double Radius { get; }
        public double Mass { get; }

        internal void Move(Vector delta)
        {
            Position = new Vector(Position.X + delta.X, Position.Y + delta.Y);
            RaiseNewPositionChangeNotification();
        }

        private void RaiseNewPositionChangeNotification()
        {
            NewPositionNotification?.Invoke(this, Position);
        }
    }
}
