using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        public MainWindowViewModel() : this(null) { }

        internal MainWindowViewModel(ModelAbstractApi modelLayerAPI)
        {
            ModelLayer = modelLayerAPI == null ? ModelAbstractApi.CreateModel() : modelLayerAPI;
            Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));

            StartSimulationCommand = new RelayCommand(StartSimulation);
            ClearBallsCommand = new RelayCommand(ClearBalls);
        }

        #region Properties and Commands

        public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();

        private int _numberOfBallsToAdd = 5;
        public int NumberOfBallsToAdd
        {
            get => _numberOfBallsToAdd;
            set
            {
                if (_numberOfBallsToAdd != value)
                {
                    _numberOfBallsToAdd = value;
                    RaisePropertyChanged(nameof(NumberOfBallsToAdd));
                }
            }
        }

        public ICommand StartSimulationCommand { get; }
        public ICommand ClearBallsCommand { get; }

        #endregion

        #region Methods

        private void StartSimulation()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));

            ModelLayer.Start(NumberOfBallsToAdd);
        }

        private void ClearBalls()
        {
            Balls.Clear();
            ModelLayer.Dispose();
            ModelLayer = ModelAbstractApi.CreateModel();
            Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Balls.Clear();
                    Observer?.Dispose();
                    ModelLayer?.Dispose();
                }
                Disposed = true;
            }
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private

        private IDisposable Observer = null;
        private ModelAbstractApi ModelLayer;
        private bool Disposed = false;

        #endregion
    }
}
