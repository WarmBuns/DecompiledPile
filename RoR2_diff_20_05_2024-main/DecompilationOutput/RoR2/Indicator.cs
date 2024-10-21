using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public class Indicator
{
	private static class IndicatorManager
	{
		private static readonly List<Indicator> runningIndicators;

		public static void AddIndicator([NotNull] Indicator indicator)
		{
			runningIndicators.Add(indicator);
			RebuildVisualizer(indicator);
		}

		public static void RemoveIndicator([NotNull] Indicator indicator)
		{
			indicator.SetVisualizerInstantiated(newVisualizerInstantiated: false);
			runningIndicators.Remove(indicator);
		}

		static IndicatorManager()
		{
			runningIndicators = new List<Indicator>();
			CameraRigController.onCameraTargetChanged += delegate
			{
				RebuildVisualizerForAll();
			};
			UICamera.onUICameraPreRender += OnPreRenderUI;
			UICamera.onUICameraPostRender += OnPostRenderUI;
			RoR2Application.onUpdate += Update;
		}

		private static void RebuildVisualizerForAll()
		{
			foreach (Indicator runningIndicator in runningIndicators)
			{
				RebuildVisualizer(runningIndicator);
			}
		}

		private static void Update()
		{
			foreach (Indicator runningIndicator in runningIndicators)
			{
				if (runningIndicator.hasVisualizer)
				{
					runningIndicator.UpdateVisualizer();
				}
			}
		}

		private static void RebuildVisualizer(Indicator indicator)
		{
			bool visualizerInstantiated = false;
			foreach (CameraRigController readOnlyInstances in CameraRigController.readOnlyInstancesList)
			{
				if (readOnlyInstances.target == indicator.owner)
				{
					visualizerInstantiated = true;
					break;
				}
			}
			indicator.SetVisualizerInstantiated(visualizerInstantiated);
		}

		private static void OnPreRenderUI(UICamera uiCam)
		{
			GameObject target = uiCam.cameraRigController.target;
			Camera sceneCam = uiCam.cameraRigController.sceneCam;
			foreach (Indicator runningIndicator in runningIndicators)
			{
				bool num = target == runningIndicator.owner;
				runningIndicator.SetVisible(target == runningIndicator.owner);
				if (num)
				{
					runningIndicator.PositionForUI(sceneCam, uiCam.camera);
				}
			}
		}

		private static void OnPostRenderUI(UICamera uiCamera)
		{
			foreach (Indicator runningIndicator in runningIndicators)
			{
				runningIndicator.SetVisible(newVisible: true);
			}
		}
	}

	private GameObject _visualizerPrefab;

	public readonly GameObject owner;

	public Transform targetTransform;

	protected List<Renderer> visualizerRenderers = new List<Renderer>();

	private bool _active;

	private bool visible = true;

	public GameObject visualizerPrefab
	{
		get
		{
			return _visualizerPrefab;
		}
		set
		{
			if (!(_visualizerPrefab == value))
			{
				_visualizerPrefab = value;
				if ((bool)visualizerInstance)
				{
					DestroyVisualizer();
					InstantiateVisualizer();
				}
			}
		}
	}

	protected GameObject visualizerInstance { get; private set; }

	protected Transform visualizerTransform { get; private set; }

	public bool hasVisualizer => visualizerInstance;

	public bool active
	{
		get
		{
			return _active;
		}
		set
		{
			if (_active != value)
			{
				_active = value;
				if (active)
				{
					IndicatorManager.AddIndicator(this);
				}
				else
				{
					IndicatorManager.RemoveIndicator(this);
				}
			}
		}
	}

	public Indicator(GameObject owner, GameObject visualizerPrefab)
	{
		this.owner = owner;
		_visualizerPrefab = visualizerPrefab;
	}

	public void SetVisualizerInstantiated(bool newVisualizerInstantiated)
	{
		if ((bool)visualizerInstance != newVisualizerInstantiated)
		{
			if (newVisualizerInstantiated)
			{
				InstantiateVisualizer();
			}
			else
			{
				DestroyVisualizer();
			}
		}
	}

	private void InstantiateVisualizer()
	{
		visualizerInstance = UnityEngine.Object.Instantiate(visualizerPrefab);
		OnInstantiateVisualizer();
	}

	private void DestroyVisualizer()
	{
		OnDestroyVisualizer();
		UnityEngine.Object.Destroy(visualizerInstance);
		visualizerInstance = null;
	}

	protected void FindRenderers(Transform root)
	{
		visualizerRenderers.AddRange(root.GetComponentsInChildren<Renderer>(includeInactive: true));
	}

	private void FindInputBindingDisplayControllers(Transform root)
	{
		InputBindingDisplayController[] componentsInChildren = root.GetComponentsInChildren<InputBindingDisplayController>(includeInactive: true);
		foreach (InputBindingDisplayController ibdc in componentsInChildren)
		{
			InputBindingDisplayController inputBindingDisplayController = ibdc;
			inputBindingDisplayController.oneFrameAfterTextUpdate = (Action)Delegate.Combine(inputBindingDisplayController.oneFrameAfterTextUpdate, (Action)delegate
			{
				FindRenderers(ibdc.transform);
			});
		}
	}

	public void OnInstantiateVisualizer()
	{
		visualizerTransform = visualizerInstance.transform;
		FindRenderers(visualizerTransform);
		FindInputBindingDisplayControllers(visualizerTransform);
		SetVisibleInternal(visible);
	}

	public virtual void OnDestroyVisualizer()
	{
		visualizerTransform = null;
		visualizerRenderers.Clear();
	}

	public virtual void UpdateVisualizer()
	{
	}

	public virtual void PositionForUI(Camera sceneCamera, Camera uiCamera)
	{
		if ((bool)targetTransform)
		{
			Vector3 position = targetTransform.position;
			Vector3 position2 = sceneCamera.WorldToScreenPoint(position);
			position2.z = ((position2.z > 0f) ? 1f : (-1f));
			Vector3 position3 = uiCamera.ScreenToWorldPoint(position2);
			if (visualizerTransform != null)
			{
				visualizerTransform.position = position3;
			}
		}
	}

	public void SetVisible(bool newVisible)
	{
		newVisible &= (bool)targetTransform;
		if (visible != newVisible)
		{
			SetVisibleInternal(newVisible);
		}
	}

	private void SetVisibleInternal(bool newVisible)
	{
		visible = newVisible;
		foreach (Renderer visualizerRenderer in visualizerRenderers)
		{
			visualizerRenderer.enabled = newVisible;
		}
	}
}
