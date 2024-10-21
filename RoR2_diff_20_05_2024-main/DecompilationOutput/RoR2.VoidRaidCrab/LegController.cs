using System;
using System.Linq;
using EntityStates.VoidRaidCrab.Joint;
using EntityStates.VoidRaidCrab.Leg;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.VoidRaidCrab;

public class LegController : MonoBehaviour
{
	private const string jointDeathStateMachineName = "Body";

	public EntityStateMachine stateMachine;

	public Animator animator;

	public string primaryLayerName;

	[Tooltip("The transform that should be considered the origin of this leg, points outward from the base, and provides a transform for consistent local space conversions. This field must always be set, and is available for the case that the object this component is attached to is not a bone meeting the metioned criteria.")]
	public Transform originTransform;

	[Header("Regeneration")]
	public float jointRegenDuration = 15f;

	[Header("Footstep Concussive Blast")]
	public float footstepBlastRadius = 20f;

	public BlastAttack.FalloffModel footstepFalloffModel = BlastAttack.FalloffModel.SweetSpot;

	public float footstepBlastForce = 500f;

	public Vector3 footstepBonusForce;

	public GameObject footstepBlastEffectPrefab;

	[Header("Stomp")]
	public Transform stompRangeNearMarker;

	public Transform stompRangeFarMarker;

	public Transform stompRangeLeftMarker;

	public Transform stompRangeRightMarker;

	public float stompAimSpeed = 15f;

	public string stompXParameter;

	public string stompYParameter;

	public string stompPlaybackRateParam;

	private int stompXParameterHash;

	private int stompYParameterHash;

	private int stompPlaybackRateHash;

	[Header("Retraction")]
	public AnimationCurve retractionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public string retractionLayerName;

	private float currentRetractionWeight;

	private float desiredRetractionWeight;

	public float retractionBlendTransitionRate = 1f;

	public bool shouldRetract;

	[NonSerialized]
	public bool isBreakSuppressed;

	private bool wasJointBodyDead;

	private bool isJointBodyCurrentlyDying;

	private float jointRegenStopwatchServer;

	private Vector3? stompTargetWorldPosition;

	private Vector2 currentLocalStompPosition = Vector2.zero;

	public CharacterBody mainBody { get; private set; }

	public GameObject mainBodyGameObject { get; private set; }

	public bool mainBodyHasEffectiveAuthority => mainBody?.hasEffectiveAuthority ?? false;

	public ChildLocator legChildLocator { get; private set; }

	public ChildLocator childLocator { get; private set; }

	public Transform toeTransform { get; private set; }

	public Transform toeTipTransform { get; private set; }

	public Transform footTranform { get; private set; }

	public CharacterMaster jointMaster { get; private set; }

	public CharacterBody jointBody
	{
		get
		{
			if (!jointMaster)
			{
				return null;
			}
			return jointMaster.GetBody();
		}
	}

	public bool IsBusy()
	{
		return !(stateMachine.state is Idle);
	}

	public bool IsStomping()
	{
		return stateMachine.state is BaseStompState;
	}

	private void OnEnable()
	{
		CharacterModel componentInParent = GetComponentInParent<CharacterModel>();
		mainBody = (componentInParent ? componentInParent.body : null);
		mainBodyGameObject = (mainBody ? mainBody.gameObject : null);
		childLocator = GetComponent<ChildLocator>();
		footTranform = childLocator.FindChild("Foot");
		toeTransform = childLocator.FindChild("Toe");
		toeTipTransform = childLocator.FindChild("ToeTip");
		stompXParameterHash = Animator.StringToHash(stompXParameter);
		stompYParameterHash = Animator.StringToHash(stompYParameter);
		stompPlaybackRateHash = Animator.StringToHash(stompPlaybackRateParam);
	}

	private void FixedUpdate()
	{
		desiredRetractionWeight = (shouldRetract ? 1f : 0f);
		currentRetractionWeight = Mathf.Clamp01(Mathf.MoveTowards(currentRetractionWeight, desiredRetractionWeight, Time.fixedDeltaTime * retractionBlendTransitionRate));
		int layerIndex = animator.GetLayerIndex(retractionLayerName);
		float weight = retractionCurve.Evaluate(currentRetractionWeight);
		animator.SetLayerWeight(layerIndex, weight);
		if (!wasJointBodyDead && IsBreakPending())
		{
			wasJointBodyDead = true;
			isJointBodyCurrentlyDying = true;
		}
		if (mainBodyHasEffectiveAuthority)
		{
			UpdateStompTargetPositionAuthority(Time.fixedDeltaTime);
		}
		if (NetworkServer.active && !jointBody)
		{
			jointRegenStopwatchServer += Time.fixedDeltaTime;
			if (jointRegenStopwatchServer >= jointRegenDuration)
			{
				RegenerateServer();
			}
		}
	}

	public bool RequestStomp(GameObject target)
	{
		if (!IsBusy())
		{
			stateMachine.SetNextState(new PreStompLegRaise
			{
				target = target
			});
			return true;
		}
		return false;
	}

	public void SetStompTargetWorldPosition(Vector3? newStompTargetWorldPosition)
	{
		stompTargetWorldPosition = newStompTargetWorldPosition;
	}

	public bool SetJointMaster(CharacterMaster master, ChildLocator legChildLocator)
	{
		this.legChildLocator = legChildLocator;
		if (!jointMaster)
		{
			jointMaster = master;
			if ((bool)jointMaster)
			{
				jointMaster.onBodyDestroyed += OnJointBodyDestroyed;
				jointMaster.onBodyStart += OnJointBodyStart;
				MirrorLegJoints();
			}
			return true;
		}
		Debug.LogError("LegController on " + base.gameObject.name + " already has a jointMaster set!");
		return false;
	}

	private void OnJointBodyStart(CharacterBody body)
	{
		wasJointBodyDead = false;
		isJointBodyCurrentlyDying = false;
		MirrorLegJoints();
	}

	private void OnJointBodyDestroyed(CharacterBody body)
	{
		isJointBodyCurrentlyDying = false;
	}

	public bool IsSupportingWeight()
	{
		if (!IsBroken() && !IsBreakPending())
		{
			return !IsBusy();
		}
		return false;
	}

	public bool CanBreak()
	{
		return jointBody;
	}

	public bool IsBreakPending()
	{
		if ((bool)jointBody)
		{
			HealthComponent healthComponent = jointBody.healthComponent;
			if ((bool)healthComponent)
			{
				return !healthComponent.alive;
			}
			return false;
		}
		return false;
	}

	public bool IsBroken()
	{
		if (!isJointBodyCurrentlyDying)
		{
			return !jointBody;
		}
		return true;
	}

	public bool DoesJointExist()
	{
		return jointBody;
	}

	public void CompleteBreakAuthority()
	{
		if (!jointMaster)
		{
			return;
		}
		CharacterBody characterBody = jointBody;
		if (!characterBody)
		{
			return;
		}
		HealthComponent healthComponent = characterBody.healthComponent;
		if ((bool)healthComponent)
		{
			if (NetworkServer.active)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.crit = false;
				damageInfo.damage = healthComponent.fullCombinedHealth;
				damageInfo.procCoefficient = 0f;
				mainBody.healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
			}
			mainBody.healthComponent.UpdateLastHitTime(0f, Vector3.zero, damageIsSilent: true, healthComponent.lastHitAttacker);
		}
		if (EntityStateMachine.FindByCustomName(characterBody.gameObject, "Body").state is PreDeathState preDeathState)
		{
			preDeathState.canProceed = true;
		}
	}

	public void RegenerateServer()
	{
		if ((bool)jointMaster)
		{
			jointRegenStopwatchServer = 0f;
			if (!jointMaster.GetBody())
			{
				jointMaster.Respawn(mainBody.transform.position, mainBody.transform.rotation);
			}
		}
	}

	private void MirrorLegJoints()
	{
		GameObject bodyObject = jointMaster.GetBodyObject();
		if ((bool)bodyObject && (bool)legChildLocator)
		{
			ChildLocatorMirrorController component = bodyObject.GetComponent<ChildLocatorMirrorController>();
			if ((bool)component)
			{
				component.referenceLocator = legChildLocator;
			}
		}
	}

	private Vector2 WorldPointToLocalStompPoint(Vector3 worldPoint)
	{
		Vector3 vector = originTransform.InverseTransformVector(worldPoint);
		return new Vector2(vector.x, vector.z);
	}

	private Vector2 LocalStompPointToStompParams(Vector2 stompPoint)
	{
		float x = originTransform.InverseTransformVector(stompRangeLeftMarker.position).x;
		float x2 = originTransform.InverseTransformVector(stompRangeRightMarker.position).x;
		float z = originTransform.InverseTransformVector(stompRangeNearMarker.position).z;
		float z2 = originTransform.InverseTransformVector(stompRangeFarMarker.position).z;
		float x3 = Util.Remap(currentLocalStompPosition.x, x, x2, -1f, 1f);
		float y = Util.Remap(currentLocalStompPosition.y, z, z2, -1f, 1f);
		return new Vector2(x3, y);
	}

	private void UpdateStompTargetPositionAuthority(float deltaTime)
	{
		Vector3 worldPoint = toeTipTransform.position;
		if (stompTargetWorldPosition.HasValue)
		{
			worldPoint = stompTargetWorldPosition.Value;
		}
		Vector2 target = WorldPointToLocalStompPoint(worldPoint);
		currentLocalStompPosition = Vector2.MoveTowards(currentLocalStompPosition, target, stompAimSpeed * deltaTime);
		Vector2 vector = LocalStompPointToStompParams(currentLocalStompPosition);
		animator.SetFloat(stompXParameterHash, vector.x);
		animator.SetFloat(stompYParameterHash, vector.y);
	}

	private bool GetKneeToToeTipRaycast(out Vector3 hitPosition, out Vector3 hitNormal, out Vector3 rayNormal)
	{
		Vector3 position = footTranform.position;
		Vector3 position2 = toeTipTransform.position;
		Vector3 vector = position2 - position;
		float magnitude = vector.magnitude;
		if (magnitude <= Mathf.Epsilon)
		{
			hitPosition = position2;
			hitNormal = Vector3.up;
			rayNormal = Vector3.down;
			return false;
		}
		Vector3 vector2 = vector / magnitude;
		RaycastHit[] array = Physics.RaycastAll(new Ray(position, vector2), magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
		float num = float.PositiveInfinity;
		int num2 = -1;
		for (int i = 0; i < array.Length; i++)
		{
			ref RaycastHit reference = ref array[i];
			if (reference.distance < num && !(reference.collider.transform.root == base.transform.root))
			{
				num2 = i;
				num = reference.distance;
			}
		}
		rayNormal = vector2;
		if (num2 != -1)
		{
			ref RaycastHit reference2 = ref array[num2];
			hitPosition = reference2.point;
			hitNormal = reference2.normal;
			return true;
		}
		hitPosition = toeTipTransform.position;
		hitNormal = -rayNormal;
		return false;
	}

	public void DoToeConcussionBlastAuthority(Vector3? positionOverride = null, bool useEffect = true)
	{
		if (!mainBodyHasEffectiveAuthority)
		{
			throw new Exception("Caller does not have authority.");
		}
		Vector3 hitPosition;
		if (positionOverride.HasValue)
		{
			hitPosition = positionOverride.Value;
		}
		else
		{
			GetKneeToToeTipRaycast(out hitPosition, out var _, out var _);
		}
		if (useEffect)
		{
			EffectData effectData = new EffectData();
			effectData.origin = hitPosition;
			effectData.scale = footstepBlastRadius;
			effectData.rotation = Quaternion.identity;
			EffectManager.SpawnEffect(footstepBlastEffectPrefab, effectData, transmit: true);
		}
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = mainBodyGameObject;
		blastAttack.teamIndex = (mainBody ? mainBody.teamComponent.teamIndex : TeamIndex.None);
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.inflictor = mainBodyGameObject;
		blastAttack.radius = footstepBlastRadius;
		blastAttack.position = hitPosition;
		blastAttack.losType = BlastAttack.LoSType.None;
		blastAttack.procCoefficient = 0f;
		blastAttack.procChainMask = default(ProcChainMask);
		blastAttack.baseDamage = 0f;
		blastAttack.baseForce = footstepBlastForce;
		blastAttack.bonusForce = footstepBonusForce;
		blastAttack.canRejectForce = false;
		blastAttack.crit = false;
		blastAttack.damageColorIndex = DamageColorIndex.Default;
		blastAttack.damageType = DamageType.Silent;
		blastAttack.impactEffect = EffectIndex.Invalid;
		blastAttack.falloffModel = footstepFalloffModel;
		blastAttack.Fire();
	}

	public GameObject CheckForStompTarget()
	{
		Vector3 a2 = stompRangeLeftMarker.position;
		Vector3 b2 = stompRangeRightMarker.position;
		Vector3 c2 = stompRangeNearMarker.position;
		Vector3 d2 = stompRangeFarMarker.position;
		Vector3 vector = Average(in a2, in b2, in c2, in d2);
		float a3 = Vector3.Distance(vector, a2);
		float b3 = Vector3.Distance(vector, b2);
		float c3 = Vector3.Distance(vector, c2);
		float d3 = Vector3.Distance(vector, d2);
		float num = Min(a3, b3, c3, d3);
		float num2 = Mathf.Sqrt(2f);
		float num3 = 1f / num2;
		float maxDistanceFilter = num * num3;
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = vector;
		bullseyeSearch.minDistanceFilter = 0f;
		bullseyeSearch.maxDistanceFilter = maxDistanceFilter;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.viewer = mainBody;
		bullseyeSearch.teamMaskFilter = TeamMask.AllExcept(mainBody.teamComponent.teamIndex);
		bullseyeSearch.filterByDistinctEntity = true;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.RefreshCandidates();
		return bullseyeSearch.GetResults().FirstOrDefault()?.healthComponent?.gameObject;
		static Vector3 Average(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d)
		{
			return Vector3Utils.Average(Vector3Utils.Average(in a, in b), Vector3Utils.Average(in c, in d));
		}
		static float Min(float a, float b, float c, float d)
		{
			return Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
		}
	}
}
