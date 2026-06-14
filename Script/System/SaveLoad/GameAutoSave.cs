namespace DreamKnight.Systems.SaveLoad
{
    public static class GameAutoSave
    {
        public static void Request(string reason = null)
        {
            GameSaveManager manager = GameSaveManager.Instance;
            if (manager == null)
                return;

            manager.SaveActiveSlot();
        }
    }
}
