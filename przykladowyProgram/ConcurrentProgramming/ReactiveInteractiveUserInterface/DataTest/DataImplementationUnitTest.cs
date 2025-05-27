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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TP.ConcurrentProgramming.Data.Test
{
    [TestClass]
    public class DataImplementationUnitTest
    {
        [TestMethod]
        public void ConstructorTestMethod()
        {
            using (DataImplementation newInstance = new DataImplementation())
            {
                IEnumerable<IBall>? ballsList = null;
                newInstance.CheckBallsList(x => ballsList = x);
                Assert.IsNotNull(ballsList);
                int numberOfBalls = 0;
                newInstance.CheckNumberOfBalls(x => numberOfBalls = x);
                Assert.AreEqual<int>(0, numberOfBalls);
            }
        }

        [TestMethod]
        public void DisposeTestMethod()
        {
            DataImplementation newInstance = new DataImplementation();
            bool newInstanceDisposed = false;
            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsFalse(newInstanceDisposed);
            newInstance.Dispose();
            newInstance.CheckObjectDisposed(x => newInstanceDisposed = x);
            Assert.IsTrue(newInstanceDisposed);
            IEnumerable<IBall>? ballsList = null;
            newInstance.CheckBallsList(x => ballsList = x);
            Assert.IsNotNull(ballsList);
            newInstance.CheckNumberOfBalls(x => Assert.AreEqual<int>(0, x));
            Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Dispose());
            Assert.ThrowsException<ObjectDisposedException>(() => newInstance.Start(0, 800, 600, (position, ball) => { }));
        }

        [TestMethod]
        public void StartTestMethod()
        {
            using (DataImplementation newInstance = new DataImplementation())
            {
                int numberOfCallbackInvoked = 0;
                int numberOfBalls2Create = 10;
                double testTableWidth = 800;
                double testTableHeight = 600;

                newInstance.Start(
                    numberOfBalls2Create,
                    testTableWidth,
                    testTableHeight,
                    (startingPosition, ball) =>
                    {
                        numberOfCallbackInvoked++;
                        Assert.IsTrue(startingPosition.X >= 0);
                        Assert.IsTrue(startingPosition.Y >= 0);
                        Assert.IsNotNull(ball);
                    });

                Assert.AreEqual<int>(numberOfBalls2Create, numberOfCallbackInvoked);
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual<int>(10, x));
            }
        }

        [TestMethod]
        public void AddBallTestMethod()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                int callbackInvoked = 0;

                // Wykonanie
                newInstance.AddBall((position, ball) =>
                {
                    callbackInvoked++;
                    Assert.IsNotNull(position);
                    Assert.IsNotNull(ball);
                });

                // Sprawdzenie
                Assert.AreEqual(1, callbackInvoked);
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(1, x));
            }
        }

        [TestMethod]
        public void RemoveLastBallTestMethod()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                newInstance.AddBall((position, ball) => { });
                newInstance.AddBall((position, ball) => { });

                // Wykonanie
                newInstance.RemoveLastBall();

                // Sprawdzenie
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(1, x));
            }
        }

        [TestMethod]
        public void RemoveLastBall_PustaLista_NieRzucaWyjatku()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                // Wykonanie i sprawdzenie - nie powinno rzucać wyjątku
                newInstance.RemoveLastBall();
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(0, x));
            }
        }

        [TestMethod]
        public void Start_ZZeroKul_NieTworzyKul()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                int callbackCount = 0;

                // Wykonanie
                newInstance.Start(0, 800, 600, (position, ball) => callbackCount++);

                // Sprawdzenie
                Assert.AreEqual(0, callbackCount);
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(0, x));
            }
        }

        [TestMethod]
        public void Start_ZNullHandler_RzucaArgumentNullException()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                // Wykonanie i sprawdzenie
                Assert.ThrowsException<ArgumentNullException>(() =>
                    newInstance.Start(5, 800, 600, null));
            }
        }

        [TestMethod]
        public void AddBall_PoDispose_RzucaObjectDisposedException()
        {
            // Przygotowanie
            DataImplementation newInstance = new DataImplementation();
            newInstance.Dispose();

            // Wykonanie i sprawdzenie
            Assert.ThrowsException<ObjectDisposedException>(() =>
                newInstance.AddBall((position, ball) => { }));
        }

        [TestMethod]
        public void RemoveLastBall_PoDispose_RzucaObjectDisposedException()
        {
            // Przygotowanie
            DataImplementation newInstance = new DataImplementation();
            newInstance.Dispose();

            // Wykonanie i sprawdzenie
            Assert.ThrowsException<ObjectDisposedException>(() =>
                newInstance.RemoveLastBall());
        }

        [TestMethod]
        public async Task KuleSieRuszaja_PoStart_PozycjeZmieniaja()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                var pozycjePoczatkowe = new List<IVector>();

                newInstance.Start(3, 800, 600, (position, ball) =>
                {
                    pozycjePoczatkowe.Add(new Vector(position.X, position.Y));
                });

                // Wykonanie - czekamy chwilę żeby kule się poruszyły
                await Task.Delay(100);

                // Sprawdzenie - sprawdzamy czy przynajmniej jedna kula zmieniła pozycję
                var pozycjeAktualne = new List<IVector>();
                newInstance.CheckBallsList(balls =>
                {
                    foreach (var ball in balls)
                    {
                        pozycjeAktualne.Add(ball.Position);
                    }
                });

                bool jakasKulaZmienilaPozycje = false;
                for (int i = 0; i < Math.Min(pozycjePoczatkowe.Count, pozycjeAktualne.Count); i++)
                {
                    if (Math.Abs(pozycjePoczatkowe[i].X - pozycjeAktualne[i].X) > 0.1 ||
                        Math.Abs(pozycjePoczatkowe[i].Y - pozycjeAktualne[i].Y) > 0.1)
                    {
                        jakasKulaZmienilaPozycje = true;
                        break;
                    }
                }

                Assert.IsTrue(jakasKulaZmienilaPozycje, "Kule powinny się poruszać po utworzeniu");
            }
        }

        [TestMethod]
        public void WielokrotneDodawanieUsuwanie_UtrzymujePoprawneLiczenie()
        {
            // Przygotowanie
            using (DataImplementation newInstance = new DataImplementation())
            {
                // Wykonanie
                newInstance.AddBall((position, ball) => { });
                newInstance.AddBall((position, ball) => { });
                newInstance.AddBall((position, ball) => { });

                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(3, x));

                newInstance.RemoveLastBall();
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(2, x));

                newInstance.AddBall((position, ball) => { });

                // Sprawdzenie
                newInstance.CheckNumberOfBalls(x => Assert.AreEqual(3, x));
            }
        }
    }
}
