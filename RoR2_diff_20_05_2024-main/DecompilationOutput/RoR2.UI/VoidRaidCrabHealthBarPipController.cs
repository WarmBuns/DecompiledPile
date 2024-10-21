using UnityEngine;

namespace RoR2.UI;

public class VoidRaidCrabHealthBarPipController : MonoBehaviour
{
	[SerializeField]
	private GameObject pipPrefab;

	[SerializeField]
	private ItemDef minHealthPercentageDef;

	public void InitializePips(PhasedInventorySetter phasedInventory)
	{
		int numPhases = phasedInventory.GetNumPhases();
		for (int i = 0; i < numPhases; i++)
		{
			float num = 0.01f * (float)phasedInventory.GetItemCountForPhase(i, minHealthPercentageDef);
			if (num > 0f)
			{
				RectTransform component = Object.Instantiate(pipPrefab, base.transform).GetComponent<RectTransform>();
				component.anchorMin = new Vector2(num, component.anchorMin.y);
				component.anchorMax = new Vector2(num, component.anchorMax.y);
			}
		}
	}
}
