using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class AdjustGridLayoutCellSize : MonoBehaviour
{
	public enum ExpandSetting
	{
		X,
		Y,
		Both
	}

	public ExpandSetting expandingSetting;

	public GridLayoutGroup gridlayout;

	private int maxConstraintCount;

	private RectTransform layoutRect;

	private void Update()
	{
		UpdateCellSize();
	}

	private void UpdateCellSize()
	{
		if ((bool)gridlayout)
		{
			maxConstraintCount = gridlayout.constraintCount;
			layoutRect = gridlayout.gameObject.GetComponent<RectTransform>();
			float num = (float)(maxConstraintCount - 1) * gridlayout.spacing.x + (float)gridlayout.padding.left + (float)gridlayout.padding.right;
			float num2 = (float)(maxConstraintCount - 1) * gridlayout.spacing.y + (float)gridlayout.padding.top + (float)gridlayout.padding.bottom;
			float width = layoutRect.rect.width;
			float height = layoutRect.rect.height;
			float num3 = width - num;
			float num4 = height - num2;
			float x = num3 / (float)maxConstraintCount;
			float y = num4 / (float)maxConstraintCount;
			Vector2 cellSize = new Vector2(gridlayout.cellSize.x, gridlayout.cellSize.y);
			if (expandingSetting == ExpandSetting.X || expandingSetting == ExpandSetting.Both)
			{
				cellSize.x = x;
			}
			if (expandingSetting == ExpandSetting.Y || expandingSetting == ExpandSetting.Both)
			{
				cellSize.y = y;
			}
			gridlayout.cellSize = cellSize;
		}
	}
}
