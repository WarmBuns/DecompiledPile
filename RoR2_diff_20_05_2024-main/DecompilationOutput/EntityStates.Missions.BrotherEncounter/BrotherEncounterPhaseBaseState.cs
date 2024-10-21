using RoR2;
using RoR2.CharacterSpeech;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.BrotherEncounter;

public abstract class BrotherEncounterPhaseBaseState : BrotherEncounterBaseState
{
	[SerializeField]
	public float durationBeforeEnablingCombatEncounter;

	[SerializeField]
	public GameObject speechControllerPrefab;

	protected ScriptedCombatEncounter phaseScriptedCombatEncounter;

	protected GameObject phaseControllerObject;

	protected GameObject phaseControllerSubObjectContainer;

	protected BossGroup phaseBossGroup;

	private bool hasSpawned;

	private bool finishedServer;

	private const float minimumDurationPerPhase = 2f;

	private Run.FixedTimeStamp healthBarShowTime = Run.FixedTimeStamp.positiveInfinity;

	protected abstract EntityState nextState { get; }

	protected abstract string phaseControllerChildString { get; }

	protected virtual float healthBarShowDelay => 0f;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)PhaseCounter.instance)
		{
			PhaseCounter.instance.GoToNextPhase();
		}
		if ((bool)childLocator)
		{
			phaseControllerObject = childLocator.FindChild(phaseControllerChildString).gameObject;
			if ((bool)phaseControllerObject)
			{
				phaseScriptedCombatEncounter = phaseControllerObject.GetComponent<ScriptedCombatEncounter>();
				phaseBossGroup = phaseControllerObject.GetComponent<BossGroup>();
				phaseControllerSubObjectContainer = phaseControllerObject.transform.Find("PhaseObjects").gameObject;
				phaseControllerSubObjectContainer.SetActive(value: true);
			}
			GameObject gameObject = childLocator.FindChild("AllPhases").gameObject;
			if ((bool)gameObject)
			{
				gameObject.SetActive(value: true);
			}
		}
		healthBarShowTime = Run.FixedTimeStamp.now + healthBarShowDelay;
		if ((bool)DirectorCore.instance)
		{
			CombatDirector[] components = DirectorCore.instance.GetComponents<CombatDirector>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].enabled = false;
			}
		}
		if (NetworkServer.active && (object)phaseScriptedCombatEncounter != null)
		{
			phaseScriptedCombatEncounter.combatSquad.onMemberAddedServer += OnMemberAddedServer;
		}
	}

	public override void OnExit()
	{
		if ((object)phaseScriptedCombatEncounter != null)
		{
			phaseScriptedCombatEncounter.combatSquad.onMemberAddedServer -= OnMemberAddedServer;
		}
		if ((bool)phaseControllerSubObjectContainer)
		{
			phaseControllerSubObjectContainer.SetActive(value: false);
		}
		if ((bool)phaseBossGroup)
		{
			phaseBossGroup.shouldDisplayHealthBarOnHud = false;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		phaseBossGroup.shouldDisplayHealthBarOnHud = healthBarShowTime.hasPassed;
		if (!hasSpawned)
		{
			if (base.fixedAge > durationBeforeEnablingCombatEncounter)
			{
				BeginEncounter();
			}
		}
		else if (NetworkServer.active && !finishedServer && base.fixedAge > 2f + durationBeforeEnablingCombatEncounter && (bool)phaseScriptedCombatEncounter && phaseScriptedCombatEncounter.combatSquad.memberCount == 0)
		{
			finishedServer = true;
			outer.SetNextState(nextState);
		}
	}

	protected void BeginEncounter()
	{
		hasSpawned = true;
		PreEncounterBegin();
		if (NetworkServer.active)
		{
			phaseScriptedCombatEncounter.BeginEncounter();
		}
	}

	protected virtual void PreEncounterBegin()
	{
	}

	protected virtual void OnMemberAddedServer(CharacterMaster master)
	{
		if ((bool)speechControllerPrefab)
		{
			Object.Instantiate(speechControllerPrefab, master.transform).GetComponent<CharacterSpeechController>().characterMaster = master;
		}
	}
}
