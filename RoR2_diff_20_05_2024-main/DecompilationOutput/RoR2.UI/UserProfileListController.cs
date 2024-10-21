using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class UserProfileListController : MonoBehaviour
{
	public delegate void ProfileSelectedDelegate(UserProfile userProfile);

	public GameObject elementPrefab;

	public RectTransform contentRect;

	[Tooltip("Whether or not \"default\" profile appears as a selectable option.")]
	public bool allowDefault = true;

	private MPEventSystemLocator eventSystemLocator;

	private readonly List<UserProfileListElementController> elementsList = new List<UserProfileListElementController>();

	private EventSystem eventSystem => eventSystemLocator.eventSystem;

	public event ProfileSelectedDelegate onProfileSelected;

	public event Action onListRebuilt;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private void OnEnable()
	{
		RebuildElements();
		SaveSystem.onAvailableUserProfilesChanged += RebuildElements;
	}

	private void OnDisable()
	{
		SaveSystem.onAvailableUserProfilesChanged -= RebuildElements;
	}

	private void RebuildElements()
	{
		foreach (Transform item in contentRect)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		elementsList.Clear();
		List<string> availableProfileNames = PlatformSystems.saveSystem.GetAvailableProfileNames();
		for (int i = 0; i < availableProfileNames.Count; i++)
		{
			if (allowDefault || !availableProfileNames[i].Equals("default", StringComparison.OrdinalIgnoreCase))
			{
				GameObject obj = UnityEngine.Object.Instantiate(elementPrefab, contentRect);
				UserProfileListElementController component = obj.GetComponent<UserProfileListElementController>();
				MPButton component2 = obj.GetComponent<MPButton>();
				component.listController = this;
				component.userProfile = PlatformSystems.saveSystem.GetProfile(availableProfileNames[i]);
				elementsList.Add(component);
				obj.SetActive(value: true);
				if (i == 0)
				{
					component2.defaultFallbackButton = true;
				}
			}
		}
		if (this.onListRebuilt != null)
		{
			this.onListRebuilt();
		}
	}

	public ReadOnlyCollection<UserProfileListElementController> GetReadOnlyElementsList()
	{
		return new ReadOnlyCollection<UserProfileListElementController>(elementsList);
	}

	public void SendProfileSelection(UserProfile userProfile)
	{
		this.onProfileSelected?.Invoke(userProfile);
	}
}
