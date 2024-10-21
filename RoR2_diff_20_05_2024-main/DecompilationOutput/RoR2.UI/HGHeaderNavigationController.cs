using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class HGHeaderNavigationController : MonoBehaviour
{
	[Serializable]
	public struct Header
	{
		public MPButton headerButton;

		public string headerName;

		public TextMeshProUGUI tmpHeaderText;

		public GameObject headerRoot;

		public bool AreConsolePlatformsSupported;

		public bool isPrimaryPlayerOnly;
	}

	[FormerlySerializedAs("buttonSelectionRoot")]
	[Header("Header Parameters")]
	public GameObject headerHighlightObject;

	public int currentHeaderIndex;

	public Color selectedTextColor = Color.white;

	public Color unselectedTextColor = Color.gray;

	public bool makeSelectedHeaderButtonNoninteractable;

	public GameObject ControllerResetButton;

	public bool isPrimaryPlayer;

	private bool IsPlatformConsole;

	[Header("Header Infos")]
	public List<Header> headers;

	private void Start()
	{
		BuildInitialHeaderList();
		RebuildHeaders();
	}

	private void LateUpdate()
	{
		for (int i = 0; i < headers.Count; i++)
		{
			Header header = headers[i];
			if ((bool)header.tmpHeaderText)
			{
				if (i == currentHeaderIndex && header.tmpHeaderText.color != selectedTextColor)
				{
					header.tmpHeaderText.color = selectedTextColor;
				}
				else
				{
					header.tmpHeaderText.color = unselectedTextColor;
				}
			}
		}
	}

	private void BuildInitialHeaderList()
	{
		List<Header> list = new List<Header>();
		foreach (Header header in headers)
		{
			if (IsPlatformConsole && !header.AreConsolePlatformsSupported)
			{
				header.headerButton.gameObject.SetActive(value: false);
			}
			else if (header.isPrimaryPlayerOnly && !isPrimaryPlayer)
			{
				header.headerButton.gameObject.SetActive(value: false);
			}
			else
			{
				list.Add(header);
			}
		}
		headers = list;
	}

	public void ChooseFirstHeader()
	{
		if (headers.Count > 0)
		{
			currentHeaderIndex = 0;
			RebuildHeaders();
		}
	}

	public void ChooseHeader(string headerName)
	{
		for (int i = 0; i < headers.Count; i++)
		{
			if (headers[i].headerName == headerName)
			{
				currentHeaderIndex = i;
				RebuildHeaders();
				break;
			}
		}
	}

	public void ChooseHeaderByButton(MPButton mpButton)
	{
		for (int i = 0; i < headers.Count; i++)
		{
			if (headers[i].headerButton == mpButton)
			{
				currentHeaderIndex = i;
				RebuildHeaders();
				break;
			}
		}
	}

	public void MoveHeaderLeft()
	{
		currentHeaderIndex = Mathf.Max(0, currentHeaderIndex - 1);
		Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
		RebuildHeaders();
	}

	public void MoveHeaderRight()
	{
		currentHeaderIndex = Mathf.Min(headers.Count - 1, currentHeaderIndex + 1);
		Util.PlaySound("Play_UI_menuClick", RoR2Application.instance.gameObject);
		RebuildHeaders();
	}

	private void BuildInitialHeadersList()
	{
		for (int i = 0; i < headers.Count; i++)
		{
			Header header = headers[i];
			if (IsPlatformConsole)
			{
				_ = header.AreConsolePlatformsSupported;
			}
		}
	}

	private void RebuildHeaders()
	{
		for (int i = 0; i < headers.Count; i++)
		{
			Header header = headers[i];
			if (IsPlatformConsole && !header.AreConsolePlatformsSupported)
			{
				header.headerRoot.SetActive(value: false);
			}
			else if (i == currentHeaderIndex)
			{
				if ((bool)header.headerRoot)
				{
					header.headerRoot.SetActive(value: true);
				}
				if ((bool)header.headerButton)
				{
					if (makeSelectedHeaderButtonNoninteractable)
					{
						header.headerButton.interactable = false;
					}
					if ((bool)headerHighlightObject)
					{
						headerHighlightObject.transform.SetParent(header.headerButton.transform, worldPositionStays: false);
						headerHighlightObject.SetActive(value: false);
						headerHighlightObject.SetActive(value: true);
						RectTransform component = headerHighlightObject.GetComponent<RectTransform>();
						component.offsetMin = Vector2.zero;
						component.offsetMax = Vector2.zero;
						component.localScale = Vector3.one;
					}
				}
			}
			else
			{
				if ((bool)header.headerButton && makeSelectedHeaderButtonNoninteractable)
				{
					header.headerButton.interactable = true;
				}
				if ((bool)header.headerRoot)
				{
					header.headerRoot.SetActive(value: false);
				}
			}
		}
		if (ControllerResetButton != null)
		{
			if (headers[currentHeaderIndex].headerName == "Controller")
			{
				ControllerResetButton.SetActive(value: true);
			}
			else
			{
				ControllerResetButton.SetActive(value: false);
			}
		}
	}

	private Header GetCurrentHeader()
	{
		return headers[currentHeaderIndex];
	}
}
