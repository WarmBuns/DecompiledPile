using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Hologram;

public class HologramProjector : MonoBehaviour
{
	[Tooltip("The range in meters at which the hologram begins to display.")]
	public float displayDistance = 15f;

	[Tooltip("The position at which to display the hologram.")]
	public Transform hologramPivot;

	[Tooltip("Whether or not the hologram will pivot to the player")]
	public bool disableHologramRotation;

	private float transformDampVelocity;

	private IHologramContentProvider contentProvider;

	private float viewerReselectTimer;

	private float viewerReselectInterval = 0.25f;

	private Transform cachedViewer;

	private CharacterBody cachedViewerBody;

	private NetworkInstanceId cachedViewerObjectId;

	private NetworkIdentity cachedNetworkIdentity;

	public GameObject hologramContentInstance;

	private Transform viewerTransform;

	private CharacterBody viewerBody;

	private void Awake()
	{
		contentProvider = GetComponent<IHologramContentProvider>();
		cachedNetworkIdentity = GetComponent<NetworkIdentity>();
	}

	private Transform FindViewer(Vector3 position)
	{
		if (viewerReselectTimer > 0f)
		{
			return cachedViewer;
		}
		viewerReselectTimer = viewerReselectInterval;
		cachedViewer = null;
		float num = float.PositiveInfinity;
		ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
		int i = 0;
		for (int count = instances.Count; i < count; i++)
		{
			GameObject bodyObject = instances[i].master.GetBodyObject();
			if ((bool)bodyObject)
			{
				float sqrMagnitude = (bodyObject.transform.position - position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					cachedViewer = bodyObject.transform;
					cachedViewerBody = bodyObject.GetComponent<CharacterBody>();
				}
			}
		}
		return cachedViewer;
	}

	private void CacheViewerBody(Transform _viewer)
	{
		if (!(_viewer == null) && (!viewerBody || !(viewerTransform == _viewer)) && _viewer.TryGetComponent<CharacterBody>(out var component))
		{
			viewerTransform = _viewer;
			viewerBody = component;
		}
	}

	public Vector3 GetHologramPivotPosition()
	{
		if (!hologramPivot)
		{
			return base.transform.position;
		}
		return hologramPivot.position;
	}

	public CharacterBody GetCachedViewerBody()
	{
		return cachedViewerBody;
	}

	public NetworkInstanceId GetCachedViewerObjectId()
	{
		return cachedViewerObjectId;
	}

	public void UpdateForViewer(Transform viewer, Vector3 pivotPosition)
	{
		bool flag = false;
		Vector3 vector = (viewer ? viewer.position : base.transform.position);
		Vector3 forward = Vector3.zero;
		if ((bool)viewer)
		{
			forward = pivotPosition - vector;
			if (forward.sqrMagnitude <= displayDistance * displayDistance)
			{
				flag = true;
			}
		}
		if (flag)
		{
			flag = contentProvider.ShouldDisplayHologram(viewer.gameObject);
		}
		if (flag)
		{
			if (!hologramContentInstance)
			{
				BuildHologram();
			}
			if ((bool)hologramContentInstance && contentProvider != null)
			{
				contentProvider.UpdateHologramContent(hologramContentInstance, viewer);
				if (!disableHologramRotation)
				{
					hologramContentInstance.transform.rotation = Util.SmoothDampQuaternion(hologramContentInstance.transform.rotation, Util.QuaternionSafeLookRotation(forward), ref transformDampVelocity, 0.2f);
				}
			}
		}
		else
		{
			DestroyHologram();
		}
	}

	private void Update()
	{
		viewerReselectTimer -= Time.deltaTime;
		Vector3 hologramPivotPosition = GetHologramPivotPosition();
		Transform viewer = FindViewer(hologramPivotPosition);
		UpdateForViewer(viewer, hologramPivotPosition);
	}

	private void BuildHologram()
	{
		DestroyHologram();
		if (contentProvider == null)
		{
			return;
		}
		GameObject hologramContentPrefab = contentProvider.GetHologramContentPrefab();
		if ((bool)hologramContentPrefab)
		{
			hologramContentInstance = Object.Instantiate(hologramContentPrefab);
			hologramContentInstance.transform.parent = (hologramPivot ? hologramPivot : base.transform);
			hologramContentInstance.transform.localPosition = Vector3.zero;
			hologramContentInstance.transform.localRotation = Quaternion.identity;
			hologramContentInstance.transform.localScale = Vector3.one;
			if ((bool)viewerTransform && !disableHologramRotation)
			{
				Vector3 obj = (hologramPivot ? hologramPivot.position : base.transform.position);
				_ = viewerTransform.position;
				Vector3 forward = obj - viewerTransform.position;
				hologramContentInstance.transform.rotation = Util.QuaternionSafeLookRotation(forward);
			}
			contentProvider.UpdateHologramContent(hologramContentInstance, viewerTransform);
		}
	}

	private void DestroyHologram()
	{
		if ((bool)hologramContentInstance)
		{
			Object.Destroy(hologramContentInstance);
		}
		hologramContentInstance = null;
	}
}
