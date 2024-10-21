using UnityEngine;

namespace RoR2;

public class PickupDisplay : MonoBehaviour
{
	[Tooltip("The vertical motion of the display model.")]
	public Wave verticalWave;

	public bool dontInstantiatePickupModel;

	[Tooltip("The speed in degrees/second at which the display model rotates about the y axis.")]
	public float spinSpeed = 75f;

	public GameObject tier1ParticleEffect;

	public GameObject tier2ParticleEffect;

	public GameObject tier3ParticleEffect;

	public GameObject equipmentParticleEffect;

	public GameObject lunarParticleEffect;

	public GameObject bossParticleEffect;

	public GameObject voidParticleEffect;

	[Tooltip("The particle system to tint.")]
	public ParticleSystem[] coloredParticleSystems;

	private PickupIndex pickupIndex = PickupIndex.none;

	private bool hidden;

	public Highlight highlight;

	private static readonly Vector3 idealModelBox = Vector3.one;

	private static readonly float idealVolume = idealModelBox.x * idealModelBox.y * idealModelBox.z;

	private GameObject modelObject;

	private GameObject modelPrefab;

	private float modelScale;

	private float localTime;

	private bool shouldUpdate = true;

	public Renderer modelRenderer { get; private set; }

	private Vector3 localModelPivotPosition => Vector3.up * verticalWave.Evaluate(localTime);

	public void SetPickupIndex(PickupIndex newPickupIndex, bool newHidden = false)
	{
		if (!(pickupIndex == newPickupIndex) || hidden != newHidden)
		{
			pickupIndex = newPickupIndex;
			hidden = newHidden;
			RebuildModel();
		}
	}

	private void DestroyModel()
	{
		if ((bool)modelObject)
		{
			Object.Destroy(modelObject);
			modelObject = null;
			modelRenderer = null;
		}
	}

	public void RebuildModel(GameObject modelObjectOverride = null)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (modelObjectOverride != null)
		{
			modelObject = modelObjectOverride;
		}
		else
		{
			GameObject gameObject = null;
			if (pickupDef != null)
			{
				gameObject = (hidden ? PickupCatalog.GetHiddenPickupDisplayPrefab() : pickupDef.displayPrefab);
			}
			if (modelPrefab != gameObject)
			{
				DestroyModel();
				modelPrefab = gameObject;
				modelScale = base.transform.lossyScale.x;
				if (!dontInstantiatePickupModel && modelPrefab != null)
				{
					modelObject = Object.Instantiate(modelPrefab);
					modelRenderer = modelObject.GetComponentInChildren<Renderer>();
					if ((bool)modelRenderer)
					{
						modelObject.transform.rotation = Quaternion.identity;
						Vector3 size = modelRenderer.bounds.size;
						float num = size.x * size.y * size.z;
						if (num <= float.Epsilon)
						{
							Debug.LogError("PickupDisplay bounds are zero! This is not allowed!");
							num = 1f;
						}
						modelScale *= Mathf.Pow(idealVolume, 1f / 3f) / Mathf.Pow(num, 1f / 3f);
						if ((bool)highlight)
						{
							highlight.targetRenderer = modelRenderer;
							highlight.isOn = true;
							highlight.pickupIndex = pickupIndex;
							highlight.ResetHighlight();
						}
					}
					modelObject.transform.parent = base.transform;
					modelObject.transform.localPosition = localModelPivotPosition;
					modelObject.transform.localRotation = Quaternion.identity;
					modelObject.transform.localScale = new Vector3(modelScale, modelScale, modelScale);
				}
			}
		}
		if ((bool)tier1ParticleEffect)
		{
			tier1ParticleEffect.SetActive(value: false);
		}
		if ((bool)tier2ParticleEffect)
		{
			tier2ParticleEffect.SetActive(value: false);
		}
		if ((bool)tier3ParticleEffect)
		{
			tier3ParticleEffect.SetActive(value: false);
		}
		if ((bool)equipmentParticleEffect)
		{
			equipmentParticleEffect.SetActive(value: false);
		}
		if ((bool)lunarParticleEffect)
		{
			lunarParticleEffect.SetActive(value: false);
		}
		if ((bool)voidParticleEffect)
		{
			voidParticleEffect.SetActive(value: false);
		}
		ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
		EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
		if (itemIndex != ItemIndex.None)
		{
			switch (ItemCatalog.GetItemDef(itemIndex).tier)
			{
			case ItemTier.Tier1:
				if ((bool)tier1ParticleEffect)
				{
					tier1ParticleEffect.SetActive(value: true);
				}
				break;
			case ItemTier.Tier2:
				if ((bool)tier2ParticleEffect)
				{
					tier2ParticleEffect.SetActive(value: true);
				}
				break;
			case ItemTier.Tier3:
				if ((bool)tier3ParticleEffect)
				{
					tier3ParticleEffect.SetActive(value: true);
				}
				break;
			case ItemTier.VoidTier1:
			case ItemTier.VoidTier2:
			case ItemTier.VoidTier3:
			case ItemTier.VoidBoss:
				if ((bool)voidParticleEffect)
				{
					voidParticleEffect.SetActive(value: true);
				}
				break;
			}
		}
		else if (equipmentIndex != EquipmentIndex.None && (bool)equipmentParticleEffect)
		{
			equipmentParticleEffect.SetActive(value: true);
		}
		if ((bool)bossParticleEffect)
		{
			bossParticleEffect.SetActive(pickupDef?.isBoss ?? false);
		}
		if ((bool)lunarParticleEffect)
		{
			lunarParticleEffect.SetActive(pickupDef?.isLunar ?? false);
		}
		if ((bool)highlight)
		{
			highlight.isOn = true;
			highlight.pickupIndex = pickupIndex;
		}
		ParticleSystem[] array = coloredParticleSystems;
		foreach (ParticleSystem obj in array)
		{
			obj.gameObject.SetActive(modelPrefab != null);
			ParticleSystem.MainModule main = obj.main;
			main.startColor = pickupDef?.baseColor ?? PickupCatalog.invalidPickupColor;
		}
	}

	private void Start()
	{
		localTime = 0f;
	}

	private void OnBecameVisible()
	{
		shouldUpdate = true;
	}

	private void OnBecameInvisible()
	{
		shouldUpdate = false;
	}

	private void Update()
	{
		if (shouldUpdate)
		{
			localTime += Time.deltaTime;
			if ((bool)modelObject)
			{
				Transform obj = modelObject.transform;
				Vector3 localEulerAngles = obj.localEulerAngles;
				localEulerAngles.y = spinSpeed * localTime;
				obj.localEulerAngles = localEulerAngles;
				obj.localPosition = localModelPivotPosition;
			}
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Matrix4x4 matrix = Gizmos.matrix;
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
		Gizmos.DrawWireCube(Vector3.zero, idealModelBox);
		Gizmos.matrix = matrix;
	}
}
