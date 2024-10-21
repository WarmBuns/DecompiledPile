using System;
using HG;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class RadialSliceGraphic : MaskableGraphic
{
	public struct DisplayData : IEquatable<DisplayData>
	{
		public Radians start;

		public Radians end;

		public float startU;

		public float endU;

		public Material material;

		public Texture texture;

		public Color color;

		public float normalizedInnerRadius;

		public Radians maxQuadWidth;

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is DisplayData other)
			{
				return Equals(other);
			}
			return false;
		}

		public static bool operator ==(DisplayData left, DisplayData right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(DisplayData left, DisplayData right)
		{
			return !left.Equals(right);
		}

		public bool Equals(DisplayData other)
		{
			if (start.Equals(other.start) && end.Equals(other.end) && startU.Equals(other.startU) && endU.Equals(other.endU) && object.Equals(material, other.material) && object.Equals(texture, other.texture) && color.Equals(other.color) && normalizedInnerRadius.Equals(other.normalizedInnerRadius))
			{
				return maxQuadWidth.Equals(other.maxQuadWidth);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((((((start.GetHashCode() * 397) ^ end.GetHashCode()) * 397) ^ startU.GetHashCode()) * 397) ^ endU.GetHashCode()) * 397) ^ ((material != null) ? material.GetHashCode() : 0)) * 397) ^ ((texture != null) ? texture.GetHashCode() : 0)) * 397) ^ color.GetHashCode()) * 397) ^ normalizedInnerRadius.GetHashCode()) * 397) ^ maxQuadWidth.GetHashCode();
		}
	}

	public RectTransform sliceCenterSticker;

	private static readonly DisplayData defaultDisplayData = new DisplayData
	{
		color = Color.white,
		start = Radians.FromRevolutions(0f),
		end = Radians.FromRevolutions(-0.4f),
		startU = 0f,
		endU = 1f,
		maxQuadWidth = (Radians)(MathF.PI / 10f),
		normalizedInnerRadius = 0.2f
	};

	private DisplayData displayData = defaultDisplayData;

	private float currentRadius;

	public override Texture mainTexture
	{
		get
		{
			if (!displayData.texture)
			{
				if (!displayData.material)
				{
					return null;
				}
				return displayData.material.mainTexture;
			}
			return displayData.texture;
		}
	}

	protected RadialSliceGraphic()
	{
		base.useLegacyMeshGeneration = false;
	}

	public void SetDisplayData(in DisplayData newDisplayData)
	{
		if (!(newDisplayData == displayData))
		{
			displayData = newDisplayData;
			UpdateDisplayData();
		}
	}

	private void UpdateDisplayData()
	{
		if (material != displayData.material)
		{
			material = displayData.material;
		}
		if (color != displayData.color)
		{
			color = displayData.color;
		}
		base.SetVerticesDirty();
	}

	private static Vector2 GetDirection(Radians radians)
	{
		return new Vector2(Mathf.Cos((float)radians), Mathf.Sin((float)radians));
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		Rect rect = base.rectTransform.rect;
		currentRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
		float outerRadius = currentRadius;
		float innerRadius = outerRadius * displayData.normalizedInnerRadius;
		Radians right = displayData.start;
		Radians left = displayData.end;
		float a = displayData.startU;
		float b = displayData.endU;
		if (left < right)
		{
			Util.Swap(ref right, ref left);
			Util.Swap(ref a, ref b);
		}
		Radians radians = left - right;
		if (!((float)radians <= 0f) && !((float)displayData.maxQuadWidth <= 0f))
		{
			int right2 = Mathf.CeilToInt((float)radians / (float)displayData.maxQuadWidth);
			Radians right3 = radians / right2;
			float num = 1f / (float)right2;
			float num2 = b - a;
			float quadStartU2 = a;
			Vector2 startDirection2 = GetDirection(right);
			Color color2 = color * displayData.color;
			for (int i = 0; i < right2; i++)
			{
				Vector2 endDirection2 = GetDirection(right + ILSpyHelper_AsRefReadOnly(i + 1) * right3);
				float num3 = a + num2 * ((float)(i + 1) * num);
				AddQuad(in startDirection2, in endDirection2, in color2, quadStartU2, num3);
				startDirection2 = endDirection2;
				quadStartU2 = num3;
			}
		}
		void AddQuad(in Vector2 startDirection, in Vector2 endDirection, in Color color, float quadStartU, float quadEndU)
		{
			int currentVertCount = vh.currentVertCount;
			vh.AddVert(startDirection * innerRadius, color, new Vector2(quadStartU, 0f));
			vh.AddVert(startDirection * outerRadius, color, new Vector2(quadStartU, 1f));
			vh.AddVert(endDirection * outerRadius, color, new Vector2(quadEndU, 1f));
			vh.AddVert(endDirection * innerRadius, color, new Vector2(quadEndU, 0f));
			vh.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
			vh.AddTriangle(currentVertCount + 2, currentVertCount + 3, currentVertCount);
		}
		static ref readonly T ILSpyHelper_AsRefReadOnly<T>(in T temp)
		{
			//ILSpy generated this function to help ensure overload resolution can pick the overload using 'in'
			return ref temp;
		}
	}

	public override bool Raycast(Vector2 sp, Camera eventCamera)
	{
		if (!base.Raycast(sp, eventCamera))
		{
			return false;
		}
		if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(base.rectTransform, sp, eventCamera, out var localPoint))
		{
			return false;
		}
		Color white = Color.white;
		bool result = PointInArc(localPoint, displayData.start, displayData.end, displayData.normalizedInnerRadius * currentRadius, currentRadius);
		color = white;
		return result;
	}

	private static bool PointInArc(Vector2 point, Radians arcStart, Radians arcEnd, float arcInnerRadius, float arcOuterRadius)
	{
		Vector2 direction = GetDirection((arcStart + arcEnd) * 0.5f);
		Radians radians = (arcEnd - arcStart).absolute;
		Vector2 normalized = point.normalized;
		float num = Vector2.Dot(direction, normalized);
		float num2 = Mathf.Cos((float)radians * 0.5f);
		if (num < num2)
		{
			return false;
		}
		float sqrMagnitude = point.sqrMagnitude;
		if (sqrMagnitude < arcInnerRadius * arcInnerRadius)
		{
			return false;
		}
		if (sqrMagnitude > arcOuterRadius * arcOuterRadius)
		{
			return false;
		}
		return true;
	}

	public override void GraphicUpdateComplete()
	{
		base.GraphicUpdateComplete();
		if ((bool)sliceCenterSticker)
		{
			Radians radians = (Radians)Mathf.LerpAngle((float)displayData.start, (float)displayData.end, 0.5f);
			float num = Mathf.Lerp(displayData.normalizedInnerRadius, 1f, 0.5f) * currentRadius;
			Vector3 localPosition = new Vector3(radians.cos * num, radians.sin * num);
			sliceCenterSticker.localPosition = localPosition;
		}
	}
}
