using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using TP.ConcurrentProgramming.BusinessLogic;

namespace TP.ConcurrentProgramming.Presentation.Model
{
    internal class PresentationModel : ModelAbstractApi
    {
        public PresentationModel() : this(null) { }

        public PresentationModel(BusinessLogicAbstractAPI logicLayer)
        {
            _logicLayer = logicLayer ?? BusinessLogicAbstractAPI.GetBusinessLogicLayer();
            _eventStream = Observable.FromEventPattern<BallChangedEventArgs>(this, nameof(BallChanged));
        }

        public override void Start(int numberOfBalls, double tableWidth, double tableHeight)
        {
            _logicLayer.Start(numberOfBalls, tableWidth, tableHeight, BallCreatedHandler);
        }

        public override IDisposable Subscribe(IObserver<IBall> observer)
        {
            return _eventStream.Subscribe(x => observer.OnNext(x.EventArgs.Ball));
        }

        public override void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PresentationModel));

            _logicLayer.Dispose();
            _disposed = true;
        }

        public event EventHandler<BallChangedEventArgs>? BallChanged;

        #region Helpers

        private bool _disposed = false;
        private readonly BusinessLogicAbstractAPI _logicLayer;
        private readonly IObservable<EventPattern<BallChangedEventArgs>> _eventStream;

        private void BallCreatedHandler(IPosition position, BusinessLogic.IBall ball)
        {
            ModelBall newBall = new(position.X, position.Y, ball)
            {
                Diameter = 20
            };
            BallChanged?.Invoke(this, new BallChangedEventArgs { Ball = newBall });
        }

        public override void AddBall(Action<IBall> observer)
        {
            _logicLayer.AddBall((pos, logicBall) =>
            {
                var modelBall = new ModelBall(pos.X, pos.Y, logicBall) { Diameter = 20.0 };
                observer(modelBall);
            });
        }

        public override void RemoveLastBall()
        {
            _logicLayer.RemoveLastBall();
        }

        #endregion
    }

    public class BallChangedEventArgs : EventArgs
    {
        public IBall Ball { get; init; }
    }
}
