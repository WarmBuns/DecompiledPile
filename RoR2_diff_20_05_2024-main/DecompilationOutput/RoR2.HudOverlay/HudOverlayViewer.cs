using System;
using System.Collections.Generic;
using HG;
using HG.Collections.Generic;
using RoR2.UI;
using UnityEngine;

namespace RoR2.HudOverlay;

public class HudOverlayViewer : MonoBehaviour
{
	public HUD hud;

	public ChildLocator childLocator;

	private CameraRigController _cameraRigController;

	private GameObject _target;

	private AssociationList<OverlayController, GameObject> overlayControllerToInstance = new AssociationList<OverlayController, GameObject>();

	private CameraRigController cameraRigController
	{
		get
		{
			return _cameraRigController;
		}
		set
		{
			if ((object)_cameraRigController != value)
			{
				if ((object)_cameraRigController != null)
				{
					OnCameraRigControllerLost(_cameraRigController);
				}
				_cameraRigController = value;
				if ((object)_cameraRigController != null)
				{
					OnCameraRigControllerDiscovered(_cameraRigController);
				}
			}
		}
	}

	public GameObject target
	{
		get
		{
			return _target;
		}
		private set
		{
			if ((object)_target != value)
			{
				if ((object)_target != null)
				{
					OnTargetLost(_target);
				}
				_target = value;
				if ((object)_target != null)
				{
					OnTargetDiscovered(_target);
				}
			}
		}
	}

	public event Action<HudOverlayViewer, GameObject> onTargetDiscovered;

	public event Action<HudOverlayViewer, GameObject> onTargetLost;

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void OnDestroy()
	{
		target = null;
		cameraRigController = null;
		List<OverlayController> list = CollectionPool<OverlayController, List<OverlayController>>.RentCollection();
		SetOverlays(list);
		CollectionPool<OverlayController, List<OverlayController>>.ReturnCollection(list);
	}

	private void Update()
	{
		cameraRigController = (hud ? hud.cameraRigController : null);
		target = (cameraRigController ? cameraRigController.target : null);
		List<OverlayController> list = CollectionPool<OverlayController, List<OverlayController>>.RentCollection();
		HudOverlayManager.GetGlobalOverlayControllers(list);
		HudOverlayManager.GetTargetTracker(target)?.GetOverlayControllers(list);
		SetOverlays(list);
		CollectionPool<OverlayController, List<OverlayController>>.ReturnCollection(list);
	}

	private void OnCameraRigControllerDiscovered(CameraRigController cameraRigController)
	{
		target = cameraRigController.target;
	}

	private void OnCameraRigControllerLost(CameraRigController cameraRigController)
	{
		target = null;
	}

	private void OnTargetDiscovered(GameObject target)
	{
	}

	private void OnTargetLost(GameObject target)
	{
	}

	private void AddOverlay(OverlayController overlayController)
	{
		Transform transform = childLocator.FindChild(overlayController.creationParams.childLocatorEntry);
		if ((bool)transform)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(overlayController.creationParams.prefab, transform);
			overlayControllerToInstance[overlayController] = gameObject;
			overlayController.OnInstanceAdded(gameObject);
		}
	}

	private void RemoveOverlay(OverlayController overlayController)
	{
		if (overlayControllerToInstance.TryGetValue(overlayController, out var value))
		{
			overlayControllerToInstance.Remove(overlayController);
			overlayController.OnInstanceRemoved(value);
			UnityEngine.Object.Destroy(value);
		}
	}

	private void SetOverlays(List<OverlayController> newOverlayControllers)
	{
		List<OverlayController> list = CollectionPool<OverlayController, List<OverlayController>>.RentCollection();
		List<OverlayController> list2 = CollectionPool<OverlayController, List<OverlayController>>.RentCollection();
		for (int num = overlayControllerToInstance.Count - 1; num >= 0; num--)
		{
			OverlayController key = overlayControllerToInstance[num].Key;
			if (!newOverlayControllers.Contains(key))
			{
				list2.Add(key);
			}
		}
		for (int i = 0; i < newOverlayControllers.Count; i++)
		{
			OverlayController overlayController = newOverlayControllers[i];
			if (!overlayControllerToInstance.ContainsKey(overlayController))
			{
				list.Add(overlayController);
			}
		}
		foreach (OverlayController item in list2)
		{
			RemoveOverlay(item);
		}
		foreach (OverlayController item2 in list)
		{
			AddOverlay(item2);
		}
		CollectionPool<OverlayController, List<OverlayController>>.ReturnCollection(list2);
		CollectionPool<OverlayController, List<OverlayController>>.ReturnCollection(list);
	}
}
