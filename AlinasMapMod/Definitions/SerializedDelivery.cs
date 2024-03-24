using System;
using Game.Progression;
using Model.OpsNew;

namespace AlinasMapMod.Definitions
{
    public class SerializedDelivery
    {
        public int Direction { get; set; } = 0;
        public int Count { get; set; } = 0;
        public string Load { get; set; } = "";
        public string CarTypeFilter { get; set; } = "";

        public SerializedDelivery()
        {
        }
        public SerializedDelivery(Section.Delivery d)
        {
            Direction = (int)d.direction;
            Count = d.count;
            Load = d.load.id;
            CarTypeFilter = d.carTypeFilter.queryString;
        }

        internal void ApplyTo(Section.Delivery delivery, ObjectCache cache)
        {
            delivery.direction = (Section.Delivery.Direction)Direction;
            delivery.count = Count;
            delivery.load = cache.Loads[Load];
            delivery.carTypeFilter = new CarTypeFilter(CarTypeFilter);
        }
    }
}