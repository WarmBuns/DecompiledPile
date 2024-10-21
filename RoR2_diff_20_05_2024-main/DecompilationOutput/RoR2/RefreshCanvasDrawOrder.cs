using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Canvas))]
public class RefreshCanvasDrawOrder : MonoBehaviour
{
	public int canvasSortingOrderDelta = 1;

	public Canvas canvas;

	private int originalCanvasSortingOrder;

	private bool cachedOriginalCanvasSortingOrder;

	private void OnEnable()
	{
		if (canvas != null)
		{
			if (!cachedOriginalCanvasSortingOrder)
			{
				originalCanvasSortingOrder = canvas.sortingOrder;
				cachedOriginalCanvasSortingOrder = true;
			}
			canvas.overrideSorting = true;
			canvas.sortingOrder = -1000;
			canvas.sortingOrder = originalCanvasSortingOrder + canvasSortingOrderDelta;
		}
	}
}
