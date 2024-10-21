using System.Collections;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class HGButtonHistory : MonoBehaviour
{
	public UILayerKey requiredTopLayer;

	public GameObject lastRememberedGameObject;

	private MPEventSystemLocator eventSystemLocator;

	private bool topLayerWasOn;

	protected MPEventSystem eventSystem => eventSystemLocator?.eventSystem;

	protected void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private void OnEnable()
	{
		SelectRememberedButton();
	}

	private void Update()
	{
		bool flag = !requiredTopLayer || requiredTopLayer.representsTopLayer;
		if (flag && !topLayerWasOn)
		{
			StartCoroutine(SelectRememberedButton());
		}
		topLayerWasOn = flag;
		if (!(eventSystem?.currentSelectedGameObject) || !topLayerWasOn)
		{
			return;
		}
		GameObject gameObject = eventSystem?.currentSelectedGameObject;
		if ((bool)gameObject)
		{
			MPButton component = gameObject.GetComponent<MPButton>();
			if ((bool)component && component.navigation.mode != 0)
			{
				lastRememberedGameObject = eventSystem?.currentSelectedGameObject;
			}
		}
	}

	private IEnumerator SelectRememberedButton()
	{
		yield return null;
		if (!lastRememberedGameObject)
		{
			yield break;
		}
		MPEventSystem mPEventSystem = eventSystem;
		if ((object)mPEventSystem != null && mPEventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
		{
			MPButton component = lastRememberedGameObject.GetComponent<MPButton>();
			if ((bool)component)
			{
				Debug.LogFormat("Applying button history ({0})", lastRememberedGameObject);
				eventSystem.SetSelectedGameObject(lastRememberedGameObject);
				component.OnSelect(null);
			}
		}
	}

	public void ClearHistory()
	{
		lastRememberedGameObject = null;
	}
}
