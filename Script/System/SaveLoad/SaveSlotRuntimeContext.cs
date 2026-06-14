namespace DreamKnight.Systems.SaveLoad
{
    public static class SaveSlotRuntimeContext
    {
        public static int PendingSlotIndex { get; private set; } = -1;
        public static bool PendingLoadExistingSave { get; private set; }
        public static bool HasPendingSlot => PendingSlotIndex >= 0;

        public static void SetPendingNewGame(int slotIndex)
        {
            PendingSlotIndex = slotIndex;
            PendingLoadExistingSave = false;
        }

        public static void SetPendingLoad(int slotIndex)
        {
            PendingSlotIndex = slotIndex;
            PendingLoadExistingSave = true;
        }

        public static void Clear()
        {
            PendingSlotIndex = -1;
            PendingLoadExistingSave = false;
        }
    }
}
