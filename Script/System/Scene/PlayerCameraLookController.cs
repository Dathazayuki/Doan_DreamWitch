using DreamKnight.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace DreamKnight.Systems.Scene
{
    [DisallowMultipleComponent]
    public class PlayerCameraLookController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private PlayerController player;

        [Header("Look Offset")]
        [SerializeField] private Vector2 maxLookOffset = new Vector2(3f, 2f);
        [SerializeField] private float moveLerpSpeed = 12f;
        [SerializeField] private float returnLerpSpeed = 8f;

        private Transform followProxy;
        private Vector3 currentOffset;

        private void Awake()
        {
            if (cinemachineCamera == null)
                cinemachineCamera = GetComponent<CinemachineCamera>();

            EnsureFollowProxy();
        }

        private void OnEnable()
        {
            EnsureFollowProxy();
            BindToPlayer(player != null ? player : FindAnyObjectByType<PlayerController>());
        }

        public void BindToPlayer(PlayerController targetPlayer)
        {
            player = targetPlayer;
            EnsureFollowProxy();

            if (cinemachineCamera == null || followProxy == null)
                return;

            if (player == null)
                return;

            followProxy.position = player.transform.position;
            cinemachineCamera.Follow = followProxy;
            cinemachineCamera.LookAt = followProxy;
        }

        private void LateUpdate()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            if (player == null)
                return;

            if (cinemachineCamera == null)
                cinemachineCamera = GetComponent<CinemachineCamera>();

            EnsureFollowProxy();

            Vector2 input = player.Input != null ? player.Input.CameraLookInput : Vector2.zero;
            Vector3 desiredOffset = new Vector3(input.x * maxLookOffset.x, input.y * maxLookOffset.y, 0f);

            float speed = input.sqrMagnitude > 0.0001f ? moveLerpSpeed : returnLerpSpeed;
            currentOffset = Vector3.Lerp(currentOffset, desiredOffset, 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.deltaTime));

            followProxy.position = player.transform.position + currentOffset;

            if (cinemachineCamera != null)
            {
                if (cinemachineCamera.Follow != followProxy)
                    cinemachineCamera.Follow = followProxy;
                if (cinemachineCamera.LookAt != followProxy)
                    cinemachineCamera.LookAt = followProxy;
            }
        }

        private void EnsureFollowProxy()
        {
            if (followProxy != null)
                return;

            GameObject go = new GameObject("PlayerCameraLookProxy");
            followProxy = go.transform;
            followProxy.SetParent(null, true);
        }
    }
}
