using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Digi;
using MIG.Shared.CSharp;
using MIG.Shared.SE;
using ProtoBuf;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace MIG.SpecCores
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SpecCoreSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            SpecBlockHooks.Init();
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
            FrameExecutor.Update();
        }

        protected override void UnloadData()
        {
            SpecBlockHooks.Close();
        }
    }
}