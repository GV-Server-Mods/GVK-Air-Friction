using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Digi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod
{
    public enum GridGroupSize
    {
        Small,
        Large,
        Mixed,
        Character
    }
    
    public class PlanetInfo
    {   
        [XmlAttribute]
        public string GeneratorName;

        [XmlAttribute]
        public long EntityId = 0;

        [XmlArray("Item")]
        public Altitude[] Altitudes;

        public FrictionSettings Friction;

        public override string ToString()
        {
            return GeneratorName + " / " + EntityId;
        }
    }

    public class Altitude
    {
        [XmlAttribute]
        public double Alt;

        [XmlAttribute]
        public float SmallStart;

        [XmlAttribute]
        public float SmallEnd;

        [XmlAttribute]
        public float MixedStart;

        [XmlAttribute]
        public float MixedEnd;

        [XmlAttribute]
        public float LargeStart;

        [XmlAttribute]
        public float LargeEnd;
        
        [XmlAttribute]
        public float CharacterStart;

        [XmlAttribute]
        public float CharacterEnd;

        public FrictionSettings Friction;
    }

    public class GlidersSettings
    {
        [XmlAttribute]
        public float ExtraElevation;
        
        [XmlAttribute]
        public float ExtraStart;
        
        [XmlAttribute]
        public float ExtraEnd;
    }

    public class SpecCore
    {
        [XmlAttribute]
        public float NoCoreExtraStart = 0f;
        
        [XmlAttribute]
        public float NoCoreExtraElevation = 0f;
        
        [XmlAttribute]
        public float NoCoreExtraEnd = 0f;

        [XmlAttribute]
        public int CoreExtraStartId = 0;
        
        [XmlAttribute]
        public int CoreExtraEndId = 0;
        
        [XmlAttribute]
        public int CoreExtraElevationId = 0;
    }

    public class FrictionSettings
    {
        public float FrictionPow;
        public float FrictionPowMlt = 1f;
        public float FrictionPow2 = 1f;
        public float FrictionPow2Mlt = 0f;
        public float FrictionConst = 0f;
        public float MaxAcceleration;
        public float MinFriction;
        public float MaxFriction;
        
        public float FrictionPowCharacter;
        public float FrictionPowMltCharacter = 1f;
        public float FrictionPow2Character = 1f;
        public float FrictionPow2MltCharacter = 0f;
        public float FrictionConstCharacter = 0f;
        public float MaxAccelerationCharacter;
        public float MinFrictionCharacter;
        public float MaxFrictionCharacter;

        public double Calculate(float spd, float min, float max, double mass, bool isCharacter)
        {

            if (min == max) {
                return -MaxAcceleration;    
            }

            var diff = max - min;
            var nowdiff = spd - min;
            var mlt = nowdiff/diff;

            double friction;
            if (!isCharacter)
            {
                mlt = MathHelper.Clamp(mlt,  MinFriction, MaxFriction);
                friction = (Math.Pow(mlt, FrictionPow)  * FrictionPowMlt + Math.Pow(mlt, FrictionPow2) * FrictionPow2Mlt + FrictionConst)  * MaxAcceleration;
                //if (MyAPIGateway.Session.GameplayFrameCounter % 100 == 0)
                //{
                //    Log.ChatError($"({mlt}^{FrictionPow}*{FrictionPowMlt} + {mlt}^{FrictionPow2}*{FrictionPow2Mlt} + {FrictionConst}) * {MaxAcceleration}");
                //}
            }
            else
            {
                mlt = MathHelper.Clamp(mlt,  MinFrictionCharacter, MaxFrictionCharacter);
                friction = (Math.Pow(mlt, FrictionPowCharacter)  * FrictionPowMltCharacter + Math.Pow(mlt, FrictionPow2Character) * FrictionPow2MltCharacter + FrictionConstCharacter)  * MaxAccelerationCharacter;
            }

            
            

            return -friction * mass;
        }
        
    }
    
    public class AirFrictionSettings
    {
        [XmlArrayItem("Planet")]
        public PlanetInfo[] Planets;

        public PlanetInfo AnyPlanet;

        public Altitude Space;
        public SpecCore SpecCore;
        public GlidersSettings Gliders;
        
        public FrictionSettings DefaultSettings;
        

        [XmlAttribute]
        public GridLinkTypeEnum ConnectionType = GridLinkTypeEnum.Physical;

        [XmlIgnore]
        public Dictionary<string, PlanetInfo> PlanetInfos = new Dictionary<string, PlanetInfo>();
        
        [XmlIgnore]
        public Dictionary<long, PlanetInfo> PlanetInfosByEntity = new Dictionary<long, PlanetInfo>();

        [XmlIgnore]
        public float MinSpeed;
        
        [XmlIgnore]
        public float MinCharacterSpeed;

        public bool ApplyForceToCharacters;

        public void OnDeserialized()
        {
            MinSpeed = float.MaxValue;
            MinCharacterSpeed = float.MaxValue;
            
            foreach (var planet in Planets)
            {
                if (!string.IsNullOrEmpty(planet.GeneratorName))
                {
                    PlanetInfos.Add(planet.GeneratorName, planet);
                }

                if (planet.EntityId != 0)
                {
                    PlanetInfosByEntity.Add(planet.EntityId, planet);
                }

                foreach (var altitude in planet.Altitudes)
                {
                    MinSpeed = Math.Min(altitude.LargeStart, MinSpeed);
                    MinSpeed = Math.Min(altitude.MixedStart, MinSpeed);
                    MinSpeed = Math.Min(altitude.SmallStart, MinSpeed);
                    
                    MinCharacterSpeed = Math.Min(altitude.SmallStart, MinCharacterSpeed);
                }
            }
        }

        public PlanetInfo GetPlanet(MyPlanet planet)
        {
            if (planet == null) return null;
            PlanetInfo info = null;
            if (PlanetInfosByEntity.TryGetValue(planet.EntityId, out info))
            {
                return info;
            }
            
            if (PlanetInfos.TryGetValue(planet.Generator.Id.SubtypeName, out info))
            {
                return info;
            }

            return AnyPlanet;
        }
    }
}