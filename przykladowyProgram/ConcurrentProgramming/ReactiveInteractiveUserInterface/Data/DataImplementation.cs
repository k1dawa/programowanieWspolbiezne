//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        public DataImplementation()
        {
            MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
        }

        public override void Start(int numberOfBalls, double tableWidth, double tableHeight, Action<IVector, IBall> upperLayerHandler)
        {
            TableWidth = tableWidth;
            TableHeight = tableHeight;

            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random rand = new();

            for (int i = 0; i < numberOfBalls; i++)
            {
                double x = rand.NextDouble() * (TableWidth - 2 * BallRadius) + BallRadius;
                double y = rand.NextDouble() * (TableHeight - 2 * BallRadius) + BallRadius;
                Vector position = new(x, y);
                Vector velocity = new((rand.NextDouble() - 0.5) * 2, (rand.NextDouble() - 0.5) * 2);

                Ball newBall = new(position, velocity);
                BallsList.Add(newBall);
                upperLayerHandler(position, newBall);
            }
        }

        public override void AddBall(Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            Random rand = new();
            double x = rand.NextDouble() * (TableWidth - 2 * BallRadius) + BallRadius;
            double y = rand.NextDouble() * (TableHeight - 2 * BallRadius) + BallRadius;
            Vector position = new(x, y);
            Vector velocity = new((rand.NextDouble() - 0.5) * 2, (rand.NextDouble() - 0.5) * 2);

            Ball newBall = new(position, velocity);
            BallsList.Add(newBall);
            upperLayerHandler(position, newBall);
        }

        public override void RemoveLastBall()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            if (BallsList.Count > 0)
                BallsList.RemoveAt(BallsList.Count - 1);
        }

        private void Move(object? _)
        {
            if (Disposed) return;

            // kolizje między kulami
            for (int i = 0; i < BallsList.Count; i++)
            {
                for (int j = i + 1; j < BallsList.Count; j++)
                {
                    HandleCollision(BallsList[i], BallsList[j]);
                }
            }

            // aktualizacja pozycji i odbicia od ścian
            foreach (Ball ball in BallsList)
            {
                Vector newPosition = (Vector)ball.Position + (Vector)ball.Velocity;

                double minX = BallRadius;
                double maxX = TableWidth - BallRadius;
                double minY = BallRadius;
                double maxY = TableHeight - BallRadius;

                double velX = ball.Velocity.X;
                double velY = ball.Velocity.Y;

                if (newPosition.X <= minX || newPosition.X >= maxX)
                {
                    velX *= -1;
                    newPosition = new Vector(Math.Clamp(newPosition.X, minX, maxX), newPosition.Y);
                }

                if (newPosition.Y <= minY || newPosition.Y >= maxY)
                {
                    velY *= -1;
                    newPosition = new Vector(newPosition.X, Math.Clamp(newPosition.Y, minY, maxY));
                }

                ball.Velocity = new Vector(velX, velY);
                ball.Move(newPosition - (Vector)ball.Position);
            }
        }

        private void HandleCollision(Ball ballA, Ball ballB)
        {
            Vector delta = (Vector)ballA.Position - (Vector)ballB.Position;
            double distance = delta.Length;

            if (distance == 0 || distance > ballA.Radius + ballB.Radius)
                return;

            Vector normal = delta / distance;
            Vector relativeVelocity = (Vector)ballA.Velocity - (Vector)ballB.Velocity;
            double velocityAlongNormal = Vector.Dot(relativeVelocity, normal);

            if (velocityAlongNormal >= 0)
                return;

            double massA = ballA.Mass;
            double massB = ballB.Mass;
            double impulse = (2 * velocityAlongNormal) / (massA + massB);

            Vector correctionA = normal * impulse * massB;
            Vector correctionB = normal * impulse * massA;

            ballA.Velocity = (Vector)ballA.Velocity - correctionA;
            ballB.Velocity = (Vector)ballB.Velocity + correctionB;
        }

        protected void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    MoveTimer.Dispose();
                    BallsList.Clear();
                }
                Disposed = true;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool Disposed = false;
        private readonly Timer MoveTimer;
        private readonly List<Ball> BallsList = new();
        private double TableWidth;
        private double TableHeight;
        private const double BallRadius = 10;

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList) => returnBallsList(BallsList);

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls) => returnNumberOfBalls(BallsList.Count);

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed) => returnInstanceDisposed(Disposed);
    }
}
