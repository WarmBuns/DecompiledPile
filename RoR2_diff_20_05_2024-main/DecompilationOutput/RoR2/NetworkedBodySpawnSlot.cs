using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class NetworkedBodySpawnSlot : MonoBehaviour, MasterSpawnSlotController.ISlot
{
	[SerializeField]
	private SpawnCard spawnCard;

	[SerializeField]
	private CharacterBody ownerBody;

	[SerializeField]
	private ChildLocator ownerChildLocator;

	[SerializeField]
	private string ownerAttachChildName;

	[SerializeField]
	private GameObject spawnEffectPrefab;

	[SerializeField]
	private GameObject killEffectPrefab;

	private CharacterMaster spawnedMaster;

	private bool isSpawnPending;

	public bool IsOpen()
	{
		if (!isSpawnPending)
		{
			return !spawnedMaster;
		}
		return false;
	}

	public void Spawn(GameObject summonerBodyObject, Xoroshiro128Plus rng, Action<MasterSpawnSlotController.ISlot, SpawnCard.SpawnResult> callback = null)
	{
		if (!NetworkServer.active || !ownerBody)
		{
			return;
		}
		Transform spawnOnTarget = ownerBody.transform;
		if (!string.IsNullOrEmpty(ownerAttachChildName) && (bool)ownerChildLocator)
		{
			Transform transform = ownerChildLocator.FindChild(ownerAttachChildName);
			if ((bool)transform)
			{
				spawnOnTarget = transform.transform;
			}
		}
		DirectorPlacementRule placementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.Direct,
			spawnOnTarget = spawnOnTarget
		};
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);
		directorSpawnRequest.summonerBodyObject = summonerBodyObject;
		directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult spawnResult)
		{
			OnSpawnedServer(ownerBody.gameObject, spawnResult, callback);
		});
		isSpawnPending = true;
		DirectorCore.instance?.TrySpawnObject(directorSpawnRequest);
	}

	public void Kill()
	{
		if (NetworkServer.active && (bool)spawnedMaster)
		{
			GameObject bodyObject = spawnedMaster.GetBodyObject();
			if ((bool)killEffectPrefab && (bool)bodyObject)
			{
				EffectData effectData = new EffectData
				{
					origin = bodyObject.transform.position,
					rotation = bodyObject.transform.rotation
				};
				EffectManager.SpawnEffect(killEffectPrefab, effectData, transmit: true);
			}
			spawnedMaster.TrueKill();
			spawnedMaster = null;
		}
	}

	private void OnSpawnedServer(GameObject ownerBodyObject, SpawnCard.SpawnResult spawnResult, Action<MasterSpawnSlotController.ISlot, SpawnCard.SpawnResult> callback)
	{
		isSpawnPending = false;
		if (spawnResult.success)
		{
			spawnedMaster = spawnResult.spawnedInstance.GetComponent<CharacterMaster>();
			spawnedMaster.onBodyDestroyed += OnBodyDestroyed;
			GameObject bodyObject = spawnedMaster.GetBodyObject();
			if ((bool)bodyObject)
			{
				if ((bool)spawnEffectPrefab)
				{
					EffectData effectData = new EffectData
					{
						origin = bodyObject.transform.position,
						rotation = bodyObject.transform.rotation
					};
					EffectManager.SpawnEffect(spawnEffectPrefab, effectData, transmit: true);
				}
				NetworkedBodyAttachment component = bodyObject.GetComponent<NetworkedBodyAttachment>();
				if ((bool)component)
				{
					component.AttachToGameObjectAndSpawn(ownerBodyObject, ownerAttachChildName);
				}
			}
		}
		callback?.Invoke(this, spawnResult);
	}

	private void OnBodyDestroyed(CharacterBody body)
	{
		if (body.master.IsDeadAndOutOfLivesServer() && (bool)spawnedMaster)
		{
			spawnedMaster.onBodyDestroyed -= OnBodyDestroyed;
			spawnedMaster = null;
		}
	}
}
