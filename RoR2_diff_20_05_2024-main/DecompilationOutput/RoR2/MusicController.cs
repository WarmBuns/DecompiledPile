using System.Collections.ObjectModel;
using RoR2.WwiseUtils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace RoR2;

public class MusicController : MonoBehaviour
{
	public delegate void PickTrackDelegate(MusicController musicController, ref MusicTrackDef newTrack);

	private struct EnemyInfo
	{
		public Ray aimRay;

		public float lookScore;

		public float proximityScore;

		public float threatScore;
	}

	private struct CalculateIntensityJob : IJobParallelFor
	{
		[ReadOnly]
		public Vector3 targetPosition;

		[ReadOnly]
		public int elementCount;

		public NativeArray<EnemyInfo> enemyInfoBuffer;

		[ReadOnly]
		public float nearDistance;

		[ReadOnly]
		public float farDistance;

		public void Execute(int i)
		{
			EnemyInfo value = enemyInfoBuffer[i];
			Vector3 vector = targetPosition - value.aimRay.origin;
			float magnitude = vector.magnitude;
			float num = Mathf.Clamp01(Vector3.Dot(vector / magnitude, value.aimRay.direction));
			float num2 = Mathf.Clamp01(Mathf.InverseLerp(farDistance, nearDistance, magnitude));
			value.lookScore = num * value.threatScore;
			value.proximityScore = num2 * value.threatScore;
			enemyInfoBuffer[i] = value;
		}

		public void CalculateSum(out float proximityScore, out float lookScore)
		{
			proximityScore = 0f;
			lookScore = 0f;
			for (int i = 0; i < elementCount; i++)
			{
				proximityScore += enemyInfoBuffer[i].proximityScore;
				lookScore += enemyInfoBuffer[i].lookScore;
			}
		}
	}

	public GameObject target;

	public bool enableMusicSystem;

	public MusicTrackDef introCutsceneMusic;

	private CameraRigController targetCamera;

	private MusicTrackDef _currentTrack;

	private RtpcSetter rtpcPlayerHealthValue;

	private RtpcSetter rtpcEnemyValue;

	private RtpcSetter rtpcTeleporterProximityValue;

	private RtpcSetter rtpcTeleporterDirectionValue;

	private RtpcSetter rtpcTeleporterPlayerStatus;

	private StateSetter stBossStatus;

	public static int timesStartRan;

	private float updateTimer;

	private const float MUSIC_UPDATE_INTERVAL = 0.25f;

	private bool wasPaused;

	private NativeArray<EnemyInfo> enemyInfoBuffer;

	private CalculateIntensityJob calculateIntensityJob;

	private JobHandle calculateIntensityJobHandle;

	public static MusicController Instance { get; private set; }

	private MusicTrackDef currentTrack
	{
		get
		{
			return _currentTrack;
		}
		set
		{
			if ((object)_currentTrack != value)
			{
				if ((object)_currentTrack != null)
				{
					_currentTrack.Stop();
				}
				_currentTrack = value;
				if ((object)_currentTrack != null)
				{
					_currentTrack.Play();
				}
			}
		}
	}

	public static event PickTrackDelegate pickTrackHook;

	private void InitializeEngineDependentValues()
	{
		rtpcPlayerHealthValue = new RtpcSetter("playerHealth");
		rtpcEnemyValue = new RtpcSetter("enemyValue");
		rtpcTeleporterProximityValue = new RtpcSetter("teleporterProximity");
		rtpcTeleporterDirectionValue = new RtpcSetter("teleporterDirection");
		rtpcTeleporterPlayerStatus = new RtpcSetter("teleporterPlayerStatus");
		stBossStatus = new StateSetter("bossStatus");
	}

	private void Update()
	{
		if (updateTimer <= 0f)
		{
			updateTimer = 0.25f;
			UpdateState();
			targetCamera = ((CameraRigController.readOnlyInstancesList.Count > 0) ? CameraRigController.readOnlyInstancesList[0] : null);
			target = (targetCamera ? targetCamera.target : null);
			ScheduleIntensityCalculation(target);
		}
		else
		{
			updateTimer -= Time.deltaTime;
		}
	}

	private void RecalculateHealth(GameObject playerObject)
	{
		rtpcPlayerHealthValue.value = 100f;
		if (!target)
		{
			return;
		}
		CharacterBody component = target.GetComponent<CharacterBody>();
		if (!component)
		{
			return;
		}
		if (component.HasBuff(JunkContent.Buffs.Deafened))
		{
			rtpcPlayerHealthValue.value = -100f;
			return;
		}
		HealthComponent healthComponent = component.healthComponent;
		if ((bool)healthComponent)
		{
			rtpcPlayerHealthValue.value = healthComponent.combinedHealthFraction * 100f;
		}
	}

	private void UpdateTeleporterParameters(TeleporterInteraction teleporter, Transform cameraTransform, CharacterBody targetBody)
	{
		float value = float.PositiveInfinity;
		float value2 = 0f;
		bool flag = true;
		stBossStatus.valueId = CommonWwiseIds.alive;
		if ((bool)teleporter)
		{
			if ((bool)cameraTransform)
			{
				Vector3 position = cameraTransform.position;
				Vector3 forward = cameraTransform.forward;
				Vector3 vector = teleporter.transform.position - position;
				float num = Vector2.SignedAngle(new Vector2(vector.x, vector.z), new Vector2(forward.x, forward.z));
				if (num < 0f)
				{
					num += 360f;
				}
				value = vector.magnitude;
				value2 = num;
			}
			if (TeleporterInteraction.instance.isIdleToCharging || TeleporterInteraction.instance.isCharging)
			{
				stBossStatus.valueId = CommonWwiseIds.alive;
			}
			else if (TeleporterInteraction.instance.isCharged)
			{
				stBossStatus.valueId = CommonWwiseIds.dead;
			}
			if (teleporter.isCharging && (bool)targetBody)
			{
				flag = teleporter.holdoutZoneController.IsBodyInChargingRadius(targetBody);
			}
		}
		value = Mathf.Clamp(value, 20f, 250f);
		rtpcTeleporterProximityValue.value = Util.Remap(value, 20f, 250f, 0f, 10000f);
		rtpcTeleporterDirectionValue.value = value2;
		rtpcTeleporterPlayerStatus.value = (flag ? 1f : 0f);
	}

	private void LateUpdate()
	{
		bool flag = Time.timeScale == 0f;
		if (wasPaused != flag)
		{
			AkSoundEngine.PostEvent(flag ? "Pause_Music" : "Unpause_Music", base.gameObject);
			wasPaused = flag;
		}
		RecalculateHealth(target);
		UpdateTeleporterParameters(TeleporterInteraction.instance, targetCamera ? targetCamera.transform : null, target ? target.GetComponent<CharacterBody>() : null);
		calculateIntensityJobHandle.Complete();
		calculateIntensityJob.CalculateSum(out var proximityScore, out var lookScore);
		float num = 0.025f;
		Mathf.Clamp(1f - rtpcPlayerHealthValue.value * 0.01f, 0.25f, 0.75f);
		float value = (proximityScore * 0.75f + lookScore * 0.25f) * num;
		rtpcEnemyValue.value = value;
		FlushValuesToEngine();
	}

	private void FlushValuesToEngine()
	{
		stBossStatus.FlushIfChanged();
		rtpcPlayerHealthValue.FlushIfChanged();
		rtpcTeleporterProximityValue.FlushIfChanged();
		rtpcTeleporterDirectionValue.FlushIfChanged();
		rtpcTeleporterPlayerStatus.FlushIfChanged();
		rtpcEnemyValue.FlushIfChanged();
	}

	private void PickCurrentTrack(ref MusicTrackDef newTrack)
	{
		SceneDef mostRecentSceneDef = SceneCatalog.mostRecentSceneDef;
		bool flag = false;
		if ((bool)TeleporterInteraction.instance && !TeleporterInteraction.instance.isIdle)
		{
			flag = true;
		}
		if (enableMusicSystem && (bool)mostRecentSceneDef)
		{
			if (flag && (bool)mostRecentSceneDef.bossTrack)
			{
				newTrack = mostRecentSceneDef.bossTrack;
			}
			else if ((bool)mostRecentSceneDef.mainTrack)
			{
				newTrack = mostRecentSceneDef.mainTrack;
			}
		}
		MusicController.pickTrackHook?.Invoke(this, ref newTrack);
	}

	private void UpdateState()
	{
		MusicTrackDef newTrack = currentTrack;
		PickCurrentTrack(ref newTrack);
		currentTrack = newTrack;
	}

	private void EnsureEnemyBufferSize(int requiredSize)
	{
		if (enemyInfoBuffer.Length < requiredSize)
		{
			if (enemyInfoBuffer.Length != 0)
			{
				enemyInfoBuffer.Dispose();
			}
			enemyInfoBuffer = new NativeArray<EnemyInfo>(requiredSize, Allocator.Persistent);
		}
	}

	private void OnDestroy()
	{
		enemyInfoBuffer.Dispose();
	}

	private void ScheduleIntensityCalculation(GameObject targetBodyObject)
	{
		if (!targetBodyObject)
		{
			return;
		}
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
		int count = teamMembers.Count;
		EnsureEnemyBufferSize(count);
		int num = 0;
		int i = 0;
		for (int num2 = count; i < num2; i++)
		{
			TeamComponent teamComponent = teamMembers[i];
			InputBankTest component = teamComponent.GetComponent<InputBankTest>();
			CharacterBody component2 = teamComponent.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				enemyInfoBuffer[num++] = new EnemyInfo
				{
					aimRay = new Ray(component.aimOrigin, component.aimDirection),
					threatScore = (component2.master ? component2.GetNormalizedThreatValue() : 0f)
				};
			}
		}
		calculateIntensityJob = new CalculateIntensityJob
		{
			enemyInfoBuffer = enemyInfoBuffer,
			elementCount = num,
			targetPosition = targetBodyObject.transform.position,
			nearDistance = 20f,
			farDistance = 75f
		};
		calculateIntensityJobHandle = calculateIntensityJob.Schedule(num, 32);
	}

	public static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/MusicController");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> result)
		{
			GameObject gameObject = Object.Instantiate(result.Result, RoR2Application.instance.transform);
			if (gameObject != null)
			{
				gameObject.GetComponent<MusicController>().PreloadForCutsceneStart();
			}
		};
	}

	public void PreloadForCutsceneStart()
	{
		Instance = this;
		enemyInfoBuffer = new NativeArray<EnemyInfo>(64, Allocator.Persistent);
		InitializeEngineDependentValues();
		SceneManager.sceneLoaded += SceneManager_OnLoadNextScene;
	}

	private void SceneManager_OnLoadNextScene(Scene arg0, LoadSceneMode arg1)
	{
		if (arg0.name == "intro")
		{
			currentTrack = introCutsceneMusic;
		}
		else
		{
			SceneManager.sceneLoaded -= SceneManager_OnLoadNextScene;
		}
	}

	public void StartIntroMusic()
	{
		AkSoundEngine.PostEvent("Play_Music_System", base.gameObject);
		currentTrack = introCutsceneMusic;
		enableMusicSystem = true;
	}
}
