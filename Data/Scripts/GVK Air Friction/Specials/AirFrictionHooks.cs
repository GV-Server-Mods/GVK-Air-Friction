using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using ServerMod;
using VRage.Game.ModAPI;
using F = System.Func<object, bool>;
using FF = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object, bool>;
using A = System.Action<object>;
using C = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object>;
using U = System.Collections.Generic.List<int>;
using L = System.Collections.Generic.IDictionary<int, float>;

namespace Scripts.Specials.ShipClass
{
    
    
    public class AirFrictionHooks
    {
        private static Func<IMyCubeGrid, double[]> getGridFriction = GetGridFriction;
        
        /// <summary>
        /// Must be inited in LoadData of MySessionComponentBase
        /// </summary>
        public static void Init()
        {
            ModConnection.SetValue("MIG.AirFriction.GetGridFriction", getGridFriction);
        }
        
        public static double[] GetGridFriction(IMyCubeGrid grid)
        {
            Ship ship;
            if (!Ship.AllShipsByGrid.TryGetValue(grid, out ship))
            {
                return null;
            }

            var values = new double[4];
            AirFriction.UpdateUiStatsShip(ship, values);
            return values;
        }
    }
}