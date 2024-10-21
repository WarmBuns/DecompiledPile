using HG;
using UnityEngine;

namespace RoR2.SurvivorMannequins;

public class SurvivorMannequinSlotController : MonoBehaviour
{
	public GameObject toggleableEffect;

	private NetworkUser _networkUser;

	private SurvivorDef _currentSurvivorDef;

	private Loadout currentLoadout;

	private bool loadoutDirty;

	private Transform mannequinInstanceTransform;

	private bool mannequinInstanceDirty;

	public NetworkUser networkUser
	{
		get
		{
			return _networkUser;
		}
		set
		{
			if ((object)_networkUser != value)
			{
				_networkUser = value;
				mannequinInstanceDirty = true;
				loadoutDirty = true;
			}
		}
	}

	private SurvivorDef currentSurvivorDef
	{
		get
		{
			return _currentSurvivorDef;
		}
		set
		{
			if ((object)_currentSurvivorDef != value)
			{
				_currentSurvivorDef = value;
				mannequinInstanceDirty = true;
			}
		}
	}

	private void Awake()
	{
		currentLoadout = new Loadout();
	}

	private void OnEnable()
	{
		NetworkUser.onLoadoutChangedGlobal += OnLoadoutChangedGlobal;
	}

	private void OnDisable()
	{
		NetworkUser.onLoadoutChangedGlobal -= OnLoadoutChangedGlobal;
	}

	private void Update()
	{
		SurvivorDef survivorDef = null;
		if ((bool)networkUser)
		{
			survivorDef = networkUser.GetSurvivorPreference();
		}
		currentSurvivorDef = survivorDef;
		if (mannequinInstanceDirty)
		{
			mannequinInstanceDirty = false;
			RebuildMannequinInstance();
		}
		if (loadoutDirty)
		{
			loadoutDirty = false;
			if ((bool)networkUser)
			{
				networkUser.networkLoadout.CopyLoadout(currentLoadout);
			}
			ApplyLoadoutToMannequinInstance();
		}
		if ((bool)toggleableEffect)
		{
			toggleableEffect.SetActive(networkUser);
		}
	}

	private void OnLoadoutChangedGlobal(NetworkUser networkUser)
	{
		if ((object)this.networkUser == networkUser)
		{
			loadoutDirty = true;
		}
	}

	private void ClearMannequinInstance()
	{
		if ((bool)mannequinInstanceTransform)
		{
			Object.Destroy(mannequinInstanceTransform.gameObject);
		}
		mannequinInstanceTransform = null;
	}

	private void RebuildMannequinInstance()
	{
		ClearMannequinInstance();
		if ((bool)currentSurvivorDef && (bool)currentSurvivorDef.displayPrefab)
		{
			mannequinInstanceTransform = Object.Instantiate(currentSurvivorDef.displayPrefab, base.transform.position, base.transform.rotation, base.transform).transform;
			CharacterSelectSurvivorPreviewDisplayController component = mannequinInstanceTransform.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
			if ((bool)component)
			{
				component.networkUser = networkUser;
			}
			ApplyLoadoutToMannequinInstance();
		}
	}

	private void ApplyLoadoutToMannequinInstance()
	{
		if (!mannequinInstanceTransform)
		{
			return;
		}
		BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(currentSurvivorDef.survivorIndex);
		int skinIndex = (int)currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
		SkinDef safe = ArrayUtils.GetSafe(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
		if ((bool)safe)
		{
			CharacterModel componentInChildren = mannequinInstanceTransform.GetComponentInChildren<CharacterModel>();
			if ((bool)componentInChildren)
			{
				safe.Apply(componentInChildren.gameObject);
			}
		}
	}

	public static void Swap(SurvivorMannequinSlotController a, SurvivorMannequinSlotController b)
	{
		if ((bool)a.mannequinInstanceTransform)
		{
			a.mannequinInstanceTransform.SetParent(b.transform, worldPositionStays: false);
		}
		if ((bool)b.mannequinInstanceTransform)
		{
			b.mannequinInstanceTransform.SetParent(a.transform, worldPositionStays: false);
		}
		Util.Swap(ref a._networkUser, ref b._networkUser);
		Util.Swap(ref a._currentSurvivorDef, ref b._currentSurvivorDef);
		Util.Swap(ref a.currentLoadout, ref b.currentLoadout);
		Util.Swap(ref a.loadoutDirty, ref b.loadoutDirty);
		Util.Swap(ref a.mannequinInstanceDirty, ref b.mannequinInstanceDirty);
		Util.Swap(ref a.mannequinInstanceTransform, ref b.mannequinInstanceTransform);
	}
}
