using System.Collections;
using UnityEngine;
using DreamKnight.Systems.Audio;
public class SceneAudioTrigger : MonoBehaviour
{
    [SerializeField] private string bgmKey = "Stage1";
    [SerializeField] private float fadeDuration = 1.0f;
    private IEnumerator Start()
    {
        // Chờ từng frame cho đến khi AudioManager được khởi tạo xong hoàn toàn
        while (AudioManager.Instance == null)
        {
            yield return null; 
        }
        // Sau khi AudioManager đã sẵn sàng, tiến hành phát nhạc
        AudioManager.Instance.PlayBGM(bgmKey, fadeDuration);
    }
}