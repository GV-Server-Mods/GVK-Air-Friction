using VRage.ModAPI;

namespace ServerMod {
    public static class NAPI
    {
        private const int FREEZE_FLAG = 4;

        public static bool isFrozen(this IMyEntity grid)
        {
            return ((int) grid.Flags | FREEZE_FLAG) == (int) grid.Flags;
        }
    }
}