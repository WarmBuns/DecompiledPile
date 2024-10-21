using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2;

[RequireComponent(typeof(Camera))]
public class UICamera : MonoBehaviour
{
	public delegate void UICameraDelegate(UICamera sceneCamera);

	private static readonly List<UICamera> instancesList = new List<UICamera>();

	public static readonly ReadOnlyCollection<UICamera> readOnlyInstancesList = new ReadOnlyCollection<UICamera>(instancesList);

	public Camera camera { get; private set; }

	public CameraRigController cameraRigController { get; private set; }

	public static event UICameraDelegate onUICameraPreCull;

	public static event UICameraDelegate onUICameraPreRender;

	public static event UICameraDelegate onUICameraPostRender;

	private void Awake()
	{
		camera = GetComponent<Camera>();
		cameraRigController = GetComponentInParent<CameraRigController>();
		camera.eventMask = ~camera.cullingMask;
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void OnPreCull()
	{
		if (UICamera.onUICameraPreCull != null)
		{
			UICamera.onUICameraPreCull(this);
		}
	}

	private void OnPreRender()
	{
		if (UICamera.onUICameraPreRender != null)
		{
			UICamera.onUICameraPreRender(this);
		}
	}

	private void OnPostRender()
	{
		if (UICamera.onUICameraPostRender != null)
		{
			UICamera.onUICameraPostRender(this);
		}
	}

	public EventSystem GetAssociatedEventSystem()
	{
		if ((bool)cameraRigController.viewer && cameraRigController.viewer.localUser != null)
		{
			return cameraRigController.viewer.localUser.eventSystem;
		}
		return null;
	}

	public static UICamera FindViewerUICamera(LocalUser localUserViewer)
	{
		if (localUserViewer != null)
		{
			for (int i = 0; i < instancesList.Count; i++)
			{
				if (instancesList[i].cameraRigController.viewer.localUser == localUserViewer)
				{
					return instancesList[i];
				}
			}
		}
		return null;
	}
}
