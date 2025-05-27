//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System.Diagnostics;
using UnderneathLayerAPI = TP.ConcurrentProgramming.Data.DataAbstractAPI;

namespace TP.ConcurrentProgramming.BusinessLogic
{
  internal class BusinessLogicImplementation : BusinessLogicAbstractAPI
  {
    #region ctor

    public BusinessLogicImplementation() : this(null)
    { }

    internal BusinessLogicImplementation(UnderneathLayerAPI? underneathLayer)
    {
      layerBellow = underneathLayer == null ? UnderneathLayerAPI.GetDataLayer() : underneathLayer;
    }

    #endregion ctor

    #region BusinessLogicAbstractAPI

    public override void Dispose()
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(BusinessLogicImplementation));
      layerBellow.Dispose();
      Disposed = true;
    }

    public override void Start(int numberOfBalls, double tableWidth, double tableHeight, Action<IPosition, IBall> upperLayerHandler)
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

        layerBellow.Start(numberOfBalls, tableWidth, tableHeight, (vector, ball) =>
        {
            upperLayerHandler(new Position(vector.X, vector.Y), new Ball(ball));
        });
    }
    public override void AddBall(Action<IPosition, IBall> upperLayerHandler)
{
    if (Disposed)
            throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

            if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));

    layerBellow.AddBall((position, ball) =>
    {
        var wrappedPosition = new Position(position.X, position.Y);
        var wrappedBall = new Ball(ball);
        upperLayerHandler(wrappedPosition, wrappedBall);
    });
}

    public override void RemoveLastBall()
    {
        if (Disposed)
            throw new ObjectDisposedException(nameof(BusinessLogicImplementation));

        layerBellow.RemoveLastBall();
    }
        #endregion BusinessLogicAbstractAPI

        #region private

        private bool Disposed = false;

    private readonly UnderneathLayerAPI layerBellow;

    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}