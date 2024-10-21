using System;
using System.Collections.Generic;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class BombArtifactManager
{
	private struct BombRequest
	{
		public Vector3 spawnPosition;

		public Vector3 raycastOrigin;

		public float bombBaseDamage;

		public GameObject attacker;

		public TeamIndex teamIndex;

		public float velocityY;
	}

	private static FloatConVar cvSpiteBombCoefficient = new FloatConVar("spite_bomb_coefficient", ConVarFlags.Cheat, "0.5", "Multiplier for number of spite bombs.");

	private static GameObject bombPrefab;

	private static readonly int maxBombCount = 30;

	private static readonly float extraBombPerRadius = 4f;

	private static readonly float bombSpawnBaseRadius = 3f;

	private static readonly float bombSpawnRadiusCoefficient = 4f;

	private static readonly float bombDamageCoefficient = 1.5f;

	private static readonly Queue<BombRequest> bombRequestQueue = new Queue<BombRequest>();

	private static readonly float bombBlastRadius = 7f;

	private static readonly float bombFuseTimeout = 8f;

	private static readonly float maxBombStepUpDistance = 8f;

	private static readonly float maxBombFallDistance = 60f;

	private static ArtifactDef myArtifact => RoR2Content.Artifacts.bombArtifactDef;

	[SystemInitializer(new Type[] { typeof(ArtifactCatalog) })]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/SpiteBomb", delegate(GameObject operationResult)
		{
			bombPrefab = operationResult;
		});
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (NetworkServer.active && !(artifactDef != myArtifact))
		{
			GlobalEventManager.onCharacterDeathGlobal += OnServerCharacterDeath;
			RoR2Application.onFixedUpdate += ProcessBombQueue;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact))
		{
			bombRequestQueue.Clear();
			RoR2Application.onFixedUpdate -= ProcessBombQueue;
			GlobalEventManager.onCharacterDeathGlobal -= OnServerCharacterDeath;
		}
	}

	private static void SpawnBomb(BombRequest bombRequest, float groundY)
	{
		Vector3 spawnPosition = bombRequest.spawnPosition;
		if (spawnPosition.y < groundY + 4f)
		{
			spawnPosition.y = groundY + 4f;
		}
		Vector3 raycastOrigin = bombRequest.raycastOrigin;
		raycastOrigin.y = groundY;
		GameObject gameObject = UnityEngine.Object.Instantiate(bombPrefab, spawnPosition, UnityEngine.Random.rotation);
		SpiteBombController component = gameObject.GetComponent<SpiteBombController>();
		DelayBlast delayBlast = component.delayBlast;
		TeamFilter component2 = gameObject.GetComponent<TeamFilter>();
		component.bouncePosition = raycastOrigin;
		component.initialVelocityY = bombRequest.velocityY;
		delayBlast.position = spawnPosition;
		delayBlast.baseDamage = bombRequest.bombBaseDamage;
		delayBlast.baseForce = 2300f;
		delayBlast.attacker = bombRequest.attacker;
		delayBlast.radius = bombBlastRadius;
		delayBlast.crit = false;
		delayBlast.procCoefficient = 0.75f;
		delayBlast.maxTimer = bombFuseTimeout;
		delayBlast.timerStagger = 0f;
		delayBlast.falloffModel = BlastAttack.FalloffModel.None;
		component2.teamIndex = bombRequest.teamIndex;
		NetworkServer.Spawn(gameObject);
	}

	private static void OnServerCharacterDeath(DamageReport damageReport)
	{
		if (damageReport.victimTeamIndex == TeamIndex.Monster)
		{
			CharacterBody victimBody = damageReport.victimBody;
			Vector3 corePosition = victimBody.corePosition;
			int num = Mathf.Min(maxBombCount, Mathf.CeilToInt(victimBody.bestFitRadius * extraBombPerRadius * cvSpiteBombCoefficient.value));
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = UnityEngine.Random.insideUnitSphere * (bombSpawnBaseRadius + victimBody.bestFitRadius * bombSpawnRadiusCoefficient);
				BombRequest bombRequest = default(BombRequest);
				bombRequest.spawnPosition = corePosition;
				bombRequest.raycastOrigin = corePosition + vector;
				bombRequest.bombBaseDamage = victimBody.damage * bombDamageCoefficient;
				bombRequest.attacker = victimBody.gameObject;
				bombRequest.teamIndex = damageReport.victimTeamIndex;
				bombRequest.velocityY = UnityEngine.Random.Range(5f, 25f);
				BombRequest item = bombRequest;
				bombRequestQueue.Enqueue(item);
			}
		}
	}

	private static void ProcessBombQueue()
	{
		if (bombRequestQueue.Count > 0)
		{
			BombRequest bombRequest = bombRequestQueue.Dequeue();
			Ray ray = new Ray(bombRequest.raycastOrigin + new Vector3(0f, maxBombStepUpDistance, 0f), Vector3.down);
			float maxDistance = maxBombStepUpDistance + maxBombFallDistance;
			if (Physics.Raycast(ray, out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				SpawnBomb(bombRequest, hitInfo.point.y);
			}
		}
	}
}
