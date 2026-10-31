using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpaceTraffic.Models
{
    public class Planet
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Size { get; set; }
        public Planet? Parent { get; set; }
        public double OrbitRadius { get; set; }
        public double InitialAngle { get; set; }
        public double AngularSpeed { get; set; }
        public int Warehouse { get; set; }
        public int WarehouseMax { get; set; }
        public int ProdMin { get; set; }
        public int ProdMax { get; set; }
        public IBrush Color { get; set; }

        public Planet()
        {
            Name = "";
            Type = "";
            Size = 1.0;
            Parent = null;
            OrbitRadius = 100.0;
            InitialAngle = 0.0;
            AngularSpeed = 0.0;
            Warehouse = 0;
            ProdMax = 0;
            ProdMin = 0;
            Color = Brushes.White;
        }

        public double GetActualAngle(double elapsedTime)
        {
            return (InitialAngle + elapsedTime * AngularSpeed) * Math.PI / 180.0;
        }

        public Point GetPosition(double elapsedTime)
        {

            //Vypocet pozice planety
            double actualAngle = GetActualAngle(elapsedTime);
            double x = Math.Sin(actualAngle) * OrbitRadius;
            double y = Math.Cos(actualAngle) * OrbitRadius;

            //Pokud má planeta parent...přičteme pozici parent. Jelikož je to rekurzivní tak to spočítá absolutní pozici
            if (Parent != null)
            {
                Point parentPosition = Parent.GetPosition(elapsedTime);
                x += parentPosition.X;
                y += parentPosition.Y;
            }
            return new Point(x, y);
        }

        public double FillRatio()
        {
            if (WarehouseMax <= 0)
                return 0;

            return Math.Clamp((double)Warehouse / WarehouseMax, 0.0, 1.0);
        }

    }
}
