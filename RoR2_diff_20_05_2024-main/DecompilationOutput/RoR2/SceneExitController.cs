using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SceneExitController : MonoBehaviour
{
	public enum ExitState
	{
		Idle,
		ExtractExp,
		TeleportOut,
		Finished
	}

	public bool useRunNextStageScene;

	public SceneDef destinationScene;

	private const float teleportOutDuration = 4f;

	private float teleportOutTimer;

	public bool isAlternatePath;

	public SceneDef tier1AlternateDestinationScene;

	public SceneDef tier2AlternateDestinationScene;

	public SceneDef tier3AlternateDestinationScene;

	public SceneDef tier4AlternateDestinationScene;

	public SceneDef tier5AlternateDestinationScene;

	public bool isColossusPortal;

	public bool portalUsedAfterTeleporterEventFinish = true;

	private ExitState exitState;

	private ConvertPlayerMoneyToExperience experienceCollector;

	public static bool isRunning { get; private set; }

	public static event Action<SceneExitController> onBeginExit;

	public static event Action<SceneExitController> onFinishExit;

	public void Begin()
	{
		if (NetworkServer.active && exitState == ExitState.Idle)
		{
			SetState((!Run.instance.ruleBook.keepMoneyBetweenStages) ? ExitState.ExtractExp : ExitState.TeleportOut);
		}
	}

	public void SetState(ExitState newState)
	{
		if (newState == exitState)
		{
			return;
		}
		exitState = newState;
		switch (exitState)
		{
		case ExitState.ExtractExp:
			if (!isRunning)
			{
				if ((bool)TeleporterInteraction.instance)
				{
					portalUsedAfterTeleporterEventFinish = TeleporterInteraction.instance.activationState >= TeleporterInteraction.ActivationState.Charged;
				}
				SceneExitController.onBeginExit?.Invoke(this);
			}
			isRunning = true;
			experienceCollector = base.gameObject.AddComponent<ConvertPlayerMoneyToExperience>();
			if (isColossusPortal)
			{
				Util.PlaySound("Play_obj_portalColossus_activate", base.gameObject);
			}
			break;
		case ExitState.TeleportOut:
		{
			ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
			for (int i = 0; i < readOnlyInstancesList.Count; i++)
			{
				CharacterMaster component = readOnlyInstancesList[i].GetComponent<CharacterMaster>();
				if ((bool)component.GetComponent<SetDontDestroyOnLoad>())
				{
					GameObject bodyObject = component.GetBodyObject();
					if ((bool)bodyObject)
					{
						GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TeleportOutController"), bodyObject.transform.position, Quaternion.identity);
						obj.GetComponent<TeleportOutController>().Networktarget = bodyObject;
						NetworkServer.Spawn(obj);
					}
				}
			}
			teleportOutTimer = 4f;
			break;
		}
		case ExitState.Finished:
			SceneExitController.onFinishExit?.Invoke(this);
			if (!Run.instance || !Run.instance.isGameOverServer)
			{
				if (useRunNextStageScene)
				{
					Stage.instance.BeginAdvanceStage(Run.instance.nextStageScene);
				}
				else if (isAlternatePath && isAlternateSceneAvailable())
				{
					destinationScene = IfLoopedUseValidLoopStage(destinationScene);
					Stage.instance.BeginAdvanceStage(destinationScene);
				}
				else if ((bool)destinationScene)
				{
					Stage.instance.BeginAdvanceStage(destinationScene);
				}
			}
			break;
		case ExitState.Idle:
			break;
		}
	}

	private bool isAlternateSceneAvailable()
	{
		int stageOrder = Stage.instance.sceneDef.stageOrder;
		int num = Run.instance.nextStageScene.stageOrder;
		if (num != stageOrder + 1 && stageOrder <= 5)
		{
			num = stageOrder + 1;
		}
		if (num == 1 && tier1AlternateDestinationScene != null)
		{
			destinationScene = tier1AlternateDestinationScene;
			return true;
		}
		if (num == 2 && tier2AlternateDestinationScene != null)
		{
			destinationScene = tier2AlternateDestinationScene;
			return true;
		}
		if (num == 3 && tier3AlternateDestinationScene != null)
		{
			destinationScene = tier3AlternateDestinationScene;
			return true;
		}
		if (num == 4 && tier4AlternateDestinationScene != null)
		{
			destinationScene = tier4AlternateDestinationScene;
			return true;
		}
		if (num == 5 && tier5AlternateDestinationScene != null)
		{
			destinationScene = tier5AlternateDestinationScene;
			return true;
		}
		return false;
	}

	private SceneDef IfLoopedUseValidLoopStage(SceneDef sceneDef)
	{
		if (Run.instance.loopClearCount > 0 && Run.instance.stageClearCount > 5 && (bool)sceneDef.loopedSceneDef)
		{
			return sceneDef.loopedSceneDef;
		}
		return sceneDef;
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			UpdateServer();
		}
	}

	private void UpdateServer()
	{
		switch (exitState)
		{
		case ExitState.ExtractExp:
			if (!experienceCollector)
			{
				SetState(ExitState.TeleportOut);
			}
			break;
		case ExitState.TeleportOut:
			teleportOutTimer -= Time.fixedDeltaTime;
			if (teleportOutTimer <= 0f)
			{
				SetState(ExitState.Finished);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case ExitState.Idle:
		case ExitState.Finished:
			break;
		}
	}

	private void OnDestroy()
	{
		isRunning = false;
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}
}
