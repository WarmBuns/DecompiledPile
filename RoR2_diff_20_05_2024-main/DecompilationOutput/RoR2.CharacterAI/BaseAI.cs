using System;
using System.Linq;
using EntityStates;
using EntityStates.AI;
using HG;
using JetBrains.Annotations;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.CharacterAI;

[RequireComponent(typeof(CharacterMaster))]
public class BaseAI : MonoBehaviour, IManagedMonoBehaviour
{
	[Serializable]
	public class Target
	{
		private readonly BaseAI owner;

		private bool unset = true;

		private GameObject _gameObject;

		private float losCheckTimer;

		public HurtBox bestHurtBox;

		private HurtBoxGroup hurtBoxGroup;

		private HurtBox mainHurtBox;

		private int bullseyeCount;

		private Vector3? lastKnownBullseyePosition;

		private Run.FixedTimeStamp lastKnownBullseyePositionTime = Run.FixedTimeStamp.negativeInfinity;

		public bool hasLoS;

		public GameObject gameObject
		{
			get
			{
				return _gameObject;
			}
			set
			{
				if ((object)value != _gameObject)
				{
					_gameObject = value;
					characterBody = gameObject?.GetComponent<CharacterBody>();
					healthComponent = characterBody?.healthComponent;
					hurtBoxGroup = characterBody?.hurtBoxGroup;
					bullseyeCount = (hurtBoxGroup ? hurtBoxGroup.bullseyeCount : 0);
					mainHurtBox = (hurtBoxGroup ? hurtBoxGroup.mainHurtBox : null);
					bestHurtBox = mainHurtBox;
					hasLoS = false;
					unset = !_gameObject;
				}
			}
		}

		public CharacterBody characterBody { get; private set; }

		public HealthComponent healthComponent { get; private set; }

		public Target([NotNull] BaseAI owner)
		{
			this.owner = owner;
		}

		public void Update()
		{
			if ((bool)gameObject)
			{
				hasLoS = (bool)bestHurtBox && owner.HasLOS(bestHurtBox.transform.position);
				if (bullseyeCount > 1 && !hasLoS)
				{
					bestHurtBox = GetBestHurtBox(out hasLoS);
				}
			}
		}

		public bool TestLOSNow()
		{
			if ((bool)bestHurtBox)
			{
				return owner.HasLOS(bestHurtBox.transform.position);
			}
			return false;
		}

		public bool GetBullseyePosition(out Vector3 position)
		{
			if ((bool)characterBody && (bool)owner.body && (characterBody.GetVisibilityLevel(owner.body) >= VisibilityLevel.Revealed || lastKnownBullseyePositionTime.timeSince >= 2f))
			{
				if ((bool)bestHurtBox)
				{
					lastKnownBullseyePosition = bestHurtBox.transform.position;
				}
				else
				{
					lastKnownBullseyePosition = null;
				}
				lastKnownBullseyePositionTime = Run.FixedTimeStamp.now;
			}
			if (lastKnownBullseyePosition.HasValue)
			{
				position = lastKnownBullseyePosition.Value;
				return true;
			}
			position = (gameObject ? gameObject.transform.position : Vector3.zero);
			return false;
		}

		private HurtBox GetBestHurtBox(out bool hadLoS)
		{
			if ((bool)owner && (bool)owner.bodyInputBank && (bool)hurtBoxGroup)
			{
				Vector3 aimOrigin = owner.bodyInputBank.aimOrigin;
				HurtBox hurtBox = null;
				float num = float.PositiveInfinity;
				HurtBox[] hurtBoxes = hurtBoxGroup.hurtBoxes;
				foreach (HurtBox hurtBox2 in hurtBoxes)
				{
					if (!hurtBox2 || !hurtBox2.transform || !hurtBox2.isBullseye)
					{
						continue;
					}
					Vector3 position = hurtBox2.transform.position;
					if (CheckLoS(aimOrigin, hurtBox2.transform.position))
					{
						float sqrMagnitude = (position - aimOrigin).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							hurtBox = hurtBox2;
						}
					}
				}
				if ((bool)hurtBox)
				{
					hadLoS = true;
					return hurtBox;
				}
			}
			hadLoS = false;
			return mainHurtBox;
		}

		public void Reset()
		{
			if (!unset)
			{
				_gameObject = null;
				characterBody = null;
				healthComponent = null;
				hurtBoxGroup = null;
				bullseyeCount = 0;
				mainHurtBox = null;
				bestHurtBox = mainHurtBox;
				hasLoS = false;
				unset = true;
				losCheckTimer = 0f;
			}
		}
	}

	public struct SkillDriverEvaluation
	{
		public AISkillDriver dominantSkillDriver;

		public Target target;

		public Target aimTarget;

		public float separationSqrMagnitude;
	}

	public struct BodyInputs
	{
		public Vector3 desiredAimDirection;

		public Vector3 moveVector;

		public bool pressSprint;

		public bool pressJump;

		public bool pressSkill1;

		public bool pressSkill2;

		public bool pressSkill3;

		public bool pressSkill4;

		public bool pressActivateEquipment;
	}

	public bool showDebugStateChanges;

	[Tooltip("If true, this character can spot enemies behind itself.")]
	public bool fullVision;

	[Tooltip("If true, this AI will not be allowed to retaliate against damage received from a source on its own team.")]
	public bool neverRetaliateFriendlies;

	public float enemyAttentionDuration = 5f;

	public MapNodeGroup.GraphType desiredSpawnNodeGraphType;

	[Tooltip("The state machine to run while the body exists.")]
	public EntityStateMachine stateMachine;

	public SerializableEntityStateType scanState;

	public bool isHealer;

	public float enemyAttention;

	public float aimVectorDampTime = 0.2f;

	public float aimVectorMaxSpeed = 6f;

	private float aimVelocity;

	private float targetRefreshTimer;

	private const float targetRefreshInterval = 0.5f;

	public LocalNavigator localNavigator = new LocalNavigator();

	public string selectedSkilldriverName;

	private const float maxVisionDistance = float.PositiveInfinity;

	[Tooltip("If true and this AI remains off-screen from players until its first shot, that shot will miss. The desired skill must handle this behaviour itself. If not this will do nothing")]
	public bool shouldMissFirstOffScreenShot;

	private float offScreenUpdateTimer;

	private const float offScreenUpdateInterval = 0.25f;

	public static readonly NodeGraphNavigationSystem nodeGraphNavigationSystem = new NodeGraphNavigationSystem();

	private BroadNavigationSystem _broadNavigationSystem;

	private BroadNavigationSystem.Agent _broadNavigationAgent = BroadNavigationSystem.Agent.invalid;

	public bool forceUpdateEveryFrame;

	public HurtBox debugEnemyHurtBox;

	private BullseyeSearch enemySearch = new BullseyeSearch();

	private BullseyeSearch buddySearch = new BullseyeSearch();

	private float skillDriverUpdateTimer;

	private const float skillDriverMinUpdateInterval = 1f / 6f;

	private const float skillDriverMaxUpdateInterval = 0.2f;

	public SkillDriverEvaluation skillDriverEvaluation;

	protected BodyInputs bodyInputs;

	public CharacterMaster master { get; protected set; }

	public CharacterBody body { get; protected set; }

	public Transform bodyTransform { get; protected set; }

	public CharacterDirection bodyCharacterDirection { get; protected set; }

	public CharacterMotor bodyCharacterMotor { get; protected set; }

	public InputBankTest bodyInputBank { get; protected set; }

	public HealthComponent bodyHealthComponent { get; protected set; }

	public SkillLocator bodySkillLocator { get; protected set; }

	public NetworkIdentity networkIdentity { get; protected set; }

	public AISkillDriver[] skillDrivers { get; protected set; }

	public bool hasAimConfirmation { get; private set; }

	public bool hasAimTarget
	{
		get
		{
			if (skillDriverEvaluation.aimTarget != null)
			{
				return skillDriverEvaluation.aimTarget.gameObject;
			}
			return false;
		}
	}

	private BroadNavigationSystem broadNavigationSystem
	{
		get
		{
			return _broadNavigationSystem;
		}
		set
		{
			if (_broadNavigationSystem != value)
			{
				_broadNavigationSystem?.ReturnAgent(ref _broadNavigationAgent);
				_broadNavigationSystem = value;
				_broadNavigationSystem?.RequestAgent(ref _broadNavigationAgent);
			}
		}
	}

	public BroadNavigationSystem.Agent broadNavigationAgent => _broadNavigationAgent;

	public bool AlwaysUpdate => forceUpdateEveryFrame;

	public Target currentEnemy { get; private set; }

	public Target leader { get; private set; }

	private Target buddy { get; set; }

	public Target customTarget { get; private set; }

	public event Action<CharacterBody> onBodyDiscovered;

	public event Action<CharacterBody> onBodyLost;

	[ContextMenu("Toggle broad navigation debug drawing")]
	private void ToggleBroadNavigationDebugDraw()
	{
		if (broadNavigationSystem is NodeGraphNavigationSystem)
		{
			NodeGraphNavigationSystem.Agent agent = (NodeGraphNavigationSystem.Agent)broadNavigationAgent;
			agent.drawPath = !agent.drawPath;
		}
	}

	public void SetBroadNavigationDebugDrawEnabled(bool newBroadNavigationDebugDrawEnabled)
	{
		if (broadNavigationSystem is NodeGraphNavigationSystem)
		{
			NodeGraphNavigationSystem.Agent agent = (NodeGraphNavigationSystem.Agent)broadNavigationAgent;
			agent.drawPath = newBroadNavigationDebugDrawEnabled;
		}
	}

	protected void Awake()
	{
		targetRefreshTimer = 0.5f;
		master = GetComponent<CharacterMaster>();
		stateMachine = GetComponent<EntityStateMachine>();
		stateMachine.enabled = false;
		networkIdentity = GetComponent<NetworkIdentity>();
		skillDrivers = GetComponents<AISkillDriver>();
		currentEnemy = new Target(this);
		leader = new Target(this);
		buddy = new Target(this);
		customTarget = new Target(this);
		broadNavigationSystem = nodeGraphNavigationSystem;
	}

	protected void OnDestroy()
	{
		broadNavigationSystem = null;
	}

	protected void Start()
	{
		if (!Util.HasEffectiveAuthority(networkIdentity))
		{
			base.enabled = false;
			if ((bool)stateMachine)
			{
				stateMachine.enabled = false;
			}
		}
		if (NetworkServer.active)
		{
			skillDriverUpdateTimer = UnityEngine.Random.value;
		}
	}

	protected void FixedUpdate()
	{
		ManagedFixedUpdate(Time.fixedDeltaTime);
	}

	public void ManagedFixedUpdate(float deltaTime)
	{
		enemyAttention -= deltaTime;
		targetRefreshTimer -= deltaTime;
		skillDriverUpdateTimer -= deltaTime;
		if ((bool)body)
		{
			if (shouldMissFirstOffScreenShot)
			{
				offScreenUpdateTimer -= deltaTime;
				if (offScreenUpdateTimer <= 0f)
				{
					offScreenUpdateTimer = 0.25f;
					shouldMissFirstOffScreenShot = OffScreenMissHelper.IsPositionOffScreen(body.transform.position);
				}
			}
			broadNavigationAgent.ConfigureFromBody(body);
			if (skillDriverUpdateTimer <= 0f)
			{
				if ((bool)skillDriverEvaluation.dominantSkillDriver && skillDriverEvaluation.dominantSkillDriver.resetCurrentEnemyOnNextDriverSelection)
				{
					currentEnemy.Reset();
					targetRefreshTimer = 0f;
				}
				if (!currentEnemy.gameObject && targetRefreshTimer <= 0f)
				{
					targetRefreshTimer = 0.5f;
					HurtBox hurtBox = null;
					hurtBox = FindEnemyHurtBox(float.PositiveInfinity, fullVision, filterByLoS: true);
					if ((bool)hurtBox && (bool)hurtBox.healthComponent)
					{
						currentEnemy.gameObject = hurtBox.healthComponent.gameObject;
						currentEnemy.bestHurtBox = hurtBox;
					}
					if ((bool)currentEnemy.gameObject)
					{
						enemyAttention = enemyAttentionDuration;
					}
				}
				BeginSkillDriver(EvaluateSkillDrivers());
			}
		}
		_broadNavigationAgent.currentPosition = GetNavigationStartPos();
		UpdateBodyInputs();
		UpdateBodyAim(deltaTime);
		debugEnemyHurtBox = currentEnemy.bestHurtBox;
	}

	private void BeginSkillDriver(SkillDriverEvaluation newSkillDriverEvaluation)
	{
		if ((bool)skillDriverEvaluation.dominantSkillDriver)
		{
			_ = skillDriverEvaluation.dominantSkillDriver.customName;
		}
		skillDriverEvaluation = newSkillDriverEvaluation;
		skillDriverUpdateTimer = UnityEngine.Random.Range(1f / 6f, 0.2f);
		if ((bool)skillDriverEvaluation.dominantSkillDriver)
		{
			_ = showDebugStateChanges;
			selectedSkilldriverName = skillDriverEvaluation.dominantSkillDriver.customName;
			if (skillDriverEvaluation.dominantSkillDriver.driverUpdateTimerOverride >= 0f)
			{
				skillDriverUpdateTimer = skillDriverEvaluation.dominantSkillDriver.driverUpdateTimerOverride;
			}
			skillDriverEvaluation.dominantSkillDriver.OnSelected();
		}
		else
		{
			_ = showDebugStateChanges;
			selectedSkilldriverName = "";
		}
	}

	public virtual void OnBodyStart(CharacterBody newBody)
	{
		body = newBody;
		bodyTransform = newBody.transform;
		bodyCharacterDirection = newBody.GetComponent<CharacterDirection>();
		bodyCharacterMotor = newBody.GetComponent<CharacterMotor>();
		bodyInputBank = newBody.GetComponent<InputBankTest>();
		bodyHealthComponent = newBody.GetComponent<HealthComponent>();
		bodySkillLocator = newBody.GetComponent<SkillLocator>();
		localNavigator.SetBody(newBody);
		_broadNavigationAgent.enabled = true;
		if ((bool)stateMachine && Util.HasEffectiveAuthority(networkIdentity))
		{
			stateMachine.enabled = true;
			stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref scanState));
		}
		base.enabled = true;
		if ((bool)bodyCharacterDirection)
		{
			bodyInputs.desiredAimDirection = bodyCharacterDirection.forward;
		}
		else
		{
			bodyInputs.desiredAimDirection = bodyTransform.forward;
		}
		if ((bool)bodyInputBank)
		{
			bodyInputBank.aimDirection = bodyInputs.desiredAimDirection;
		}
		this.onBodyDiscovered?.Invoke(newBody);
	}

	public virtual void OnBodyDeath(CharacterBody characterBody)
	{
		OnBodyLost(characterBody);
	}

	public virtual void OnBodyDestroyed(CharacterBody characterBody)
	{
		OnBodyLost(characterBody);
	}

	public virtual void OnBodyLost(CharacterBody characterBody)
	{
		if ((object)body != null)
		{
			base.enabled = false;
			body = null;
			bodyTransform = null;
			bodyCharacterDirection = null;
			bodyCharacterMotor = null;
			bodyInputBank = null;
			bodyHealthComponent = null;
			bodySkillLocator = null;
			localNavigator.SetBody(null);
			_broadNavigationAgent.enabled = false;
			if ((bool)stateMachine)
			{
				stateMachine.enabled = false;
				stateMachine.SetNextState(new Idle());
			}
			this.onBodyLost?.Invoke(characterBody);
		}
	}

	public virtual void OnBodyDamaged(DamageReport damageReport)
	{
		DamageInfo damageInfo = damageReport.damageInfo;
		if ((bool)damageInfo.attacker && (bool)body && (!currentEnemy.gameObject || enemyAttention <= 0f) && damageInfo.attacker != body.gameObject && (!neverRetaliateFriendlies || !damageReport.isFriendlyFire))
		{
			currentEnemy.gameObject = damageInfo.attacker;
			enemyAttention = enemyAttentionDuration;
		}
	}

	[Obsolete]
	public virtual bool HasLOS(Vector3 start, Vector3 end)
	{
		if (!body)
		{
			return false;
		}
		Vector3 direction = end - start;
		float magnitude = direction.magnitude;
		if (body.visionDistance < magnitude)
		{
			return false;
		}
		Ray ray = default(Ray);
		ray.origin = start;
		ray.direction = direction;
		RaycastHit hitInfo;
		return !Physics.Raycast(ray, out hitInfo, magnitude, LayerIndex.world.mask);
	}

	public virtual bool HasLOS(Vector3 end)
	{
		if (!body || !bodyInputBank)
		{
			return false;
		}
		Vector3 aimOrigin = bodyInputBank.aimOrigin;
		Vector3 direction = end - aimOrigin;
		float magnitude = direction.magnitude;
		if (body.visionDistance < magnitude)
		{
			return false;
		}
		Ray ray = default(Ray);
		ray.origin = aimOrigin;
		ray.direction = direction;
		RaycastHit hitInfo;
		return !Physics.Raycast(ray, out hitInfo, magnitude, LayerIndex.world.mask);
	}

	private Vector3? GetNavigationStartPos()
	{
		if (!body)
		{
			return null;
		}
		return body.footPosition;
	}

	public NodeGraph GetDesiredSpawnNodeGraph()
	{
		return SceneInfo.instance.GetNodeGraph(desiredSpawnNodeGraphType);
	}

	public void SetGoalPosition(Vector3? goalPos)
	{
		BroadNavigationSystem.Agent agent = broadNavigationAgent;
		agent.goalPosition = goalPos;
	}

	public void SetGoalPosition(Target goalTarget)
	{
		Vector3? goalPosition = null;
		if (goalTarget.GetBullseyePosition(out var position))
		{
			Vector3 value = position;
			goalPosition = value;
		}
		SetGoalPosition(goalPosition);
	}

	public void DebugDrawPath(Color color, float duration)
	{
		if (broadNavigationSystem is NodeGraphNavigationSystem)
		{
			((NodeGraphNavigationSystem.Agent)broadNavigationAgent).DebugDrawPath(color, duration);
		}
	}

	private static bool CheckLoS(Vector3 start, Vector3 end)
	{
		Vector3 direction = end - start;
		RaycastHit hitInfo;
		return !Physics.Raycast(start, direction, out hitInfo, direction.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
	}

	public HurtBox GetBestHurtBox(GameObject target)
	{
		HurtBoxGroup hurtBoxGroup = target.GetComponent<CharacterBody>()?.hurtBoxGroup;
		if ((bool)hurtBoxGroup && hurtBoxGroup.bullseyeCount > 1 && (bool)bodyInputBank)
		{
			Vector3 aimOrigin = bodyInputBank.aimOrigin;
			HurtBox hurtBox = null;
			float num = float.PositiveInfinity;
			HurtBox[] hurtBoxes = hurtBoxGroup.hurtBoxes;
			foreach (HurtBox hurtBox2 in hurtBoxes)
			{
				if (!hurtBox2.isBullseye)
				{
					continue;
				}
				Vector3 position = hurtBox2.transform.position;
				if (CheckLoS(aimOrigin, hurtBox2.transform.position))
				{
					float sqrMagnitude = (position - aimOrigin).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						hurtBox = hurtBox2;
					}
				}
			}
			if ((bool)hurtBox)
			{
				return hurtBox;
			}
		}
		return Util.FindBodyMainHurtBox(target);
	}

	public void ForceAcquireNearestEnemyIfNoCurrentEnemy()
	{
		if ((bool)currentEnemy.gameObject)
		{
			return;
		}
		if (!body)
		{
			Debug.LogErrorFormat("BaseAI.ForceAcquireNearestEnemyIfNoCurrentEnemy for CharacterMaster '{0}' failed: AI has no body to search from.", base.gameObject.name);
			return;
		}
		HurtBox hurtBox = FindEnemyHurtBox(float.PositiveInfinity, full360Vision: true, filterByLoS: false);
		if ((bool)hurtBox && (bool)hurtBox.healthComponent)
		{
			currentEnemy.gameObject = hurtBox.healthComponent.gameObject;
			currentEnemy.bestHurtBox = hurtBox;
		}
	}

	private HurtBox FindEnemyHurtBox(float maxDistance, bool full360Vision, bool filterByLoS)
	{
		if (!body)
		{
			return null;
		}
		enemySearch.viewer = body;
		enemySearch.teamMaskFilter = TeamMask.allButNeutral;
		enemySearch.teamMaskFilter.RemoveTeam(master.teamIndex);
		enemySearch.sortMode = BullseyeSearch.SortMode.Distance;
		enemySearch.minDistanceFilter = 0f;
		enemySearch.maxDistanceFilter = maxDistance;
		enemySearch.searchOrigin = bodyInputBank.aimOrigin;
		enemySearch.searchDirection = bodyInputBank.aimDirection;
		enemySearch.maxAngleFilter = (full360Vision ? 180f : 90f);
		enemySearch.filterByLoS = filterByLoS;
		enemySearch.RefreshCandidates();
		return enemySearch.GetResults().FirstOrDefault();
	}

	public bool GameObjectPassesSkillDriverFilters(Target target, AISkillDriver skillDriver, out float separationSqrMagnitude)
	{
		separationSqrMagnitude = 0f;
		if (!target.gameObject)
		{
			return false;
		}
		float num = 1f;
		if ((bool)target.healthComponent)
		{
			num = target.healthComponent.combinedHealthFraction;
		}
		if (num < skillDriver.minTargetHealthFraction || num > skillDriver.maxTargetHealthFraction)
		{
			return false;
		}
		float num2 = 0f;
		if ((bool)body)
		{
			num2 = body.radius;
		}
		float num3 = 0f;
		if ((bool)target.characterBody)
		{
			num3 = target.characterBody.radius;
		}
		Vector3 vector = (bodyInputBank ? bodyInputBank.aimOrigin : bodyTransform.position);
		target.GetBullseyePosition(out var position);
		float sqrMagnitude = (position - vector).sqrMagnitude;
		separationSqrMagnitude = sqrMagnitude - num3 * num3 - num2 * num2;
		if (separationSqrMagnitude < skillDriver.minDistanceSqr || separationSqrMagnitude > skillDriver.maxDistanceSqr)
		{
			return false;
		}
		if (skillDriver.selectionRequiresTargetLoS && !target.hasLoS)
		{
			return false;
		}
		return true;
	}

	private void UpdateTargets()
	{
		currentEnemy.Update();
		leader.Update();
	}

	protected SkillDriverEvaluation? EvaluateSingleSkillDriver(in SkillDriverEvaluation currentSkillDriverEvaluation, AISkillDriver aiSkillDriver, float myHealthFraction)
	{
		if (!body || !bodySkillLocator)
		{
			return null;
		}
		float separationSqrMagnitude = float.PositiveInfinity;
		if (aiSkillDriver.noRepeat && currentSkillDriverEvaluation.dominantSkillDriver == aiSkillDriver)
		{
			return null;
		}
		if (aiSkillDriver.maxTimesSelected >= 0 && aiSkillDriver.timesSelected >= aiSkillDriver.maxTimesSelected)
		{
			return null;
		}
		Target target = null;
		if (aiSkillDriver.requireEquipmentReady && body.equipmentSlot.stock <= 0)
		{
			return null;
		}
		if (aiSkillDriver.skillSlot != SkillSlot.None)
		{
			GenericSkill skill = bodySkillLocator.GetSkill(aiSkillDriver.skillSlot);
			if (aiSkillDriver.requireSkillReady && (!skill || !skill.IsReady()))
			{
				return null;
			}
			if ((bool)aiSkillDriver.requiredSkill && (!skill || !(skill.skillDef == aiSkillDriver.requiredSkill)))
			{
				return null;
			}
		}
		if (aiSkillDriver.minUserHealthFraction > myHealthFraction || aiSkillDriver.maxUserHealthFraction < myHealthFraction)
		{
			return null;
		}
		if ((bool)bodyCharacterMotor && !bodyCharacterMotor.isGrounded && aiSkillDriver.selectionRequiresOnGround)
		{
			return null;
		}
		switch (aiSkillDriver.moveTargetType)
		{
		case AISkillDriver.TargetType.CurrentEnemy:
			if (GameObjectPassesSkillDriverFilters(currentEnemy, aiSkillDriver, out separationSqrMagnitude))
			{
				target = currentEnemy;
			}
			break;
		case AISkillDriver.TargetType.NearestFriendlyInSkillRange:
			if ((bool)bodyInputBank)
			{
				buddySearch.teamMaskFilter = TeamMask.none;
				buddySearch.teamMaskFilter.AddTeam(master.teamIndex);
				buddySearch.sortMode = BullseyeSearch.SortMode.Distance;
				buddySearch.minDistanceFilter = aiSkillDriver.minDistanceSqr;
				buddySearch.maxDistanceFilter = aiSkillDriver.maxDistance;
				buddySearch.searchOrigin = bodyInputBank.aimOrigin;
				buddySearch.searchDirection = bodyInputBank.aimDirection;
				buddySearch.maxAngleFilter = 180f;
				buddySearch.filterByLoS = aiSkillDriver.activationRequiresTargetLoS;
				buddySearch.RefreshCandidates();
				if ((bool)body)
				{
					buddySearch.FilterOutGameObject(body.gameObject);
				}
				buddySearch.FilterCandidatesByHealthFraction(aiSkillDriver.minTargetHealthFraction, aiSkillDriver.maxTargetHealthFraction);
				HurtBox hurtBox = buddySearch.GetResults().FirstOrDefault();
				if ((bool)hurtBox && (bool)hurtBox.healthComponent)
				{
					buddy.gameObject = hurtBox.healthComponent.gameObject;
					buddy.bestHurtBox = hurtBox;
				}
				if (GameObjectPassesSkillDriverFilters(buddy, aiSkillDriver, out separationSqrMagnitude))
				{
					target = buddy;
				}
			}
			break;
		case AISkillDriver.TargetType.CurrentLeader:
			if (GameObjectPassesSkillDriverFilters(leader, aiSkillDriver, out separationSqrMagnitude))
			{
				target = leader;
			}
			break;
		case AISkillDriver.TargetType.Custom:
			if (GameObjectPassesSkillDriverFilters(customTarget, aiSkillDriver, out separationSqrMagnitude))
			{
				target = customTarget;
			}
			break;
		}
		if (target == null)
		{
			return null;
		}
		Target target2 = null;
		if (aiSkillDriver.aimType != 0)
		{
			bool flag = aiSkillDriver.selectionRequiresAimTarget;
			switch (aiSkillDriver.aimType)
			{
			case AISkillDriver.AimType.AtMoveTarget:
				target2 = target;
				break;
			case AISkillDriver.AimType.AtCurrentEnemy:
				target2 = currentEnemy;
				break;
			case AISkillDriver.AimType.AtCurrentLeader:
				target2 = leader;
				break;
			default:
				flag = false;
				break;
			}
			if (flag && (target2 == null || !target2.gameObject))
			{
				return null;
			}
		}
		SkillDriverEvaluation value = default(SkillDriverEvaluation);
		value.dominantSkillDriver = aiSkillDriver;
		value.target = target;
		value.aimTarget = target2;
		value.separationSqrMagnitude = separationSqrMagnitude;
		return value;
	}

	public SkillDriverEvaluation EvaluateSkillDrivers()
	{
		UpdateTargets();
		float myHealthFraction = 1f;
		if ((bool)bodyHealthComponent)
		{
			myHealthFraction = bodyHealthComponent.combinedHealthFraction;
		}
		if ((bool)bodySkillLocator)
		{
			if ((bool)this.skillDriverEvaluation.dominantSkillDriver && (bool)this.skillDriverEvaluation.dominantSkillDriver.nextHighPriorityOverride)
			{
				SkillDriverEvaluation? skillDriverEvaluation = EvaluateSingleSkillDriver(in this.skillDriverEvaluation, this.skillDriverEvaluation.dominantSkillDriver.nextHighPriorityOverride, myHealthFraction);
				if (skillDriverEvaluation.HasValue)
				{
					return skillDriverEvaluation.Value;
				}
			}
			for (int i = 0; i < skillDrivers.Length; i++)
			{
				AISkillDriver aISkillDriver = skillDrivers[i];
				if (aISkillDriver.enabled)
				{
					SkillDriverEvaluation? skillDriverEvaluation2 = EvaluateSingleSkillDriver(in this.skillDriverEvaluation, aISkillDriver, myHealthFraction);
					if (skillDriverEvaluation2.HasValue)
					{
						return skillDriverEvaluation2.Value;
					}
				}
			}
		}
		return default(SkillDriverEvaluation);
	}

	protected void UpdateBodyInputs()
	{
		if (stateMachine.state is BaseAIState baseAIState)
		{
			bodyInputs = baseAIState.GenerateBodyInputs(in bodyInputs);
		}
		if ((bool)bodyInputBank)
		{
			bodyInputBank.skill1.PushState(bodyInputs.pressSkill1);
			bodyInputBank.skill2.PushState(bodyInputs.pressSkill2);
			bodyInputBank.skill3.PushState(bodyInputs.pressSkill3);
			bodyInputBank.skill4.PushState(bodyInputs.pressSkill4);
			bodyInputBank.jump.PushState(bodyInputs.pressJump);
			bodyInputBank.sprint.PushState(bodyInputs.pressSprint);
			bodyInputBank.activateEquipment.PushState(bodyInputs.pressActivateEquipment);
			bodyInputBank.moveVector = bodyInputs.moveVector;
		}
	}

	protected void UpdateBodyAim(float deltaTime)
	{
		hasAimConfirmation = false;
		if ((bool)bodyInputBank)
		{
			Vector3 aimDirection = bodyInputBank.aimDirection;
			Vector3 desiredAimDirection = bodyInputs.desiredAimDirection;
			if (desiredAimDirection != Vector3.zero)
			{
				Quaternion target = Util.QuaternionSafeLookRotation(desiredAimDirection);
				Vector3 vector = Util.SmoothDampQuaternion(Util.QuaternionSafeLookRotation(aimDirection), maxSpeed: aimVectorMaxSpeed, target: target, currentVelocity: ref aimVelocity, smoothTime: aimVectorDampTime, deltaTime: deltaTime) * Vector3.forward;
				bodyInputBank.aimDirection = vector;
				hasAimConfirmation = Vector3.Dot(vector, desiredAimDirection) >= 0.95f;
			}
		}
	}

	[ConCommand(commandName = "ai_draw_path", flags = ConVarFlags.Cheat, helpText = "Enables or disables the drawing of the specified AI's broad navigation path. Format: ai_draw_path <CharacterMaster selector> <0/1>")]
	private static void CCAiDrawPath(ConCommandArgs args)
	{
		CharacterMaster argCharacterMasterInstance = args.GetArgCharacterMasterInstance(0);
		args.GetArgBool(1);
		if (!argCharacterMasterInstance)
		{
			throw new ConCommandException("Could not find target.");
		}
		BaseAI component = argCharacterMasterInstance.GetComponent<BaseAI>();
		if (!component)
		{
			throw new ConCommandException("Target has no AI.");
		}
		component.ToggleBroadNavigationDebugDraw();
	}
}
