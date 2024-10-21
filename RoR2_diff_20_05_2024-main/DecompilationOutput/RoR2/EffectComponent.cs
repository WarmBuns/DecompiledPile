using System;
using RoR2.Orbs;
using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
public class EffectComponent : MonoBehaviour
{
	[HideInInspector]
	[Tooltip("This is assigned to the prefab automatically by EffectCatalog at runtime. Do not set this value manually.")]
	public EffectIndex effectIndex = EffectIndex.Invalid;

	[NonSerialized]
	public EffectData effectData;

	[Tooltip("Positions the effect at the transform referenced by the effect data if available.")]
	public bool positionAtReferencedTransform;

	[Tooltip("Parents the effect to the transform object referenced by the effect data if available.")]
	public bool parentToReferencedTransform;

	[Tooltip("Indicates that this is a special effect that won't use effectdata (mostly used to hide errors and warnings for specific effects that cannot be created with SpawnEffect)")]
	public bool noEffectData;

	[Tooltip("Causes this object to adopt the scale received in the effectdata.")]
	public bool applyScale;

	[Tooltip("The sound to play whenever this effect is dispatched, regardless of whether or not it actually ends up spawning.")]
	public string soundName;

	[Tooltip("Ignore Z scale when adopting scale values from effectdata.")]
	public bool disregardZScale;

	private bool didResolveReferencedObject;

	private GameObject referencedObject;

	private bool didResolveReferencedChildTransform;

	private Transform referencedChildTransform;

	private bool didResolveReferencedHurtBox;

	private GameObject referencedHurtBoxGameObject;

	public void Reset()
	{
		if (noEffectData || effectData == null)
		{
			return;
		}
		Transform transform = null;
		if (positionAtReferencedTransform | parentToReferencedTransform)
		{
			transform = effectData.ResolveChildLocatorTransformReference();
		}
		if ((bool)transform)
		{
			if (positionAtReferencedTransform)
			{
				base.transform.position = transform.position;
				base.transform.rotation = transform.rotation;
			}
			if (parentToReferencedTransform)
			{
				base.transform.SetParent(transform, worldPositionStays: true);
			}
		}
		if (applyScale)
		{
			float scale = effectData.scale;
			base.transform.localScale = new Vector3(scale, scale, scale);
		}
	}

	private void Start()
	{
		if (effectData == null)
		{
			Debug.LogErrorFormat(base.gameObject, "Object {0} should not be instantiated by means other than EffectManager.SpawnEffect. This WILL result in an NRE!!! Use EffectManager.SpawnEffect or don't use EffectComponent!!!!!", base.gameObject);
		}
		Reset();
	}

	[ContextMenu("Attempt to Upgrade Sfx Setup")]
	private void AttemptToUpgradeSfxSetup()
	{
		AkEvent[] components = GetComponents<AkEvent>();
		int num = 1281810935;
		if (components.Length == 1 && components[0].triggerList.Count == 1 && components[0].triggerList[0] == num)
		{
			string objectName = components[0].data.WwiseObjectReference.ObjectName;
			soundName = objectName;
			UnityEngine.Object.DestroyImmediate(components[0]);
			UnityEngine.Object.DestroyImmediate(GetComponent<AkGameObj>());
			Rigidbody component = GetComponent<Rigidbody>();
			if (component.isKinematic)
			{
				UnityEngine.Object.DestroyImmediate(component);
			}
			EditorUtil.SetDirty(this);
			EditorUtil.SetDirty(base.gameObject);
		}
	}

	private void OnValidate()
	{
		if (!Application.isPlaying && effectIndex != EffectIndex.Invalid)
		{
			effectIndex = EffectIndex.Invalid;
		}
		if (Application.isPlaying)
		{
			return;
		}
		bool num = GetComponent<AkGameObj>();
		bool flag = GetComponent<OrbEffect>();
		if (num && !flag)
		{
			AkEvent[] components = GetComponents<AkEvent>();
			int item = 1281810935;
			if (components.Length == 1 && components[0].triggerList.Contains(item))
			{
				Debug.LogWarningFormat(base.gameObject, "Effect {0} has an attached AkGameObj. You probably want to use the soundName field of its EffectComponent instead.", Util.GetGameObjectHierarchyName(base.gameObject));
			}
		}
	}

	public GameObject GetReferencedObject()
	{
		if (!didResolveReferencedObject)
		{
			referencedObject = effectData.ResolveNetworkedObjectReference();
			didResolveReferencedObject = true;
		}
		return referencedObject;
	}

	public Transform GetReferencedChildTransform()
	{
		if (!didResolveReferencedChildTransform)
		{
			referencedChildTransform = effectData.ResolveChildLocatorTransformReference();
			didResolveReferencedChildTransform = true;
		}
		return referencedChildTransform;
	}

	public GameObject GetReferencedHurtBoxGameObject()
	{
		if (!didResolveReferencedHurtBox)
		{
			referencedHurtBoxGameObject = effectData.ResolveHurtBoxReference();
			didResolveReferencedHurtBox = true;
		}
		return referencedHurtBoxGameObject;
	}
}
