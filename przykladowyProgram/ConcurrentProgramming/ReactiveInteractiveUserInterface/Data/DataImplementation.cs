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

        public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));
            if (upperLayerHandler == null)
                throw new ArgumentNullException(nameof(upperLayerHandler));

            Random random = new Random();
            for (int i = 0; i < numberOfBalls; i++)
            {
                // Generuj początkową pozycję w środku planszy z marginesem
                double startX = random.NextDouble() * (TableWidth - 2 * BallRadius) + BallRadius;
                double startY = random.NextDouble() * (TableHeight - 2 * BallRadius) + BallRadius;

                Vector startingPosition = new(startX, startY); // ✅ Deklaracja przed użyciem
                Vector velocity = new Vector((random.NextDouble() - 0.5) * 2, (random.NextDouble() - 0.5) * 2);

                Ball newBall = new(startingPosition, velocity); // ✅ OK
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
        private const double TableWidth = 400;
        private const double TableHeight = 400;
        private const double BallRadius = 10;

        private void Move(object? _)
        {
            foreach (Ball ball in BallsList)
            {
                Vector newPosition = (Vector)ball.Position + (Vector)ball.Velocity;

                double minX = BallRadius;
                double maxX = TableWidth - BallRadius;
                double minY = BallRadius;
                double maxY = TableHeight - BallRadius;

                double newVelX = ball.Velocity.X;
                double newVelY = ball.Velocity.Y;

                // odbijanie od lewej / prawej
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
