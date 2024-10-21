using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class PieChartLegend : MonoBehaviour
{
	[SerializeField]
	private PieChartMeshController _source;

	public RectTransform container;

	public GameObject stripPrefab;

	private UIElementAllocator<ChildLocator> stripAllocator;

	private PieChartMeshController _subscribedSource;

	public PieChartMeshController source
	{
		get
		{
			return _source;
		}
		set
		{
			if ((object)_source != value)
			{
				_source = value;
				subscribedSource = source;
			}
		}
	}

	private PieChartMeshController subscribedSource
	{
		get
		{
			return _subscribedSource;
		}
		set
		{
			if ((object)_subscribedSource != value)
			{
				if ((bool)_subscribedSource)
				{
					_subscribedSource.onRebuilt -= OnSourceUpdated;
				}
				_subscribedSource = value;
				if ((bool)_subscribedSource)
				{
					_subscribedSource.onRebuilt += OnSourceUpdated;
				}
			}
		}
	}

	private void Awake()
	{
		InitStripAllocator();
		subscribedSource = source;
	}

	private void OnDestroy()
	{
		subscribedSource = null;
	}

	private void OnValidate()
	{
		InitStripAllocator();
		subscribedSource = source;
		Invoke("Rebuild", 0f);
	}

	private void InitStripAllocator()
	{
		if (stripAllocator != null && (stripAllocator.containerTransform != container || stripAllocator.elementPrefab != stripPrefab))
		{
			stripAllocator.AllocateElements(0);
			stripAllocator = null;
		}
		if (stripAllocator == null)
		{
			stripAllocator = new UIElementAllocator<ChildLocator>(container, stripPrefab, markElementsUnsavable: true, acquireExistingChildren: true);
		}
	}

	private void Rebuild()
	{
		if (!source || !stripAllocator.containerTransform || !stripAllocator.elementPrefab)
		{
			stripAllocator.AllocateElements(0);
			return;
		}
		int num = 0;
		for (int i = 0; i < source.sliceCount; i++)
		{
			if (!(source.GetSliceInfo(i).weight / source.totalSliceWeight <= source.minimumRequiredWeightToDisplay) || num < 16)
			{
				num++;
			}
		}
		int maxSlices = _source.MaxSlices;
		stripAllocator.AllocateElements((num > maxSlices) ? maxSlices : num);
		ReadOnlyCollection<ChildLocator> elements = stripAllocator.elements;
		int j = 0;
		int num2 = num;
		int num3 = 0;
		for (; j < num2 && j < elements.Count; j++)
		{
			PieChartMeshController.SliceInfo sliceInfo = source.GetSliceInfo(j);
			if (source.GetSliceInfo(j).weight / source.totalSliceWeight <= source.minimumRequiredWeightToDisplay && num3 >= 16)
			{
				continue;
			}
			num3++;
			ChildLocator childLocator = elements[j];
			Graphic graphic = childLocator.FindChild("ColorBox")?.GetComponent<Graphic>();
			TMP_Text tMP_Text = childLocator.FindChild("Label")?.GetComponent<TMP_Text>();
			bool flag = num >= maxSlices && j == maxSlices - 1;
			if ((bool)graphic)
			{
				graphic.color = sliceInfo.color;
				graphic.gameObject.SetActive(!flag);
			}
			if ((bool)tMP_Text)
			{
				if (flag)
				{
					tMP_Text.SetText("...");
					break;
				}
				tMP_Text.SetText(sliceInfo.tooltipContent.GetTitleText());
			}
		}
	}

	private void OnSourceUpdated()
	{
		Rebuild();
	}
}
