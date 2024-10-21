using System;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SceneExitControllerColossusPortal : MonoBehaviour
{
	public enum ExitState
	{
		Idle,
		ExtractExp,
		TeleportOut,
		Finished
	}

	public bool useRunNextStageScene;

	private SceneDef destinationScene;

	public SceneDef t2DestinationScene;

	public SceneDef t3DestinationScene;

	public SceneDef t4DestinationScene;

	private const float teleportOutDuration = 4f;

	private float teleportOutTimer;

	private ExitState exitState;

	private ConvertPlayerMoneyToExperience experienceCollector;

	public static bool isRunning { get; private set; }

	public static event Action<SceneExitControllerColossusPortal> onBeginExit;

	public static event Action<SceneExitControllerColossusPortal> onFinishExit;

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
				SceneExitControllerColossusPortal.onBeginExit?.Invoke(this);
			}
			isRunning = true;
			experienceCollector = base.gameObject.AddComponent<ConvertPlayerMoneyToExperience>();
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
			SceneExitControllerColossusPortal.onFinishExit?.Invoke(this);
			if ((bool)Run.instance && Run.instance.isGameOverServer)
			{
				break;
			}
			if (useRunNextStageScene)
			{
				Stage.instance.BeginAdvanceStage(Run.instance.nextStageScene);
				break;
			}
			SetNextStage();
			if ((bool)destinationScene)
			{
				Stage.instance.BeginAdvanceStage(destinationScene);
			}
			break;
		case ExitState.Idle:
			break;
		}
	}

	private void SetNextStage()
	{
		switch (Stage.instance.sceneDef.stageOrder)
		{
		case 1:
			destinationScene = t2DestinationScene;
			break;
		case 2:
			destinationScene = t3DestinationScene;
			break;
		default:
			destinationScene = t4DestinationScene;
			break;
		}
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
