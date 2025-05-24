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
    public abstract class DataAbstractAPI : IDisposable
    {
        #region Layer Factory

        private static Lazy<DataAbstractAPI> dataInstance = new(() => new DataImplementation());
        public static DataAbstractAPI GetDataLayer()
        {
            return dataInstance.Value;
        }

        public static void ResetDataLayer()
        {
            dataInstance = new(() => new DataImplementation());
        }

        #endregion Layer Factory

        #region public API

        public abstract void Start(int numberOfBalls, double tableWidth, double tableHeight, Action<IVector, IBall> upperLayerHandler);
        public abstract void AddBall(Action<IVector, IBall> upperLayerHandler);
        public abstract void RemoveLastBall();


        #endregion public API

        #region IDisposable

        public abstract void Dispose();

        #endregion IDisposable

        #region private

        private static Lazy<DataAbstractAPI> modelInstance = new Lazy<DataAbstractAPI>(() => new DataImplementation());

        #endregion private
    }
    public interface IVector
  {
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    double X { get; }

    /// <summary>
    /// The y component of the vector.
    /// </summary>
    double Y { get; }
  }

    public interface IBall
    {
        event EventHandler<IVector> NewPositionNotification;

        IVector Velocity { get; set; }

        double Diameter { get; }

        double Mass { get; }

        double Radius { get; }

        IVector Position { get; }
    }

    }