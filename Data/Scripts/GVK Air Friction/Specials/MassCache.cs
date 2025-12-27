using System.Collections.Generic;
using MIG.Shared.SE;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod
{
    public class MassCache {
        private int lastCalculated;
        private Vector3D com;
        private double mass;
        private Ship ship;

        public Vector3D CenterOfMass { get { UpdateMass(false); return com; }}
        public double ShipMass { get { UpdateMass(false); return mass; }}

        public MassCache(Ship ship) { this.ship = ship; }

        public void DropCache() { lastCalculated = -1; }
            
        public void UpdateMass(bool force) {
            if (MyAPIGateway.Session.GameplayFrameCounter == lastCalculated && !force) return;
            var grid = ship.Grids.FirstElement();
            if (grid == null) return;
            if (grid.Physics == null) return;

            lastCalculated = MyAPIGateway.Session.GameplayFrameCounter;

            var COM_ship = grid.Physics.CenterOfMassWorld;
            float grid_mass = grid.Physics.Mass;

            var subgrids = MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Physical);
            if (subgrids.Count > 1) {
                foreach (MyCubeGrid subgrid in subgrids) {
                    if (subgrid != grid && subgrid.Physics != null) {
                        COM_ship = COM_ship + (subgrid.Physics.CenterOfMassWorld - COM_ship) * (subgrid.Physics.Mass / (grid_mass + subgrid.Physics.Mass));
                        grid_mass = grid_mass + subgrid.Physics.Mass;
                    }
                }
                com = COM_ship;
                mass = grid_mass;
            } else {
                mass = grid.Physics.Mass;
                com = grid.Physics.CenterOfMassWorld;
            }				 
        }
    }
}