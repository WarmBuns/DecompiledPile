using UnityEngine;

namespace RoR2;

public class TemporaryOverlay : MonoBehaviour
{
	[HideInInspector]
	internal TemporaryOverlayInstance instance;

	public Material originalMaterial;

	public CharacterModel inspectorCharacterModel;

	public bool animateShaderAlpha;

	public AnimationCurve alphaCurve;

	public float duration;

	public bool destroyComponentOnEnd;

	public bool destroyObjectOnEnd;

	public GameObject destroyEffectPrefab;

	public string destroyEffectChildString;

	public Material originalMaterialInstance
	{
		get
		{
			InitializeReference();
			return instance.originalMaterial;
		}
		set
		{
			InitializeReference();
			instance.originalMaterial = value;
		}
	}

	[HideInInspector]
	public Material materialInstance
	{
		get
		{
			InitializeReference();
			return instance.materialInstance;
		}
		set
		{
			InitializeReference();
			instance.materialInstance = value;
		}
	}

	public CharacterModel inspectorCharacterModelInstance
	{
		get
		{
			InitializeReference();
			return instance.inspectorCharacterModel;
		}
		set
		{
			InitializeReference();
			instance.inspectorCharacterModel = value;
		}
	}

	public bool animateShaderAlphaInstance
	{
		get
		{
			InitializeReference();
			return instance.animateShaderAlpha;
		}
		set
		{
			InitializeReference();
			instance.animateShaderAlpha = value;
		}
	}

	public AnimationCurve alphaCurveInstance
	{
		get
		{
			InitializeReference();
			return instance.alphaCurve;
		}
		set
		{
			InitializeReference();
			instance.alphaCurve = value;
		}
	}

	public float durationInstance
	{
		get
		{
			InitializeReference();
			return instance.duration;
		}
		set
		{
			InitializeReference();
			instance.duration = value;
		}
	}

	public bool destroyComponentOnEndInstance
	{
		get
		{
			InitializeReference();
			return instance.destroyComponentOnEnd;
		}
		set
		{
			InitializeReference();
			instance.destroyComponentOnEnd = value;
		}
	}

	public bool destroyObjectOnEndInstance
	{
		get
		{
			InitializeReference();
			return instance.destroyObjectOnEnd;
		}
		set
		{
			InitializeReference();
			instance.destroyObjectOnEnd = value;
		}
	}

	public GameObject destroyEffectPrefabInstance
	{
		get
		{
			InitializeReference();
			return instance.destroyEffectPrefab;
		}
		set
		{
			InitializeReference();
			instance.destroyEffectPrefab = value;
		}
	}

	public string destroyEffectChildStringInstance
	{
		get
		{
			InitializeReference();
			return instance.destroyEffectChildString;
		}
		set
		{
			InitializeReference();
			instance.destroyEffectChildString = value;
		}
	}

	private void Awake()
	{
		InitializeReference();
		CopyDataFromPrefabToInstance();
	}

	private void InitializeReference()
	{
		if (instance == null)
		{
			instance = TemporaryOverlayManager.AddOverlay(base.gameObject);
			instance.componentReference = this;
		}
	}

	private void CopyDataFromPrefabToInstance()
	{
		InitializeReference();
		instance.originalMaterial = originalMaterial;
		instance.inspectorCharacterModel = inspectorCharacterModel;
		instance.animateShaderAlpha = animateShaderAlpha;
		instance.alphaCurve = alphaCurve;
		instance.duration = duration;
		instance.destroyComponentOnEnd = destroyComponentOnEnd;
		instance.destroyObjectOnEnd = destroyObjectOnEnd;
		instance.destroyEffectChildString = destroyEffectChildString;
	}

	private void Start()
	{
		InitializeReference();
		instance.Start();
	}

	private void SetupMaterial()
	{
		instance.SetupMaterial();
	}

	public void AddToCharacerModel(CharacterModel characterModel)
	{
		InitializeReference();
		instance.AddToCharacterModel(characterModel);
	}

	public void RemoveFromCharacterModel()
	{
		InitializeReference();
		instance.RemoveFromCharacterModel();
	}

	private void OnDestroy()
	{
		DestroyInstanceReference();
	}

	internal void DestroyInstanceReference()
	{
		instance = null;
	}
}
