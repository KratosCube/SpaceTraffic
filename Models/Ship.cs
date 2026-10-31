using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace SpaceTraffic.Models
{
    public class Ship
    {
        public string Name { get; set; } = "";
        public double Speed { get; set; }
        public string Start { get; set; } = "";
        public int Capacity { get; set; }
        public int TransferSpeed { get; set; }
        public int RemainingTransfer { get; set; } = 0;
        public string Strategy { get; set; } = "";
        public int MaxLoadTime { get; set; }
        public int Load { get; set; } = 0;
        public string State { get; set; } = "Loading";
        public int LoadTime { get; set; } = 0;
        public Planet? Target { get; set; }
        public Planet? DockedAt { get; set; }
        
        public Point PreviousPosition { get; set; }
        public Point Position { get; set; }

        public Ship()
        {
            Name = "";
            Speed = 0;
            Start = "";
            Capacity = 0;
            TransferSpeed = 0;
            Strategy = "";
            State = "Loading";
            MaxLoadTime = 0;
        }


        //Pro výpočet naplnění lodi
        public double FillRatio()
        {
            return Capacity > 0 ? (double)Load / Capacity : 0;
        }

        public Point InterpolatePosition(Ship ship, double progress)
        {
            double x = ship.PreviousPosition.X + (ship.Position.X - ship.PreviousPosition.X) * progress;

            double y = ship.PreviousPosition.Y + (ship.Position.Y - ship.PreviousPosition.Y) * progress;

            return new Point(x, y);
        }
    }
}
