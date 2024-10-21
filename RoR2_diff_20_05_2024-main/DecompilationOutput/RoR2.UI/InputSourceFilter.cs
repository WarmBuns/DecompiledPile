using System.Collections;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class InputSourceFilter : MonoBehaviour
{
	public MPEventSystem.InputSource requiredInputSource;

	public GameObject[] objectsToFilter;

	private MPEventSystemLocator eventSystemLocator;

	private bool wasOn;

	protected MPEventSystem eventSystem => eventSystemLocator?.eventSystem;

	private void Start()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		Refresh(forceRefresh: true);
	}

	private void Update()
	{
		Refresh();
	}

	private void OnEnable()
	{
		Refresh(forceRefresh: true);
	}

	private IEnumerator RefreshFilteredObjects_Coroutine()
	{
		yield return null;
		if ((bool)eventSystemLocator && (bool)eventSystemLocator.eventSystem)
		{
			eventSystemLocator.eventSystem.RefreshControlGlyphs();
		}
		for (int i = 0; i < objectsToFilter.Length; i++)
		{
			MPEventSystemLocator[] componentsInChildren = objectsToFilter[i].gameObject.GetComponentsInChildren<MPEventSystemLocator>();
			foreach (MPEventSystemLocator mPEventSystemLocator in componentsInChildren)
			{
				if (mPEventSystemLocator.eventSystem != null)
				{
					mPEventSystemLocator.eventSystem.RefreshControlGlyphs();
				}
			}
		}
	}

	private void Refresh(bool forceRefresh = false)
	{
		if (eventSystem == null)
		{
			Debug.LogWarningFormat("InputSourceFilter.Refresh: Null eventSystem on {0}", base.gameObject.name);
		}
		bool flag = eventSystem?.currentInputSource == requiredInputSource;
		if (flag != wasOn || forceRefresh)
		{
			for (int i = 0; i < objectsToFilter.Length; i++)
			{
				objectsToFilter[i].SetActive(flag);
			}
		}
		if (forceRefresh || (flag && !wasOn))
		{
			StartCoroutine(RefreshFilteredObjects_Coroutine());
		}
		wasOn = flag;
	}
}
