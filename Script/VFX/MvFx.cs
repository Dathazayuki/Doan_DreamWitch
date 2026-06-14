using System;
using UnityEngine;

namespace Mv
{
	public class MvFx : MonoBehaviour
	{
		private enum PlayState
		{
			Init = 0,
			Play = 1,
			Pause = 2,
			Stop = 3
		}

		[SerializeField]
		private ParticleSystem[] _PsBuff;

		private bool _Event_LifeEnd_OncePlay;

		public Action<MvFx> Event_LifeEnd;

		private PlayState _PlayState;

		[SerializeField]
		[Header("Tốc độ phát lại khi hiệu ứng dừng (ví dụ: khi bạn muốn nhanh chóng kết thúc hình ảnh)")]
		private float _StopSimSpeed = 8f;

		[Header("Thông số kỹ thuật SE")]
		[SerializeField]
		private int _AudioSE;

		[Header("Bỏ qua giới hạn khoảng cách đối với SE")]
		[SerializeField]
		private bool _AudioPitchRand;

		[Header("Bỏ qua giới hạn khoảng cách đối với SE")]
		[SerializeField]
		private bool _AudioIgnoreDistLimit;

		[Header("Tọa độ lệch về phía Đông Nam (được sử dụng khi vị trí của một sự kiện như cảnh báo sạt lở đá nằm quá cao)")]
		[SerializeField]
		private Vector2 _AudioPosOfs;

		public bool IsPlay => _PlayState == PlayState.Play;

		public bool IsStop => _PlayState == PlayState.Stop;

		public bool IsLifeEnd => _Event_LifeEnd_OncePlay;

		private void Awake()
		{
			if (_PsBuff == null || _PsBuff.Length == 0)
			{
				_PsBuff = GetComponentsInChildren<ParticleSystem>(true);
			}
			_PlayState = PlayState.Init;
		}

		private void OnEnable()
		{
			Play(true);
		}

		public void setPausePlaySpeedZero()
		{
			for (int i = 0; i < _PsBuff.Length; i++)
			{
				if (_PsBuff[i] == null) continue;
				var main = _PsBuff[i].main;
				main.simulationSpeed = 0f;
			}
		}

		public void Play(Vector2 pos, Quaternion rot, bool isFlip, bool replay = true, Transform parentTrans = null, MonoBehaviour timeRateOwner = null, float vol = 1f)
		{
			transform.SetPositionAndRotation(pos, rot);
			if (parentTrans != null) transform.SetParent(parentTrans, true);
			Play(replay, parentTrans, timeRateOwner, vol);
		}

		public void Play(bool replay = true, Transform parentTrans = null, MonoBehaviour timeRateOwner = null, float vol = 1f)
		{
			if (parentTrans != null) transform.SetParent(parentTrans, true);
			if (replay)
			{
				for (int i = 0; i < _PsBuff.Length; i++)
				{
					if (_PsBuff[i] == null) continue;
					_PsBuff[i].Clear(true);
					_PsBuff[i].Play(true);
				}
			}
			_PlayState = PlayState.Play;
			_Event_LifeEnd_OncePlay = false;

			if (_AudioSE > 0 && DreamKnight.Systems.Audio.AudioManager.Instance != null)
			{
				float pitchRand = _AudioPitchRand ? 0.08f : 0f;
				Vector3 soundPos = transform.position + new Vector3(_AudioPosOfs.x, _AudioPosOfs.y, 0f);
				DreamKnight.Systems.Audio.AudioManager.Instance.PlaySFXAt(_AudioSE, soundPos, vol, pitchRand, _AudioIgnoreDistLimit);
			}
		}

		public void Pause()
		{
			for (int i = 0; i < _PsBuff.Length; i++)
			{
				if (_PsBuff[i] == null) continue;
				_PsBuff[i].Pause(true);
			}
			_PlayState = PlayState.Pause;
		}

		public void unregisterParent()
		{
			transform.SetParent(null, true);
		}

		public void Stop(bool inactive = false)
		{
			for (int i = 0; i < _PsBuff.Length; i++)
			{
				if (_PsBuff[i] == null) continue;
				var main = _PsBuff[i].main;
				main.simulationSpeed = Mathf.Max(0.01f, _StopSimSpeed);
				_PsBuff[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
			}
			_PlayState = PlayState.Stop;
			_Event_LifeEnd_OncePlay = true;
			Event_LifeEnd?.Invoke(this);
			if (inactive)
			{
				gameObject.SetActive(false);
			}
		}
	}
}
