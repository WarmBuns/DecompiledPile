using System;
using System.Collections.Generic;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class MageCalibrationController : MonoBehaviour
{
	[Serializable]
	public struct CalibrationInfo
	{
		public bool enableCalibrationOverlay;

		public Material calibrationOverlayMaterial;
	}

	public CalibrationInfo[] calibrationInfos;

	public SkinnedMeshRenderer calibrationOverlayRenderer;

	[Tooltip("The state machine upon which to perform the calibration state.")]
	public EntityStateMachine stateMachine;

	[Tooltip("The priority with which the calibration state will try to interrupt the current state.")]
	public InterruptPriority calibrationStateInterruptPriority;

	private CharacterBody characterBody;

	private bool hasEffectiveAuthority;

	private MageElement _currentElement;

	private static readonly int[] elementCounter = new int[4];

	private MageElement currentElement
	{
		get
		{
			return _currentElement;
		}
		set
		{
			if (value != _currentElement)
			{
				_currentElement = value;
				RefreshCalibrationElement(_currentElement);
			}
		}
	}

	private void Awake()
	{
		characterBody = GetComponent<CharacterBody>();
		characterBody.onInventoryChanged += OnInventoryChanged;
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	private void Start()
	{
		currentElement = GetAwardedElementFromInventory();
		RefreshCalibrationElement(currentElement);
	}

	private void OnDestroy()
	{
		characterBody.onInventoryChanged -= OnInventoryChanged;
	}

	private void OnInventoryChanged()
	{
		base.enabled = true;
	}

	private void FixedUpdate()
	{
		base.enabled = false;
		currentElement = GetAwardedElementFromInventory();
		if (hasEffectiveAuthority && currentElement == MageElement.None)
		{
			MageElement mageElement = CalcElementToAward();
			if (mageElement != 0 && !(stateMachine.state is MageCalibrate))
			{
				MageCalibrate mageCalibrate = new MageCalibrate();
				mageCalibrate.element = mageElement;
				stateMachine.SetInterruptState(mageCalibrate, calibrationStateInterruptPriority);
			}
		}
	}

	private MageElement GetAwardedElementFromInventory()
	{
		Inventory inventory = characterBody.inventory;
		if ((bool)inventory)
		{
			MageElement mageElement = (MageElement)inventory.GetItemCount(JunkContent.Items.MageAttunement);
			if ((int)mageElement >= 0 && (int)mageElement < 4)
			{
				return mageElement;
			}
		}
		return MageElement.None;
	}

	private MageElement CalcElementToAward()
	{
		for (int i = 0; i < elementCounter.Length; i++)
		{
			elementCounter[i] = 0;
		}
		Inventory inventory = characterBody.inventory;
		if (!inventory)
		{
			return MageElement.None;
		}
		List<ItemIndex> itemAcquisitionOrder = inventory.itemAcquisitionOrder;
		for (int j = 0; j < itemAcquisitionOrder.Count; j++)
		{
			ItemCatalog.GetItemDef(itemAcquisitionOrder[j]);
		}
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(inventory.currentEquipmentIndex);
		if ((bool)equipmentDef)
		{
			MageElement mageElement = equipmentDef.mageElement;
			if (mageElement != 0)
			{
				elementCounter[(uint)mageElement] += 2;
			}
		}
		MageElement result = MageElement.None;
		int num = 0;
		MageElement mageElement2 = MageElement.Fire;
		while ((int)mageElement2 < 4)
		{
			int num2 = elementCounter[(uint)mageElement2];
			if (num2 > num)
			{
				result = mageElement2;
				num = num2;
			}
			mageElement2++;
		}
		if (num >= 5)
		{
			return result;
		}
		return MageElement.None;
	}

	public MageElement GetActiveCalibrationElement()
	{
		return currentElement;
	}

	public void SetElement(MageElement newElement)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		Inventory inventory = characterBody.inventory;
		if ((bool)inventory)
		{
			MageElement mageElement = (MageElement)inventory.GetItemCount(JunkContent.Items.MageAttunement);
			if (mageElement != newElement)
			{
				int count = newElement - mageElement;
				inventory.GiveItem(JunkContent.Items.MageAttunement, count);
			}
		}
	}

	public void RefreshCalibrationElement(MageElement targetElement)
	{
		CalibrationInfo calibrationInfo = calibrationInfos[(uint)targetElement];
		calibrationOverlayRenderer.enabled = calibrationInfo.enableCalibrationOverlay;
		calibrationOverlayRenderer.material = calibrationInfo.calibrationOverlayMaterial;
	}
}
