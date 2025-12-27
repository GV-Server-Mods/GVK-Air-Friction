using System;
using System.Text;
using Digi;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ServerMod
{
    public class MyHudSpeedInfo2 : MyHStat
    {
        public override void Update()
        {
            ValueStringDirty();
        }

        public override string GetId()
        {
            return "AirFriction_SpeedRange";
        }
        
        public override string ToString()
        {
            var t2 = AirFrictionSession.LastFrameClientValues[AirFriction.MinSpeed];
            var t3 = AirFrictionSession.LastFrameClientValues[AirFriction.MaxSpeed];
            if (t2 == 0 && t3 == 0)
            {
                return "";
            }
            if (t2 == t3)
            {
                return $"[{t2:N0} m/s]";
            }
            return $"[{t2:N0}-{t3:N0} m/s]";
		}
    }
    
    public class MyHudSpeedInfo : MyHStat
    {
        public override void Update()
        {
            CurrentValue = (float) AirFrictionSession.LastFrameClientValues[AirFriction.CurrentSpeed];
            ValueStringDirty();
        }

        public override string GetId()
        {
            return "AirFriction_CurrentSpeed";
        }
        
        public override string ToString()
        {
            var t1 = AirFrictionSession.LastFrameClientValues[AirFriction.CurrentSpeed];
            if (t1 < 10.0f) {
                return $"{t1:N1} m/s";
            } else {                
                return $"{t1:N0} m/s";
            }
		}
	}
    
    
    public class MyHudSpeed1 : MyHStat
    {
        public override void Update()
        {
            var current = AirFrictionSession.LastFrameClientValues[AirFriction.CurrentSpeed];
            var min = AirFrictionSession.LastFrameClientValues[AirFriction.MinSpeed];
            CurrentValue = (float) Math.Min(1, current/min);
        }

        public override string GetId()
        {
            return "AirFriction_MinSpeed";
        }
        
        public override string ToString()
        {
            return ((double)CurrentValue).toMlt();
        }
    }
    
    public class MyHudSpeedInfo3 : MyHStat
    {
        public override void Update()
        {
            CurrentValue = (float) AirFrictionSession.LastFrameClientValues[AirFriction.AppliedForce];
        }

        public override string GetId()
        {
            return "AirFriction_AppliedForce";
        }
        
        public override string ToString()
        {
            if (CurrentValue == 0)
            {
                return "";
            }
            var sb = new StringBuilder();
            MyValueFormatter.AppendForceInBestUnit(CurrentValue, sb);
            return sb.ToString();
        }
    }
    
    public class MyHudSpeed2 : MyHStat
    {
        public override void Update()
        {
            var min = AirFrictionSession.LastFrameClientValues[AirFriction.MinSpeed];
            var max = AirFrictionSession.LastFrameClientValues[AirFriction.MaxSpeed];
            if (min == max)
            {
                CurrentValue = 0f;
                return;
            }
            var current = AirFrictionSession.LastFrameClientValues[AirFriction.CurrentSpeed]-min;
            max = max-min;
            if (current > 0)
            {
                CurrentValue = (float) MathHelper.Clamp(current/max, 0, 1);
            }
            else
            {
                CurrentValue = 0f;
            }
        }

        public override string GetId()
        {
            return "AirFriction_CurrentSpeed2";
        }
        
        public override string ToString()
        {
            return "";
        }
    }
	
    public abstract class MyHStat : IMyHudStat
    {
        public virtual float MaxValue => 1f;
        public virtual float MinValue => 0.0f;

        private float m_currentValue;
        private string m_valueStringCache;

        public abstract void Update();
        public abstract String GetId();
        
        public MyStringHash Id { get; protected set; }

        public MyHStat()
        {
            Id = MyStringHash.GetOrCompute(GetId());
        }
        
        public float CurrentValue
        {
            get { return m_currentValue; }
            protected set
            {
                if (m_currentValue == value)
                {
                    return;
                }
                m_currentValue = value;
                ValueStringDirty();
            }
        }

        public void ValueStringDirty()
        {
            m_valueStringCache = null;
        }

        public string GetValueString()
        {
            if (m_valueStringCache == null)
            {
                m_valueStringCache = ToString();
            }
            return m_valueStringCache;
        }
        
        public override string ToString() => string.Format("{0:0}", (float)(CurrentValue * 100.0));
    }
    
}