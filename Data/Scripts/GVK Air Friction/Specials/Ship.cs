using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.ModAPI;

namespace ServerMod
{
    public class Ship : MyGridGroupsDefaultEventHandler
    {
        private static Guid GUID = new Guid("467cf810-1bf0-4135-89ad-152cc64ed6b3");
        public HashSet<MyCubeGrid> Grids = new HashSet<MyCubeGrid>();
        public MassCache MassCache;
        public static HashSet<Ship> AllShips = new HashSet<Ship>();
        public static Dictionary<IMyCubeGrid, Ship> AllShipsByGrid = new Dictionary<IMyCubeGrid, Ship>();

        public int Id;
        public static int LastId;

        public Ship(IMyGridGroupData obj) : base(obj)
        {
            lock (typeof(Ship))
            {
                Id = ++LastId;
            }
            
            MassCache = new MassCache(this);
            AllShips.Add(this);

            var list = new List<IMyCubeGrid>();
            foreach (var grid in obj.GetGrids(list))
            {
                AllShipsByGrid[grid] = this;
            }
        }

        protected override void OnGridAdded(IMyCubeGrid arg2, IMyGridGroupData prevGroup)
        {
            var grid = (MyCubeGrid) arg2;
            Grids.Add(grid);
            AllShipsByGrid[arg2] = this;
            MassCache.DropCache();
            base.OnGridAdded(arg2, prevGroup);
        }

        protected override void OnGridMerged(IMyCubeGrid baseGrid, IMyCubeGrid merged)
        {
            MassCache.DropCache();
            AllShipsByGrid[merged] = this;
            base.OnGridMerged(baseGrid, merged);
        }

        protected override void OnGridRemoved(IMyCubeGrid arg2, IMyGridGroupData nextGroup)
        {
            var grid = (MyCubeGrid) arg2;
            Grids.Remove(grid);
            MassCache.DropCache();
            
            base.OnGridRemoved(arg2, nextGroup);
        }

        protected override void OnGridSplited(IMyCubeGrid basegrid, IMyCubeGrid removedGrid)
        {
            var grid = (MyCubeGrid) removedGrid;
            Grids.Remove(grid);
            MassCache.DropCache();
            base.OnGridSplited(basegrid, removedGrid);
        }

        protected override void OnReleased()
        {
            base.OnReleased();
            AllShips.Remove(this);
        }

        protected override Guid GetGuid()
        {
            return GUID;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var grid in Grids)
            {
                sb.Append(
                    $"Grid [{(grid.GridSizeEnum == MyCubeSize.Large ? "L" : "S")}{grid.DisplayNameText} {grid.BlocksCount}]");
            }
            return $"Ship {Id} Grids {Grids.Count} : {sb.ToString()}";
        }
    }
}