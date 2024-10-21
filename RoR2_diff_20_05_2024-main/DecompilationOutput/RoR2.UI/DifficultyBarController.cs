using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[ExecuteAlways]
public class DifficultyBarController : MonoBehaviour
{
	[Serializable]
	public struct SegmentDef
	{
		[Tooltip("The default English string to use for the element at design time.")]
		public string debugString;

		[Tooltip("The final language token to use for this element at runtime.")]
		public string token;

		[Tooltip("The color to use for the panel.")]
		public Color color;
	}

	private class SegmentImageAnimation
	{
		public Image segmentImage;

		public float age;

		public float duration;

		public AnimationCurve colorCurve;

		public Color color0;

		public Color color1;

		public void Update(float t)
		{
			segmentImage.color = Color.Lerp(color0, color1, colorCurve.Evaluate(t));
		}
	}

	[Header("Component References")]
	public RectTransform viewPort;

	public RectTransform segmentContainer;

	public Image difficultyBarBackdrop;

	[Header("Layout")]
	[Tooltip("How wide each segment should be.")]
	public float elementWidth;

	public float levelsPerSegment;

	public float debugTime;

	[Header("Segment Parameters")]
	public SegmentDef[] segmentDefs;

	[Tooltip("The prefab to instantiate for each segment.")]
	public GameObject segmentPrefab;

	[Header("Colors")]
	public float pastSaturationMultiplier;

	public float pastValueMultiplier;

	public Color pastLabelColor;

	public float currentSaturationMultiplier;

	public float currentValueMultiplier;

	public Color currentLabelColor;

	public float upcomingSaturationMultiplier;

	public float upcomingValueMultiplier;

	public Color upcomingLabelColor;

	[Tooltip("If the increasedDifficultyBarFX user setting is enabled and the next difficulty color's value is below this, flash white instead.")]
	public float backdropFlashWhiteThreshold = 0.43f;

	[Header("Animations")]
	public AnimationCurve fadeAnimationCurve;

	public float fadeAnimationDuration = 1f;

	public AnimationCurve flashAnimationCurve;

	public float flashAnimationDuration = 0.5f;

	private int currentSegmentIndex = -1;

	private static readonly Color labelFadedColor = Color.Lerp(Color.gray, Color.white, 0.5f);

	[SerializeField]
	private string onDifficultyChangeSoundString = "Play_wDifficulty_New";

	[Header("Final Segment")]
	public Sprite finalSegmentSprite;

	private HUD hud;

	private float scrollX;

	private float scrollXRaw;

	private float lastSegmentScroll;

	[Tooltip("Do not set this manually. Regenerate the children instead.")]
	public Image[] images;

	[Tooltip("Do not set this manually. Regenerate the children instead.")]
	public TextMeshProUGUI[] labels;

	public RawImage[] wormGearImages;

	public float UVScaleToScrollX;

	public float gearUVOffset;

	private readonly List<SegmentImageAnimation> playingAnimations = new List<SegmentImageAnimation>();

	private static Color ColorMultiplySaturationAndValue(ref Color col, float saturationMultiplier, float valueMultiplier)
	{
		Color.RGBToHSV(col, out var H, out var S, out var V);
		return Color.HSVToRGB(H, S * saturationMultiplier, V * valueMultiplier);
	}

	private void OnCurrentSegmentIndexChanged(int newSegmentIndex)
	{
		if (!Application.isPlaying)
		{
			return;
		}
		int num = newSegmentIndex - 1;
		float width = viewPort.rect.width;
		int i = 0;
		for (int num2 = images.Length - 1; i < num2; i++)
		{
			Image obj = images[i];
			TextMeshProUGUI textMeshProUGUI = labels[i];
			RectTransform rectTransform = obj.rectTransform;
			bool flag2 = (obj.enabled = rectTransform.offsetMax.x + scrollX >= 0f && rectTransform.offsetMin.x + scrollX <= width);
			textMeshProUGUI.enabled = flag2;
		}
		int num3 = images.Length - 1;
		Image obj2 = images[num3];
		TextMeshProUGUI textMeshProUGUI2 = labels[num3];
		bool flag4 = (obj2.enabled = obj2.rectTransform.offsetMax.x + scrollX >= 0f);
		textMeshProUGUI2.enabled = flag4;
		for (int j = 0; j <= num; j++)
		{
			images[j].color = ColorMultiplySaturationAndValue(ref segmentDefs[j].color, pastSaturationMultiplier, pastValueMultiplier);
			labels[j].color = pastLabelColor;
		}
		for (int k = newSegmentIndex + 1; k < images.Length; k++)
		{
			images[k].color = ColorMultiplySaturationAndValue(ref segmentDefs[k].color, upcomingSaturationMultiplier, upcomingValueMultiplier);
			labels[k].color = upcomingLabelColor;
		}
		Image image = ((num != -1) ? images[num] : null);
		Image image2 = ((newSegmentIndex != -1) ? images[newSegmentIndex] : null);
		TextMeshProUGUI textMeshProUGUI3 = ((newSegmentIndex != -1) ? labels[newSegmentIndex] : null);
		if ((bool)image)
		{
			playingAnimations.Add(new SegmentImageAnimation
			{
				age = 0f,
				duration = fadeAnimationDuration,
				segmentImage = image,
				colorCurve = fadeAnimationCurve,
				color0 = segmentDefs[num].color,
				color1 = ColorMultiplySaturationAndValue(ref segmentDefs[num].color, pastSaturationMultiplier, pastValueMultiplier)
			});
		}
		Color color = ColorMultiplySaturationAndValue(ref segmentDefs[newSegmentIndex].color, currentSaturationMultiplier, currentValueMultiplier);
		if ((bool)image2)
		{
			playingAnimations.Add(new SegmentImageAnimation
			{
				age = 0f,
				duration = flashAnimationDuration,
				segmentImage = image2,
				colorCurve = flashAnimationCurve,
				color0 = color,
				color1 = Color.white
			});
		}
		if ((bool)hud && (bool)difficultyBarBackdrop)
		{
			UserProfile userProfile = hud.localUserViewer.userProfile;
			if (userProfile != null && userProfile.increasedDifficultyBarFX)
			{
				Color.RGBToHSV(segmentDefs[newSegmentIndex].color, out var _, out var _, out var V);
				playingAnimations.Add(new SegmentImageAnimation
				{
					age = 0f,
					duration = flashAnimationDuration,
					segmentImage = difficultyBarBackdrop,
					colorCurve = flashAnimationCurve,
					color0 = difficultyBarBackdrop.color,
					color1 = ((V < backdropFlashWhiteThreshold) ? Color.white : color)
				});
				if (newSegmentIndex > 0)
				{
					Util.PlaySound(onDifficultyChangeSoundString, RoR2Application.instance.gameObject);
				}
			}
		}
		if ((bool)textMeshProUGUI3)
		{
			textMeshProUGUI3.color = currentLabelColor;
		}
	}

	private void SetSegmentScroll(float segmentScroll)
	{
		if (segmentScroll == lastSegmentScroll)
		{
			return;
		}
		lastSegmentScroll = segmentScroll;
		float num = segmentDefs.Length + 2;
		if (segmentScroll > num)
		{
			segmentScroll = num - 1f + (segmentScroll - Mathf.Floor(segmentScroll));
		}
		scrollXRaw = (segmentScroll - 1f) * (0f - elementWidth);
		scrollX = Mathf.Floor(scrollXRaw);
		int num2 = currentSegmentIndex;
		currentSegmentIndex = Mathf.Clamp(Mathf.FloorToInt(segmentScroll), 0, segmentContainer.childCount - 1);
		if (num2 != currentSegmentIndex)
		{
			OnCurrentSegmentIndexChanged(currentSegmentIndex);
		}
		Vector2 offsetMin = segmentContainer.offsetMin;
		offsetMin.x = scrollX;
		segmentContainer.offsetMin = offsetMin;
		if ((bool)segmentContainer && segmentContainer.childCount > 0)
		{
			int num3 = segmentContainer.childCount - 1;
			RectTransform rectTransform = (RectTransform)segmentContainer.GetChild(num3);
			RectTransform rectTransform2 = (RectTransform)rectTransform.Find("Label");
			_ = labels[num3];
			if (segmentScroll >= (float)(num3 - 1))
			{
				float num4 = elementWidth;
				Vector2 offsetMin2 = rectTransform.offsetMin;
				offsetMin2.x = CalcSegmentStartX(num3);
				rectTransform.offsetMin = offsetMin2;
				Vector2 offsetMax = rectTransform.offsetMax;
				offsetMax.x = offsetMin2.x + num4;
				rectTransform.offsetMax = offsetMax;
				rectTransform2.anchorMin = new Vector2(0f, 0f);
				rectTransform2.anchorMax = new Vector2(0f, 1f);
				rectTransform2.offsetMin = new Vector2(0f, 0f);
				rectTransform2.offsetMax = new Vector2(elementWidth, 0f);
			}
			else
			{
				rectTransform.offsetMax = rectTransform.offsetMin + new Vector2(elementWidth, 0f);
				SetLabelDefaultDimensions(rectTransform2);
			}
		}
	}

	private float CalcSegmentStartX(int i)
	{
		return (float)i * elementWidth;
	}

	private float CalcSegmentEndX(int i)
	{
		return (float)(i + 1) * elementWidth;
	}

	private void SetLabelDefaultDimensions(RectTransform labelRectTransform)
	{
		labelRectTransform.anchorMin = new Vector2(0f, 0f);
		labelRectTransform.anchorMax = new Vector2(1f, 1f);
		labelRectTransform.pivot = new Vector2(0.5f, 0.5f);
		labelRectTransform.offsetMin = new Vector2(0f, 0f);
		labelRectTransform.offsetMax = new Vector2(0f, 0f);
	}

	private void SetSegmentCount(uint desiredCount)
	{
		if ((bool)segmentContainer && (bool)segmentPrefab)
		{
			uint num = (uint)segmentContainer.childCount;
			if (images == null || images.Length != desiredCount)
			{
				images = new Image[desiredCount];
				labels = new TextMeshProUGUI[desiredCount];
			}
			int i = 0;
			for (int num2 = Mathf.Min(images.Length, segmentContainer.childCount); i < num2; i++)
			{
				Transform child = segmentContainer.GetChild(i);
				images[i] = child.GetComponent<Image>();
				labels[i] = child.Find("Label").GetComponent<TextMeshProUGUI>();
			}
			while (num > desiredCount)
			{
				UnityEngine.Object.DestroyImmediate(segmentContainer.GetChild((int)(num - 1)).gameObject);
				num--;
			}
			for (; num < desiredCount; num++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(segmentPrefab, segmentContainer);
				gameObject.SetActive(value: true);
				images[i] = gameObject.GetComponent<Image>();
				labels[i] = gameObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
				i++;
			}
		}
	}

	private void SetupSegments()
	{
		if ((bool)segmentContainer && (bool)segmentPrefab)
		{
			SetSegmentCount((uint)segmentDefs.Length);
			for (int i = 0; i < segmentContainer.childCount; i++)
			{
				SetupSegment((RectTransform)segmentContainer.GetChild(i), ref segmentDefs[i], i);
			}
			SetupFinalSegment((RectTransform)segmentContainer.GetChild(segmentContainer.childCount - 1));
		}
	}

	private static void ScaleLabelToWidth(TextMeshProUGUI label, float width)
	{
		RectTransform obj = (RectTransform)label.transform;
		float x = label.textBounds.size.x;
		Vector3 localScale = obj.localScale;
		localScale.x = width / x;
		obj.localScale = localScale;
	}

	private void SetupFinalSegment(RectTransform segmentTransform)
	{
		TextMeshProUGUI[] array = segmentTransform.GetComponentsInChildren<TextMeshProUGUI>();
		int num = 4;
		if (array.Length < num)
		{
			TextMeshProUGUI[] array2 = new TextMeshProUGUI[num];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i];
			}
			for (int j = array.Length; j < num; j++)
			{
				array2[j] = UnityEngine.Object.Instantiate(array[0].gameObject, segmentTransform).GetComponent<TextMeshProUGUI>();
			}
			array = array2;
		}
		int k = 0;
		for (int num2 = array.Length; k < num2; k++)
		{
			TextMeshProUGUI obj = array[k];
			obj.enableWordWrapping = false;
			obj.overflowMode = TextOverflowModes.Overflow;
			obj.alignment = TextAlignmentOptions.MidlineLeft;
			obj.text = Language.GetString(segmentDefs[segmentDefs.Length - 1].token);
			obj.enableAutoSizing = true;
			Vector3 localPosition = obj.transform.localPosition;
			localPosition.x = (float)k * elementWidth;
			obj.transform.localPosition = localPosition;
		}
		segmentTransform.GetComponent<Image>().sprite = finalSegmentSprite;
	}

	private void SetupSegment(RectTransform segmentTransform, ref SegmentDef segmentDef, int i)
	{
		Vector2 offsetMin = segmentTransform.offsetMin;
		Vector2 offsetMax = segmentTransform.offsetMax;
		offsetMin.x = CalcSegmentStartX(i);
		offsetMax.x = CalcSegmentEndX(i);
		segmentTransform.offsetMin = offsetMin;
		segmentTransform.offsetMax = offsetMax;
		segmentTransform.GetComponent<Image>().color = segmentDef.color;
		((RectTransform)segmentTransform.Find("Label")).GetComponent<LanguageTextMeshController>().token = segmentDef.token;
	}

	private void Awake()
	{
		hud = GetComponentInParent<HUD>();
		SetupSegments();
	}

	private void DoBarUpdates(float deltaTime)
	{
		if ((bool)Run.instance)
		{
			SetSegmentScroll((Run.instance.ambientLevel - 1f) / levelsPerSegment);
		}
		if (Application.isPlaying)
		{
			RunAnimations(deltaTime);
		}
	}

	private void Update()
	{
		DoBarUpdates(Time.deltaTime);
		UpdateGears();
	}

	private void UpdateGears()
	{
		RawImage[] array = wormGearImages;
		foreach (RawImage obj in array)
		{
			Rect uvRect = obj.uvRect;
			float num = Mathf.Sign(uvRect.width);
			uvRect.x = scrollXRaw * UVScaleToScrollX * num + ((num < 0f) ? gearUVOffset : 0f);
			obj.uvRect = uvRect;
		}
	}

	private void RunAnimations(float deltaTime)
	{
		for (int num = playingAnimations.Count - 1; num >= 0; num--)
		{
			SegmentImageAnimation segmentImageAnimation = playingAnimations[num];
			segmentImageAnimation.age += deltaTime;
			float num2 = Mathf.Clamp01(segmentImageAnimation.age / segmentImageAnimation.duration);
			segmentImageAnimation.Update(num2);
			if (num2 >= 1f)
			{
				playingAnimations.RemoveAt(num);
			}
		}
	}
}
