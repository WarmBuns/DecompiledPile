using System;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GauntletMissionController : NetworkBehaviour
{
	public class GauntletMissionBaseState : EntityState
	{
		protected GauntletMissionController gauntletMissionController => instance;
	}

	public class MissionCompleted : GauntletMissionBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			base.gauntletMissionController.clearedEffect.SetActive(value: true);
		}
	}

	[Header("Behavior Values")]
	public float dleayBeforeStart;

	public float timeLimit;

	public float degenTickFrequency;

	public float percentDegenPerSecond;

	[Header("Cached Components")]
	private EntityStateMachine mainStateMachine;

	public GameObject[] gauntletShards;

	public GameObject clearedEffect;

	public GameObject gauntletPortal;

	private bool gauntletEnd;

	private bool slowDeathEffectActive;

	private float degenTimer;

	public static GauntletMissionController instance { get; private set; }

	public static event Action onInstanceChangedGlobal;

	private void Awake()
	{
		mainStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Main");
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		GauntletMissionController.onInstanceChangedGlobal?.Invoke();
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		GauntletMissionController.onInstanceChangedGlobal?.Invoke();
	}

	public void GauntletMissionTimesUp()
	{
		gauntletEnd = true;
		mainStateMachine.SetNextState(new MissionCompleted());
	}

	private void OnPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
	{
		generationMethod = GenerateGauntletShards;
	}

	private void GenerateGauntletShards()
	{
		_ = gauntletShards.LongLength;
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	[Server]
	private void FixedUpdateServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GauntletMissionController::FixedUpdateServer()' called on client");
		}
		else
		{
			if (!gauntletEnd)
			{
				return;
			}
			degenTimer += Time.fixedDeltaTime;
			if (!(degenTimer > 1f / degenTickFrequency))
			{
				return;
			}
			degenTimer -= 1f / degenTickFrequency;
			foreach (TeamComponent teamMember in TeamComponent.GetTeamMembers(TeamIndex.Player))
			{
				float damage = percentDegenPerSecond / 100f / degenTickFrequency * teamMember.body.healthComponent.combinedHealth;
				teamMember.body.healthComponent.TakeDamage(new DamageInfo
				{
					damage = damage,
					position = teamMember.body.corePosition,
					damageType = DamageType.Silent
				});
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
