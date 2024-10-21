using System;
using System.Collections.ObjectModel;
using HG;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[ExecuteAlways]
public class PieChartMeshController : MonoBehaviour
{
	[Serializable]
	public struct SliceInfo : IEquatable<SliceInfo>
	{
		[Min(0f)]
		public float weight;

		public Color color;

		public TooltipContent tooltipContent;

		public bool Equals(SliceInfo other)
		{
			if (weight.Equals(other.weight) && color.Equals(other.color))
			{
				return tooltipContent.Equals(other.tooltipContent);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is SliceInfo other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((weight.GetHashCode() * 397) ^ color.GetHashCode()) * 397) ^ tooltipContent.GetHashCode();
		}

		public static bool operator ==(SliceInfo left, SliceInfo right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SliceInfo left, SliceInfo right)
		{
			return !left.Equals(right);
		}
	}

	public RectTransform container;

	public GameObject slicePrefab;

	[Range(0f, 1f)]
	public float normalizedInnerRadius;

	[Tooltip("The minimum value required to display percentage and show an entry in the legend.")]
	public float minimumRequiredWeightToDisplay = 0.03f;

	public Texture texture;

	public Material material;

	[ColorUsage(true, true)]
	public Color color;

	public float uTile = 1f;

	public bool individualSegmentUMapping;

	[SerializeField]
	private int maxSlices = 16;

	[SerializeField]
	private SliceInfo[] sliceInfos = Array.Empty<SliceInfo>();

	private int totalSliceCount;

	private UIElementAllocator<RadialSliceGraphic> sliceAllocator;

	private bool slicesDirty = true;

	public int MaxSlices => maxSlices;

	public float totalSliceWeight { get; private set; }

	public int sliceCount => sliceInfos.Length;

	public event Action onRebuilt;

	private void Awake()
	{
		InitSliceAllocator();
	}

	private void Update()
	{
		if (slicesDirty)
		{
			slicesDirty = false;
			RebuildSlices();
		}
	}

	private void OnValidate()
	{
		InitSliceAllocator();
		slicesDirty = true;
	}

	private void InitSliceAllocator()
	{
		sliceAllocator = new UIElementAllocator<RadialSliceGraphic>(container, slicePrefab, markElementsUnsavable: true, acquireExistingChildren: true);
	}

	public void SetSlices([NotNull] SliceInfo[] newSliceInfos)
	{
		if (!ArrayUtils.SequenceEquals(sliceInfos, newSliceInfos))
		{
			Array.Resize(ref sliceInfos, newSliceInfos.Length);
			Array.Copy(newSliceInfos, sliceInfos, sliceInfos.Length);
			slicesDirty = true;
		}
	}

	private void RebuildSlices()
	{
		totalSliceWeight = 0f;
		totalSliceCount = 0;
		for (int i = 0; i < sliceInfos.Length; i++)
		{
			float weight = sliceInfos[i].weight;
			if (!(weight <= 0f))
			{
				totalSliceCount++;
				totalSliceWeight += weight;
			}
		}
		sliceAllocator.AllocateElements(totalSliceCount);
		Radians left = new Radians(MathF.PI / 2f);
		float num = 0f;
		ReadOnlyCollection<RadialSliceGraphic> elements = sliceAllocator.elements;
		int num2 = 0;
		int num3 = 0;
		int count = elements.Count;
		while (num3 < count)
		{
			ref SliceInfo reference = ref sliceInfos[num2++];
			if (reference.weight <= 0f)
			{
				continue;
			}
			float num4 = reference.weight / totalSliceWeight;
			Radians radians = left - new Radians(num4 * (MathF.PI * 2f));
			Radians left2 = new Radians(MathF.PI * 2f);
			float num5 = num - num4 * uTile;
			RadialSliceGraphic radialSliceGraphic = elements[num3++];
			RadialSliceGraphic.DisplayData newDisplayData = new RadialSliceGraphic.DisplayData
			{
				start = left,
				end = radians,
				startU = (individualSegmentUMapping ? 0f : num),
				endU = (individualSegmentUMapping ? uTile : num5),
				normalizedInnerRadius = normalizedInnerRadius,
				maxQuadWidth = left2 / ILSpyHelper_AsRefReadOnly(720f),
				material = material,
				texture = texture,
				color = reference.color * color
			};
			radialSliceGraphic.SetDisplayData(in newDisplayData);
			TooltipProvider component = radialSliceGraphic.GetComponent<TooltipProvider>();
			if ((bool)component)
			{
				component.SetContent(reference.tooltipContent);
			}
			ChildLocator component2 = radialSliceGraphic.GetComponent<ChildLocator>();
			if ((bool)component2)
			{
				Transform transform = component2.FindChild("SliceCenterStickerLabel");
				if ((bool)transform)
				{
					transform.GetComponent<TMP_Text>().SetText((num4 >= minimumRequiredWeightToDisplay) ? Language.GetStringFormatted("PERCENT_FORMAT", num4) : string.Empty);
				}
			}
			left = radians;
			num = num5;
		}
		this.onRebuilt?.Invoke();
		static ref readonly T ILSpyHelper_AsRefReadOnly<T>(in T temp)
		{
			//ILSpy generated this function to help ensure overload resolution can pick the overload using 'in'
			return ref temp;
		}
	}

	public SliceInfo GetSliceInfo(int i)
	{
		return sliceInfos[i];
	}
}
