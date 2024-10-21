using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
[ExecuteInEditMode]
[SelectionBase]
public class ItemDisplayRuleComponent : MonoBehaviour
{
	public Object keyAsset;

	public LimbFlags limbMask;

	[HideInInspector]
	[SerializeField]
	private ItemDisplayRuleType _ruleType;

	public string nameInLocator;

	[HideInInspector]
	[SerializeField]
	private GameObject _prefab;

	private GameObject prefabInstance;

	private ItemDisplayRule itemDisplayRule;

	public ItemDisplayRuleType ruleType
	{
		get
		{
			return _ruleType;
		}
		set
		{
			_ruleType = value;
			if (_ruleType != 0)
			{
				prefab = null;
			}
		}
	}

	public GameObject prefab
	{
		get
		{
			return _prefab;
		}
		set
		{
			if (!prefabInstance || _prefab != value)
			{
				_prefab = value;
				BuildPreview();
			}
		}
	}

	private void Start()
	{
		BuildPreview();
	}

	public bool SetItemDisplayRule(ItemDisplayRule newItemDisplayRule, ChildLocator childLocator, Object newKeyAsset)
	{
		itemDisplayRule = newItemDisplayRule;
		string childName = itemDisplayRule.childName;
		Transform transform = childLocator.FindChild(childName);
		if (!transform)
		{
			Debug.LogWarningFormat(childLocator.gameObject, "Could not fully load item display rules for {0} because child {1} could not be found in the child locator.", childLocator.gameObject.name, childName);
			return false;
		}
		base.transform.parent = transform;
		keyAsset = newKeyAsset;
		nameInLocator = itemDisplayRule.childName;
		ruleType = itemDisplayRule.ruleType;
		switch (ruleType)
		{
		case ItemDisplayRuleType.ParentedPrefab:
			base.transform.localPosition = itemDisplayRule.localPos;
			base.transform.localEulerAngles = itemDisplayRule.localAngles;
			base.transform.localScale = itemDisplayRule.localScale;
			prefab = itemDisplayRule.followerPrefab;
			limbMask = LimbFlags.None;
			break;
		case ItemDisplayRuleType.LimbMask:
			prefab = null;
			limbMask = itemDisplayRule.limbMask;
			break;
		}
		return true;
	}

	private void DestroyPreview()
	{
		if ((bool)prefabInstance)
		{
			Object.DestroyImmediate(prefabInstance);
		}
		prefabInstance = null;
	}

	private void BuildPreview()
	{
		DestroyPreview();
		if ((bool)prefab)
		{
			prefabInstance = Object.Instantiate(prefab);
			prefabInstance.name = "Preview";
			prefabInstance.transform.parent = base.transform;
			prefabInstance.transform.localPosition = Vector3.zero;
			prefabInstance.transform.localRotation = Quaternion.identity;
			prefabInstance.transform.localScale = Vector3.one;
			SetPreviewFlags(prefabInstance.transform);
		}
	}

	private static void SetPreviewFlags(Transform transform)
	{
		transform.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
		foreach (Transform item in transform)
		{
			SetPreviewFlags(item);
		}
	}

	private void OnDestroy()
	{
		DestroyPreview();
	}
}
