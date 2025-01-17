﻿using Furniture;
using Interfaces;
using Vertex;
using Zones;


namespace RoomClass.Zones
{

    public class SolutionClass
    {
        public List<AnnealingZone> Zones { get; set; }
        private List<GeneralFurniture> Doors { get; set; }

        public int Aisle { get; set; }
        public double Cost { get; set; }

        public int RoomWidth { get; private set; }
        public int RoomHeight { get; private set; }


        public SolutionClass(List<AnnealingZone> zones, int aisle, int roomWidth, int roomHeight, List<GeneralFurniture> doors)
        {
            Aisle = aisle;
            RoomWidth = roomWidth;
            RoomHeight = roomHeight;
            Doors = doors;
            Zones = zones;

            //foreach (var zone in zones)
            //{
            //    zone.Center[0] = RoomWidth / 2;
            //    zone.Center[1] = RoomHeight / 2;
            //    VertexManipulator.VertexResetting(zone.Vertices, zone.Center, zone.Width, zone.Height);
            //}

            Cost = FindCost();
        }

        public void PrepareSolutionForSA()
        {
            Random random = new();
            AnnealingZone annealingZone = null;
            List<AnnealingZone> zones = new List<AnnealingZone>(Zones);
            decimal[] offset = new decimal[2];

            // ←↑
            annealingZone = zones[random.Next(zones.Count)];
            offset[0] = annealingZone.Vertices[1, 0];
            offset[1] = annealingZone.Vertices[1, 1];
            annealingZone.Move(-offset[0], -offset[1]);
            zones.Remove(annealingZone);

            // ↑→
            annealingZone = zones[random.Next(zones.Count)];
            offset[0] = RoomWidth - annealingZone.Vertices[0, 0];
            offset[1] = annealingZone.Vertices[0, 1];
            annealingZone.Move(offset[0], -offset[1]);
            zones.Remove(annealingZone);

            // ↓←
            annealingZone = zones[random.Next(zones.Count)];
            offset[0] = annealingZone.Vertices[2, 0];
            offset[1] = RoomHeight - annealingZone.Vertices[2, 1];
            annealingZone.Move(-offset[0], offset[1]);
            zones.Remove(annealingZone);

            // ↓→
            annealingZone = zones[random.Next(zones.Count)];
            offset[0] = RoomWidth - annealingZone.Vertices[3, 0];
            offset[1] = RoomHeight - annealingZone.Vertices[3, 1];
            annealingZone.Move(offset[0], offset[1]);
            zones.Remove(annealingZone);

        }


        public static SolutionClass GenerateNeighbour(double maxStep, SolutionClass initialSolution)
        {
            Random random = new Random();
            int randomZoneNumber = random.Next(initialSolution.Zones.Count);
            //Take a random zone
            //TODO To make sure about deep copy here
            AnnealingZone neighbourZone;
            List<AnnealingZone> deepZonesCopy = new(initialSolution.Zones.ToList());

            for (int i = random.Next(1, initialSolution.Zones.Count); i > 0; i--)
            {
                while (true)
                {
                    neighbourZone = new AnnealingZone(initialSolution.Zones[randomZoneNumber]);
                    RandomizeZone(neighbourZone, (decimal)maxStep);
                    if (VertexManipulator.IsPolygonInsideRoom(neighbourZone, initialSolution.RoomWidth, initialSolution.RoomHeight))
                    {
                        deepZonesCopy[randomZoneNumber] = neighbourZone;
                        break;
                    }
                    randomZoneNumber = random.Next(initialSolution.Zones.Count);
                }
            }
            //Do a random action (moving or resizing)

            return new SolutionClass(deepZonesCopy, initialSolution.Aisle, initialSolution.RoomWidth, initialSolution.RoomHeight, initialSolution.Doors);

        }

        public double FindCost()
        {
            double cost = 0;

            cost += OverlappingPenalty();
            cost += FreeSpacePenalty();
            cost += ZoneShapePenalty();
            cost += SpaceRatioPenalty();
            cost += ByWallPenalty();
            cost += DoorSpacePenalty();

            return cost;
        }

        private double OverlappingPenalty()
        {
            double area = 0;

            for (int i = 0; i < Zones.Count - 1; i++)
            {
                for (int j = i + 1; j < Zones.Count; j++)
                {
                    VertexManipulator.VertexExpanding(Zones[j].Vertices, Aisle);

                    if (DeterminRectangleCollision(Zones[i], Zones[j]))
                    {
                        area += FindOverlapArea(Zones[i], Zones[j]);
                    }

                    VertexManipulator.VertexExpanding(Zones[j].Vertices, -Aisle);
                }
            }

            return Math.Sqrt(area);
        }

        private double FreeSpacePenalty()
        {
            double area = 0;
            foreach (var item in Zones)
            {
                item.Resize(Aisle * 2, Aisle * 2);
                area += item.Area;
                item.Resize(-Aisle * 2, -Aisle * 2);
            }
            return Math.Sqrt(RoomHeight * RoomWidth - area);
        }

        private double ZoneShapePenalty()
        {
            double penalty = 0;

            foreach (var item in Zones)
            {
                if (item.ExtendedHeight > item.ExtendedWidth * 3 || item.ExtendedWidth > item.ExtendedHeight * 3)
                {
                    decimal maxDim = Math.Max(item.ExtendedWidth, item.ExtendedHeight);
                    decimal minDim = Math.Min(item.ExtendedWidth, item.ExtendedHeight);

                    penalty += (double)((maxDim - 3 * minDim) / 4);
                }
            }
            return penalty;
        }

        private double SpaceRatioPenalty()
        {
            double penalty = 0;
            double allFurnituresArea = Zones.Select(x => x.FurnitureArea).Sum();
            double allZonesArea = Zones.Select(x => x.Area).Sum();

            foreach (var item in Zones)
            {
                penalty += Math.Abs((item.FurnitureArea / allFurnituresArea) - (item.Area / allZonesArea));
            }
            return penalty;
        }

        private double ByWallPenalty()
        {
            double penalty = 0;
            List<double> distances = new List<double>();

            foreach (var item in Zones)
            {
                distances.Clear();
                distances = FindDistances(item.Vertices);


                penalty += distances.Min();
                distances.Remove(distances.Min());
                penalty += distances.Min();
                distances.Remove(distances.Min());
                penalty += distances.Min() / 2;

            }

            return penalty;
        }

        private double DoorSpacePenalty()
        {

            double overlapArea = 0;

            foreach (var item in Doors)
            {
                VertexManipulator.VertexExpanding(item.Vertices, 0, Aisle);
            }


            foreach (var item in Zones)
            {
                foreach (var itemDoor in Doors)
                {
                    overlapArea += FindOverlapArea<IPolygon>(itemDoor, item);
                }

            }


            foreach (var item in Doors)
            {
                VertexManipulator.VertexExpanding(item.Vertices, 0, -Aisle);
            }

            return Math.Sqrt(overlapArea);
        }


        //private static bool IsVertexInsideRoom(decimal x, decimal y, int roomWidth, int roomHeight)
        //{
        //    if (x > roomWidth || x < 0)
        //        return false;

        //    if (y > roomHeight || y < 0)
        //        return false;

        //    return true;
        //}

        //private static bool IsZoneInsideRoom(IPolygon zone, int roomWidth, int roomHeight)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        if (!IsVertexInsideRoom(zone.Vertices[i, 0], zone.Vertices[i, 1], roomWidth, roomHeight))
        //            return false;
        //    }
        //    return true;
        //}

        private List<double> FindDistances(decimal[,] vertices)
        {
            List<double> result = new List<double>();
            double distanceX;
            double distanceY;

            for (int i = 0; i < 4; i++)
            {
                distanceX = double.MaxValue;
                distanceY = double.MaxValue;

                // An offset to find the distance to the closest corner

                if ((double)(RoomWidth - vertices[i, 0]) < distanceX)
                    distanceX = (double)(RoomWidth - vertices[i, 0]);

                // choosing the closest wall
                if ((double)(vertices[i, 0]) < distanceX)
                    distanceX = (double)(vertices[i, 0]);

                // distance to the right or left wall
                result.Add(distanceX);

                if ((double)(RoomHeight - vertices[i, 1]) < distanceY)
                    distanceY = (double)(RoomWidth - vertices[i, 1]);

                if ((double)(vertices[i, 1]) < distanceY)
                    distanceY = (double)(vertices[i, 1]);

                // distance to the upper or bottom wall
                result.Add(distanceY);

                //Pifagor's Theorem
                result.Add(Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2)));
            }

            return result;
        }


        private double FindOverlapArea<T>(T zone1, T zone2) where T : IPolygon
        {
            /*
    x1, y1 - левая нижняя точка первого прямоугольника
    x2, y2 - правая верхняя точка первого прямоугольника
    x3, y3 - левая нижняя точка второго прямоугольника
    x4, y4 - правая верхняя точка второго прямоугольника
            */


            decimal left = Math.Max(zone1.Vertices[1, 0], zone2.Vertices[1, 0]);
            decimal top = Math.Min(zone1.Vertices[3, 1], zone2.Vertices[3, 1]);
            decimal right = Math.Min(zone1.Vertices[3, 0], zone2.Vertices[3, 0]);
            decimal bottom = Math.Max(zone1.Vertices[1, 1], zone2.Vertices[1, 1]);

            decimal width = right - left;
            decimal height = top - bottom;

            if (width < 0 || height < 0)
                return 0;

            return (double)(width * height);
        }

        private bool DeterminRectangleCollision(AnnealingZone rect1, AnnealingZone rect2)
        {
            if (
                rect1.Center[0] - rect1.ExtendedWidth / 2 < rect2.Center[0] + rect2.ExtendedWidth / 2 &&
                rect1.Center[0] + rect1.ExtendedWidth / 2 > rect2.Center[0] - rect2.ExtendedWidth / 2 &&
                rect1.Center[1] - rect1.ExtendedHeight / 2 < rect2.Center[1] + rect2.ExtendedHeight / 2 &&
                rect1.ExtendedHeight / 2 + rect1.Center[1] > rect2.Center[1] - rect2.ExtendedHeight / 2
              )
                return true;
            return false;
        }

        private static bool RandomBoolean()
        {
            Random random = new();
            if (random.Next(2) > 0)
            {
                return true;
            }
            return false;
        }

        private static void RandomizeZone(AnnealingZone randomZone, decimal maxStep)
        {
            Random random = new();

            if (RandomBoolean())
            {
                //TODO Evade collision between zone and room for each of (resizing or moving)
                //resizing options

                if (randomZone.isStorage == true)
                {
                    RandomizeZone(randomZone, maxStep);
                    return;
                }

                switch (random.Next(6))
                {

                    case 0:
                        randomZone.Resize((decimal)maxStep, (decimal)maxStep);
                        break;

                    case 1:
                        randomZone.Resize(0, (decimal)maxStep);
                        break;

                    case 2:
                        randomZone.Resize((decimal)maxStep, 0);
                        break;

                    case 3:
                        randomZone.Resize(-(decimal)maxStep, -(decimal)maxStep);
                        break;

                    case 4:
                        randomZone.Resize(0, -(decimal)maxStep);
                        break;

                    case 5:
                        randomZone.Resize(-(decimal)maxStep, 0);
                        break;

                    default:
                        break;
                }

            }

            else
            {
                //moving options

                switch (random.Next(6))
                {

                    case 0:
                        randomZone.Center[0] += (decimal)maxStep;
                        break;

                    case 1:
                        randomZone.Center[1] += (decimal)maxStep;
                        break;

                    case 2:
                        randomZone.Center[0] += (decimal)maxStep;
                        randomZone.Center[1] += (decimal)maxStep;
                        break;

                    case 3:
                        randomZone.Center[0] -= (decimal)maxStep;
                        break;

                    case 4:
                        randomZone.Center[1] -= (decimal)maxStep;
                        break;

                    case 5:
                        randomZone.Center[0] -= (decimal)maxStep;
                        randomZone.Center[1] -= (decimal)maxStep;
                        break;

                    default:
                        break;
                }

                VertexManipulator.VertexResetting(randomZone.Vertices, randomZone.Center, (int)randomZone.ExtendedWidth, (int)randomZone.ExtendedHeight);

            }


        }

    }
}
