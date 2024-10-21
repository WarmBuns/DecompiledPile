using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class SimpleDialogBox : UIBehaviour
{
	public struct TokenParamsPair
	{
		public string token;

		public object[] formatParams;

		public TokenParamsPair(string token, params object[] formatParams)
		{
			this.token = token;
			this.formatParams = formatParams;
		}
	}

	public static readonly List<SimpleDialogBox> instancesList = new List<SimpleDialogBox>();

	public GameObject rootObject;

	public GameObject buttonPrefab;

	public RectTransform buttonContainer;

	public TextMeshProUGUI headerLabel;

	public TextMeshProUGUI descriptionLabel;

	private MPButton defaultChoice;

	public object[] descriptionFormatParameters;

	public TokenParamsPair headerToken
	{
		set
		{
			headerLabel.text = GetString(value);
		}
	}

	public TokenParamsPair descriptionToken
	{
		set
		{
			descriptionLabel.text = GetString(value);
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		instancesList.Add(this);
	}

	protected override void OnDisable()
	{
		instancesList.Remove(this);
		base.OnDisable();
	}

	private static string GetString(TokenParamsPair pair)
	{
		string text = Language.GetString(pair.token);
		if (pair.formatParams != null && pair.formatParams.Length != 0)
		{
			text = string.Format(text, pair.formatParams);
		}
		return text;
	}

	private MPButton AddButton(UnityAction callback, string displayToken, params object[] formatParams)
	{
		GameObject gameObject = Object.Instantiate(buttonPrefab, buttonContainer);
		MPButton component = gameObject.GetComponent<MPButton>();
		string text = Language.GetString(displayToken);
		if (formatParams != null && formatParams.Length != 0)
		{
			text = string.Format(text, formatParams);
		}
		gameObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
		component.onClick.AddListener(callback);
		gameObject.SetActive(value: true);
		if (!defaultChoice)
		{
			defaultChoice = component;
		}
		if (buttonContainer.childCount > 1)
		{
			int siblingIndex = gameObject.transform.GetSiblingIndex();
			int num = siblingIndex - 1;
			int num2 = siblingIndex + 1;
			MPButton mPButton = null;
			MPButton mPButton2 = null;
			if (num > 0)
			{
				mPButton = buttonContainer.GetChild(num).GetComponent<MPButton>();
			}
			if (num2 < buttonContainer.childCount)
			{
				mPButton2 = buttonContainer.GetChild(num2).GetComponent<MPButton>();
			}
			if ((bool)mPButton)
			{
				UnityEngine.UI.Navigation navigation = mPButton.navigation;
				navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation.selectOnRight = component;
				mPButton.navigation = navigation;
				UnityEngine.UI.Navigation navigation2 = component.navigation;
				navigation2.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation2.selectOnLeft = mPButton;
				component.navigation = navigation2;
			}
			if ((bool)mPButton2)
			{
				UnityEngine.UI.Navigation navigation3 = mPButton2.navigation;
				navigation3.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation3.selectOnLeft = mPButton;
				mPButton2.navigation = navigation3;
				UnityEngine.UI.Navigation navigation4 = component.navigation;
				navigation4.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation4.selectOnRight = mPButton2;
				component.navigation = navigation4;
			}
		}
		return component;
	}

	public MPButton AddCommandButton(string consoleString, string displayToken, params object[] formatParams)
	{
		return AddButton(delegate
		{
			if (!string.IsNullOrEmpty(consoleString))
			{
				Console.instance.SubmitCmd(null, consoleString, recordSubmit: true);
			}
			Object.Destroy(rootObject);
		}, displayToken, formatParams);
	}

	public MPButton AddCancelButton(string displayToken, params object[] formatParams)
	{
		return AddButton(delegate
		{
			Object.Destroy(rootObject);
		}, displayToken, formatParams);
	}

	public MPButton AddActionButton(UnityAction action, string displayToken, bool destroyDialog = true, params object[] formatParams)
	{
		return AddButton(delegate
		{
			action();
			if (destroyDialog)
			{
				Object.Destroy(rootObject);
			}
		}, displayToken, formatParams);
	}

	protected override void Start()
	{
		base.Start();
		if ((bool)defaultChoice)
		{
			defaultChoice.defaultFallbackButton = true;
		}
	}

	private void Update()
	{
		buttonContainer.gameObject.SetActive(buttonContainer.childCount > 0);
	}

	public static SimpleDialogBox Create(MPEventSystem owner = null)
	{
		GameObject gameObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/SimpleDialogRoot"));
		if ((bool)owner)
		{
			MPEventSystemProvider component = gameObject.GetComponent<MPEventSystemProvider>();
			component.eventSystem = owner;
			component.fallBackToMainEventSystem = false;
			component.eventSystem.SetSelectedGameObject(null);
		}
		return gameObject.transform.GetComponentInChildren<SimpleDialogBox>();
	}
}
