using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

public class DialogBoxManager : MonoBehaviour
{
	private static DialogBoxManager instance;

	private static bool isAwait;

	private static DialogBoxManager Instance
	{
		get
		{
			if (!instance)
			{
				Init();
			}
			return instance;
		}
	}

	public static void Init()
	{
		instance = new GameObject("DialogBoxManager").AddComponent<DialogBoxManager>();
		Object.DontDestroyOnLoad(instance.gameObject);
	}

	public static void DialogBoxDelay(string headerToken, string descriptionToken, string displayToken, float delay)
	{
		Instance.StartCoroutine(Instance.DialogBoxDelayCoroutine(headerToken, descriptionToken, displayToken, delay));
	}

	private IEnumerator DialogBoxDelayCoroutine(string headerToken, string descriptionToken, string displayToken, float delay)
	{
		isAwait = true;
		yield return new WaitForSeconds(delay);
		isAwait = false;
		DialogBox(headerToken, descriptionToken, displayToken);
	}

	public static SimpleDialogBox DialogBox(string headerToken, string descriptionToken, string displayToken, bool allowMultipleInstances = false)
	{
		if ((allowMultipleInstances || SimpleDialogBox.instancesList.Count == 0) && !isAwait)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair(headerToken);
			simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(descriptionToken);
			simpleDialogBox.AddCancelButton(displayToken);
			simpleDialogBox.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
			SelectButton(simpleDialogBox);
			return simpleDialogBox;
		}
		return null;
	}

	public static SimpleDialogBox DialogBox(SimpleDialogBox.TokenParamsPair headerToken, SimpleDialogBox.TokenParamsPair descriptionToken, string displayToken, bool allowMultipleInstances = false)
	{
		if ((allowMultipleInstances || SimpleDialogBox.instancesList.Count == 0) && !isAwait)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			simpleDialogBox.headerToken = headerToken;
			simpleDialogBox.descriptionToken = descriptionToken;
			simpleDialogBox.AddCancelButton(displayToken);
			simpleDialogBox.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
			SelectButton(simpleDialogBox);
			return simpleDialogBox;
		}
		return null;
	}

	public static SimpleDialogBox DialogBox(string headerToken, string descriptionToken, UnityAction action, string displayToken, bool destroyDialog = true, params object[] formatParams)
	{
		if (SimpleDialogBox.instancesList.Count == 0 && !isAwait)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair(headerToken);
			simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair(descriptionToken);
			simpleDialogBox.AddActionButton(action, displayToken, true);
			simpleDialogBox.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
			SelectButton(simpleDialogBox);
			return simpleDialogBox;
		}
		return null;
	}

	public static SimpleDialogBox DialogBox(SimpleDialogBox.TokenParamsPair headerToken, SimpleDialogBox.TokenParamsPair descriptionToken, UnityAction action, string displayToken, bool destroyDialog = true, params object[] formatParams)
	{
		if (SimpleDialogBox.instancesList.Count == 0 && !isAwait)
		{
			SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
			simpleDialogBox.headerToken = headerToken;
			simpleDialogBox.descriptionToken = descriptionToken;
			simpleDialogBox.AddActionButton(action, displayToken, true);
			simpleDialogBox.rootObject.transform.SetParent(RoR2Application.instance.mainCanvas.transform);
			SelectButton(simpleDialogBox);
			return simpleDialogBox;
		}
		return null;
	}

	public static void SelectButton(SimpleDialogBox dialogBox)
	{
		dialogBox.GetComponentInChildren<MPButton>().Select();
	}
}
