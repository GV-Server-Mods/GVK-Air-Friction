using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace GV_Max_Rotation
{
    [MySessionComponentDescriptor(MyUpdateOrder.Simulation, 999)]
    public class RelativeTopSpeed : MySessionComponentBase
    {
        public const float MaxMassAngMult = 0.1f; //smaller fraction means lower max rotation, 1 means no effect
        public const float MaxSpeedAngMult = 0.25f; //smaller fraction means lower max rotation, 1 means no effect
        public const float MaxMass = 8000000f;
        public const float MinMass = 1f;
        public const float MaxSpeed = 100f;
        public const float MinSpeed = 1f;
        public const float MaxAng = 10f;
        public const float MinAng = 0.01f;

        private byte waitInterval = 0;
        private readonly List<MyCubeGrid> ActiveGrids = new List<MyCubeGrid>();
        private readonly List<MyCubeGrid> PassiveGrids = new List<MyCubeGrid>();
        private readonly List<MyCubeGrid> DisabledGrids = new List<MyCubeGrid>();

        private readonly MyObjectBuilderType beaconTypeId = typeof(MyObjectBuilder_Beacon);

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Entities.OnEntityAdd += AddGrid;
            MyAPIGateway.Entities.OnEntityRemove += RemoveGrid;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= AddGrid;
            MyAPIGateway.Entities.OnEntityRemove -= RemoveGrid;
        }

        private void AddGrid(IMyEntity ent)
        {
            MyCubeGrid grid = ent as MyCubeGrid;
            if (grid == null || grid.Physics == null || grid.GridSizeEnum == MyCubeSize.Small)
                return;

            //Ignoring suspension wheels and debris
            if (grid.BlocksCount <= 2)
            {
                return;
            }

            RegisterOrUpdateGridStatus(grid, grid.IsStatic);
            grid.OnStaticChanged += RegisterOrUpdateGridStatus;
        }

        private void RemoveGrid(IMyEntity ent)
        {
            MyCubeGrid grid = ent as MyCubeGrid;
            if (grid == null || grid.Physics == null || grid.GridSizeEnum == MyCubeSize.Small) { return; }

            grid.OnStaticChanged -= RegisterOrUpdateGridStatus;
            ActiveGrids.Remove(grid);
            PassiveGrids.Remove(grid);
            DisabledGrids.Remove(grid);
        }

        private bool IsMoving(IMyEntity ent)
        {
            return ent.Physics.LinearVelocity.LengthSquared() > 1.0f || ent.Physics.AngularVelocity.LengthSquared() > 0.01f;
        }

        private void RegisterOrUpdateGridStatus(MyCubeGrid grid, bool isStatic)
        {
            if (isStatic)
            {
                if (!DisabledGrids.Contains(grid))
                {
                    DisabledGrids.Add(grid);
                }

                PassiveGrids.Remove(grid);
                ActiveGrids.Remove(grid);
            }
            else if (ShouldMonitor(grid))
            {
                if (!ActiveGrids.Contains(grid))
                {
                    ActiveGrids.Add(grid);
                }

                PassiveGrids.Remove(grid);
                DisabledGrids.Remove(grid);
            }
            else
            {
                if (!PassiveGrids.Contains(grid))
                {
                    PassiveGrids.Add(grid);
                }

                ActiveGrids.Remove(grid);
                DisabledGrids.Remove(grid);
            }
        }

        public override void Simulate()
        {
            // update active / passive grids every 3 seconds
            if (waitInterval == 0)
            {
                for (int i = PassiveGrids.Count - 1; i >= 0; i--)
                {
                    MyCubeGrid grid = PassiveGrids[i];
                    if (ShouldMonitor(grid))
                    {
                        if (!ActiveGrids.Contains(grid))
                        {
                            ActiveGrids.Add(grid);
                        }

                        PassiveGrids.RemoveAtFast(i);
                    }
                }
                for (int i = ActiveGrids.Count - 1; i >= 0; i--)
                {
                    MyCubeGrid grid = ActiveGrids[i];
                    if (!ShouldMonitor(grid))
                    {
                        if (!PassiveGrids.Contains(grid))
                        {
                            PassiveGrids.Add(grid);
                        }

                        ActiveGrids.RemoveAtFast(i);
                    }
                }

                waitInterval = 15; // reset, was 180
            }

            for (int i = 0; i < ActiveGrids.Count; i++)
            {
                UpdateGrid(i);
            }

            waitInterval--;
        }

        private bool ShouldMonitor(MyCubeGrid grid)
        {
            if (grid == null || grid.Physics == null || grid.MarkedForClose) { return false; }
            return IsMoving(grid) && grid.BlocksCounters.GetValueOrDefault(beaconTypeId) > 0;
        }

        private void UpdateGrid(int index)
        {

            MyCubeGrid grid = ActiveGrids[index];

            float speed = MathHelper.Clamp(Math.Abs(grid.Physics.Speed), MinSpeed, MaxSpeed);
            float mass = grid.Physics.Mass;

            Vector3 ang = grid.Physics.AngularVelocity;

            if (ang.LengthSquared() > (MinAng * MinAng))
            {
                var angMassReduction = 1 + ((mass - MinMass) / (MaxMass - MinMass)) * (MaxMassAngMult - 1);
                var angSpeedReduction = 1 + ((speed - MinSpeed) / (MaxSpeed - MinSpeed) * (MaxSpeedAngMult - 1));
                float reducedAng = MathHelper.Clamp(MaxAng * angMassReduction * angSpeedReduction, MinAng, MaxAng);
                if (ang.Length() > reducedAng)
                {
                    ang = Vector3.Normalize(ang) * reducedAng;
                    grid.Physics.AngularVelocity = ang;
                }
            }
        }
    }
}
