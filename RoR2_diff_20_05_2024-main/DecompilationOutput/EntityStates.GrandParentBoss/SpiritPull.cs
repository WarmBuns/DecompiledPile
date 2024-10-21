using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class SpiritPull : BaseState
{
	private BullseyeSearch enemyFinder;

	public static float lockOnAngle;

	private Vector3 teleporterIndicatorInstance;

	private Transform modelTransform;

	public static float pullTimer;

	public static float zoneRadius;

	public static float initialDelay;

	private float duration = 4f;

	public static float maxRange;

	public static string teleportZoneString;

	public static GameObject teleportZoneEffect;

	public static GameObject playerTeleportEffect;

	public static float effectsDuration = 2f;

	public static float playerTeleportEffectsDuration = 1f;

	private bool effectsDone;

	private bool gatheredVictims;

	private bool teleported;

	private float stopwatch;

	private List<Vector3> Startpositions;

	private List<Vector3> Endpositions;

	public static int stacks;

	private List<HurtBox> results = new List<HurtBox>();

	private HurtBox foundBullseye;

	private BullseyeSearch search;

	private GameObject spiritPullLocationObject;

	public static string indicatorOnPlayerSoundLoop;

	public static string indicatorOnPlayerSoundStop;

	public static string teleportedPlayerSound;

	public override void OnEnter()
	{
		base.OnEnter();
		Startpositions = new List<Vector3>();
		Endpositions = new List<Vector3>();
		Ray aimRay = GetAimRay();
		modelTransform = GetModelTransform();
		enemyFinder = new BullseyeSearch();
		enemyFinder.maxDistanceFilter = 2000f;
		enemyFinder.maxAngleFilter = lockOnAngle;
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.filterByLoS = false;
		enemyFinder.sortMode = BullseyeSearch.SortMode.Distance;
		enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		enemyFinder.RefreshCandidates();
		foundBullseye = enemyFinder.GetResults().LastOrDefault();
		_ = Vector3.zero;
		if ((bool)foundBullseye)
		{
			teleporterIndicatorInstance = foundBullseye.transform.position;
			if (Physics.Raycast(teleporterIndicatorInstance, Vector3.down, out var hitInfo, 500f, LayerIndex.world.mask))
			{
				teleporterIndicatorInstance = hitInfo.point;
				_ = teleporterIndicatorInstance;
			}
			EffectData effectData = new EffectData
			{
				origin = teleporterIndicatorInstance,
				rotation = teleportZoneEffect.transform.rotation,
				scale = zoneRadius * 2f
			};
			EffectManager.SpawnEffect(teleportZoneEffect, effectData, transmit: true);
			if (spiritPullLocationObject == null)
			{
				spiritPullLocationObject = new GameObject();
			}
			spiritPullLocationObject.transform.position = effectData.origin;
			Util.PlaySound(indicatorOnPlayerSoundLoop, spiritPullLocationObject);
		}
	}

	private Transform FindTargetFarthest(Vector3 point, TeamIndex enemyTeam)
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(enemyTeam);
		float num = 0f;
		Transform result = null;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			float num2 = Vector3.SqrMagnitude(teamMembers[i].transform.position - point);
			if (num2 > num && num2 < maxRange)
			{
				num = num2;
				result = teamMembers[i].transform;
			}
		}
		return result;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		pullTimer -= Time.deltaTime;
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= effectsDuration && !gatheredVictims)
		{
			effectsDone = true;
			gatheredVictims = true;
			GetPlayersInsideTeleportZone();
		}
		if (stopwatch >= effectsDuration + playerTeleportEffectsDuration && !teleported)
		{
			teleported = true;
			TeleportPlayers();
		}
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if (effectsDone)
		{
			SetPositions();
		}
		if (stopwatch >= effectsDuration + playerTeleportEffectsDuration + duration && base.isAuthority)
		{
			effectsDone = false;
			outer.SetNextStateToMain();
		}
	}

	private void GetPlayersInsideTeleportZone()
	{
		Startpositions.Clear();
		Endpositions.Clear();
		if (!foundBullseye)
		{
			return;
		}
		for (int i = 0; i < stacks; i++)
		{
			search = new BullseyeSearch();
			search.filterByLoS = false;
			search.maxDistanceFilter = zoneRadius;
			search.searchOrigin = new Vector3(teleporterIndicatorInstance.x, teleporterIndicatorInstance.y + zoneRadius * (float)i, teleporterIndicatorInstance.z);
			search.sortMode = BullseyeSearch.SortMode.Distance;
			search.teamMaskFilter = TeamMask.allButNeutral;
			search.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
			search.RefreshCandidates();
			search.queryTriggerInteraction = QueryTriggerInteraction.Collide;
			for (int j = 0; j < search.GetResults().ToList().Count; j++)
			{
				if (!results.Contains(search.GetResults().ToList()[j]))
				{
					results.Add(search.GetResults().ToList()[j]);
				}
			}
		}
		if (results.Count <= 0)
		{
			return;
		}
		for (int k = 0; k < results.Count; k++)
		{
			HurtBox hurtBox = results[k];
			Transform transform = hurtBox.healthComponent.body.modelLocator.modelTransform;
			EffectData effectData = new EffectData
			{
				origin = hurtBox.healthComponent.body.footPosition
			};
			EffectManager.SpawnEffect(playerTeleportEffect, effectData, transmit: true);
			if ((bool)transform)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(transform.gameObject);
				temporaryOverlayInstance.duration = 0.6f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matGrandparentTeleportFlash");
				temporaryOverlayInstance.AddToCharacterModel(transform.GetComponent<CharacterModel>());
			}
		}
	}

	private void TeleportPlayers()
	{
		if (results.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < results.Count; i++)
		{
			HurtBox hurtBox = results[i];
			CharacterModel component = hurtBox.healthComponent.body.modelLocator.modelTransform.GetComponent<CharacterModel>();
			HurtBoxGroup component2 = hurtBox.healthComponent.body.modelLocator.modelTransform.GetComponent<HurtBoxGroup>();
			Startpositions.Add(hurtBox.transform.position);
			if ((bool)component2)
			{
				int hurtBoxesDeactivatorCounter = component2.hurtBoxesDeactivatorCounter + 1;
				component2.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			if ((bool)component)
			{
				component.invisibilityCount++;
			}
			CharacterMotor component3 = hurtBox.healthComponent.gameObject.GetComponent<CharacterMotor>();
			if ((bool)component3)
			{
				component3.enabled = false;
			}
			duration = initialDelay;
			GameObject teleportEffectPrefab = Run.instance.GetTeleportEffectPrefab(base.gameObject);
			if ((bool)teleportEffectPrefab)
			{
				Object.Instantiate(teleportEffectPrefab, base.gameObject.transform.position, Quaternion.identity);
			}
			Util.PlaySound(teleportedPlayerSound, hurtBox.gameObject);
			base.characterMotor.velocity = Vector3.zero;
			Vector3 position = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild(teleportZoneString).position;
			NodeGraph groundNodes = SceneInfo.instance.groundNodes;
			NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(position, base.characterBody.hullClassification);
			groundNodes.GetNodePosition(nodeIndex, out position);
			position += hurtBox.healthComponent.body.transform.position - hurtBox.healthComponent.body.footPosition;
			position = new Vector3(position.x, position.y + 0.1f, position.z);
			Endpositions.Add(position);
		}
	}

	private void SetPositions()
	{
		if (results.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < results.Count; i++)
		{
			CharacterMotor component = results[i].healthComponent.gameObject.GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				Vector3 position = Vector3.Lerp(Startpositions[i], Endpositions[i], stopwatch / duration);
				component.Motor.SetPositionAndRotation(position, Quaternion.identity);
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		Util.PlaySound(indicatorOnPlayerSoundStop, spiritPullLocationObject);
		if (results.Count <= 0)
		{
			return;
		}
		for (int i = 0; i < results.Count; i++)
		{
			HurtBox hurtBox = results[i];
			CharacterModel component = hurtBox.healthComponent.body.modelLocator.modelTransform.GetComponent<CharacterModel>();
			HurtBoxGroup component2 = hurtBox.healthComponent.body.modelLocator.modelTransform.GetComponent<HurtBoxGroup>();
			Transform transform = hurtBox.healthComponent.body.modelLocator.modelTransform;
			if ((bool)transform)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(transform.gameObject);
				temporaryOverlayInstance.duration = 0.6f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matGrandparentTeleportFlash");
				temporaryOverlayInstance.AddToCharacterModel(transform.GetComponent<CharacterModel>());
			}
			EffectData effectData = new EffectData
			{
				origin = hurtBox.healthComponent.body.footPosition
			};
			EffectManager.SpawnEffect(playerTeleportEffect, effectData, transmit: true);
			if ((bool)component2)
			{
				int hurtBoxesDeactivatorCounter = component2.hurtBoxesDeactivatorCounter - 1;
				component2.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			if ((bool)component)
			{
				component.invisibilityCount--;
			}
			CharacterMotor component3 = hurtBox.healthComponent.gameObject.GetComponent<CharacterMotor>();
			if ((bool)component3)
			{
				component3.enabled = true;
			}
		}
	}
}
