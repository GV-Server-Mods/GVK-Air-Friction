using System;
using System.Collections.Generic;
using Digi;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace ServerMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class PlanetsAndCharactersCache : MySessionComponentBase
    {
        private static PlanetsAndCharactersCache instance; 
        public static readonly Dictionary<long, MyPlanet> Planets = new Dictionary<long, MyPlanet>();
        public static readonly Dictionary<long, IMyCharacter> Characters = new Dictionary<long, IMyCharacter>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            instance = this;
        }
        
        public override void LoadData()
        {
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
        }
        
        private void OnEntityAdded(IMyEntity ent)
        {
            try
            {
                var p = ent as MyPlanet;
                if (p != null)
                {
                    if (!Planets.ContainsKey(p.EntityId))
                    {
                        Planets.Add(ent.EntityId, p);
                        p.OnMarkForClose += OnMarkForClose;
                        return;
                    } 
                }

                var ch = ent as IMyCharacter;
                if (ch != null)
                {
                    if (!Characters.ContainsKey(ch.EntityId))
                    {
                        Characters.Add(ent.EntityId, ch);
                        ch.OnMarkForClose += OnMarkForClose;
                        return;
                    } 
                }
            }
            catch (Exception e)
            {
                Log.ChatError(e);
            }
        }
        
        private void OnMarkForClose(IMyEntity ent)
        {
            if (ent is MyPlanet)
            {
                Planets.Remove(ent.EntityId);
                ent.OnMarkForClose -= OnMarkForClose;
            }

            if (ent is IMyCharacter)
            {
                Characters.Remove(ent.EntityId);
                ent.OnMarkForClose -= OnMarkForClose;
            }
        }

        public static MyPlanet GetPlanet(Vector3D vector)
        {
            var bb = new BoundingBoxD(vector, vector);
            foreach (var x in Planets)
                if (x.Value.IntersectsWithGravityFast(ref bb))
                    return x.Value;
            return null;
        }
    }
}