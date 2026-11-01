using Avalonia;
using SpaceTraffic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SpaceTraffic.Simulations
{
    public class Simulation
    {
        private readonly List<Planet> planets;
        private readonly List<Ship> ships;
        private readonly List<Destination> destinations;
        public DateTime CurrentDate { get; private set; } = new DateTime(2026, 9, 21);

        public Simulation(
            List<Planet> planets,
            List<Ship> ships,
            List<Destination> destinations)
        {
            this.planets = planets;
            this.ships = ships;
            this.destinations = destinations;
        }

        //Simulace generovaní do skladů
        public void SimulateDay()
        {
            // Přidání jednoho dne k aktuálnímu datu
            CurrentDate = CurrentDate.AddDays(1);
            foreach (Planet planet in planets)
            {

                // Generování nákladu do skladu planety, pokud není plný
                if (planet.Warehouse < planet.WarehouseMax)
                {
                    int generatedLoad = Random.Shared.Next(planet.ProdMin, planet.ProdMax + 1);

                    if (planet.WarehouseMax - planet.Warehouse < generatedLoad)
                        planet.Warehouse = planet.WarehouseMax;
                    else
                        planet.Warehouse += generatedLoad;
                }

                // Debug.WriteLine($"{planet.Name}: {planet.Warehouse}");
            }
            // Rozlišení stavu každé lodi a provedení příslušné akce
            foreach (Ship ship in ships)
            {
                ship.JustDocked = false;
                ship.RemainingTransfer = ship.TransferSpeed;
                if (ship.State == "ReadyToDepart")
                {
                    DepartShip(ship);
                }
                else if (ship.State == "Loading")
                {
                    LoadShip(ship);
                }
                else if (ship.State == "Flying")
                {
                    MoveShip(ship);
                }
                else if (ship.State == "Unloading")
                {
                    UnloadShip(ship);
                }
            }


            //Debug.WriteLine("--------------------");


            
            
        }

        // Metoda pro naložení nákladu na loď
        private void LoadShip(Ship ship)
        {
            if (ship.State != "Loading")
                return;
            if (ship.DockedAt == null)
                return;

            Planet planet = ship.DockedAt;

            // Výpočet množství nákladu, které lze naložit na loď
            int load = Math.Min(
                ship.Capacity - ship.Load,
                Math.Min(ship.RemainingTransfer, planet.Warehouse));


            // Výpočet maximálního množství nákladu, které lze naložit na loď podle strategie
            int possibleLoad = Math.Min(ship.Capacity - ship.Load, ship.RemainingTransfer);

            // Přičtení do nákladu lodi
            ship.Load += load;

            // Odečtení ze skladu planety
            planet.Warehouse -= load;
            Debug.WriteLine($"Ship {ship.Name} loaded {load} units from {planet.Name}. Ship load: {ship.Load}, Planet warehouse: {planet.Warehouse}");

            // Kontrola strategie lodi a případné změny stavu
            switch (ship.Strategy)
            {
                case "FULL":
                    if (ship.Load == ship.Capacity)
                        ship.State = "ReadyToDepart";
                    break;

                case "IMMEDIATE":
                    if (ship.Load > 0 && (ship.Load == ship.Capacity || (ship.Load > 0 && planet.Warehouse < possibleLoad)))
                        ship.State = "ReadyToDepart";
                    break;

                case "MAX_LOADTIME":
                    if (ship.Load > 0 && (ship.Load == ship.Capacity || ship.LoadTime == ship.MaxLoadTime))
                    {
                        ship.State = "ReadyToDepart";
                        ship.LoadTime = 0;
                    }
                    else
                    {
                        ship.LoadTime++;
                    }
                    break;

                default:
                    Debug.WriteLine($"Unknown strategy {ship.Strategy} for ship {ship.Name}");
                    break;
            }
        }


        // Metoda pro vyložení nákladu z lodi
        private void UnloadShip(Ship ship)
        {
            if (ship.State != "Unloading")
                return;
            if (ship.DockedAt == null)
                return;

            // Výpočet množství nákladu, které lze vyložit z lodi
            int unload = Math.Min(ship.Load, ship.TransferSpeed);

            
            ship.RemainingTransfer = ship.TransferSpeed - unload;

            ship.Load -= unload;

            // Nastavení stavu lodi na "Loading" po vyložení nákladu
            if (ship.Load == 0)
            {
                
                ship.State = "Loading";
                if (ship.RemainingTransfer > 0)
                {
                    LoadShip(ship);
                }
            }
                
        }


        // Metoda pro výběr cílové destinace pro loď
        private Destination? SelectDestination(Ship ship)
        {
            if (ship.DockedAt == null)
                return null;

            // Filtrace destinací podle aktuální planety (source), kde je loď zakotvena
            List<Destination> possibleDestinations =
                destinations.Where(d => d.Source == ship.DockedAt.Name).ToList();

            int randomValue = Random.Shared.Next(0, 100);
            int propabilitySum = 0;
            Destination? selectedDestination = null;

            // Výběr destinace na základě náhodného čísla a pravděpodobností
            for (int i = 0; i < possibleDestinations.Count; i++)
            {
                propabilitySum += possibleDestinations[i].Probability;
                if (randomValue < propabilitySum)
                {
                    selectedDestination = possibleDestinations[i];
                    break;
                }
            }
            return selectedDestination;

        }

        // Metoda pro odlet lodi z planety a nastaveníjejího cíle
        private void DepartShip(Ship ship)
        {
            if (ship.DockedAt == null)
                return;
            // Výběr cílové destinace pro loď
            Destination? destination = SelectDestination(ship);
            if (destination == null)
                return;

            Planet? targetPlanet = planets.Find(p => p.Name == destination.Target);
            if (targetPlanet == null)
                return;
            ship.Target = targetPlanet;

            // Výpočet pozice lodi na základě aktuálního času a pozice cílové planety
            double elapsed = (CurrentDate - new DateTime(2026, 9, 21)).TotalDays;

            ship.Position = ship.DockedAt.GetPosition(elapsed);
            ship.PreviousPosition = ship.Position;

            ship.DockedAt = null;
            ship.State = "Flying";
            
            Debug.WriteLine($"Ship {ship.Name} departed from {destination.Source} to {destination.Target}. Load: {ship.Load}");
            MoveShip(ship);

        }

        // Metoda pro pohyb lodi směrem k cílové planetě
        private void MoveShip(Ship ship)
        {
            double elapsed = (CurrentDate - new DateTime(2026, 9, 21)).TotalDays;
            if (ship.State != "Flying" || ship.Target == null)
                return;
            Planet targetPlanet = ship.Target;
            Point targetPosition = ship.Target.GetPosition(elapsed);
            Point shipPosition = ship.Position;

            // Výpočet vzdálenosti mezi pozicí lodi a cílovou planetou
            Point dxy = new Point(targetPosition.X - shipPosition.X, targetPosition.Y - shipPosition.Y);
            double distance = Math.Sqrt(dxy.X * dxy.X + dxy.Y * dxy.Y);

            // Pokud je vzdálenost menší nebo rovna rychlosti lodi, loď "dorazí" na cílovou planetu
            if (distance <= ship.Speed)
            {
                ship.PreviousPosition = ship.Position;
                ship.Position = targetPosition;
                ship.DockedAt = targetPlanet;
                ship.Target = null;
                ship.State = "Unloading";
                ship.LoadTime = 0;
                ship.JustDocked = true;
                Debug.WriteLine($"Ship {ship.Name} arrived at {targetPlanet.Name}. Load: {ship.Load}");
                return;
            }

            // Výpočet směru pohybu lodi směrem k cílové planetě
            if (distance == 0)
                return;
            double directionX = dxy.X / distance;
            double directionY = dxy.Y / distance;

            double moveDistance = ship.Speed;

            double newX = shipPosition.X + directionX * moveDistance;
            double newY = shipPosition.Y + directionY * moveDistance;
            ship.PreviousPosition = ship.Position;
            ship.Position = new Point(newX, newY);
        }


    }
}
