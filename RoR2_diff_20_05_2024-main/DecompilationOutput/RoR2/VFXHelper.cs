using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace RoR2;

public class VFXHelper : IDisposable
{
	private class VFXTransformController : MonoBehaviour
	{
		public Transform followedTransform;

		public bool usePosition;

		public bool useRotation;

		public bool useScale;

		protected new Transform transform { get; private set; }

		private void Awake()
		{
			transform = base.transform;
		}

		private void LateUpdate()
		{
			if ((bool)followedTransform)
			{
				if (usePosition)
				{
					transform.position = followedTransform.position;
				}
				if (useRotation)
				{
					transform.rotation = followedTransform.rotation;
				}
				if (useScale)
				{
					transform.localScale = followedTransform.lossyScale;
				}
			}
		}
	}

	private static readonly Stack<VFXHelper> pool = new Stack<VFXHelper>();

	private GameObject _vfxPrefabReference;

	private object _vfxPrefabAddress;

	public Transform followedTransform;

	public bool useFollowedTransformPosition = true;

	public bool useFollowedTransformRotation = true;

	public bool useFollowedTransformScale = true;

	public GameObject vfxPrefabReference
	{
		get
		{
			return _vfxPrefabReference;
		}
		set
		{
			_vfxPrefabReference = value;
			_vfxPrefabAddress = null;
		}
	}

	public object vfxPrefabAddress
	{
		get
		{
			return _vfxPrefabAddress;
		}
		set
		{
			_vfxPrefabReference = null;
			_vfxPrefabAddress = null;
		}
	}

	public GameObject vfxInstance { get; private set; }

	public Transform vfxInstanceTransform { get; private set; }

	private VFXTransformController vfxInstanceTransformController { get; set; }

	public bool enabled
	{
		get
		{
			return vfxInstance;
		}
		set
		{
			if ((bool)vfxInstance == value)
			{
				return;
			}
			if (value)
			{
				vfxInstance = null;
				vfxInstanceTransform = null;
				Vector3 vector = Vector3.zero;
				Quaternion quaternion = Quaternion.identity;
				Vector3 vector2 = Vector3.one;
				if ((bool)followedTransform)
				{
					vector = (useFollowedTransformPosition ? followedTransform.position : vector);
					quaternion = (useFollowedTransformRotation ? followedTransform.rotation : quaternion);
					vector2 = (useFollowedTransformScale ? followedTransform.lossyScale : vector2);
				}
				if (vfxPrefabAddress != null)
				{
					vfxInstance = Addressables.InstantiateAsync(instantiateParameters: new InstantiationParameters(vector, quaternion, null), key: vfxPrefabAddress).WaitForCompletion();
				}
				else if ((bool)vfxPrefabReference)
				{
					vfxInstance = UnityEngine.Object.Instantiate(vfxPrefabReference, vector, quaternion);
				}
				if ((bool)vfxInstance)
				{
					vfxInstanceTransform = vfxInstance.transform;
					vfxInstanceTransform.localScale = vector2;
					vfxInstanceTransformController = vfxInstance.AddComponent<VFXTransformController>();
					vfxInstanceTransformController.usePosition = useFollowedTransformPosition;
					vfxInstanceTransformController.useRotation = useFollowedTransformRotation;
					vfxInstanceTransformController.useScale = useFollowedTransformScale;
				}
			}
			else
			{
				VfxKillBehavior.KillVfxObject(vfxInstance);
				vfxInstance = null;
				vfxInstanceTransform = null;
				vfxInstanceTransformController = null;
			}
		}
	}

	private VFXHelper()
	{
	}

	public static VFXHelper Rent()
	{
		if (pool.Count <= 0)
		{
			return new VFXHelper();
		}
		return pool.Pop();
	}

	public static VFXHelper Return(VFXHelper instance)
	{
		instance.Dispose();
		pool.Push(instance);
		return null;
	}

	public void Dispose()
	{
		enabled = false;
		vfxPrefabAddress = null;
		vfxPrefabReference = null;
		followedTransform = null;
		useFollowedTransformPosition = true;
		useFollowedTransformRotation = true;
		useFollowedTransformScale = true;
	}
}
