using System;
using System.Collections.Generic;
using EntityStates.Seeker;
using HG;
using UnityEngine;

namespace RoR2;

public class CameraTargetParams : MonoBehaviour
{
	[Serializable]
	public enum AimType
	{
		Standard,
		FirstPerson,
		Aura,
		Sprinting,
		OverTheShoulder,
		ZoomedOut,
		GroundToAir
	}

	public class AimRequest : IDisposable
	{
		public readonly AimType aimType;

		private Action<AimRequest> disposeCallback;

		public AimRequest(AimType type, Action<AimRequest> onDispose)
		{
			disposeCallback = onDispose;
			aimType = type;
		}

		public void Dispose()
		{
			disposeCallback?.Invoke(this);
			disposeCallback = null;
		}
	}

	public struct CameraParamsOverrideRequest
	{
		public CharacterCameraParamsData cameraParamsData;

		public float priority;
	}

	internal class CameraParamsOverride
	{
		public CharacterCameraParamsData cameraParamsData;

		public float priority;

		public float enterStartTime;

		public float enterEndTime;

		public float exitStartTime;

		public float exitEndTime;

		public float CalculateAlpha(float t)
		{
			float num = 1f;
			if (t < enterEndTime)
			{
				num *= Mathf.Clamp01(Mathf.InverseLerp(enterStartTime, enterEndTime, t));
			}
			if (t > exitStartTime)
			{
				num *= Mathf.Clamp01(Mathf.InverseLerp(exitEndTime, exitStartTime, t));
			}
			return easeCurve.Evaluate(num);
		}
	}

	public struct CameraParamsOverrideHandle
	{
		internal readonly CameraParamsOverride target;

		public bool isValid => target != null;

		internal CameraParamsOverrideHandle(CameraParamsOverride target)
		{
			this.target = target;
		}
	}

	public CharacterCameraParams cameraParams;

	public Transform cameraPivotTransform;

	[Obsolete]
	[ShowFieldObsolete]
	public float fovOverride;

	[HideInInspector]
	public Vector2 recoil;

	[HideInInspector]
	public bool dontRaycastToPivot;

	private static float targetRecoilDampTime = 0.08f;

	private static float recoilDampTime = 0.05f;

	private Vector2 targetRecoil;

	private Vector2 recoilVelocity;

	private Vector2 targetRecoilVelocity;

	private List<AimRequest> aimRequestStack = new List<AimRequest>();

	private static readonly AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	private List<CameraParamsOverride> cameraParamsOverrides;

	private CharacterCameraParamsData _currentCameraParamsData;

	public ref CharacterCameraParamsData currentCameraParamsData => ref _currentCameraParamsData;

	public void AddRecoil(float verticalMin, float verticalMax, float horizontalMin, float horizontalMax)
	{
		targetRecoil += new Vector2(UnityEngine.Random.Range(horizontalMin, horizontalMax), UnityEngine.Random.Range(verticalMin, verticalMax));
	}

	public AimRequest RequestAimType(AimType aimType)
	{
		switch (aimType)
		{
		case AimType.Aura:
		{
			CharacterCameraParamsData data3 = cameraParams.data;
			data3.idealLocalCameraPos.value += new Vector3(0f, 1.5f, -7f);
			CameraParamsOverrideHandle overrideHandle3 = AddParamsOverride(new CameraParamsOverrideRequest
			{
				cameraParamsData = data3,
				priority = 0.1f
			}, 0.5f);
			AimRequest aimRequest4 = new AimRequest(aimType, delegate(AimRequest aimRequest)
			{
				RemoveRequest(aimRequest);
				RemoveParamsOverride(overrideHandle3, 0.5f);
			});
			aimRequestStack.Add(aimRequest4);
			return aimRequest4;
		}
		case AimType.ZoomedOut:
		{
			CharacterCameraParamsData data2 = cameraParams.data;
			data2.idealLocalCameraPos.value += new Vector3(0f, 2f, -20f);
			CameraParamsOverrideHandle overrideHandle2 = AddParamsOverride(new CameraParamsOverrideRequest
			{
				cameraParamsData = data2,
				priority = 0.1f
			}, 0.5f);
			AimRequest aimRequest3 = new AimRequest(aimType, delegate(AimRequest aimRequest)
			{
				RemoveRequest(aimRequest);
				RemoveParamsOverride(overrideHandle2, 0.5f);
			});
			aimRequestStack.Add(aimRequest3);
			return aimRequest3;
		}
		case AimType.GroundToAir:
		{
			CharacterCameraParamsData data = cameraParams.data;
			data.idealLocalCameraPos.value += new Vector3(0f, 3f, -25f);
			CameraParamsOverrideHandle overrideHandle = AddParamsOverride(new CameraParamsOverrideRequest
			{
				cameraParamsData = data,
				priority = 0.1f
			}, 0.4f);
			AimRequest aimRequest2 = new AimRequest(aimType, delegate(AimRequest aimRequest)
			{
				RemoveRequest(aimRequest);
				RemoveParamsOverride(overrideHandle, UnseenHand.cameraTransitionOutTime);
			});
			aimRequestStack.Add(aimRequest2);
			return aimRequest2;
		}
		default:
			return null;
		}
	}

	private void RemoveRequest(AimRequest request)
	{
		aimRequestStack.Remove(request);
	}

	private void Awake()
	{
		CharacterBody component = GetComponent<CharacterBody>();
		if ((bool)component && cameraPivotTransform == null)
		{
			cameraPivotTransform = component.aimOriginTransform;
		}
		cameraParamsOverrides = CollectionPool<CameraParamsOverride, List<CameraParamsOverride>>.RentCollection();
	}

	private void OnDestroy()
	{
		cameraParamsOverrides = CollectionPool<CameraParamsOverride, List<CameraParamsOverride>>.ReturnCollection(cameraParamsOverrides);
	}

	private void Update()
	{
		targetRecoil = Vector2.SmoothDamp(targetRecoil, Vector2.zero, ref targetRecoilVelocity, targetRecoilDampTime, 180f, Time.deltaTime);
		recoil = Vector2.SmoothDamp(recoil, targetRecoil, ref recoilVelocity, recoilDampTime, 180f, Time.deltaTime);
		CalcParams(out currentCameraParamsData);
		float time = Time.time;
		for (int num = cameraParamsOverrides.Count - 1; num >= 0; num--)
		{
			if (cameraParamsOverrides[num].exitEndTime <= time)
			{
				cameraParamsOverrides.RemoveAt(num);
			}
		}
	}

	public CameraParamsOverrideHandle AddParamsOverride(CameraParamsOverrideRequest request, float transitionDuration = 0.2f)
	{
		float time = Time.time;
		CameraParamsOverride cameraParamsOverride = new CameraParamsOverride
		{
			cameraParamsData = request.cameraParamsData,
			enterStartTime = time,
			enterEndTime = time + transitionDuration,
			exitStartTime = float.PositiveInfinity,
			exitEndTime = float.PositiveInfinity,
			priority = request.priority
		};
		int i;
		for (i = 0; i < cameraParamsOverrides.Count && !(request.priority <= cameraParamsOverrides[i].priority); i++)
		{
		}
		cameraParamsOverrides.Insert(i, cameraParamsOverride);
		return new CameraParamsOverrideHandle(cameraParamsOverride);
	}

	public CameraParamsOverrideHandle RemoveParamsOverride(CameraParamsOverrideHandle handle, float transitionDuration = 0.2f)
	{
		if (cameraParamsOverrides == null)
		{
			return default(CameraParamsOverrideHandle);
		}
		CameraParamsOverride cameraParamsOverride = null;
		for (int i = 0; i < cameraParamsOverrides.Count; i++)
		{
			if (handle.target == cameraParamsOverrides[i])
			{
				cameraParamsOverride = cameraParamsOverrides[i];
				break;
			}
		}
		if (cameraParamsOverride == null || cameraParamsOverride.exitStartTime != float.PositiveInfinity)
		{
			return default(CameraParamsOverrideHandle);
		}
		cameraParamsOverride.exitEndTime = (cameraParamsOverride.exitStartTime = Time.time) + transitionDuration;
		return default(CameraParamsOverrideHandle);
	}

	public void CalcParams(out CharacterCameraParamsData dest)
	{
		dest = (cameraParams ? cameraParams.data : CharacterCameraParamsData.basic);
		float time = Time.time;
		for (int i = 0; i < cameraParamsOverrides.Count; i++)
		{
			CameraParamsOverride cameraParamsOverride = cameraParamsOverrides[i];
			if (cameraParamsOverride != null && cameraParamsOverride.cameraParamsData.overrideFirstPersonFadeDuration > 0f)
			{
				dest.overrideFirstPersonFadeDuration += cameraParamsOverride.cameraParamsData.overrideFirstPersonFadeDuration;
			}
			CharacterCameraParamsData.Blend(in cameraParamsOverride.cameraParamsData, ref dest, cameraParamsOverride.CalculateAlpha(time));
		}
	}
}
