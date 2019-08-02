using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<ZoneType> ZoneTypes = null;
        private static Dictionary<string, Func<ZoneType, WorldManager, Zone>> ZoneFactoryFunctions = null;
        private static bool ZoneTypesInitialized = false;

        private static void InitializeZoneTypes()
        {
            if (ZoneTypesInitialized)
                return;
            ZoneTypesInitialized = true;

            ZoneTypes = FileUtils.LoadJsonListFromDirectory<ZoneType>(ContentPaths.room_types, null, d => d.Name);
            ZoneFactoryFunctions = new Dictionary<string, Func<ZoneType, WorldManager, Zone>>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(ZoneFactoryAttribute), typeof(Zone), new Type[]
            {
                typeof(ZoneType),
                typeof(WorldManager)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is ZoneFactoryAttribute) as ZoneFactoryAttribute;
                if (attribute == null) continue;
                ZoneFactoryFunctions[attribute.Name] = (data, world) => method.Invoke(null, new Object[] { data, world }) as Zone;
            }
        }

        public static MaybeNull<ZoneType> GetZoneType(string Name)
        {
            InitializeZoneTypes();
            return ZoneTypes.Where(r => r.Name == Name).FirstOrDefault();
        }
      
        public static MaybeNull<Zone> CreateZone(string name, WorldManager world)
        {
            InitializeZoneTypes();
            if (ZoneFactoryFunctions.ContainsKey(name) && GetZoneType(name).HasValue(out var zoneType))
                return ZoneFactoryFunctions[name](zoneType, world);
            return null;
        }

        public static IEnumerable<ZoneType> EnumerateZoneTypes()
        {
            InitializeZoneTypes();
            return ZoneTypes;
        }
    }
}
