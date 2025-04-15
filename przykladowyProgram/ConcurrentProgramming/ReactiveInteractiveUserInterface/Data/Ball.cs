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
  internal class Ball : IBall
  {
    #region ctor

    internal Ball(Vector initialPosition, Vector initialVelocity, double diameter)
    {
      Position = initialPosition;
      Velocity = initialVelocity;
      Diameter = diameter;
    }

    #endregion ctor

    #region IBall

    public event EventHandler<IVector>? NewPositionNotification;

    public IVector Velocity { get; set; }

    #endregion IBall

    #region private

    public Vector Position { get; private set; }
    public double Diameter { get; set; }
    private void RaiseNewPositionChangeNotification()
    {
      NewPositionNotification?.Invoke(this, Position);
    }

    internal void Move(Vector delta,double containerWidth, double containerHeight)
    {
      double radius = Diameter / 2.0;
      
      // Obliczamy nową pozycję
      double newX = Position.X + delta.X;
      double newY = Position.Y + delta.Y;
      
      // Ograniczamy ruch do wnętrza prostokąta uwzględniając średnicę kuli
      newX = Math.Max(radius, Math.Min(containerWidth - radius, newX));
      newY = Math.Max(radius, Math.Min(containerHeight - radius, newY));
      
      // Aktualizujemy pozycję
      Position = new Vector(newX, newY);
      
      // Odbijamy wektor prędkości jeśli kula dotarła do granicy
      if (newX <= radius || newX >= containerWidth - radius)
        Velocity = new Vector(-Velocity.X, Velocity.Y);
      
      if (newY <= radius || newY >= containerHeight - radius)
        Velocity = new Vector(Velocity.X, -Velocity.Y);
      
      RaiseNewPositionChangeNotification();
    }

    #endregion private
  }
}