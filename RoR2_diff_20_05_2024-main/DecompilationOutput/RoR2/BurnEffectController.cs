using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class BurnEffectController : MonoBehaviour
{
	public class EffectParams
	{
		public string startSound;

		public string stopSound;

		public Material overlayMaterial;

		public GameObject fireEffectPrefab;
	}

	private List<BurnEffectControllerHelper> burnEffectInstances;

	public GameObject target;

	private TemporaryOverlayInstance temporaryOverlay;

	private int soundID;

	public EffectParams effectType = normalEffect;

	public static EffectParams normalEffect;

	public static EffectParams helfireEffect;

	public static EffectParams poisonEffect;

	public static EffectParams blightEffect;

	public static EffectParams strongerBurnEffect;

	public float fireParticleSize = 5f;

	[InitDuringStartup]
	private static void Init()
	{
		normalEffect = new EffectParams
		{
			startSound = "Play_item_proc_igniteOnKill_Loop",
			stopSound = "Stop_item_proc_igniteOnKill_Loop"
		};
		LegacyResourcesAPI.LoadAsyncCallback("Materials/matOnFire", delegate(Material val)
		{
			normalEffect.overlayMaterial = val;
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/FireEffect", delegate(GameObject val)
		{
			normalEffect.fireEffectPrefab = val;
		});
		helfireEffect = new EffectParams
		{
			startSound = "Play_item_proc_igniteOnKill_Loop",
			stopSound = "Stop_item_proc_igniteOnKill_Loop"
		};
		LegacyResourcesAPI.LoadAsyncCallback("Materials/matOnHelfire", delegate(Material val)
		{
			helfireEffect.overlayMaterial = val;
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/HelfireEffect", delegate(GameObject val)
		{
			helfireEffect.fireEffectPrefab = val;
		});
		poisonEffect = new EffectParams();
		LegacyResourcesAPI.LoadAsyncCallback("Materials/matPoisoned", delegate(Material val)
		{
			poisonEffect.overlayMaterial = val;
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/PoisonEffect", delegate(GameObject val)
		{
			poisonEffect.fireEffectPrefab = val;
		});
		blightEffect = new EffectParams();
		LegacyResourcesAPI.LoadAsyncCallback("Materials/matBlighted", delegate(Material val)
		{
			blightEffect.overlayMaterial = val;
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/BlightEffect", delegate(GameObject val)
		{
			blightEffect.fireEffectPrefab = val;
		});
		strongerBurnEffect = new EffectParams();
		LegacyResourcesAPI.LoadAsyncCallback("Materials/matStrongerBurn", delegate(Material val)
		{
			strongerBurnEffect.overlayMaterial = val;
		});
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/StrongerBurnEffect", delegate(GameObject val)
		{
			strongerBurnEffect.fireEffectPrefab = val;
		});
	}

	private void Start()
	{
		if (effectType == null)
		{
			Debug.LogError("BurnEffectController on " + base.gameObject.name + " has no effect type!");
			return;
		}
		Util.PlaySound(effectType.startSound, base.gameObject);
		burnEffectInstances = new List<BurnEffectControllerHelper>();
		if (effectType.overlayMaterial != null)
		{
			temporaryOverlay = TemporaryOverlayManager.AddOverlay(base.gameObject);
			temporaryOverlay.originalMaterial = effectType.overlayMaterial;
		}
		if (!target)
		{
			return;
		}
		CharacterModel component = target.GetComponent<CharacterModel>();
		if (!component)
		{
			return;
		}
		if (temporaryOverlay != null)
		{
			temporaryOverlay.AddToCharacterModel(component);
		}
		CharacterBody body = component.body;
		CharacterModel.RendererInfo[] baseRendererInfos = component.baseRendererInfos;
		if (!body)
		{
			return;
		}
		for (int i = 0; i < baseRendererInfos.Length; i++)
		{
			if (!baseRendererInfos[i].ignoreOverlays)
			{
				BurnEffectControllerHelper burnEffectControllerHelper = AddFireParticles(baseRendererInfos[i].renderer, body.coreTransform);
				if ((bool)burnEffectControllerHelper)
				{
					burnEffectInstances.Add(burnEffectControllerHelper);
				}
			}
		}
	}

	private void OnDestroy()
	{
		Util.PlaySound(effectType.stopSound, base.gameObject);
		if (temporaryOverlay != null)
		{
			temporaryOverlay.Destroy();
		}
		for (int i = 0; i < burnEffectInstances.Count; i++)
		{
			if ((bool)burnEffectInstances[i])
			{
				burnEffectInstances[i].EndEffect();
			}
		}
	}

	private BurnEffectControllerHelper AddFireParticles(Renderer modelRenderer, Transform targetParentTransform)
	{
		if (modelRenderer is MeshRenderer || modelRenderer is SkinnedMeshRenderer)
		{
			GameObject fireEffectPrefab = effectType.fireEffectPrefab;
			EffectManagerHelper andActivatePooledEffect = EffectManager.GetAndActivatePooledEffect(fireEffectPrefab, targetParentTransform);
			if (!andActivatePooledEffect)
			{
				Debug.LogWarning("Could not spawn the BurnEffect prefab: " + fireEffectPrefab?.ToString() + ".");
				return null;
			}
			BurnEffectControllerHelper component = andActivatePooledEffect.GetComponent<BurnEffectControllerHelper>();
			if (!component)
			{
				Debug.LogWarning("Burn effect " + fireEffectPrefab?.ToString() + " doesn't have a BurnEffectControllerHelper applied.  It can't be applied.");
				andActivatePooledEffect.ReturnToPool();
				return null;
			}
			component.InitializeBurnEffect(modelRenderer);
			return component;
		}
		return null;
	}

	public void HandleDestroy()
	{
		Object.Destroy(base.gameObject);
	}
}
