using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        public DataImplementation()
        {
            LogTimer = new System.Timers.Timer(100); // co 100 ms
            LogTimer.Elapsed += OnLogTimerElapsed;
            LogTimer.AutoReset = true;
            LogTimer.Start();
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

            lock (BallsList)
            {
                for (int i = 0; i < numberOfBalls; i++)
                {
                    double x = rand.NextDouble() * (TableWidth - 2 * BallRadius) + BallRadius;
                    double y = rand.NextDouble() * (TableHeight - 2 * BallRadius) + BallRadius;
                    Vector position = new(x, y);
                    Vector velocity = new((rand.NextDouble() - 0.5) * 200, (rand.NextDouble() - 0.5) * 200);

                    Ball newBall = new(position, velocity);
                    BallsList.Add(newBall);
                    upperLayerHandler(position, newBall);
                    StartBallThread(newBall);
                }
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
            Vector velocity = new((rand.NextDouble() - 0.5) * 200, (rand.NextDouble() - 0.5) * 200);

            Ball newBall = new(position, velocity);

            lock (BallsList)
            {
                BallsList.Add(newBall);
            }

            upperLayerHandler(position, newBall);
            StartBallThread(newBall);
        }

        public override void RemoveLastBall()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            lock (BallsList)
            {
                if (BallsList.Count > 0)
                {
                    BallsList[^1].ShouldStop = true;
                    BallsList.RemoveAt(BallsList.Count - 1);
                }
            }
        }

        private void StartBallThread(Ball ball)
        {
            Task.Run(() =>
            {
                Stopwatch stopwatch = new();
                stopwatch.Start();

                while (!Disposed && !ball.ShouldStop)
                {
                    double deltaTime = stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Restart();

                    lock (BallsList)
                    {
                        foreach (var otherBall in BallsList)
                        {
                            if (otherBall != ball)
                            {
                                HandleCollision(ball, otherBall);
                            }
                        }

                        MoveBall(ball, deltaTime);
                    }

                    Thread.Sleep(1);
                }
            });
        }

        private void MoveBall(Ball ball, double deltaTime)
        {
            Vector deltaPosition = ((Vector)ball.Velocity) * deltaTime;
            Vector newPosition = (Vector)ball.Position + deltaPosition;

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

            LogQueue.Enqueue($"{DateTime.Now:HH:mm:ss.fff};{ball.Id};{newPosition.X:F2};{newPosition.Y:F2}");
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

        private void OnLogTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (Disposed) return;

            lock (LogFileLock)
            {
                try
                {
                    using var writer = new StreamWriter(LogFilePath, append: true);
                    while (LogQueue.TryDequeue(out string? log))
                    {
                        writer.WriteLine(log);
                    }
                }
                catch (IOException ex)
                {
                    Debug.WriteLine($"[Log Write] IO error: {ex.Message}");
                }
            }
        }

        protected void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    LogTimer?.Stop();
                    LogTimer?.Dispose();

                    lock (BallsList)
                    {
                        foreach (var ball in BallsList)
                            ball.ShouldStop = true;

                        BallsList.Clear();
                    }
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
        private readonly ConcurrentQueue<string> LogQueue = new();
        private readonly List<Ball> BallsList = new();
        private double TableWidth;
        private double TableHeight;
        private const double BallRadius = 10;

        private System.Timers.Timer? LogTimer;
        private readonly object LogFileLock = new();
        private readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "diagnostics.csv");

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            lock (BallsList)
            {
                returnBallsList(BallsList);
            }
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            lock (BallsList)
            {
                returnNumberOfBalls(BallsList.Count);
            }
        }

        [Conditional("DEBUG")]
        internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
        {
            returnInstanceDisposed(Disposed);
        }
    }

    // Zakładam, że klasa Ball rozszerza IBall i zawiera pole ShouldStop.
    // Dodaj następujące pole do klasy Ball:
    // public volatile bool ShouldStop = false;
}
