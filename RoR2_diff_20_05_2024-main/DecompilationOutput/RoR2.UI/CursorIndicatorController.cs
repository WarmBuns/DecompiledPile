using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class CursorIndicatorController : MonoBehaviour
{
	public enum CursorImage
	{
		None,
		Pointer,
		Hover
	}

	[Serializable]
	public struct CursorSet
	{
		public GameObject pointerObject;

		public GameObject hoverObject;

		public GameObject GetGameObject(CursorImage cursorImage)
		{
			return cursorImage switch
			{
				CursorImage.None => null, 
				CursorImage.Pointer => pointerObject, 
				CursorImage.Hover => hoverObject, 
				_ => null, 
			};
		}
	}

	[NonSerialized]
	public CursorSet noneCursorSet;

	public CursorSet mouseCursorSet;

	public CursorSet gamepadCursorSet;

	private GameObject currentChildIndicator;

	public RectTransform containerTransform;

	private Color cachedIndicatorColor = Color.clear;

	public void SetCursor(CursorSet cursorSet, CursorImage cursorImage, Color color)
	{
		GameObject gameObject = cursorSet.GetGameObject(cursorImage);
		bool flag = color != cachedIndicatorColor;
		if (gameObject != currentChildIndicator)
		{
			if ((bool)currentChildIndicator)
			{
				currentChildIndicator.SetActive(value: false);
			}
			currentChildIndicator = gameObject;
			if ((bool)currentChildIndicator)
			{
				currentChildIndicator.SetActive(value: true);
			}
			flag = true;
		}
		if (flag)
		{
			cachedIndicatorColor = color;
			ApplyIndicatorColor();
		}
	}

	private void ApplyIndicatorColor()
	{
		if ((bool)currentChildIndicator)
		{
			Image[] componentsInChildren = currentChildIndicator.GetComponentsInChildren<Image>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = cachedIndicatorColor;
			}
		}
	}

	public void SetPosition(Vector2 position)
	{
		containerTransform.position = position;
	}
}
