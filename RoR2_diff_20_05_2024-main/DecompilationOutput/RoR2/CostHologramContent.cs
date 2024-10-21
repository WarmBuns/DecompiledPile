using System.Text;
using TMPro;
using UnityEngine;

namespace RoR2;

public class CostHologramContent : MonoBehaviour
{
	public int displayValue;

	public TextMeshPro targetTextMesh;

	public CostTypeIndex costType;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	private int oldDisplayValue;

	private void FixedUpdate()
	{
		if ((bool)targetTextMesh && oldDisplayValue != displayValue)
		{
			oldDisplayValue = displayValue;
			sharedStringBuilder.Clear();
			Color color = Color.white;
			CostTypeDef costTypeDef = CostTypeCatalog.GetCostTypeDef(costType);
			if (costTypeDef != null)
			{
				costTypeDef.BuildCostStringStyled(displayValue, sharedStringBuilder, forWorldDisplay: true, includeColor: false);
				color = costTypeDef.GetCostColor(forWorldDisplay: true);
			}
			targetTextMesh.SetText(sharedStringBuilder);
			targetTextMesh.color = color;
		}
	}
}
