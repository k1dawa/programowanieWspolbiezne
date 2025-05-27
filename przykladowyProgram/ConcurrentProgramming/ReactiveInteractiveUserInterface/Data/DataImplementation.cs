using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TP.ConcurrentProgramming.Data
{
    internal class DataImplementation : DataAbstractAPI
    {
        public DataImplementation()
        {
            MoveTaskTokenSource = new CancellationTokenSource();
            MoveTask = Task.Run(() => MoveLoopAsync(MoveTaskTokenSource.Token));
            LogTaskTokenSource = new CancellationTokenSource();
            LogTask = Task.Run(() => WriteLogToFile(LogTaskTokenSource.Token));
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
        }

        public override void RemoveLastBall()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            lock (BallsList)
            {
                if (BallsList.Count > 0)
                    BallsList.RemoveAt(BallsList.Count - 1);
            }
        }

        private async Task MoveLoopAsync(CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            long lastTick = stopwatch.ElapsedMilliseconds;

            while (!token.IsCancellationRequested)
            {
                long now = stopwatch.ElapsedMilliseconds;
                double deltaTime = (now - lastTick) / 1000.0; // sekundy
                lastTick = now;

                lock (BallsList)
                {
                    Move(deltaTime);
                }

                await Task.Delay(1, token); // nie blokuj CPU
            }
        }

        private void Move(double deltaTime)
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

                // Logowanie pozycji
                LogQueue.Enqueue($"{DateTime.Now:HH:mm:ss.fff};{ball.Id};{newPosition.X:F2};{newPosition.Y:F2}");
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
        

        private async Task WriteLogToFile(CancellationToken token)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "diagnostics.csv");

                Console.WriteLine($"Zapisuję do: {path}");
                System.Diagnostics.Debug.WriteLine($"Zapisuję do: {path}");

                using var writer = new StreamWriter(path, append: true);

                while (!token.IsCancellationRequested)
                {
                    while (LogQueue.TryDequeue(out string? log))
                    {
                        await writer.WriteLineAsync(log);
                    }

                    await writer.FlushAsync();
                    await Task.Delay(3000, token);
                }
            }
            catch (TaskCanceledException) { }
        }


        protected void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    MoveTaskTokenSource?.Cancel();

                    try
                    {
                        MoveTask?.Wait();
                    }
                    catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
                    {}

                    MoveTaskTokenSource?.Dispose();

                    lock (BallsList)
                    {
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
        private Task? MoveTask;
        private Task? LogTask;
        private CancellationTokenSource? MoveTaskTokenSource;
        private CancellationTokenSource? LogTaskTokenSource;
        private readonly ConcurrentQueue<string> LogQueue = new();
        private readonly List<Ball> BallsList = new();
        private double TableWidth;
        private double TableHeight;
        private const double BallRadius = 10;

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
}
