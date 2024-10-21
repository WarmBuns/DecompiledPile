using System.Collections.Generic;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

public class ScorchlingController : MonoBehaviour
{
	public CharacterBody characterBody;

	public CharacterMotor characterMotor;

	private RigidbodyMotor rigidBodyMotor;

	public string burrowingSoundString;

	public Transform burrowFX;

	public float timeInAirToTele;

	public float timeInAirToShutOffDustTrail = 0.2f;

	private float inAirTimer;

	public GameObject breachEffectPrefab;

	public float breachEffectRadius = 5f;

	public float teleCooldown;

	private GameObject dustTrail;

	private GameObject baseDirt;

	private GameObject armatureObject;

	private GameObject meshObject;

	public bool isBurrowed = true;

	public bool isRecentlyBurrowed = true;

	public bool canTeleport = true;

	private bool dustTrailActive;

	private float teleportTimer;

	private float teleportCooldownDuration = 3f;

	private Vector3 lastSafePosition;

	private GenericSkill breachSkill;

	private GenericSkill lavaBombSkill;

	private GenericSkill ensureBurrowSkill;

	private Quaternion breachBaseDirtRotation;

	private int originalLayer;

	private float cachedYPosition;

	private void Start()
	{
		ChildLocator component = characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>();
		dustTrail = component.FindChild("BurrowLocation").gameObject;
		baseDirt = component.FindChild("BaseDirt").gameObject;
		armatureObject = component.FindChild("Armature").gameObject;
		meshObject = component.FindChild("Mesh").gameObject;
		dustTrailActive = dustTrail.activeInHierarchy;
		SkillLocator skillLocator = characterBody.skillLocator;
		breachSkill = skillLocator.GetSkill(SkillSlot.Primary);
		lavaBombSkill = skillLocator.GetSkill(SkillSlot.Secondary);
		ensureBurrowSkill = skillLocator.GetSkill(SkillSlot.Utility);
		rigidBodyMotor = GetComponent<RigidbodyMotor>();
		Burrow();
		teleportTimer = teleportCooldownDuration;
	}

	private void Update()
	{
		if (!isBurrowed)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		teleportTimer += deltaTime;
		if (!characterMotor.isGrounded)
		{
			if (characterBody.footPosition.y > cachedYPosition)
			{
				cachedYPosition = characterBody.footPosition.y;
				return;
			}
			inAirTimer += deltaTime;
			cachedYPosition = characterBody.footPosition.y;
			if (dustTrailActive && inAirTimer > timeInAirToShutOffDustTrail)
			{
				dustTrailActive = false;
				dustTrail.SetActive(value: false);
				EffectManager.SpawnEffect(breachEffectPrefab, new EffectData
				{
					origin = characterBody.footPosition,
					scale = breachEffectRadius
				}, transmit: true);
			}
			if (!(teleportTimer < teleportCooldownDuration) && canTeleport && inAirTimer > timeInAirToTele && NetworkServer.active)
			{
				Vector3 bestTeleportPosition = GetBestTeleportPosition();
				TeleportHelper.TeleportBody(characterBody, bestTeleportPosition);
				inAirTimer = 0f;
				teleportTimer = 0f;
			}
		}
		else
		{
			inAirTimer = 0f;
			cachedYPosition = float.MaxValue;
			lastSafePosition = characterBody.footPosition;
			if (!dustTrailActive)
			{
				dustTrail.SetActive(value: true);
				dustTrailActive = true;
				EffectManager.SpawnEffect(breachEffectPrefab, new EffectData
				{
					origin = characterBody.footPosition,
					scale = breachEffectRadius
				}, transmit: true);
			}
		}
	}

	public void ResetTeleportTimer()
	{
		teleportTimer = 0f;
		inAirTimer = 0f;
	}

	public void SetTeleportPermission(bool b)
	{
		canTeleport = b;
		ResetTeleportTimer();
	}

	private Vector3 GetBestTeleportPosition()
	{
		NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
		List<NodeGraph.NodeIndex> list = nodeGraph.FindNodesInRange(characterBody.corePosition, 3f, 16f, HullMask.Human);
		if (list.Count < 1)
		{
			return lastSafePosition;
		}
		nodeGraph.GetNodePosition(list[0], out var position);
		return position;
	}

	public void Burrow()
	{
		isRecentlyBurrowed = true;
		isBurrowed = true;
		SetTeleportPermission(b: true);
		if (NetworkServer.active)
		{
			breachSkill.stock = 1;
			lavaBombSkill.stock = 0;
			ensureBurrowSkill.stock = 5;
			characterMotor.walkSpeedPenaltyCoefficient = 1f;
			if (characterBody.GetBuffCount(RoR2Content.Buffs.HiddenInvincibility.buffIndex) < 1)
			{
				characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
			}
		}
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(characterBody.teamComponent.teamIndex).intVal;
		characterMotor.Motor.RebuildCollidableLayers();
		if ((bool)baseDirt && (bool)dustTrail)
		{
			baseDirt.SetActive(value: false);
			dustTrail.SetActive(value: true);
			dustTrailActive = true;
			armatureObject.SetActive(value: false);
			meshObject.SetActive(value: false);
		}
	}

	public void LateUpdate()
	{
		if (!isBurrowed && (bool)baseDirt && NetworkServer.active && baseDirt.transform.rotation != breachBaseDirtRotation)
		{
			baseDirt.transform.rotation = breachBaseDirtRotation;
		}
	}

	public void Breach()
	{
		isBurrowed = false;
		SetTeleportPermission(b: false);
		if (NetworkServer.active)
		{
			characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
			breachSkill.stock = 0;
			lavaBombSkill.stock = 1;
			ensureBurrowSkill.stock = 5;
			characterMotor.walkSpeedPenaltyCoefficient = 0f;
			if ((bool)characterBody)
			{
				characterBody.isSprinting = false;
			}
			if ((bool)rigidBodyMotor)
			{
				rigidBodyMotor.moveVector = Vector3.zero;
			}
			breachBaseDirtRotation = baseDirt.transform.rotation;
			breachBaseDirtRotation.eulerAngles = new Vector3(breachBaseDirtRotation.eulerAngles.x, Mathf.Floor(breachBaseDirtRotation.eulerAngles.y), breachBaseDirtRotation.eulerAngles.z);
			baseDirt.transform.localEulerAngles = Vector3.zero;
			baseDirt.transform.rotation = breachBaseDirtRotation;
		}
		base.gameObject.layer = originalLayer;
		characterMotor.Motor.RebuildCollidableLayers();
		if ((bool)baseDirt && (bool)dustTrail)
		{
			baseDirt.SetActive(value: true);
			dustTrail.SetActive(value: false);
			dustTrailActive = false;
			armatureObject.SetActive(value: true);
			meshObject.SetActive(value: true);
		}
	}
}
