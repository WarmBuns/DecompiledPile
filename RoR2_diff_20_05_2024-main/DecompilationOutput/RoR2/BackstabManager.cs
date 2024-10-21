using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2;

public static class BackstabManager
{
	private class BackstabVisualizer
	{
		private class IndicatorInfo
		{
			public GameObject gameObject;

			public Transform transform;

			public ParticleSystem particleSystem;

			public ParticleSystemRenderer renderer;

			public float lastDisplayTime;
		}

		private CameraRigController camera;

		public CharacterBody targetBody;

		private readonly Dictionary<CharacterBody, IndicatorInfo> bodyToIndicator = new Dictionary<CharacterBody, IndicatorInfo>();

		private static readonly Dictionary<CharacterBody, IndicatorInfo> buffer = new Dictionary<CharacterBody, IndicatorInfo>();

		private readonly Stack<IndicatorInfo> indicatorPool = new Stack<IndicatorInfo>();

		private static GameObject indicatorPrefab;

		public void Install(CameraRigController newCamera)
		{
			camera = newCamera;
			RoR2Application.onLateUpdate += UpdateIndicators;
		}

		public void Uninstall()
		{
			RoR2Application.onLateUpdate -= UpdateIndicators;
			camera = null;
			foreach (KeyValuePair<CharacterBody, IndicatorInfo> item in bodyToIndicator)
			{
				UnityEngine.Object.Destroy(item.Value.gameObject);
			}
			bodyToIndicator.Clear();
			foreach (IndicatorInfo item2 in indicatorPool)
			{
				UnityEngine.Object.Destroy(item2.gameObject);
			}
			indicatorPool.Clear();
		}

		[InitDuringStartup]
		private static void OnLoad()
		{
			LegacyResourcesAPI.LoadAsyncCallback("Prefabs/VFX/BackstabIndicator", delegate(GameObject operationResult)
			{
				indicatorPrefab = operationResult;
			});
		}

		private void UpdateIndicators()
		{
			Vector3 corePosition = targetBody.corePosition;
			TeamIndex teamIndex = targetBody.teamComponent.teamIndex;
			buffer.Clear();
			float unscaledTime = Time.unscaledTime;
			foreach (CharacterBody readOnlyInstances in CharacterBody.readOnlyInstancesList)
			{
				bool num = TeamManager.IsTeamEnemy(teamIndex, readOnlyInstances.teamComponent.teamIndex);
				bool flag = false;
				if (num)
				{
					flag = IsBackstab(readOnlyInstances.corePosition - corePosition, readOnlyInstances);
				}
				if (num && flag)
				{
					IndicatorInfo value = null;
					if (!bodyToIndicator.TryGetValue(readOnlyInstances, out value))
					{
						value = AddIndicator(readOnlyInstances);
					}
					value.lastDisplayTime = unscaledTime;
				}
			}
			List<CharacterBody> list = CollectionPool<CharacterBody, List<CharacterBody>>.RentCollection();
			foreach (KeyValuePair<CharacterBody, IndicatorInfo> item in bodyToIndicator)
			{
				if (item.Value.lastDisplayTime != unscaledTime)
				{
					list.Add(item.Key);
				}
			}
			foreach (CharacterBody item2 in list)
			{
				RemoveIndicator(item2);
			}
			list = CollectionPool<CharacterBody, List<CharacterBody>>.ReturnCollection(list);
		}

		private IndicatorInfo AddIndicator(CharacterBody victimBody)
		{
			IndicatorInfo indicatorInfo = null;
			if (indicatorPool.Count > 0)
			{
				indicatorInfo = indicatorPool.Pop();
			}
			else
			{
				indicatorInfo = new IndicatorInfo();
				indicatorInfo.gameObject = UnityEngine.Object.Instantiate(indicatorPrefab);
				indicatorInfo.transform = indicatorInfo.gameObject.transform;
				indicatorInfo.particleSystem = indicatorInfo.gameObject.GetComponent<ParticleSystem>();
				indicatorInfo.renderer = indicatorInfo.gameObject.GetComponent<ParticleSystemRenderer>();
				indicatorInfo.renderer.enabled = false;
			}
			indicatorInfo.gameObject.SetActive(value: true);
			indicatorInfo.particleSystem.Play();
			bodyToIndicator[victimBody] = indicatorInfo;
			return indicatorInfo;
		}

		private void RemoveIndicator(CharacterBody victimBody)
		{
			IndicatorInfo indicatorInfo = bodyToIndicator[victimBody];
			if ((bool)indicatorInfo.gameObject)
			{
				indicatorInfo.particleSystem.Stop();
				indicatorInfo.gameObject.SetActive(value: false);
			}
			bodyToIndicator.Remove(victimBody);
			indicatorPool.Push(indicatorInfo);
		}

		public void OnPreCull()
		{
			foreach (KeyValuePair<CharacterBody, IndicatorInfo> item in bodyToIndicator)
			{
				CharacterBody key = item.Key;
				Transform transform = item.Value.transform;
				if ((bool)key)
				{
					Vector3? bodyForward = GetBodyForward(key);
					if (bodyForward.HasValue)
					{
						transform.forward = -bodyForward.Value;
					}
					transform.position = key.corePosition - transform.forward * key.radius;
					item.Value.renderer.enabled = true;
				}
			}
		}

		public void OnPostRender()
		{
			foreach (KeyValuePair<CharacterBody, IndicatorInfo> item in bodyToIndicator)
			{
				item.Value.renderer.enabled = false;
			}
		}

		private void Update()
		{
			UpdateIndicators();
		}
	}

	public static GameObject backstabImpactEffectPrefab = null;

	private static readonly bool enableVisualizerSystem = false;

	private static readonly float showBackstabThreshold = Mathf.Cos(MathF.PI / 4f);

	private static readonly Dictionary<CameraRigController, BackstabVisualizer> camToVisualizer = new Dictionary<CameraRigController, BackstabVisualizer>();

	public static bool IsBackstab(Vector3 attackerCorePositionToHitPosition, CharacterBody victimBody)
	{
		if (!victimBody.canReceiveBackstab)
		{
			return false;
		}
		Vector3? bodyForward = GetBodyForward(victimBody);
		if (bodyForward.HasValue)
		{
			return Vector3.Dot(attackerCorePositionToHitPosition, bodyForward.Value) > 0f;
		}
		return false;
	}

	private static Vector3? GetBodyForward(CharacterBody characterBody)
	{
		Vector3? vector = null;
		return (!characterBody.characterDirection) ? new Vector3?(characterBody.transform.forward) : new Vector3?(characterBody.characterDirection.forward);
	}

	[InitDuringStartup]
	private static void Init()
	{
		if (enableVisualizerSystem)
		{
			InitVisualizerSystem();
		}
		LegacyResourcesAPI.LoadAsyncCallback("Effects/ImpactEffects/BackstabSpark", delegate(GameObject operationResult)
		{
			backstabImpactEffectPrefab = operationResult;
		});
	}

	private static bool ShouldShowBackstab(Vector3 attackerCorePosition, CharacterBody victimBody)
	{
		if (!victimBody.canReceiveBackstab)
		{
			return false;
		}
		Vector3? bodyForward = GetBodyForward(victimBody);
		if (bodyForward.HasValue)
		{
			return Vector3.Dot((attackerCorePosition - victimBody.corePosition).normalized, bodyForward.Value) > showBackstabThreshold;
		}
		return false;
	}

	private static void InitVisualizerSystem()
	{
		CameraRigController.onCameraTargetChanged += OnCameraTargetChanged;
		CameraRigController.onCameraEnableGlobal += OnCameraDiscovered;
		CameraRigController.onCameraDisableGlobal += OnCameraLost;
		SceneCamera.onSceneCameraPreCull += OnSceneCameraPreCull;
		SceneCamera.onSceneCameraPostRender += OnSceneCameraPostRender;
	}

	private static void OnCameraTargetChanged(CameraRigController camera, GameObject target)
	{
		RefreshCamera(camera);
	}

	private static void OnCameraDiscovered(CameraRigController camera)
	{
		RefreshCamera(camera);
	}

	private static void OnCameraLost(CameraRigController camera)
	{
		RefreshCamera(camera);
	}

	private static void OnSceneCameraPreCull(SceneCamera sceneCam)
	{
		if (camToVisualizer.TryGetValue(sceneCam.cameraRigController, out var value))
		{
			value.OnPreCull();
		}
	}

	private static void OnSceneCameraPostRender(SceneCamera sceneCam)
	{
		if (camToVisualizer.TryGetValue(sceneCam.cameraRigController, out var value))
		{
			value.OnPostRender();
		}
	}

	private static void RefreshCamera(CameraRigController camera)
	{
		BackstabVisualizer value;
		bool num = camToVisualizer.TryGetValue(camera, out value);
		GameObject gameObject = (camera.isActiveAndEnabled ? camera.target : null);
		CharacterBody characterBody = (gameObject ? gameObject.GetComponent<CharacterBody>() : null);
		bool flag = (bool)characterBody && characterBody.canPerformBackstab;
		if (num != flag)
		{
			if (flag)
			{
				value = new BackstabVisualizer();
				camToVisualizer.Add(camera, value);
				value.Install(camera);
			}
			else
			{
				value.Uninstall();
				camToVisualizer.Remove(camera);
				value = null;
			}
		}
		if (value != null)
		{
			value.targetBody = characterBody;
		}
	}
}
