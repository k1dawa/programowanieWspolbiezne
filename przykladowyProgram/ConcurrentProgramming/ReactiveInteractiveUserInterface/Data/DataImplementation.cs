using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

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

            lock (BallsLock)
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

                    // Tworzenie i uruchamianie wątku dla każdej kuli
                    BallWorker worker = new BallWorker(newBall, BallsLock, BallsList, () => Disposed, TableWidth, TableHeight, BallRadius);
                    Thread ballThread = new Thread(worker.Run);
                    
                    BallWorkers.Add(worker);
                    BallThreads.Add(ballThread);
                    ballThread.Start();
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

            lock (BallsLock)
            {
                BallsList.Add(newBall);
            }

            upperLayerHandler(position, newBall);

            // Tworzenie i uruchamianie wątku dla nowej kuli
            BallWorker worker = new BallWorker(newBall, BallsLock, BallsList, () => Disposed, TableWidth, TableHeight, BallRadius);
            Thread ballThread = new Thread(worker.Run);
            
            BallWorkers.Add(worker);
            BallThreads.Add(ballThread);
            ballThread.Start();
        }

        public override void RemoveLastBall()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(DataImplementation));

            BallWorker workerToStop = null;
            Thread threadToStop = null;

            lock (BallsLock)
            {
                if (BallsList.Count > 0)
                {
                    // Pobierz ostatnie elementy
                    workerToStop = BallWorkers[BallWorkers.Count - 1];
                    threadToStop = BallThreads[BallThreads.Count - 1];

                    // Usuń z list
                    BallsList.RemoveAt(BallsList.Count - 1);
                    BallWorkers.RemoveAt(BallWorkers.Count - 1);
                    BallThreads.RemoveAt(BallThreads.Count - 1);
                }
            }

            // Zatrzymaj wątek poza lockiem
            if (workerToStop != null && threadToStop != null)
            {
                workerToStop.Stop();
                threadToStop.Join();
            }
        }

        private void OnLogTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (Disposed) return;

            lock (BallsLock)
            {
                foreach (Ball ball in BallsList)
                {
                    LogQueue.Enqueue($"{DateTime.Now:HH:mm:ss.fff};{ball.Id};{ball.Position.X:F2};{ball.Position.Y:F2}");
                }
            }

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
                    lock (BallsLock)
                    {
                        Disposed = true;
                    }

                    LogTimer?.Stop();
                    LogTimer?.Dispose();

                    // Zatrzymaj wszystkie workery
                    foreach (var worker in BallWorkers)
                    {
                        worker.Stop();
                    }

                    // Poczekaj na zakończenie wszystkich wątków
                    foreach (var thread in BallThreads)
                    {
                        thread.Join();
                    }

                    lock (BallsLock)
                    {
                        BallsList.Clear();
                        BallWorkers.Clear();
                        BallThreads.Clear();
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
        private readonly List<BallWorker> BallWorkers = new();
        private readonly List<Thread> BallThreads = new();
        private readonly object BallsLock = new();
        
        private double TableWidth;
        private double TableHeight;
        private const double BallRadius = 10;

        private System.Timers.Timer? LogTimer;

        private readonly object LogFileLock = new();
        private readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "diagnostics.csv");

        #region BallWorker - Klasa wewnętrzna do zarządzania pojedynczą kulą w osobnym wątku

        private class BallWorker
        {
            private readonly Ball _ball;
            private readonly object _ballsLock;
            private readonly List<Ball> _ballsList;
            private readonly Func<bool> _isDisposed;
            private readonly double _tableWidth;
            private readonly double _tableHeight;
            private readonly double _ballRadius;
            private volatile bool _shouldStop = false;

            public BallWorker(Ball ball, object ballsLock, List<Ball> ballsList, Func<bool> isDisposed, 
                             double tableWidth, double tableHeight, double ballRadius)
            {
                _ball = ball;
                _ballsLock = ballsLock;
                _ballsList = ballsList;
                _isDisposed = isDisposed;
                _tableWidth = tableWidth;
                _tableHeight = tableHeight;
                _ballRadius = ballRadius;
            }

            public void Run()
            {
                DateTime lastUpdateTime = DateTime.UtcNow;

                while (!_isDisposed() && !_shouldStop)
                {
                    var now = DateTime.UtcNow;
                    double deltaTime = (now - lastUpdateTime).TotalSeconds;
                    lastUpdateTime = now;

                    lock (_ballsLock)
                    {
                        if (_isDisposed() || _shouldStop) break;

                        // Sprawdź kolizje z innymi kulami
                        HandleCollisions();

                        // Aktualizuj pozycję
                        UpdatePosition(deltaTime);
                    }

                    Thread.Sleep(16); // ~60 FPS
                }
            }

            public void Stop()
            {
                _shouldStop = true;
            }

            private void UpdatePosition(double deltaTime)
            {
                Vector deltaPosition = ((Vector)_ball.Velocity) * deltaTime;
                Vector newPosition = (Vector)_ball.Position + deltaPosition;

                double minX = _ballRadius;
                double maxX = _tableWidth - _ballRadius;
                double minY = _ballRadius;
                double maxY = _tableHeight - _ballRadius;

                double velX = _ball.Velocity.X;
                double velY = _ball.Velocity.Y;

                // Odbicie od ścian
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

                _ball.Velocity = new Vector(velX, velY);
                _ball.Move(newPosition - (Vector)_ball.Position);
            }

            private void HandleCollisions()
            {
                foreach (Ball otherBall in _ballsList)
                {
                    if (otherBall == _ball) continue;

                    Vector delta = (Vector)_ball.Position - (Vector)otherBall.Position;
                    double distance = delta.Length;

                    if (distance == 0 || distance > _ball.Radius + otherBall.Radius)
                        continue;

                    Vector normal = delta / distance;
                    Vector relativeVelocity = (Vector)_ball.Velocity - (Vector)otherBall.Velocity;
                    double velocityAlongNormal = Vector.Dot(relativeVelocity, normal);

                    if (velocityAlongNormal >= 0)
                        continue;

                    double massA = _ball.Mass;
                    double massB = otherBall.Mass;
                    double impulse = (2 * velocityAlongNormal) / (massA + massB);

                    Vector correctionA = normal * impulse * massB;
                    Vector correctionB = normal * impulse * massA;

                    _ball.Velocity = (Vector)_ball.Velocity - correctionA;
                    otherBall.Velocity = (Vector)otherBall.Velocity + correctionB;
                }
            }
        }

        #endregion

        [Conditional("DEBUG")]
        internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
        {
            lock (BallsLock)
            {
                returnBallsList(BallsList);
            }
        }

        [Conditional("DEBUG")]
        internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
        {
            lock (BallsLock)
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
