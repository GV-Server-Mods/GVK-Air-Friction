using System;
using System.Collections.Generic;
using Digi;
using Sandbox.Game.Entities;
using Scripts.Specials.ShipClass;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod
{
    public static class AirFriction {
        
        private static Dictionary<int, float> LimitsDictionary = new Dictionary<int, float>();
        public static List<MyCubeGrid> GridsCache = new List<MyCubeGrid>();

        public static int MinSpeed = 0;
        public static int MaxSpeed = 1;
        public static int CurrentSpeed = 2;
        public static int AppliedForce = 3;
        
        public static void UpdateUiStatsShip(Ship ship, double[] values)
        {
            GridsCache.Clear();
            foreach (var grid in ship.Grids)
            {
                GridsCache.Add(grid);
            }
            
            Vector3 velocity = Vector3.Zero;
            bool hasSmall = false;
            bool hasLarge = false;
            
            for (var index = GridsCache.Count - 1; index >= 0; index--)
            {
                var grid = GridsCache[index];
                if (grid.Closed || grid.MarkedForClose || grid.Physics == null || !grid.Physics.Enabled || grid.IsPreview || grid.isFrozen())
                {
                    values[MinSpeed] = 0;
                    values[MaxSpeed] = 0;
                    values[CurrentSpeed] = 0;
                    values[AppliedForce] = 0;
                    GridsCache.RemoveAt(index);
                    return;
                }

                hasLarge |= grid.GridSizeEnum == MyCubeSize.Large;
                hasSmall |= grid.GridSizeEnum == MyCubeSize.Small;
                
                velocity += grid.Physics.LinearVelocity;
            }
            
            if (GridsCache.Count == 0)
            {
                values[MinSpeed] = 0;
                values[MaxSpeed] = 0;
                values[CurrentSpeed] = 0;
                values[AppliedForce] = 0;
                return;
            }
            
            velocity /= GridsCache.Count;
            var spd = velocity.Length();
            var settings = AirFrictionSession.Instance.Settings;

            GridGroupSize size = hasLarge ? GridGroupSize.Large : GridGroupSize.Small;
            if (hasLarge && hasSmall) size = GridGroupSize.Mixed;

            MyPlanet planet = PlanetsAndCharactersCache.GetPlanet(GridsCache[0].WorldMatrix.Translation);
            PlanetInfo planetInfo = settings.GetPlanet(planet);
            

            float min, max;
            FrictionSettings fsettings;
            GetMinMaxSpeed(planetInfo, planet, GridsCache[0].PositionComp.WorldVolume.Center, GridsCache[0], size, out min, out max, out fsettings);
            
            if (spd < min)
            {
                values[MinSpeed] = min;
                values[MaxSpeed] = max;
                values[CurrentSpeed] = spd;
                values[AppliedForce] = 0;
                return;
            }
            
            var power = fsettings.Calculate(spd, min, max, ship.MassCache.ShipMass, false);

            values[MinSpeed] = min;
            values[MaxSpeed] = max;
            values[CurrentSpeed] = spd;
            values[AppliedForce] = - power;
        }
        
        public static void UpdateUiStatsCharacter(IMyCharacter character, double[] values)
        {
            if (character.IsDead || character.Physics == null)
            {
                values[MinSpeed] = 0;
                values[MaxSpeed] = 0;
                values[CurrentSpeed] = 0;
                values[AppliedForce] = 0;
                return;
            }
            
            var velocity = character.Physics.LinearVelocity;

            var spd = velocity.Length();
            var settings = AirFrictionSession.Instance.Settings;
            
            GridGroupSize size = GridGroupSize.Character;

            MyPlanet planet = PlanetsAndCharactersCache.GetPlanet(character.WorldMatrix.Translation);
            PlanetInfo planetInfo = settings.GetPlanet(planet);
            

            float min, max;
            FrictionSettings fsettings;
            GetMinMaxSpeed(planetInfo, planet, character.PositionComp.WorldVolume.Center, null, size, out min, out max, out fsettings);

            if (spd < min)
            {
                values[MinSpeed] = min;
                values[MaxSpeed] = max;
                values[CurrentSpeed] = spd;
                values[AppliedForce] = 0;
                return;
            }
            
            var power = fsettings.Calculate(spd, min, max, character.Physics.Mass, true);
            
            values[MinSpeed] = min;
            values[MaxSpeed] = max;
            values[CurrentSpeed] = spd;
            values[AppliedForce] = - power;
        }

        public static void ApplyFrictionCharacter(IMyCharacter character)
        {
            if (character.IsDead || character.Physics == null) return;
            var velocity = character.Physics.LinearVelocity;

            var spd = velocity.Length();
            var settings = AirFrictionSession.Instance.Settings;
            if (spd < settings.MinSpeed && settings.SpecCore == null)
            {
                return;
            }
            

            MyPlanet planet = PlanetsAndCharactersCache.GetPlanet(character.WorldMatrix.Translation);
            PlanetInfo planetInfo = settings.GetPlanet(planet);
            

            float min, max;
            FrictionSettings fsettings;
            GetMinMaxSpeed(planetInfo, planet, character.WorldMatrix.Translation,   null,GridGroupSize.Character, out min, out max, out fsettings);

            if (spd < min)
            {
                return;
            }
            
            var power = fsettings.Calculate(spd, min, max, character.Physics.Mass, true);;
            
            var d = (Vector3D)velocity;
            d.Normalize();


            character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, d * power, null, null, max);
        }
        
        public static void ApplyFrictionShip(Ship ship)
        {
            GridsCache.Clear();
            foreach (var grid in ship.Grids)
            {
                GridsCache.Add(grid);
            }
            
            Vector3 velocity = Vector3.Zero;
            bool hasSmall = false;
            bool hasLarge = false;
            
            for (var index = GridsCache.Count - 1; index >= 0; index--)
            {
                var grid = GridsCache[index];
                if (grid.Closed || grid.MarkedForClose || grid.Physics == null || !grid.Physics.Enabled || grid.IsPreview || grid.isFrozen())
                {
                    GridsCache.RemoveAt(index);
                    return;
                }

                hasLarge |= grid.GridSizeEnum == MyCubeSize.Large;
                hasSmall |= grid.GridSizeEnum == MyCubeSize.Small;
                
                velocity += grid.Physics.LinearVelocity;
            }
            
            if (GridsCache.Count == 0)
            {
                return;
            }
            
            velocity /= GridsCache.Count;
            var spd = velocity.Length();
            var settings = AirFrictionSession.Instance.Settings;
            if (spd < settings.MinSpeed && settings.SpecCore == null)
            {
                return;
            }
                

            GridGroupSize size = hasLarge ? GridGroupSize.Large : GridGroupSize.Small;
            if (hasLarge && hasSmall) size = GridGroupSize.Mixed;

            MyPlanet planet = PlanetsAndCharactersCache.GetPlanet(GridsCache[0].WorldMatrix.Translation);
            PlanetInfo planetInfo = settings.GetPlanet(planet);
            

            float min, max;
            FrictionSettings fsettings;
            GetMinMaxSpeed(planetInfo, planet, GridsCache[0].PositionComp.WorldVolume.Center, GridsCache[0], size, out min, out max, out fsettings);

            if (spd < min)
            {
                return;
            }

            var power = fsettings.Calculate(spd, min, max, ship.MassCache.ShipMass, false);

            var d = (Vector3D)velocity;
            d.Normalize();

            
            //Log.ChatError("Force: " + power);
            //ship.MassCache.DropCache();
            GridsCache[0].Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, d * power, ship.MassCache.CenterOfMass, null, max);
        }
        
        public static double GetElevation(MyPlanet closestPlanet, Vector3 position) {
            if (closestPlanet == null) {
                return -1d;
            } else {
                //if (!closestPlanet.HasAtmosphere) return -1d;
                var distance = (position - closestPlanet.PositionComp.GetPosition()).Length();
                return Math.Max(0, distance - closestPlanet.AverageRadius);
            }
        }

        public static void GetMinMaxSpeed(PlanetInfo info, MyPlanet closestPlanet, Vector3D position, MyCubeGrid grid, GridGroupSize size, out float min, out float max, out FrictionSettings fsettings)
        {
            var elevation = GetElevation(closestPlanet, position);
            if (elevation < 0) {
                elevation = 9999999d;
            }

            var settings = AirFrictionSession.Instance.Settings;
            float extraStart = 0;
            float extraEnd = 0;

            if (settings.SpecCore != null && size != GridGroupSize.Character)
            {
                object gridCore = SpecBlockHooks.GetMainSpecCore(grid);
                
                LimitsDictionary.Clear();
                SpecBlockHooks.GetSpecCoreLimits(gridCore, LimitsDictionary, SpecBlockHooks.GetSpecCoreLimitsEnum.DynamicLimits);

                if (gridCore == null)
                {
                    extraStart += settings.SpecCore.NoCoreExtraStart;
                    extraEnd += settings.SpecCore.NoCoreExtraEnd;
                    elevation += settings.SpecCore.NoCoreExtraElevation;
                }
                else
                {
                    extraStart += LimitsDictionary.GetOr(settings.SpecCore.CoreExtraStartId, 0);
                    extraEnd += LimitsDictionary.GetOr(settings.SpecCore.CoreExtraEndId, 0);
                    elevation += LimitsDictionary.GetOr(settings.SpecCore.CoreExtraElevationId, 0);
                }
            }

            GetMinMaxSpeed(info, elevation, size, out min, out max, out fsettings);
            
            min += extraStart;
            max += extraEnd;
        }
        
        public static void GetMinMaxSpeed (PlanetInfo info, double elevation, GridGroupSize size, out float start, out float end, out FrictionSettings settings) {
            if (info != null)
            {
                foreach (var alt in info.Altitudes)
                {
                    if (elevation < alt.Alt)
                    {
                        GetMinMaxSpeed(info, alt, size, out start, out end, out settings);
                        return;
                    }
                }
            }

            GetMinMaxSpeed(null, AirFrictionSession.Instance.Settings.Space, size, out start, out end, out settings);
        }

        public static void GetMinMaxSpeed(PlanetInfo info, Altitude alt, GridGroupSize size, out float start, out float end, out FrictionSettings settings)
        {
            settings = (alt.Friction ?? info.Friction) ?? AirFrictionSession.Instance.Settings.DefaultSettings;
            switch (size)
            {
                case GridGroupSize.Small:
                    start = alt.SmallStart;
                    end = alt.SmallEnd;
                    return;
                case GridGroupSize.Large:
                    start = alt.LargeStart;
                    end = alt.LargeEnd;
                    return;
                case GridGroupSize.Character:
                    start = alt.CharacterStart;
                    end = alt.CharacterEnd;
                    return;
                default:
                    start = alt.MixedStart;
                    end = alt.MixedEnd;
                    return;
            }
        }

    }
}