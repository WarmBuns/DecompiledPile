using System.Collections.Generic;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2;

public class SeekerSoulSpiralManager
{
	public enum SoulSpiralCommandType
	{
		RegisterIncomingSpiral,
		Boost,
		EndBoost
	}

	public SeekerController seekerController;

	private NetworkIdentity seekerNetworkIdentity;

	public CharacterBody seekerBody;

	private SoulSpiralProjectile[] soulSpiralArray;

	private SoulSpiralOrbiter soulSpiralOrbiter;

	private static GameObject soulSpiralBoostVFXPrefab;

	private static string soulSpiralBoostVFXPrefabGuid = "085f424d57ade224388b2ec514439cd9";

	private static string soulSpiralBoostVFXParentName = "Base";

	private EffectManagerHelper boostFXInstance;

	private Transform boostFXParent;

	public bool runFixedUpdate;

	private int soulSpiralLimit = 6;

	private int spiralsRequested;

	private int nextIndex;

	public bool hasAuthority
	{
		get
		{
			if (seekerNetworkIdentity == null)
			{
				return false;
			}
			return Util.HasEffectiveAuthority(seekerNetworkIdentity);
		}
	}

	public SeekerSoulSpiralManager(SeekerController _seekerController)
	{
		seekerController = _seekerController;
		seekerNetworkIdentity = _seekerController.GetComponent<NetworkIdentity>();
		seekerBody = _seekerController.GetComponent<CharacterBody>();
		soulSpiralArray = new SoulSpiralProjectile[soulSpiralLimit * 2];
		soulSpiralOrbiter = new SoulSpiralOrbiter(_seekerController, this);
		ChildLocator childLocator = seekerBody.modelLocator?.modelTransform.GetComponent<ChildLocator>();
		if (childLocator != null)
		{
			int childIndex = childLocator.FindChildIndex(soulSpiralBoostVFXParentName);
			boostFXParent = childLocator.FindChild(childIndex)?.transform;
		}
		if (!(soulSpiralBoostVFXPrefab == null))
		{
			return;
		}
		if (!Addressables.LoadAssetAsync<GameObject>(soulSpiralBoostVFXPrefabGuid).IsValid())
		{
			Debug.LogError("SeekerSoulSpiralManager failed to load " + soulSpiralBoostVFXPrefabGuid + ". Check the path and try again?");
			return;
		}
		LegacyResourcesAPI.LoadAsyncCallback(soulSpiralBoostVFXPrefabGuid, delegate(GameObject result)
		{
			soulSpiralBoostVFXPrefab = result;
		});
	}

	public void ReceiveSpiralCommand(SoulSpiralCommandType msg)
	{
		switch (msg)
		{
		case SoulSpiralCommandType.RegisterIncomingSpiral:
			RegisterIncomingSpiral();
			break;
		case SoulSpiralCommandType.Boost:
			BoostSpirals();
			break;
		case SoulSpiralCommandType.EndBoost:
			NonAuthorityEndBoost();
			break;
		}
	}

	public void MyFixedUpdate()
	{
		soulSpiralOrbiter.UpdateSpirals(ref soulSpiralArray);
	}

	public void FireSoulSpiral(FireProjectileInfo projectileInfo)
	{
		runFixedUpdate = hasAuthority;
		if (!CheckOurSpiralBudget())
		{
			BoostSpirals();
		}
		else
		{
			FireSoulSpiralInternal(projectileInfo);
		}
	}

	public void BoostSpirals()
	{
		FireBoostFX();
		if (hasAuthority)
		{
			soulSpiralOrbiter.BoostAuthority();
		}
	}

	public void FireBoostFX()
	{
		if (!(boostFXParent == null))
		{
			if (boostFXInstance != null && boostFXInstance.gameObject.activeSelf)
			{
				EndBoostFX();
			}
			boostFXInstance = EffectManager.GetAndActivatePooledEffect(soulSpiralBoostVFXPrefab, boostFXParent, inResetLocal: true);
			boostFXInstance.transform.SetParent(boostFXParent, worldPositionStays: false);
		}
	}

	public void EndBoostFX()
	{
		if (!(boostFXInstance == null))
		{
			boostFXInstance.ReturnToPool();
		}
	}

	public void NonAuthorityEndBoost()
	{
		EndBoostFX();
		int num = soulSpiralArray.Length;
		for (int i = 0; i < num; i++)
		{
			if (!(soulSpiralArray[i] == null))
			{
				soulSpiralArray[i].EndBoost();
			}
		}
	}

	private void FireSoulSpiralInternal(FireProjectileInfo projectileInfo)
	{
		RegisterIncomingSpiral();
		ProjectileManager.instance.FireProjectile(projectileInfo);
	}

	public void RegisterIncomingSpiral()
	{
		if (spiralsRequested < 1)
		{
			RoR2Application.onUpdate += DiscoverUnassignedSpirals;
		}
		spiralsRequested++;
	}

	private void DiscoverUnassignedSpirals()
	{
		if (seekerController == null)
		{
			StopListeningForUnassignedSpirals();
			return;
		}
		if (spiralsRequested > 0)
		{
			List<SoulSpiralProjectile> unassignedSoulSpirals = SoulSpiralProjectile.unassignedSoulSpirals;
			int count = unassignedSoulSpirals.Count;
			bool flag = hasAuthority;
			for (int i = 0; i < count; i++)
			{
				SoulSpiralProjectile soulSpiralProjectile = unassignedSoulSpirals[i];
				if (!(soulSpiralProjectile == null) && !(soulSpiralProjectile.projectileController.owner != seekerController.gameObject))
				{
					if (!TryToCacheSoulSpiral(soulSpiralProjectile, out var spiralPosition))
					{
						break;
					}
					soulSpiralProjectile.AssignSpiralOwner(this);
					if (flag)
					{
						SetSpiralOrbitPosition(soulSpiralProjectile, spiralPosition);
					}
					if (!soulSpiralProjectile.projectileController.isPrediction)
					{
						spiralsRequested--;
					}
				}
			}
		}
		if (spiralsRequested < 1)
		{
			RoR2Application.onUpdate -= DiscoverUnassignedSpirals;
		}
	}

	private bool TryToCacheSoulSpiral(SoulSpiralProjectile soulSpiral, out int spiralPosition)
	{
		spiralPosition = -1;
		for (int i = 0; i < soulSpiralLimit; i++)
		{
			if (soulSpiralArray[nextIndex] == null)
			{
				if (soulSpiral.projectileController.isPrediction)
				{
					soulSpiralArray[nextIndex * 2] = soulSpiral;
				}
				else
				{
					soulSpiralArray[nextIndex] = soulSpiral;
				}
				spiralPosition = nextIndex;
				if (!soulSpiral.projectileController.isPrediction)
				{
					nextIndex = (nextIndex + 1) % soulSpiralLimit;
				}
				soulSpiralOrbiter.ResetValues(soulSpiral);
				return true;
			}
			nextIndex = (nextIndex + 1) % soulSpiralLimit;
		}
		return false;
	}

	private bool CheckOurSpiralBudget()
	{
		int num = spiralsRequested;
		for (int i = 0; i < soulSpiralLimit; i++)
		{
			if (soulSpiralArray[i] != null)
			{
				num++;
			}
		}
		return num < soulSpiralLimit;
	}

	private float GetSoulSpiralOffsetDegreesByIndex(int index)
	{
		return index switch
		{
			1 => 240f, 
			2 => 120f, 
			3 => 300f, 
			4 => 60f, 
			5 => 180f, 
			_ => 0f, 
		};
	}

	private void SetSpiralOrbitPosition(SoulSpiralProjectile soulSpiral, int positionIndex)
	{
		soulSpiral.SetInitialDegreesFromOwnerForward(GetSoulSpiralOffsetDegreesByIndex(positionIndex), positionIndex);
	}

	public void ShutdownAndCleanupSpirals()
	{
		if (NetworkServer.active)
		{
			DestroyAllSpirals();
			CatchAndDestroyUnassignedSpirals();
		}
		StopListeningForUnassignedSpirals();
		runFixedUpdate = false;
	}

	private void CatchAndDestroyUnassignedSpirals()
	{
		if (seekerController == null || seekerController.gameObject == null)
		{
			return;
		}
		List<SoulSpiralProjectile> unassignedSoulSpirals = SoulSpiralProjectile.unassignedSoulSpirals;
		for (int num = unassignedSoulSpirals.Count - 1; num >= 0; num--)
		{
			SoulSpiralProjectile soulSpiralProjectile = unassignedSoulSpirals[num];
			if (!(soulSpiralProjectile == null) && !(soulSpiralProjectile.projectileController.owner != seekerController.gameObject))
			{
				soulSpiralProjectile.DestroySpiral();
			}
		}
	}

	private void StopListeningForUnassignedSpirals()
	{
		if (spiralsRequested > 0)
		{
			RoR2Application.onUpdate -= DiscoverUnassignedSpirals;
		}
		spiralsRequested = 0;
	}

	public void DestroyAllSpirals()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		runFixedUpdate = false;
		int num = soulSpiralArray.Length;
		for (int i = 0; i < num; i++)
		{
			if (soulSpiralArray[i] != null)
			{
				soulSpiralArray[i].DestroySpiral();
			}
		}
	}
}
