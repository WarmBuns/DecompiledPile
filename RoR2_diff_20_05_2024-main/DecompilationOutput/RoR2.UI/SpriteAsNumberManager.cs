using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2.UI;

public class SpriteAsNumberManager : MonoBehaviour, ILayoutGroup, ILayoutController
{
	private enum Alignment
	{
		Right,
		Center,
		Left
	}

	private enum SpriteCharacterType
	{
		Number,
		Punctuation
	}

	[SerializeField]
	[Tooltip("This is only used internally on editor refresh to tell the object how to format itself.")]
	private bool internalUseOnlyIsTimer;

	[SerializeField]
	private Alignment textAlignment = Alignment.Center;

	[SerializeField]
	private int debugValue;

	[FormerlySerializedAs("spaceBetweenImages")]
	[SerializeField]
	[Tooltip("Additional Space between non-punctuation characters.")]
	private float spaceBetweenCharacters;

	[SerializeField]
	[Tooltip("Will scale punctuation sprites up or down (1 = normal). It will not change the spacing around the sprite.")]
	private float scalePunctuationBy;

	[SerializeField]
	private float spaceBetweenPunctuation;

	[SerializeField]
	private Transform imagePrefab;

	[SerializeField]
	private int startingNumberOfPrefabs;

	[SerializeField]
	private Sprite colonSprite;

	[SerializeField]
	private Sprite forwardSlashSprite;

	[SerializeField]
	private Sprite questionMarkSprite;

	[SerializeField]
	private Sprite negativeSymbolSprite;

	[SerializeField]
	private Sprite percentSymbolSprite;

	[SerializeField]
	private Sprite dollarSignSymbolSprite;

	[SerializeField]
	private Sprite xSprite;

	private const int CONST_COLON = -1111;

	private const int CONST_NEGATIVE = -2222;

	private const int CONST_FSLASH = -3333;

	private const int CONST_PERCENT = -4444;

	private const int CONST_DOLLARSIGN = -5555;

	private const int CONST_X = -6666;

	private RectTransform rectTransform;

	private int cachedValue = -1111;

	private float cachedCanvasHeight;

	private float defaultSpriteHeight;

	private float defaultSpriteWidth;

	private bool isVisible = true;

	[SerializeField]
	private Sprite[] referenceArray;

	private List<Image> imageList = new List<Image>();

	private List<RectTransform> rtList = new List<RectTransform>();

	private List<SpriteCharacterType> charTypeList = new List<SpriteCharacterType>();

	private int totalImagesInUse;

	private bool finishedSetup;

	private void Awake()
	{
		TrySetup();
	}

	public void TrySetup()
	{
		if (!finishedSetup)
		{
			rectTransform = GetComponent<RectTransform>();
			rtList = new List<RectTransform>();
			imageList = new List<Image>();
			charTypeList = new List<SpriteCharacterType>();
			defaultSpriteHeight = referenceArray[0].rect.height;
			defaultSpriteWidth = referenceArray[0].rect.width;
			LoadAnyExistingChildren();
			int num = Mathf.Max(startingNumberOfPrefabs - base.transform.childCount, 0);
			for (int i = 0; i < num; i++)
			{
				InstantiateNewImage();
			}
			totalImagesInUse = 0;
			RebuildLayout();
			finishedSetup = true;
		}
	}

	public void LoadAnyExistingChildren()
	{
		foreach (RectTransform item in base.transform)
		{
			if (item.TryGetComponent<Image>(out var component))
			{
				rtList.Add(item);
				imageList.Add(component);
				charTypeList.Add(SpriteCharacterType.Number);
			}
		}
	}

	public void SetVisible(bool b)
	{
		isVisible = b;
		HandleActivationOfImages();
	}

	public void SetPercentValue(int intPercent)
	{
		TrySetup();
		if (intPercent != cachedValue || !isVisible)
		{
			cachedValue = intPercent;
			isVisible = true;
			List<int> list = new List<int>();
			list.Add(-4444);
			LoadListWithDigitPositions(intPercent, ref list);
			UpdateSpriteObjectsWithListValues(list);
		}
	}

	public void SetItemCount(int count)
	{
		TrySetup();
		if (count != cachedValue || !isVisible)
		{
			cachedValue = count;
			isVisible = true;
			List<int> list = new List<int>();
			LoadListWithDigitPositions(count, ref list);
			list.Add(-6666);
			UpdateSpriteObjectsWithListValues(list);
		}
	}

	public void SetTimerValue(int totalTimeInSeconds)
	{
		TrySetup();
		if (totalTimeInSeconds == cachedValue && isVisible)
		{
			return;
		}
		cachedValue = totalTimeInSeconds;
		isVisible = true;
		List<int> list = new List<int>();
		float num = totalTimeInSeconds;
		int num2 = Mathf.Min(Mathf.FloorToInt(num * (1f / 60f)), 999);
		int value = (int)num - num2 * 60;
		list.Add(GetDigitPosition(value, 0));
		list.Add(GetDigitPosition(value, 1));
		list.Add(-1111);
		int num3 = Mathf.Max(GetTotalDigitPositions(num2), 2);
		for (int i = 0; i < num3; i++)
		{
			int digitPosition = GetDigitPosition(num2, i);
			if (digitPosition > 0 || i < 2)
			{
				list.Add(digitPosition);
			}
		}
		UpdateSpriteObjectsWithListValues(list);
	}

	public void SetHitPointValues(int currentHP, int totalHP)
	{
		TrySetup();
		List<int> list = new List<int>();
		LoadListWithDigitPositions(totalHP, ref list);
		list.Add(-3333);
		LoadListWithDigitPositions(currentHP, ref list);
		UpdateSpriteObjectsWithListValues(list);
	}

	public int GetDigitPosition(int value, int position)
	{
		value = Mathf.Abs(value);
		int num = (int)Mathf.Pow(10f, (float)position + 1f);
		position = (int)Mathf.Pow(10f, position);
		if (value >= num)
		{
			float num2 = value / num;
			value -= (int)num2 * num;
		}
		return value / position;
	}

	public void LoadListWithDigitPositions(int value, ref List<int> list, int forceZeroTillAtLeastXPosition = 0)
	{
		bool flag = value < 0;
		value = Mathf.Abs(value);
		if (value == 0 && forceZeroTillAtLeastXPosition == 0)
		{
			list.Add(0);
			return;
		}
		int num = Math.Max(GetTotalDigitPositions(value), forceZeroTillAtLeastXPosition);
		for (int i = 0; i < num; i++)
		{
			int digitPosition = GetDigitPosition(value, i);
			list.Add(digitPosition);
		}
		if (flag)
		{
			list.Add(-2222);
		}
	}

	public int GetTotalDigitPositions(int val)
	{
		val = Mathf.Abs(val);
		int i;
		for (i = 0; (float)val >= Mathf.Pow(10f, i); i++)
		{
		}
		return i;
	}

	public void SetSpriteColor(Color color)
	{
		for (int i = 0; i < totalImagesInUse; i++)
		{
			imageList[i].color = color;
		}
	}

	public void UpdateSpriteObjectsWithListValues(List<int> listValues, int startingImageIndex = 0)
	{
		VerifyAllObjectsInList();
		int count = listValues.Count;
		int count2;
		for (count2 = imageList.Count; count2 <= startingImageIndex + count; count2 = imageList.Count)
		{
			InstantiateNewImage();
		}
		totalImagesInUse = count2 + startingImageIndex;
		for (int i = 0; i < count2; i++)
		{
			if (i >= count)
			{
				totalImagesInUse = i;
				break;
			}
			Image image = imageList[i];
			switch (listValues[i])
			{
			case -1111:
				image.sprite = colonSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			case -2222:
				image.sprite = negativeSymbolSprite ?? questionMarkSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			case -3333:
				image.sprite = forwardSlashSprite ?? questionMarkSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			case -4444:
				image.sprite = percentSymbolSprite ?? questionMarkSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			case -5555:
				image.sprite = dollarSignSymbolSprite ?? questionMarkSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			case -6666:
				image.sprite = xSprite ?? questionMarkSprite;
				charTypeList[i] = SpriteCharacterType.Punctuation;
				break;
			default:
				image.sprite = referenceArray[Mathf.Clamp(listValues[i], 0, 9)];
				charTypeList[i] = SpriteCharacterType.Number;
				break;
			}
		}
		UpdateSpriteSizes();
		RebuildLayout();
	}

	private void VerifyAllObjectsInList()
	{
		int count = rtList.Count;
		if (count < 1)
		{
			return;
		}
		for (int num = count - 1; num >= 0; num--)
		{
			if (rtList[num] == null || imageList[num] == null)
			{
				rtList.RemoveAt(num);
				imageList.RemoveAt(num);
				charTypeList.RemoveAt(num);
			}
		}
	}

	private void UpdateSpriteSizes()
	{
		cachedCanvasHeight = rectTransform.rect.height;
		float num = cachedCanvasHeight / defaultSpriteHeight;
		Vector2 sizeDelta = default(Vector2);
		for (int i = 0; i < totalImagesInUse; i++)
		{
			if (charTypeList[i] == SpriteCharacterType.Punctuation)
			{
				sizeDelta.x = imageList[i].sprite.rect.width * num * scalePunctuationBy;
				sizeDelta.y = imageList[i].sprite.rect.height * num * scalePunctuationBy;
			}
			else
			{
				sizeDelta.x = imageList[i].sprite.rect.width * num;
				sizeDelta.y = imageList[i].sprite.rect.height * num;
			}
			sizeDelta.x = Mathf.Round(sizeDelta.x * 100f) / 100f;
			sizeDelta.y = Mathf.Round(sizeDelta.y * 100f) / 100f;
			rtList[i].sizeDelta = sizeDelta;
		}
	}

	private void InstantiateNewImage()
	{
		RectTransform component = UnityEngine.Object.Instantiate(imagePrefab, rectTransform).GetComponent<RectTransform>();
		rtList.Add(component);
		Image component2 = component.GetComponent<Image>();
		imageList.Add(component2);
		charTypeList.Add(SpriteCharacterType.Number);
		component.sizeDelta = new Vector2(defaultSpriteWidth, defaultSpriteHeight);
		component2.preserveAspect = true;
	}

	private void RebuildLayout()
	{
		if (base.transform.childCount < 1)
		{
			return;
		}
		HandleActivationOfImages();
		if (isVisible && totalImagesInUse >= 1)
		{
			if (cachedCanvasHeight != this.rectTransform.rect.height)
			{
				UpdateSpriteSizes();
			}
			float num = this.rectTransform.rect.height / defaultSpriteHeight;
			float singleImageWidth = defaultSpriteWidth * num;
			float totalImageWidth = GetTotalImageWidth();
			float num2 = GetStartingX(textAlignment, singleImageWidth, totalImageWidth, rtList[0].pivot.x);
			float y = 0f;
			for (int i = 0; i < totalImagesInUse; i++)
			{
				RectTransform rectTransform = rtList[i];
				bool num3 = charTypeList[i] != SpriteCharacterType.Punctuation && i + 1 < totalImagesInUse && charTypeList[i + 1] != SpriteCharacterType.Punctuation;
				singleImageWidth = rtList[i].rect.width * 0.5f;
				num2 -= singleImageWidth;
				rectTransform.anchoredPosition = new Vector2(num2, y);
				num2 = ((!num3) ? (num2 - (singleImageWidth + spaceBetweenPunctuation)) : (num2 - (singleImageWidth + spaceBetweenCharacters)));
			}
		}
	}

	private void InitializeVariablesForInEditorViewing()
	{
		rectTransform = GetComponent<RectTransform>();
		rtList = new List<RectTransform>();
		imageList = new List<Image>();
		charTypeList = new List<SpriteCharacterType>();
		defaultSpriteHeight = referenceArray[0].rect.height;
		defaultSpriteWidth = referenceArray[0].rect.width;
		LoadAnyExistingChildren();
		totalImagesInUse = rtList.Count;
	}

	private float GetTotalImageWidth()
	{
		float num = 0f;
		for (int i = 0; i < totalImagesInUse; i++)
		{
			num += rtList[i].rect.width;
			num = ((charTypeList[i] != 0) ? (num + spaceBetweenPunctuation) : (num + spaceBetweenCharacters));
		}
		return num;
	}

	private float GetStartingX(Alignment textAlignment, float singleImageWidth, float totalImageWidth, float singleImageAlignment)
	{
		singleImageAlignment *= singleImageWidth;
		float num = rectTransform.pivot.x * rectTransform.rect.width;
		switch (textAlignment)
		{
		case Alignment.Left:
			return totalImageWidth - (singleImageWidth - singleImageAlignment) - num;
		case Alignment.Right:
			return num;
		default:
			num = rectTransform.rect.width * 0.5f - num;
			return num + totalImageWidth * 0.5f;
		}
	}

	private void HandleActivationOfImages()
	{
		for (int num = rtList.Count - 1; num >= 0; num--)
		{
			if (rtList[num] == null)
			{
				rtList.RemoveAt(num);
				imageList.RemoveAt(num);
				if (num < charTypeList.Count)
				{
					charTypeList.RemoveAt(num);
				}
			}
			else if (num < totalImagesInUse)
			{
				rtList[num].gameObject.SetActive(isVisible);
			}
			else
			{
				rtList[num].gameObject.SetActive(value: false);
			}
		}
	}

	public void SetLayoutHorizontal()
	{
	}

	public void SetLayoutVertical()
	{
		VerifyAllObjectsInList();
		RebuildLayout();
	}

	[ContextMenu("Format As Timer")]
	public void FormatAsTimer()
	{
		InitializeVariablesForInEditorViewing();
		SetTimerValue(754);
	}

	[ContextMenu("Format As ItemCounter")]
	public void FormatAsItemCounter()
	{
		InitializeVariablesForInEditorViewing();
		SetItemCount(debugValue);
	}

	[ContextMenu("Format As Hitpoints")]
	public void FormatAsHitpoints()
	{
		InitializeVariablesForInEditorViewing();
		SetHitPointValues(1234, 5768);
	}

	[ContextMenu("Format as Percent")]
	public void FormatAsPercent()
	{
		InitializeVariablesForInEditorViewing();
		SetPercentValue(100);
	}
}
