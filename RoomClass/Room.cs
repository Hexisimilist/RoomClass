﻿namespace RoomClass
{
    public class Room
    {
        public List<Furniture> FurnitureList { get; private set; }
        private Furniture[] Doors { get; set; }
        private Furniture[]? Windows { get; set; }
        public int RoomLength { get; private set; }
        public int RoomWidth { get; private set; }
        public double Penalty { get; private set; }
        //List<Zones> ZonesList { get; private set; } //DEW EET
        public bool WindowsInRoom { get; private set; }
        public Room(int length, int width, Furniture[] doors, List<Furniture> items, bool windowed, Furniture[]? windows = null)
        {
            RoomLength = length;
            RoomWidth = width;

            FurnitureList = new();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not null)
                    FurnitureList.Add(items[i]);
            }

            if (doors is null)
                throw new ArgumentNullException(nameof(doors), "The doors array is null!");
            Doors = doors;
            if (windows is null && windowed)
                throw new ArgumentNullException(nameof(windows), "The windows array is null!");
            Windows = windows;
            WindowsInRoom = true;


            if (FurnitureList.Count == 0)
                throw new Exception("The room has no furniture!");

        }

        public Room(int length, int width, Furniture[] doors, params Furniture[] items)
        {
            RoomLength = length;
            RoomWidth = width;

            FurnitureList = new();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is not null)
                    FurnitureList.Add(items[i]);
            }

            if (doors is null)
                throw new ArgumentNullException(nameof(doors), "The door array is null!");
            Doors = doors;
            WindowsInRoom = false;

            if (FurnitureList.Count == 0)
                throw new Exception("The room has no furniture!");
        }

        public void PenaltyEvaluation()
        {
            for (int i = 0; i < FurnitureList.Count; i++)
            {
                Penalty += OutOfBoundsDeterminer(FurnitureList[i]);

                if (FurnitureList[i].NearWall >= 0)
                    Penalty += NearWallPenalty(FurnitureList[i]);

                for (int k = 0; k < Doors.GetLength(0); k++)
                {
                    if (Collision(Doors[i], FurnitureList[i]))
                        Penalty += 10;
                }

                if (Windows is not null)
                {
                    if (!FurnitureList[i].IgnoreWindows)
                        for (int n = 0; n < Doors.GetLength(0); n++)
                        {
                            if (Collision(Windows[i], FurnitureList[i]))
                                Penalty += 10;
                        }
                }

                for (int j = i + 1; j < FurnitureList.Count; j++)
                {
                    if (Collision(FurnitureList[i], FurnitureList[j]))
                        Penalty += 10;
                }
            }
        }

        private int OutOfBoundsDeterminer(Furniture furniture)
        {
            int fine = 0;

            if (furniture.Center[1] < RoomLength && furniture.Center[0] < RoomWidth &&
                furniture.Center[1] > 0 && furniture.Center[0] > 0)
            {
                for (int j = 0; j < furniture.Vertices.GetLength(0); j++)
                {
                    if (furniture.Vertices[j, 0] > RoomWidth || furniture.Vertices[j, 1] > RoomLength)
                    {
                        fine += 10;
                        furniture.IsOutOfBounds = true;
                        break;
                    }
                    furniture.IsOutOfBounds = false;
                }
            }
            else
            {
                furniture.IsOutOfBounds = true;
                return fine += 10;
            }

            return fine;
        }

        public static bool Collision(Furniture item1, Furniture item2)
        {
            for (int i = 0; i < item2.Vertices.GetLength(0); i++)
            {
                double[] point = new double[] { item2.Vertices[i, 0], item2.Vertices[i, 1] };

                if (DetermineCollision(item1.Vertices, point))
                {
                    item2.IsCollided = true;
                    item1.IsCollided = true;
                    return true;
                }
            }
            item1.IsCollided = false;
            item2.IsCollided = false;
            return false;
        }

        private int NearWallPenalty(Furniture furniture)
        {
            int fine = 0;
            bool check = false;

            for (int i = 0; i < furniture.Vertices.GetLength(0); i++)
            {
                if (furniture.Vertices[i, 1] <= (0 + furniture.NearWall))
                {
                    check = true;
                    if (!(furniture.Rotation >= 260 && furniture.Rotation <= 280))
                        fine += 5;
                    break;
                }
                if (furniture.Vertices[i, 0] <= (0 + furniture.NearWall))
                {
                    check = true;
                    if (!((furniture.Rotation >= 350 && furniture.Rotation < 360) ||
                        (furniture.Rotation >= 0 && furniture.Rotation <= 10)))
                        fine += 5;
                    break;
                }
                if (furniture.Vertices[i, 1] >= (RoomLength - furniture.NearWall))
                {
                    check = true;
                    if (!(furniture.Rotation >= 80 && furniture.Rotation <= 100))
                        fine += 5;
                    break;
                }
                if (furniture.Vertices[i, 0] >= (RoomWidth - furniture.NearWall))
                {
                    check = true;
                    if (!(furniture.Rotation >= 170 && furniture.Rotation < 190))
                        fine += 5;
                    break;
                }
            }

            if (!check)
            {
                fine += 10;
            }

            return fine;
        }

        private static bool DetermineCollision(double[,] vertices, double[] point)
        {
            bool collision = false;

            for (int i = 0, j = vertices.GetLength(0) - 1; i < vertices.GetLength(0); j = i++)
            {
                if (((vertices[i, 1] <= point[1] && point[1] < vertices[j, 1]) ||
                    (vertices[j, 1] <= point[1] && point[1] < vertices[i, 1])) &&
                    point[0] < (vertices[j, 0] - vertices[i, 0]) * (point[1] - vertices[i, 1]) /
                    (vertices[j, 1] - vertices[i, 1]) + vertices[i, 0])
                    collision = !collision;
            }
            return collision;
        }
    }
}