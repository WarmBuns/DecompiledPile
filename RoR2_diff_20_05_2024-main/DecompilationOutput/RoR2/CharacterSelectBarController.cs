using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using HG;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RoR2;

[RequireComponent(typeof(MPEventSystemLocator))]
public class CharacterSelectBarController : MonoBehaviour
{
	public struct SurvivorPickInfo
	{
		public LocalUser localUser;

		public SurvivorDef pickedSurvivor;
	}

	[Serializable]
	public class SurvivorPickInfoUnityEvent : UnityEvent<SurvivorPickInfo>
	{
	}

	[Header("Prefabs")]
	public GameObject choiceButtonPrefab;

	public GameObject fillButtonPrefab;

	[Obsolete("No longer used.", false)]
	[ShowFieldObsolete]
	public GameObject WIPButtonPrefab;

	[FormerlySerializedAs("gridLayoutGroup")]
	[Header("Required References")]
	public GridLayoutGroup iconContainerGrid;

	[Header("Events")]
	public SurvivorPickInfoUnityEvent onSurvivorPicked;

	public MPEventSystemLocator eventSystemLocator;

	private UIElementAllocator<SurvivorIconController> survivorIconControllers;

	private UIElementAllocator<RectTransform> fillerIcons;

	private LocalUser currentLocalUser;

	[Obsolete("Use iconContainerGrid instead", false)]
	public ref GridLayoutGroup gridLayoutGroup => ref iconContainerGrid;

	public SurvivorIconController pickedIcon { get; private set; }

	private bool isEclipseRun
	{
		get
		{
			if ((bool)PreGameController.instance)
			{
				return PreGameController.instance.gameModeIndex == GameModeCatalog.FindGameModeIndex("EclipseRun");
			}
			return false;
		}
	}

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
		survivorIconControllers = new UIElementAllocator<SurvivorIconController>((RectTransform)iconContainerGrid.transform, choiceButtonPrefab);
		survivorIconControllers.onCreateElement = OnCreateSurvivorIcon;
		fillerIcons = new UIElementAllocator<RectTransform>((RectTransform)iconContainerGrid.transform, fillButtonPrefab);
	}

	private void LateUpdate()
	{
		EnforceValidChoice();
	}

	private void OnEnable()
	{
		eventSystemLocator.onEventSystemDiscovered += OnEventSystemDiscovered;
		if ((bool)eventSystemLocator.eventSystem)
		{
			OnEventSystemDiscovered(eventSystemLocator.eventSystem);
		}
	}

	private void OnDisable()
	{
		eventSystemLocator.onEventSystemDiscovered -= OnEventSystemDiscovered;
	}

	public void ApplyPickToPickerSurvivorPreference(SurvivorPickInfo survivorPickInfo)
	{
		if (survivorPickInfo.localUser != null)
		{
			survivorPickInfo.localUser.userProfile.SetSurvivorPreference(survivorPickInfo.pickedSurvivor);
		}
	}

	public void ApplyPickToEclipseSurvivorPreference(SurvivorPickInfo survivorPickInfo)
	{
	}

	private void OnEventSystemDiscovered(MPEventSystem eventSystem)
	{
		currentLocalUser = eventSystem.localUser;
		Build();
		SurvivorDef localUserExistingSurvivorPreference = GetLocalUserExistingSurvivorPreference();
		PickIconBySurvivorDef(localUserExistingSurvivorPreference);
	}

	private void OnCreateSurvivorIcon(int index, SurvivorIconController survivorIconController)
	{
		survivorIconController.characterSelectBarController = this;
		survivorIconController.hgButton.onClick.AddListener(delegate
		{
			PickIcon(survivorIconController);
		});
	}

	private int FindIconIndexForSurvivorDef(SurvivorDef survivorDef)
	{
		ReadOnlyCollection<SurvivorIconController> elements = survivorIconControllers.elements;
		for (int i = 0; i < elements.Count; i++)
		{
			if ((object)elements[i].survivorDef == survivorDef)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindIconIndex(SurvivorIconController survivorIconController)
	{
		ReadOnlyCollection<SurvivorIconController> elements = survivorIconControllers.elements;
		for (int i = 0; i < elements.Count; i++)
		{
			if ((object)elements[i] == survivorIconController)
			{
				return i;
			}
		}
		return -1;
	}

	private void PickIconBySurvivorDef(SurvivorDef survivorDef)
	{
		int num = FindIconIndexForSurvivorDef(survivorDef);
		if (num >= 0)
		{
			PickIcon(survivorIconControllers.elements[num]);
		}
	}

	private void PickIcon(SurvivorIconController newPickedIcon)
	{
		if ((object)pickedIcon != newPickedIcon)
		{
			pickedIcon = newPickedIcon;
			onSurvivorPicked?.Invoke(new SurvivorPickInfo
			{
				localUser = currentLocalUser,
				pickedSurvivor = newPickedIcon.survivorDef
			});
			Util.PlaySound("Kill_SelectScreen", base.gameObject);
		}
	}

	private bool ShouldDisplaySurvivor(SurvivorDef survivorDef)
	{
		if ((bool)survivorDef && !survivorDef.hidden)
		{
			return survivorDef.CheckUserHasRequiredEntitlement(currentLocalUser);
		}
		return false;
	}

	private SurvivorDef GetLocalUserExistingSurvivorPreference()
	{
		return currentLocalUser?.userProfile.GetSurvivorPreference();
	}

	public void Build()
	{
		List<SurvivorDef> list = new List<SurvivorDef>();
		foreach (SurvivorDef orderedSurvivorDef in SurvivorCatalog.orderedSurvivorDefs)
		{
			if (ShouldDisplaySurvivor(orderedSurvivorDef))
			{
				list.Add(orderedSurvivorDef);
			}
		}
		int count = list.Count;
		int desiredCount = Math.Max(CalcGridCellCount(count, iconContainerGrid.constraintCount) - count, 0);
		survivorIconControllers.AllocateElements(count);
		fillerIcons.AllocateElements(desiredCount);
		fillerIcons.MoveElementsToContainerEnd();
		ReadOnlyCollection<SurvivorIconController> elements = survivorIconControllers.elements;
		for (int i = 0; i < count; i++)
		{
			SurvivorDef survivorDef = list[i];
			SurvivorIconController survivorIconController = elements[i];
			survivorIconController.survivorDef = survivorDef;
			survivorIconController.hgButton.defaultFallbackButton = i == 0;
			if (count > 1)
			{
				int num = 0;
				int num2 = 0;
				if (i == 0)
				{
					num = count - 1;
					num2 = i + 1;
				}
				else if (i == count - 1)
				{
					num = i - 1;
					num2 = 0;
				}
				else
				{
					num = i - 1;
					num2 = i + 1;
				}
				UnityEngine.UI.Navigation navigation = survivorIconController.hgButton.navigation;
				navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
				navigation.selectOnLeft = elements[num].hgButton;
				navigation.selectOnRight = elements[num2].hgButton;
				if (i - iconContainerGrid.constraintCount >= 0)
				{
					navigation.selectOnUp = elements[i - iconContainerGrid.constraintCount].hgButton;
				}
				if (i + iconContainerGrid.constraintCount < count)
				{
					navigation.selectOnDown = elements[i + iconContainerGrid.constraintCount].hgButton;
				}
				survivorIconController.hgButton.navigation = navigation;
			}
		}
	}

	private static int CalcGridCellCount(int elementCount, int gridFixedDimensionLength)
	{
		return (int)Mathf.Ceil((float)elementCount / (float)gridFixedDimensionLength) * gridFixedDimensionLength;
	}

	private void EnforceValidChoice()
	{
		int num = FindIconIndex(pickedIcon);
		if ((bool)pickedIcon && pickedIcon.survivorIsAvailable)
		{
			return;
		}
		int num2 = -1;
		ReadOnlyCollection<SurvivorIconController> elements = survivorIconControllers.elements;
		while (num2 < elements.Count)
		{
			int num3 = num + num2;
			if (0 <= num3 && num3 < elements.Count)
			{
				SurvivorIconController survivorIconController = elements[num3];
				if (survivorIconController.survivorIsAvailable)
				{
					PickIcon(survivorIconController);
					break;
				}
			}
			if (num2 >= 0)
			{
				num2++;
			}
			num2 = -num2;
		}
	}
}
