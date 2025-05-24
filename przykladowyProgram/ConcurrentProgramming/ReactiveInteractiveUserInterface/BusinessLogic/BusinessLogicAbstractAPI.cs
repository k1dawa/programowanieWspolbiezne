//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

namespace TP.ConcurrentProgramming.BusinessLogic
{
  public abstract class BusinessLogicAbstractAPI : IDisposable
  {
        #region Layer Factory

    private static Lazy<BusinessLogicAbstractAPI> logicInstance = new(() => new BusinessLogicImplementation());

    public static BusinessLogicAbstractAPI GetBusinessLogicLayer()
    {
        return logicInstance.Value;
    }

    public static void ResetLogicLayer()
    {
        logicInstance = new(() => new BusinessLogicImplementation());
    }

        #endregion Layer Factory

        #region Layer API

        public static readonly Dimensions GetDimensions = new(10.0, 10.0, 10.0);

    public abstract void Start(int numberOfBalls, double tableWidth, double tableHeight, Action<IPosition, IBall> upperLayerHandler);
    public abstract void AddBall(Action<IPosition, IBall> handler);
    public abstract void RemoveLastBall();

        #region IDisposable

        public abstract void Dispose();

    #endregion IDisposable

    #endregion Layer API

    #region private

    #endregion private
  }
  /// <summary>
  /// Immutable type representing table dimensions
  /// </summary>
  /// <param name="BallDimension"></param>
  /// <param name="TableHeight"></param>
  /// <param name="TableWidth"></param>
  /// <remarks>
  /// Must be abstract
  /// </remarks>
  public record Dimensions(double BallDimension, double TableHeight, double TableWidth);

  public interface IPosition
  {
    double X { get; init; }
    double Y { get; init; }
  }

  public interface IBall 
  {
    event EventHandler<IPosition> NewPositionNotification;
  }
}