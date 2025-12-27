using System;
using Digi;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod
{
   [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class AirFrictionSession : MySessionComponentBase
    {
        public static AirFrictionSession Instance;
        public AirFrictionSettings Settings;
        
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            Instance = this;
          
            MyAPIGateway.GridGroups.AddGridGroupLogic(GridLinkTypeEnum.Electrical, CreateGG);
        }

        public override void LoadData()
        {
            base.LoadData();
            LoadModConfig();
        }

        private AirFrictionSettings GetDefault()
        {
            var settings = new AirFrictionSettings();
            settings.Gliders = null;
            settings.SpecCore = new SpecCore()
            {
                CoreExtraElevationId = -36,
                CoreExtraStartId = -34,
                CoreExtraEndId = -35,
                NoCoreExtraElevation = -5000,
                NoCoreExtraStart = -20,
                NoCoreExtraEnd = -20,
            };
            settings.Space = new Altitude()
            {
                Alt = 0,
                LargeStart = 80,
                LargeEnd = 100,
                MixedStart = 80,
                MixedEnd = 100,
                SmallStart = 90,
                SmallEnd = 100,
            };

            settings.AnyPlanet = new PlanetInfo()
            {
                Altitudes = new Altitude[]
                {
                    new Altitude()
                    {
                        Alt = 10000,
                        LargeStart = 30,
                        LargeEnd = 50,
                        MixedStart = 30,
                        MixedEnd = 50,
                        SmallStart = 30,
                        SmallEnd = 50,
                    }
                }
            };

            settings.Planets = new PlanetInfo[]
            {
                new PlanetInfo()
                {
                    GeneratorName = "EarthLike",
                    Altitudes = new Altitude[]
                    {
                        new Altitude()
                        {
                            Alt = 6000,
                            LargeStart = 50,
                            LargeEnd = 60,
                            MixedStart = 50,
                            MixedEnd = 60,
                            SmallStart = 60,
                            SmallEnd = 70,
                        },
                        new Altitude()
                        {
                            Alt = 12000,
                            LargeStart = 60,
                            LargeEnd = 70,
                            MixedStart = 60,
                            MixedEnd = 70,
                            SmallStart = 70,
                            SmallEnd = 80,
                        },
                        new Altitude()
                        {
                            Alt = 20000,
                            LargeStart = 70,
                            LargeEnd = 80,
                            MixedStart = 70,
                            MixedEnd = 80,
                            SmallStart = 80,
                            SmallEnd = 90,
                        }
                    }
                }
            };

            settings.ConnectionType = GridLinkTypeEnum.Electrical;

            settings.DefaultSettings = new FrictionSettings();
            settings.DefaultSettings.FrictionPow = 3;
            settings.DefaultSettings.MaxAcceleration = 1000;
            settings.DefaultSettings.MaxFriction = 100000f;
            
            settings.DefaultSettings.FrictionPowCharacter = 3;
            settings.DefaultSettings.MaxAccelerationCharacter = 60;
            settings.DefaultSettings.MaxFrictionCharacter = 100000f;
            
            settings.ApplyForceToCharacters = true;
            return settings;
        }
        
        public static T LoadFirstModFile<T>(string name, Func<T> defaultGenerator)
        {
            name = $"Data/{name}.xml";
            foreach (var Mod in MyAPIGateway.Session.Mods)
            {
                if (!MyAPIGateway.Utilities.FileExistsInModLocation(name, Mod)) continue;
                
                try
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation(name, Mod))
                    {
                        return MyAPIGateway.Utilities.SerializeFromXML<T>(reader.ReadToEnd());
                    }
                }
                catch (Exception exc)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(()=>
                    {
                        MyAPIGateway.Utilities.InvokeOnGameThread(()=>
                        {
                            MyAPIGateway.Utilities.InvokeOnGameThread(()=>
                            {
                                Log.Error(exc);
                            });
                        });
                    });
                    return defaultGenerator();
                }
            }

            return defaultGenerator();
        }
        
        private void LoadModConfig()
        {
            Settings = LoadFirstModFile<AirFrictionSettings>("AirFrictionSettings", GetDefault);
            Settings.OnDeserialized();
        }

        public static MyGridGroupsDefaultEventHandler CreateGG(IMyGridGroupData data)
        {
            var ship = new Ship(data);
            return ship;
        }

        public static double[] LastFrameClientValues = new double[4];
        
        public void UpdateStats()
        {
            var cu = MyAPIGateway.Session.LocalHumanPlayer?.Controller?.ControlledEntity;
            if (cu != null)
            {
                var cockpit = cu as IMyCubeBlock;
                if (cockpit == null)
                {
                    var character = cu as IMyCharacter;
                    if (character != null)
                    {
                        if (Instance.Settings.ApplyForceToCharacters)
                        {
                            AirFriction.UpdateUiStatsCharacter(character, LastFrameClientValues);
                            return;
                        }
                        else
                        {
                            LastFrameClientValues[AirFriction.MinSpeed] = 0;
                            LastFrameClientValues[AirFriction.MaxSpeed] = 0;
                            LastFrameClientValues[AirFriction.CurrentSpeed] = (character.Physics?.LinearVelocity ?? Vector3.Zero).Length();
                            LastFrameClientValues[AirFriction.AppliedForce] = 0;
                            return;
                        }
                    }
                }
                else
                {
                    var grid = (MyCubeGrid)cockpit.CubeGrid;
                    foreach (var ship in Ship.AllShips)
                    {
                        if (ship.Grids.Contains(grid))
                        {
                            AirFriction.UpdateUiStatsShip(ship, LastFrameClientValues);
                            return;
                        }
                    }
                }
            }
            
            LastFrameClientValues[AirFriction.MinSpeed] = 0;
            LastFrameClientValues[AirFriction.MaxSpeed] = 0;
            LastFrameClientValues[AirFriction.CurrentSpeed] = 0;
            LastFrameClientValues[AirFriction.AppliedForce] = 0;
        }

        
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            
            if (Instance.Settings == null)
            {
                return;
            }

            if (Settings.SpecCore != null && !SpecBlockHooks.IsReady())
            {
                return;
            }
            
            foreach (var ship in Ship.AllShips)
            {
                AirFriction.ApplyFrictionShip(ship);
            }

            if (Instance.Settings.ApplyForceToCharacters)
            {
                foreach (var character in PlanetsAndCharactersCache.Characters)
                {
                    AirFriction.ApplyFrictionCharacter(character.Value);
                }
            }

            UpdateStats();
        }
    }
}