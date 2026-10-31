using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SpaceTraffic.Models;
using SpaceTraffic.Services;
using SpaceTraffic.Simulations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Timers;
using System.Xml.Linq;

namespace SpaceTraffic;

public partial class DrawingPanel : UserControl
{
    //string path = "C:\\Users\\vasac\\Downloads\\src\\src\\data\\complex";
    private readonly int m_StartTime;

    private double scale = 60;

    double maxDistance;
    double minDistance;
    private readonly List<Planet> planets;
    private readonly List<Destination> destanations;
    private readonly List<Ship> ships;

    private readonly Simulation simulation;

    private readonly List<Rect> occupiedLabels = new();

    private long lastSimulationTick;
    private double simulationIntervalMs = 1000.0;
    public DrawingPanel()
    {
        string[] args = Environment.GetCommandLineArgs();
        string scenario = args.Length > 1 ? args[1] : "default";
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "data",
            scenario
        );
        InitializeComponent();

        m_StartTime = Environment.TickCount;
        var timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) => this.InvalidateVisual();
        timer.Start();


        var simulationTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        SceneLoader loader = new SceneLoader();
        planets = loader.LoadPlanets(path);
        destanations = loader.LoadDestinations(path);
        ships = loader.LoadShips(path);
        foreach (var ship in ships)
        {
            ship.DockedAt = planets.Find(p => p.Name == ship.Start);
        }

        simulation = new Simulation(planets, ships, destanations);

        lastSimulationTick = Environment.TickCount64;

        simulationTimer.Tick += (_, _) =>
        {
            long now = Environment.TickCount64;

            simulationIntervalMs = now - lastSimulationTick;
            lastSimulationTick = now;

            simulation.SimulateDay();
        };

        simulationTimer.Start();



    }


    public override void Render(DrawingContext context)
    {

        base.Render(context);
        occupiedLabels.Clear();
        var rect = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Black, rect);

        double maxDistance = planets.Max(p => p.OrbitRadius);
        scale = ((Math.Min(Bounds.Width, Bounds.Height) * 0.45) / maxDistance);
        double progress = GetDayProgress();
        double elapsed =
    (simulation.CurrentDate - new DateTime(2026, 9, 21)).TotalDays
    + progress;
        //Posunutí souřadnicového systému do středu 
        using (context.PushTransform(Matrix.CreateTranslation(Bounds.Width * 0.5, Bounds.Height * 0.5)))
        {
            foreach (var planet in planets)
            {
                DrawPlanet(context, planet,scale, elapsed);
            }
            foreach (Ship ship in ships)
            {
                DrawShip(context, ship, scale, elapsed, progress);
            }
        }
        DrawDate(context);

        //TODO: vykreslen� vesmirnych teles a transportnich lodi v aktualnim case
    }



    private double GetDayProgress()
    {
        double elapsedMilliseconds =
            Environment.TickCount64 - lastSimulationTick;

        return Math.Clamp(
            elapsedMilliseconds / simulationIntervalMs,
            0.0,
            1.0
        );
    }

    private void DrawPlanet(DrawingContext context, Planet planet, double scale, double elapsed)
    {
        Point planetPosition = planet.GetPosition(elapsed);

        //vzkresleni oběžné dráhy planety, když má rodice
        if (planet.Parent != null)
        {
            Point parentPosition = planet.Parent.GetPosition(elapsed);
            double x = parentPosition.X * scale;
            double y = parentPosition.Y * scale;


            using (context.PushTransform(Matrix.CreateTranslation(x, y)))
            {
                DrawObeznaDraha(context, planet, scale);
            }
        }

        //vykresnení planet a měsíců
        using (context.PushTransform(Matrix.CreateTranslation(planetPosition.X * scale, planetPosition.Y * scale)))
        {
            DrawBody(context, planet);
            DrawPlanetLabel(context, planet, planet.Size * scale * 0.2, new Point(planetPosition.X * scale, planetPosition.Y * scale));
        }
    }

    private void DrawPlanetLabel(
    DrawingContext context,
    Planet planet,
    double radius,
    Point planetScreenPosition)
    {

        // Pokud planeta produkuje nějaké zboží tak vykresluji i její staty.
        string planetStats = "";
        if (planet.WarehouseMax != 0)
            planetStats = $"{planet.Warehouse}/{ planet.WarehouseMax}";

        FormattedText planetName = new FormattedText(
            $"{planet.Name}\n",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            Brushes.White
        );

        double barWidth = 60;
        double barHeight = 16;
        double spacing = 4;
        double margin = 5;

        // Rozměry CELÉHO popisku včetně ukazatele
        double labelWidth = Math.Max(planetName.Width, barWidth);
        double labelHeight = planetName.Height + spacing + barHeight;

        // Možné pozice vzhledem ke středu planety
        Point[] positions =
        {
            new Point(radius + margin, -labelHeight / 2),          // Vpravo
            new Point(-radius - margin - labelWidth, -labelHeight / 2), // Vlevo
            new Point(-labelWidth / 2, -radius - margin - labelHeight), // Nahoře
            new Point(-labelWidth / 2, radius + margin)             // Dole
        };

        foreach (Point position in positions)
        {
            Rect candidate = new Rect(
                planetScreenPosition.X + position.X,
                planetScreenPosition.Y + position.Y,
                labelWidth,
                labelHeight
            );

            if (!IsLabelPositionFree(candidate))
                continue;

            occupiedLabels.Add(candidate);

            // Posuneme název i ukazatel společně
            using (context.PushTransform(
                Matrix.CreateTranslation(position.X, position.Y)))
            {
                // Název a množství ve skladu
                context.DrawText(planetName, new Point(0, 0));

                // Ukazatel přímo pod textem
                if (planet.WarehouseMax != 0)
                DrawBar(
                    context,
                    0,
                    planetName.Height + spacing,
                    barWidth,
                    barHeight,
                    planet.FillRatio(),
                    planet.Warehouse,
                    planet.WarehouseMax
                );
            }

            break;
        }
    }


    private bool IsLabelPositionFree(Rect candidate)
    {
        double margin = 5;

        // Hranice viditelné oblasti
        double left = -Bounds.Width / 2 + margin;
        double right = Bounds.Width / 2 - margin;
        double top = -Bounds.Height / 2 + margin;
        double bottom = Bounds.Height / 2 - margin;

        // Kontrola, zda popisek nepřesahuje okno
        if (candidate.Left < left ||
            candidate.Right > right ||
            candidate.Top < top ||
            candidate.Bottom > bottom)
        {
            return false;
        }

        // Kontrola překryvu s ostatními popisky
        foreach (Rect occupied in occupiedLabels)
        {
            if (candidate.Intersects(occupied))
            {
                return false;
            }
        }

        return true;
    }

    //vykreslení planety
    private void DrawBody(DrawingContext context, Planet planet)
    {
        double planetSize = planet.Size * scale * 0.2;
        context.DrawEllipse(planet.Color, null, new Rect(-planetSize, -planetSize, 2 * planetSize, 2 * planetSize));
    }

    //vykreslení oběžné dráhy planety
    private void DrawObeznaDraha(DrawingContext context, Planet planet, double scale)
    {

        Pen dashedPen = new Pen(Brushes.Gray, 1)
        {
            DashStyle = DashStyle.Dash
        };

        context.DrawEllipse(null, dashedPen,
            new Rect(-planet.OrbitRadius * scale, -planet.OrbitRadius * scale, 2 * planet.OrbitRadius * scale, 2 * planet.OrbitRadius * scale));

    }



    private void DrawShip(DrawingContext context, Ship ship, double scale, double elapsed, double progress)
    {
        

        Point shipPosition;

        // Pokud je loď zakotvená, pohybuje se společně s planetou.
        if (ship.DockedAt != null)
        {
            //shipPosition = new Point(ship.DockedAt.GetPosition(elapsed).X + ship.DockedAt.Size , ship.DockedAt.GetPosition(elapsed).Y);
            shipPosition = ship.DockedAt.GetPosition(elapsed);
        }
        // Pokud loď letí, použijeme pozici vypočítanou v Simulation.MoveShip().
        else
        {

            //interpolovana pozice
            double x = ship.PreviousPosition.X + (ship.Position.X - ship.PreviousPosition.X) * progress;

            double y = ship.PreviousPosition.Y+ (ship.Position.Y - ship.PreviousPosition.Y) * progress;


            shipPosition = ship.InterpolatePosition(ship,progress);
        }
        Point shipScreenPosition = new Point(
            shipPosition.X * scale,
            shipPosition.Y * scale
        );

        // Převod světových souřadnic na souřadnice vykreslování.
        using (context.PushTransform(
            Matrix.CreateTranslation(
                shipPosition.X * scale,
                shipPosition.Y * scale)))
        {
            double shipSize = 2.5;

            context.DrawEllipse(
                Brushes.Red,
                null,
                new Rect(-shipSize, -shipSize, 2 * shipSize, 2 * shipSize)
            );
            DrawShipLabel(context, ship, shipScreenPosition);
        }
    }

    private void DrawShipLabel(
    DrawingContext context,
    Ship ship,
    Point shipScreenPosition)
    {
        // TEXT: Název lodi
        FormattedText shipName = new FormattedText(
            ship.Name,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            Brushes.White
        );
        

        // Určení šipky podle stavu lodi
        string arrow = "";

        // Když loď nabírá náklad tak se zobrazuje šipka nahoru
        if (ship.State == "Loading")
            arrow = "↑";
        // Při vykládce šipka dolů
        else if (ship.State == "Unloading")
            arrow = "↓";

        
        FormattedText? shipState = null;
        
        // TEXT: ShipState se ukazuje pouze pokud se nakládá nebo vykládá
        if (arrow != "")
        {
            shipState = new FormattedText(
                arrow,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                16,
                Brushes.White
            );
        }

        // Rozměry jednotlivých prvků baru
        double barWidth = 60;
        double barHeight = 16;
        double spacing = 4;
        double margin = 8;

        // Vyhradíme místo také pro šipku
        double arrowWidth = shipState != null ? shipState.Width + spacing : 0;

        
        double contentWidth = Math.Max(shipName.Width, barWidth);

        double labelWidth = arrowWidth + contentWidth;
        double labelHeight = shipName.Height + spacing + barHeight;

        // Pozice pro popisky vzhledem ke středu lodi
        Point[] positions =
        {
        new Point(margin, -labelHeight / 2),                // Vpravo
        new Point(-labelWidth - margin, -labelHeight / 2),  // Vlevo
        new Point(-labelWidth / 2, -labelHeight - margin),  // Nahoře
        new Point(-labelWidth / 2, margin)                  // Dole
    };


        Point? selectedPosition = null;
        
        Rect selectedCandidate = default;

        // Najdeme první volnou pozici
        foreach (Point position in positions)
        {
            Rect candidate = new Rect(
                shipScreenPosition.X + position.X,
                shipScreenPosition.Y + position.Y,
                labelWidth,
                labelHeight
            );

            if (IsLabelPositionFree(candidate))
            {
                selectedPosition = position;
                selectedCandidate = candidate;
                break;
            }
        }

        if (selectedPosition == null)
            return;

        // Přidání do seznamu osazených pozic labely na obrazovce
        occupiedLabels.Add(selectedCandidate);

        // Posun celého popisku na vybranou pozici
        using (context.PushTransform(Matrix.CreateTranslation(selectedPosition.Value.X, selectedPosition.Value.Y)))
        {
            // Šipka se nyní přesouvá společně s popiskem
            if (shipState != null)
            {
                double arrowY = (labelHeight - shipState.Height) / 2;

                context.DrawText(
                    shipState,
                    new Point(0, arrowY)
                );
            }

            // Obsah začíná napravo od šipky
            double contentX = arrowWidth;

            // Název lodi
            context.DrawText(
                shipName,
                new Point(contentX, 0)
            );

            double barY = shipName.Height + spacing;

            DrawBar(
    context,
    contentX,
    barY,
    60,
    16,
    ship.FillRatio(),
    ship.Load,
    ship.Capacity
);


        }
    }

    private void DrawBar(
    DrawingContext context,
    double x,
    double y,
    double width,
    double height,
    double fillRatio,
    int currentValue,
    int maxValue)
    {
        fillRatio = Math.Clamp(fillRatio, 0.0, 1.0);

        Rect backgroundRect = new Rect(x, y, width, height);

        Rect filledRect = new Rect(
            x,
            y,
            width * fillRatio,
            height
        );

        context.FillRectangle(Brushes.AliceBlue, backgroundRect);
        context.FillRectangle(Brushes.Green, filledRect);


        // TEXT: Aktuální náklad
        FormattedText current = new FormattedText(
            $"{currentValue}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            Brushes.Black
        );
        // TEXT: Maximální kapacita
        FormattedText maxCapacity = new FormattedText(
            $"{maxValue}",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            Brushes.Black
        );

        // Aktuální množství vlevo
        context.DrawText(
            current,
            new Point(x + 2, y)
        );

        // Maximum vpravo
        context.DrawText(
            maxCapacity,
            new Point(x + width - maxCapacity.Width - 2, y)
        );

    }


    //Vykreslení dne
    private void DrawDate(DrawingContext context)
    {
        string dateText = simulation.CurrentDate.ToString("dd.MM.yyyy");

        FormattedText text = new FormattedText(
            dateText,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            18,
            Brushes.White
        );

        context.DrawText(text, new Point(15, 15));
    }
}