using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class PickupPickerPanel : MonoBehaviour
{
	public GridLayoutGroup gridlayoutGroup;

	public RectTransform buttonContainer;

	public GameObject buttonPrefab;

	public Image[] coloredImages;

	public Image[] darkColoredImages;

	public int maxColumnCount;

	private UIElementAllocator<MPButton> buttonAllocator;

	[SerializeField]
	private bool shouldRemoveChoice;

	[SerializeField]
	private bool needsNetworking;

	[SerializeField]
	private NetworkMultiOptionPickupPicker networkMultiOptionPickupPicker;

	[Header("Events")]
	public PickupPickerEvent pickupSelected;

	public PickupPickerEvent pickupBaseContentReady;

	private PickupPickerController.Option[] pickupOptions;

	public Action OnPanelSetupComplete;

	public PickupPickerController pickerController { get; set; }

	private void Awake()
	{
		buttonAllocator = new UIElementAllocator<MPButton>(buttonContainer, buttonPrefab);
		buttonAllocator.onCreateElement = OnCreateButton;
		gridlayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		gridlayoutGroup.constraintCount = maxColumnCount;
		ScoreboardController.onScoreboardOpen += DestroyIt;
	}

	private void OnDestroy()
	{
		ScoreboardController.onScoreboardOpen -= DestroyIt;
	}

	private void DestroyIt()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnCreateButton(int index, MPButton button)
	{
		button.onClick.AddListener(delegate
		{
			pickerController.SubmitChoice(index);
			if (pickerController.multiOptionsAvailable || shouldRemoveChoice)
			{
				RemovePickupButtonAvailability(index);
			}
		});
		button.onSelect.AddListener(delegate
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupOptions[index].pickupIndex);
			pickupSelected?.Invoke(button, pickupDef);
		});
	}

	public void RemovePickupButtonAvailability(int pickupOption)
	{
		Sprite sprite = LegacyResourcesAPI.Load<Sprite>("Textures/MiscIcons/texUnlockIcon");
		pickupOptions[pickupOption].available = false;
		MPButton mPButton = buttonAllocator.elements[pickupOption];
		Image component = mPButton.GetComponent<ChildLocator>().FindChild("Icon").GetComponent<Image>();
		component.color = Color.gray;
		component.sprite = sprite;
		mPButton.interactable = false;
	}

	public void SetPickupOptions(PickupPickerController.Option[] options)
	{
		pickupOptions = options;
		buttonAllocator.AllocateElements(options.Length);
		ReadOnlyCollection<MPButton> elements = buttonAllocator.elements;
		Sprite sprite = LegacyResourcesAPI.Load<Sprite>("Textures/MiscIcons/texUnlockIcon");
		if (options.Length != 0)
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(options[0].pickupIndex);
			Color baseColor = pickupDef.baseColor;
			Color darkColor = pickupDef.darkColor;
			Image[] array = coloredImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].color *= baseColor;
			}
			array = darkColoredImages;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].color *= darkColor;
			}
		}
		for (int j = 0; j < options.Length; j++)
		{
			MPButton mPButton = elements[j];
			int num = j - j % maxColumnCount;
			int num2 = j % maxColumnCount;
			int num3 = num2 - maxColumnCount;
			int num4 = num2 - 1;
			int num5 = num2 + 1;
			int num6 = num2 + maxColumnCount;
			UnityEngine.UI.Navigation navigation = mPButton.navigation;
			navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
			if (num4 >= 0)
			{
				MPButton selectOnLeft = elements[num + num4];
				navigation.selectOnLeft = selectOnLeft;
			}
			if (num5 < maxColumnCount && num + num5 < options.Length)
			{
				MPButton selectOnRight = elements[num + num5];
				navigation.selectOnRight = selectOnRight;
			}
			if (num + num3 >= 0)
			{
				MPButton selectOnUp = elements[num + num3];
				navigation.selectOnUp = selectOnUp;
			}
			if (num + num6 < options.Length)
			{
				MPButton selectOnDown = elements[num + num6];
				navigation.selectOnDown = selectOnDown;
			}
			mPButton.navigation = navigation;
			ref PickupPickerController.Option reference = ref options[j];
			PickupDef pickupDef2 = PickupCatalog.GetPickupDef(reference.pickupIndex);
			Image component = mPButton.GetComponent<ChildLocator>().FindChild("Icon").GetComponent<Image>();
			if (reference.available)
			{
				component.color = Color.white;
				component.sprite = pickupDef2?.iconSprite;
				mPButton.interactable = true;
			}
			else
			{
				component.color = Color.gray;
				component.sprite = sprite;
				mPButton.interactable = false;
			}
			pickupBaseContentReady?.Invoke(mPButton, pickupDef2);
		}
		OnPanelSetupComplete?.Invoke();
	}
}
