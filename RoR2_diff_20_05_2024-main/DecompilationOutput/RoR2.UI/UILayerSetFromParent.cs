using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(HGButton))]
public class UILayerSetFromParent : MonoBehaviour
{
	private UILayerKey FoundLayer;

	[SerializeField]
	private UILayer layerToFind;

	private void Awake()
	{
		HGButton component = GetComponent<HGButton>();
		if ((object)component == null)
		{
			return;
		}
		IUILayerKeyProvider[] componentsInParent = base.gameObject.GetComponentsInParent<IUILayerKeyProvider>();
		for (int i = 0; i < componentsInParent.Length; i++)
		{
			UILayerKey uILayerKey = componentsInParent[i].GetUILayerKey();
			if ((object)uILayerKey.layer == layerToFind)
			{
				FoundLayer = uILayerKey;
				component.requiredTopLayer = uILayerKey;
				break;
			}
		}
	}

	public void SetLayerActive(bool NewActive)
	{
		if ((bool)FoundLayer)
		{
			FoundLayer.enabled = NewActive;
		}
	}
}
