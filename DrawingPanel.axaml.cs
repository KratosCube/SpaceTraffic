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
    private readonly int m_StartTime;

    // Měřítko pro převod simulovaných souřadnic na souřadnice pro vykreslování
    private double scale = 60;
    private readonly List<Planet> planets;
    private readonly List<Destination> destanations;
    private readonly List<Ship> ships;

    private readonly Simulation simulation;

    // Seznam obsazených pozic popisků na obrazovce
    private readonly List<Rect> occupiedLabels = new();

    // Poslední časový okamžik, kdy byla provedena simulace
    private long lastSimulationTick;
    // Interval simulace - 1 sekunda
    private double simulationIntervalMs = 1000.0;
    public DrawingPanel(string scenario)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "data", scenario);
        InitializeComponent();
        // Timer pro výpočet plynulého pohybu planet a lodí
        m_StartTime = Environment.TickCount;
        var timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) => this.InvalidateVisual();
        timer.Start();

        // Timer pro simulaci
        var simulationTimer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        SceneLoader loader = new SceneLoader();
        planets = loader.LoadPlanets(path);
        destanations = loader.LoadDestinations(path);
        ships = loader.LoadShips(path);

        // Nastavení počáteční pozice lodi na základě její startovní planety
        foreach (var ship in ships)
        {
            ship.DockedAt = planets.Find(p => p.Name == ship.Start);
        }

        // Vytvoření instance simulace
        simulation = new Simulation(planets, ships, destanations);

        lastSimulationTick = Environment.TickCount64;

        // Nastavení události Tick pro simulaci
        simulationTimer.Tick += (_, _) =>
        {
            long now = Environment.TickCount64;

            simulationIntervalMs = now - lastSimulationTick;
            lastSimulationTick = now;

            // Provedení simulace pro jeden den
            simulation.SimulateDay();
        };

        simulationTimer.Start();



    }


    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Vymazání uložených pozic labelů z předchozího vykreslení
        occupiedLabels.Clear();

        // Vykreslení černého pozadí přes celé okno
        var rect = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Black, rect);

        // Výpočet největší vzdálenosti tělesa od středu soustavy
        double maxDistance = planets.Max(p => GetMaxOrbitDistance(p));

        // Přizpůsobení měřítka velikosti okna při zachování poměru souřadnic
        scale = ((Math.Min(Bounds.Width, Bounds.Height) * 0.45) / maxDistance);

        // Zjištění, jaká část simulačního dne již uplynula
        double progress = GetDayProgress();

        // Výpočet času pro plynulé vykreslování pohybu
        double elapsed = (simulation.CurrentDate - new DateTime(2126, 9, 21)).TotalDays + progress;

        // Posunutí souřadnicového systému do středu okna
        using (context.PushTransform(Matrix.CreateTranslation(Bounds.Width * 0.5, Bounds.Height * 0.5)))
        {
            // Vykreslení všech vesmírných těles a jejich oběžných drah
            foreach (var planet in planets)
            {
                DrawPlanet(context, planet, scale, elapsed);
            }

            // Vykreslení všech transportních lodí a jejich aktuálního stavu
            foreach (Ship ship in ships)
            {
                DrawShip(context, ship, scale, elapsed, progress);
            }
        }

        // Vykreslení aktuálního simulačního data v levém horním rohu okna
        DrawDate(context);
    }


    // Metoda pro získání progresu dne jako hodnoty mezi 0 a 1
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

    // Metoda pro získání maximální vzdálenosti planety od středu soustavy
    // Pokud má planeta rodiče, tak se vzdálenost sčítá s vzdáleností rodiče
    private double GetMaxOrbitDistance(Planet planet)
    {
        double distance = planet.OrbitRadius;

        if (planet.Parent != null)
            distance += GetMaxOrbitDistance(planet.Parent);

        return distance;
    }

    // Metoda pro vykreslení planety a její oběžné dráhy
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
                DrawOrbit(context, planet, scale);
            }
        }

        // Vykreslení planet a měsíců
        using (context.PushTransform(Matrix.CreateTranslation(planetPosition.X * scale, planetPosition.Y * scale)))
        {
            DrawBody(context, planet);
            DrawPlanetLabel(context, planet, planet.Size * scale * 0.2, new Point(planetPosition.X * scale, planetPosition.Y * scale));
        }
    }

    // Vykreslení těla planety
    private void DrawBody(DrawingContext context, Planet planet)
    {
        double planetSize = planet.Size * scale * 0.2;
        context.DrawEllipse(planet.Color, null, new Rect(-planetSize, -planetSize, 2 * planetSize, 2 * planetSize));
    }

    // Vykreslení oběžné dráhy planety
    private void DrawOrbit(DrawingContext context, Planet planet, double scale)
    {

        Pen dashedPen = new Pen(Brushes.Gray, 1)
        {
            DashStyle = DashStyle.Dash
        };

        context.DrawEllipse(null, dashedPen,
            new Rect(-planet.OrbitRadius * scale, -planet.OrbitRadius * scale, 2 * planet.OrbitRadius * scale, 2 * planet.OrbitRadius * scale));

    }


    // Metoda pro vykreslení lodi
    private void DrawShip(DrawingContext context, Ship ship, double scale, double elapsed, double progress)
    {


        Point shipPosition;

        // Pokud je loď zakotvená, pohybuje se společně s planetou.
        if (ship.DockedAt != null && !ship.JustDocked)
        {
            //shipPosition = new Point(ship.DockedAt.GetPosition(elapsed).X + ship.DockedAt.Size , ship.DockedAt.GetPosition(elapsed).Y);
            shipPosition = ship.DockedAt.GetPosition(elapsed);
        }
        // Pokud loď letí, použijeme pozici vypočítanou v Simulation.MoveShip().
        else
        {

            //interpolovana pozice
            shipPosition = ship.InterpolatePosition(ship, progress);
        }
        Point shipScreenPosition = new Point(
            shipPosition.X * scale,
            shipPosition.Y * scale
        );

        // Převod světových souřadnic na souřadnice vykreslování.
        using (context.PushTransform(Matrix.CreateTranslation(shipPosition.X * scale, shipPosition.Y * scale)))
        {
            double shipSize = 2.5;

            context.DrawEllipse(Brushes.Red, null, new Rect(-shipSize, -shipSize, 2 * shipSize, 2 * shipSize));
            DrawShipLabel(context, ship, shipScreenPosition);
        }
    }

    // Metoda pro vykreslení popisku lodi
    private void DrawShipLabel(DrawingContext context, Ship ship, Point shipScreenPosition)
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
        else if (ship.State == "Unloading" && !ship.JustDocked)
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
        // Najdeme první volnou pozici
        Point? selectedPosition = FindLabelPosition(shipScreenPosition, 2.5, labelWidth, labelHeight, margin);

        if (selectedPosition == null)
            return;

        Rect selectedCandidate = new Rect(
            shipScreenPosition.X + selectedPosition.Value.X,
            shipScreenPosition.Y + selectedPosition.Value.Y,
            labelWidth,
            labelHeight
        );

        // Přidání do seznamu osazených pozic labely na obrazovce
        occupiedLabels.Add(selectedCandidate);

        // Posun celého popisku na vybranou pozici
        using (context.PushTransform(Matrix.CreateTranslation(selectedPosition.Value.X, selectedPosition.Value.Y)))
        {
            // Šipka se nyní přesouvá společně s popiskem
            if (shipState != null)
            {
                double arrowY = (labelHeight - shipState.Height) / 2;

                context.DrawText(shipState, new Point(0, arrowY));
            }

            // Obsah začíná napravo od šipky
            double contentX = arrowWidth;

            // Název lodi
            context.DrawText(shipName, new Point(contentX, 0));

            double barY = shipName.Height + spacing;

            DrawBar(context, contentX, barY, 60, 16, ship.FillRatio(), ship.Load, ship.Capacity);


        }
    }

    // Metoda pro vykreslení popisku planety
    private void DrawPlanetLabel(DrawingContext context, Planet planet, double radius, Point planetScreenPosition)
    {
        // Pokud planeta produkuje nějaké zboží tak vykresluji i její staty.
        FormattedText planetName = new FormattedText(
            planet.Name,
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

        // Rozměry celého popisku včetně ukazatele
        // Pokud planeta nemá ukazatel skladu tak se nezapočítává
        bool showBar = planet.WarehouseMax != 0;

        double labelWidth = showBar ? Math.Max(planetName.Width, barWidth) : planetName.Width;

        double labelHeight = planetName.Height;

        if (showBar)
        {
            labelHeight += spacing + barHeight;
        }

        // Možné pozice vzhledem ke středu planety
        Point? selectedPosition = FindLabelPosition(planetScreenPosition, radius, labelWidth, labelHeight, margin);

        if (selectedPosition == null)
            return;

        Rect candidate = new Rect(planetScreenPosition.X + selectedPosition.Value.X, planetScreenPosition.Y + selectedPosition.Value.Y, labelWidth, labelHeight);

        occupiedLabels.Add(candidate);

        // Posuneme název i ukazatel společně
        using (context.PushTransform(Matrix.CreateTranslation(selectedPosition.Value.X, selectedPosition.Value.Y)))
        {
            // Název a množství ve skladu
            context.DrawText(planetName, new Point(0, 0));

            // Ukazatel přímo pod textem
            if (showBar)
                DrawBar(context, 0, planetName.Height + spacing, barWidth, barHeight, planet.FillRatio(), planet.Warehouse, planet.WarehouseMax);
        }
    }

    // Metoda pro kontrolu, zda je pozice popisku volná
    private bool IsLabelPositionFree(Rect candidate)
    {
        double margin = 5;

        // Hranice viditelné oblasti
        double left = -Bounds.Width / 2 + margin;
        double right = Bounds.Width / 2 - margin;
        double top = -Bounds.Height / 2 + margin;
        double bottom = Bounds.Height / 2 - margin;

        // Kontrola, zda popisek nepřesahuje okno
        if (candidate.Left < left || candidate.Right > right || candidate.Top < top || candidate.Bottom > bottom)
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

    // Metoda pro nalezení vhodné pozice popisku vzhledem k objektu
    private Point? FindLabelPosition(Point objectPosition, double radius, double labelWidth, double labelHeight, double margin)
    {
        for (double extra = 0; extra <= 160; extra += 20)
        {
            double distance = radius + margin + extra;

            Point[] positions =
            {
                new Point(distance, -labelHeight / 2),
                new Point(-distance - labelWidth, -labelHeight / 2),
                new Point(-labelWidth / 2, -distance - labelHeight),
                new Point(-labelWidth / 2, distance),
                new Point(distance, -distance - labelHeight),
                new Point(distance, distance),
                new Point(-distance - labelWidth, -distance - labelHeight),
                new Point(-distance - labelWidth, distance)
            };

            // Procházení všech možných pozic a hledání první volné
            foreach (Point position in positions)
            {
                Rect candidate = new Rect(objectPosition.X + position.X, objectPosition.Y + position.Y, labelWidth, labelHeight);

                if (IsLabelPositionFree(candidate))
                    return position;
            }
        }

        return null;
    }

    // Metoda pro vykreslení baru s aktuálním a maximálním množstvím
    private void DrawBar(DrawingContext context, double x, double y, double width, double height, double fillRatio, int currentValue, int maxValue)
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
        context.DrawText(current, new Point(x + 2, y));

        // Maximum vpravo
        context.DrawText(maxCapacity, new Point(x + width - maxCapacity.Width - 2, y)
        );

    }


    //Vykreslení data simulovaného dne
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