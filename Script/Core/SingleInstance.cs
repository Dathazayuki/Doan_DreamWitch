using System.Threading;
using UnityEngine;

public class SingleInstance : MonoBehaviour
{
    private const string MutexName = "DreamKnight_Unique_Game_Instance";

    private static Mutex mutex;
    private static bool hasLocalInstance;

    private void Awake()
    {
        if (hasLocalInstance)
        {
            Destroy(gameObject);
            return;
        }

        bool createdNew;

        mutex = new Mutex(
            true,
            MutexName,
            out createdNew);

        if (!createdNew)
        {
            mutex.Dispose();
            mutex = null;
            Application.Quit();
            return;
        }

        hasLocalInstance = true;
        SetTargetFrameRate60();
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        ReleaseMutex();
    }

    private static void ReleaseMutex()
    {
        if (mutex == null)
            return;

        mutex.ReleaseMutex();
        mutex.Dispose();
        mutex = null;
        hasLocalInstance = false;
    }

    private static void SetTargetFrameRate60()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
