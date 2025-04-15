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
using System.Threading;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        #region ctor

        public DataImplementation()
        {
            MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
        }

        #endregion ctor

        #region DataAbstractAPI

        public override void Start(int numberOfBalls, double tableWidth, double tableHeight, Action<IVector, IBall> upperLayerHandler)
        {
            this.TableWidth = tableWidth;
            this.TableHeight = tableHeight;
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random random = new Random();
            for (int i = 0; i < numberOfBalls; i++)
            {
                double startX = random.NextDouble() * (TableWidth - 2 * BallRadius);
                double startY = random.NextDouble() * (TableHeight - 2 * BallRadius);

                Vector startingPosition = new(startX, startY); 
                Vector velocity = new Vector((random.NextDouble() - 0.5) * 2, (random.NextDouble() - 0.5) * 2);

                Ball newBall = new(startingPosition, velocity); 
                upperLayerHandler(startingPosition, newBall);
                BallsList.Add(newBall);
            }
        }


        #endregion DataAbstractAPI

        #region IDisposable

        protected virtual void Dispose(bool disposing)
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
            else
                throw new ObjectDisposedException(nameof(DataImplementation));
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        private bool Disposed = false;
        private readonly Timer MoveTimer;
        private Random RandomGenerator = new();
        private List<Ball> BallsList = new();

        // Parametry planszy
        private double TableWidth;
        private double TableHeight;
        private const double BallRadius = 10;

        private void Move(object? _)
        {
            foreach (Ball ball in BallsList.ToList())
            {
                Vector newPosition = (Vector)ball.Position + (Vector)ball.Velocity;

                double minX = 0;
                double maxX = TableWidth - 2 * BallRadius;
                double minY = 0;
                double maxY = TableHeight - 2 * BallRadius;

                double newVelX = ball.Velocity.X;
                double newVelY = ball.Velocity.Y;

                // odbijanie 
                if (newPosition.X <= minX || newPosition.X >= maxX)
                {
                    newVelX *= -1;
                    newPosition = new Vector(
                        Math.Clamp(newPosition.X, minX, maxX),
                        newPosition.Y
                    );
                }

                // odbijanie od góry / dołu
                if (newPosition.Y <= minY || newPosition.Y >= maxY)
                {
                    newVelY *= -1;
                    newPosition = new Vector(
                        newPosition.X,
                        Math.Clamp(newPosition.Y, minY, maxY)
                    );
                }

                // zaktualizuj prędkość i pozycję
                ball.Velocity = new Vector(newVelX, newVelY);
                ball.Move(newPosition - ball.Position);
            }
        }

        public override void AddBall(Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random random = new Random();
            double startX = random.NextDouble() * (TableWidth - 2 * BallRadius);
            double startY = random.NextDouble() * (TableHeight - 2 * BallRadius);
            Vector startingPosition = new(startX, startY);
            Vector velocity = new((random.NextDouble() - 0.5) * 2, (random.NextDouble() - 0.5) * 2);

            Ball newBall = new(startingPosition, velocity);
            BallsList.Add(newBall);
            upperLayerHandler(startingPosition, newBall);
        }

        public override void RemoveLastBall()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (BallsList.Count > 0)
                BallsList.RemoveAt(BallsList.Count - 1);
        }

        #endregion private

        #region TestingInfrastructure

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            returnBallsList(BallsList);
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            returnNumberOfBalls(BallsList.Count);
        }

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }

        #endregion TestingInfrastructure
    }
}
