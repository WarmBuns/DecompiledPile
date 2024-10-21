using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(HudElement))]
public class SniperRangeIndicator : MonoBehaviour
{
	public TextMeshProUGUI label;

	private HudElement hudElement;

	private void Awake()
	{
		hudElement = GetComponent<HudElement>();
	}

	private void FixedUpdate()
	{
		float num = float.PositiveInfinity;
		if ((bool)hudElement.targetCharacterBody)
		{
			InputBankTest component = hudElement.targetCharacterBody.GetComponent<InputBankTest>();
			if ((bool)component)
			{
				Ray ray = new Ray(component.aimOrigin, component.aimDirection);
				RaycastHit hitInfo = default(RaycastHit);
				if (Util.CharacterRaycast(hudElement.targetCharacterBody.gameObject, ray, out hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
				{
					num = hitInfo.distance;
				}
			}
		}
		label.text = "Dis: " + ((num > 999f) ? "999m" : $"{Mathf.FloorToInt(num):D3}m");
	}
}
