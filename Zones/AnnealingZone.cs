﻿using Furniture;
using Vertex;

//TODO Add new method to copy data from ExtendedZone -> ZoneClass after Simulated Annealing (likely by ID property)


namespace Zones
{
    public class AnnealingZone : Zone
    {
        public decimal ExtendedWidth { get; set; }
        public decimal ExtendedHeight { get; set; }

        public AnnealingZone(Zone zone) : base(zone)
        {
            ExtendedWidth = zone.Width;
            ExtendedHeight = zone.Height;
        }

        public override void Resize(decimal deltaWidth, decimal deltaHeight)
        {
            ExtendedWidth += deltaWidth;
            ExtendedHeight += deltaHeight;
            Area = (double)(ExtendedWidth * ExtendedHeight);
            VertexManipulator.VertexExpanding(Vertices, deltaWidth, deltaHeight);
        }

        public Zone toZone()
        {

            throw new NotImplementedException();
        }

    }
}
