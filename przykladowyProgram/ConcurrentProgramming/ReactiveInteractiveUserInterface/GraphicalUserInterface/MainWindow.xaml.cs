//__________________________________________________________________________________________
//
//  Copyright 2024 Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and to get started
//  comment using the discussion panel at
//  https://github.com/mpostol/TP/discussions/182
//__________________________________________________________________________________________

using System;
using System.Windows;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
  /// <summary>
  /// View implementation
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }
  private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      if (int.TryParse(BallCountTextBox.Text, out int ballCount) && ballCount > 0)
      {
        MainWindowViewModel viewModel = (MainWindowViewModel)DataContext;
        viewModel.Start(ballCount);
      }
      else
      {
        MessageBox.Show("Proszę podać poprawną liczbę kul (liczbę większą od 0).", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  
    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
      if (DataContext is MainWindowViewModel viewModel)
      {
        viewModel.Dispose(); // Lub viewModel.Stop() jeśli masz taką metodę
      }
    }
    /// <summary>
    /// Raises the <seealso cref="System.Windows.Window.Closed"/> event.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    protected override void OnClosed(EventArgs e)
    {
      if (DataContext is MainWindowViewModel viewModel)
        viewModel.Dispose();
      base.OnClosed(e);
    }
  }
}