using UnityEngine;

namespace RoR2;

public class TemporaryOverlayInstance
{
	public GameObject gameObject;

	public int managerIndex;

	private TemporaryOverlay _componentReference;

	public bool hasComponentReference;

	public Material originalMaterial;

	public Material materialInstance;

	private bool isAssigned;

	public CharacterModel assignedCharacterModel;

	public CharacterModel inspectorCharacterModel;

	public bool animateShaderAlpha;

	public AnimationCurve alphaCurve;

	public float duration;

	public bool destroyComponentOnEnd;

	public bool destroyObjectOnEnd;

	public GameObject destroyEffectPrefab;

	public string destroyEffectChildString;

	public float stopwatch;

	public bool initialized;

	public bool transmit = true;

	public TemporaryOverlay componentReference
	{
		get
		{
			return _componentReference;
		}
		set
		{
			_componentReference = value;
			hasComponentReference = hasComponentReference || _componentReference != null;
		}
	}

	public TemporaryOverlayInstance(GameObject _gameObject)
	{
		gameObject = _gameObject;
	}

	public void Start()
	{
		if (!initialized)
		{
			SetupMaterial();
			if ((bool)inspectorCharacterModel)
			{
				AddToCharacterModel(inspectorCharacterModel);
			}
		}
	}

	internal bool ValidateOverlay()
	{
		if (gameObject != null)
		{
			if (hasComponentReference)
			{
				return componentReference != null;
			}
			return true;
		}
		return false;
	}

	public void Destroy()
	{
		CleanupEffect();
	}

	public void CleanupEffect()
	{
		RemoveFromCharacterModel();
		if ((bool)materialInstance)
		{
			Object.Destroy(materialInstance);
		}
		if (!destroyObjectOnEnd && !destroyComponentOnEnd)
		{
			return;
		}
		if (hasComponentReference && componentReference != null)
		{
			componentReference.DestroyInstanceReference();
			if (destroyComponentOnEnd)
			{
				Object.Destroy(componentReference);
			}
		}
		componentReference = null;
		if (destroyObjectOnEnd && gameObject != null)
		{
			Object.Destroy(gameObject);
		}
		gameObject = null;
	}

	public void SetupMaterial()
	{
		if (!materialInstance)
		{
			materialInstance = new Material(originalMaterial);
		}
	}

	public void AddToCharacterModel(CharacterModel characterModel)
	{
		SetupMaterial();
		if ((bool)characterModel)
		{
			characterModel.AddTempOverlay(this);
			characterModel.forceUpdate = true;
			isAssigned = true;
			assignedCharacterModel = characterModel;
		}
		initialized = true;
	}

	public void RemoveFromCharacterModel()
	{
		if ((bool)assignedCharacterModel)
		{
			assignedCharacterModel.RemoveTempOverlay(this);
			assignedCharacterModel.forceUpdate = true;
			isAssigned = false;
			assignedCharacterModel = null;
		}
	}
}
