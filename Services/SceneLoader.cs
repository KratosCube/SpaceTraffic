using Avalonia.Controls.Shapes;
using Avalonia.Media;
using SpaceTraffic.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace SpaceTraffic.Services
    {
        public class SceneLoader
        {
        private class Pair
        {
            public string? Name { get; set; }
            public string? Parent { get; set; }

            public Pair()
            {
                Name = "";
                Parent = "";
            }
        }
            public List<Planet> LoadPlanets(string path) {
                string[] lines = File.ReadAllLines(path);
                List<Planet> planets = new List<Planet>();
                List<Pair> planetConnection = new List<Pair>();

                for (int i = 0; i < lines.Length; i++)
                {
                    //vyhledání části Bodies
                    if (lines[i].Contains("[BODIES]"))
                    {

                    for (int j = 1; ; j++)
                        {
                        if (lines[i+j].StartsWith("#")) continue; 
                            // Když se narazí na další sekci tak se loop končí
                            if (lines[i + j].Contains("[DESTINATIONS]"))
                                break;
                            //vytvoření planety z naparsovaných částí !(jeste není nastavený parent)
                            if (!lines[i + j].IsWhiteSpace())
                            {
                                // 0           1           2          3       4            5             6              7              8          9         10
                                // name        type       size    parent       orbit_radius  initial_angle  angular_speed  warehouse  prod_min  prod_max
                                string[] planetData = lines[i + j].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            Planet planet = new Planet()
                            {
                                Name = planetData[0],
                                Type = planetData[1],
                                Size = double.Parse(planetData[2], CultureInfo.InvariantCulture),
                                OrbitRadius = double.Parse(planetData[4], CultureInfo.InvariantCulture),
                                InitialAngle = double.Parse(planetData[5], CultureInfo.InvariantCulture),
                                AngularSpeed = double.Parse(planetData[6], CultureInfo.InvariantCulture),
                                Warehouse = int.Parse(planetData[7], CultureInfo.InvariantCulture),
                                // Pokud je Produkce 0 tak planeta nevytváří žádné zboží. Nemá pak cenu vykreslovat její bar
                                WarehouseMax = int.Parse(planetData[9], CultureInfo.InvariantCulture) == 0 ? 0 : int.Parse(planetData[7], CultureInfo.InvariantCulture) * 2,
                                ProdMin = int.Parse(planetData[8], CultureInfo.InvariantCulture),
                                ProdMax = int.Parse(planetData[9], CultureInfo.InvariantCulture),
                                Color = Brushes.White
                                /*
                                Name = "";
                                Size = 1.0;
                                Parent = null;
                                OrbitRadius = 100.0;
                                InitialAngle = 0.0;
                                AngularSpeed = 0.0;
                                Color = Brushes.White;
                                */
                            };
                                Pair pair = new Pair()
                                {
                                    Name = planetData[0],
                                    Parent = planetData[3]
                                }; 
                                
                                planetConnection.Add(pair);
                                planets.Add(planet);
                            }
                        }
                    }
                }
            SetParents(planets, planetConnection);
            return planets;
            }

        private void SetParents(List<Planet> list, List<Pair> planetConnections)
        {
            foreach (Pair pair in planetConnections)
            {
                // Najdeme planetu podle jejího názvu
                Planet? planet = list.Find(p => p.Name == pair.Name);


                // Slunce nemá rodiče
                if (pair.Parent == "-")
                {
                    planet.Parent = null;
                    continue;
                }

                // Najdeme rodičovskou parent
                Planet? parent = list.Find(p => p.Name == pair.Parent);


                // Nastavíme parent
                planet.Parent = parent;
            }
        }
        public List<Destination> LoadDestinations(string path)
        {
            List<Destination> destinations = new List<Destination>();

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                //vyhledání části Bodies
                if (lines[i].Contains("[DESTINATIONS]"))
                {

                    for (int j = 1; ; j++)
                    {
                        if (lines[i + j].StartsWith("#")) continue;
                        // Když se narazí na další sekci tak se loop končí
                        if (lines[i + j].Contains("[SHIPS]"))
                            break;
                        //vytvoření planety z naparsovaných částí !(jeste není nastavený parent)
                        if (!lines[i + j].IsWhiteSpace())
                        {
                            // 0           1           2
                            // source       destination   probability
                            string[] destinationData = lines[i + j].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            Destination destination= new Destination()
                            {
                                Source = destinationData[0],
                                Target = destinationData[1],
                                Probability = int.Parse(destinationData[2])
                            };
                            
                            destinations.Add(destination);
                        }
                    }
                }
            }

            return destinations;
        }

        public List<Ship> LoadShips(string path)
        {
            List<Ship> ships = new List<Ship>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                //vyhledání části SHIPS
                if (lines[i].Contains("[SHIPS]"))
                {

                    for (int j = 1; i + j < lines.Length; j++)
                    {
                        if (lines[i + j].StartsWith("#")) continue;

                        //vytvoření lodi z naparsovaných částí
                        if (!lines[i + j].IsWhiteSpace())
                        {
                            // 0           1           2           3           4           5                6
                            // name       speed    start       capacity   transfer_speed  strategy       max_loadtime
                            string[] shipData = lines[i + j].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            Ship ship = new Ship()
                            {
                                Name = shipData[0],
                                Speed = double.Parse(shipData[1], CultureInfo.InvariantCulture),
                                Start = shipData[2],
                                Capacity = int.Parse(shipData[3]),
                                TransferSpeed = int.Parse(shipData[4]),
                                Strategy = shipData[5],
                                MaxLoadTime = int.Parse(shipData[6]),
                                RemainingTransfer = int.Parse(shipData[4])
                            };

                            ships.Add(ship);
                        }
                    }
                }
            }

            return ships;
        }

    }
    }
