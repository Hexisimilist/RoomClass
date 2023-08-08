﻿namespace Furniture
{
    public class FurnitureDataFlags
    {
        public bool IgnoreWindows { get; protected set; }
        public int NearWall { get; protected set; }                 //Maximum distance allowed between a wall and the furniture object (-1 is set to ignore this charactiristic)
        public int Parent { get; }
        public bool Accessible { get; protected set; }


        public FurnitureDataFlags(bool ignoreWindows = true, int nearWall = 1, int parent = -1, bool accessible = false)
        {
            IgnoreWindows = ignoreWindows;
            NearWall = nearWall;
            Parent = parent;
            Accessible = accessible;
        }
    }
}
