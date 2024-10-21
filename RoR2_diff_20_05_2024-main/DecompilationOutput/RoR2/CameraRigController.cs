using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JetBrains.Annotations;
using RoR2.CameraModes;
using RoR2.ConVar;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class CameraRigController : MonoBehaviour
{
	[Obsolete("CameraMode objects are now used instead of enums.", true)]
	public enum CameraMode
	{
		None,
		PlayerBasic,
		Fly,
		SpectateOrbit,
		SpectateUser
	}

	public struct AimAssistInfo
	{
		public HurtBox aimAssistHurtbox;

		public Vector3 localPositionOnHurtbox;

		public Vector3 worldPosition;
	}

	[Header("Component References")]
	[Tooltip("The main camera for rendering the scene.")]
	public Camera sceneCam;

	[Tooltip("The UI camera.")]
	public Camera uiCam;

	[Tooltip("The skybox camera.")]
	public Camera skyboxCam;

	[Tooltip("The particle system to play while sprinting.")]
	public ParticleSystem sprintingParticleSystem;

	[Tooltip("The particle system to play while skills are disabled.")]
	public ParticleSystem disabledSkillsParticleSystem;

	[Header("Camera Parameters")]
	[Tooltip("The default FOV of this camera.")]
	public float baseFov = 60f;

	[Tooltip("The viewport to use.")]
	public Rect viewport = new Rect(0f, 0f, 1f, 1f);

	[Tooltip("The maximum distance of the raycast used to determine the aim vector.")]
	public float maxAimRaycastDistance = 1000f;

	[Tooltip("If true, treat this camera as though it's in a cutscene")]
	public bool isCutscene;

	[Header("Near-Camera Character Dithering")]
	public bool enableFading = true;

	public float fadeStartDistance = 1f;

	public float fadeEndDistance = 4f;

	[Header("Behavior")]
	[Tooltip("Whether or not to create a HUD.")]
	public bool createHud = true;

	[Tooltip("Whether or not this camera being enabled forces player-owned cameras to be disabled.")]
	public bool suppressPlayerCameras;

	[Tooltip("Forces music to be enabled for this camera.")]
	public bool enableMusic;

	private ulong musicListenerId;

	private bool musicEnabled;

	[Header("Target (for debug only)")]
	public GameObject nextTarget;

	public static Action OnChangeHUD;

	public static Action OnHUDUpdated;

	private CameraModeBase _cameraMode;

	private GameObject _target;

	private CameraState desiredCameraState;

	public CameraState currentCameraState;

	public bool doNotOverrideCameraState;

	private CameraState lerpCameraState;

	private float lerpCameraTime = 1f;

	private float lerpCameraTimeScale = 1f;

	private ICameraStateProvider overrideCam;

	private CameraModeBase.CameraModeContext cameraModeContext;

	private NetworkUser _viewer;

	private LocalUser _localUserViewer;

	private static GameObject DamageNumberManagerPrefab;

	private static GameObject HudPrefab;

	private static string previousHudPath;

	public CameraTargetParams targetParams;

	private const string musicSystemDeviceShareSet = "Music_Audio_Device";

	public AimAssistInfo lastAimAssist;

	public AimAssistInfo aimAssist;

	private static List<CameraRigController> instancesList = new List<CameraRigController>();

	public static readonly ReadOnlyCollection<CameraRigController> readOnlyInstancesList = instancesList.AsReadOnly();

	public static FloatConVar aimStickExponent = new FloatConVar("aim_stick_exponent", ConVarFlags.None, "1", "The exponent for stick input used for aiming.");

	public static FloatConVar aimStickDualZoneThreshold = new FloatConVar("aim_stick_dual_zone_threshold", ConVarFlags.None, "0.90", "The threshold for stick dual zone behavior.");

	public static FloatConVar aimStickDualZoneSlope = new FloatConVar("aim_stick_dual_zone_slope", ConVarFlags.None, "0.40", "The slope value for stick dual zone behavior.");

	public static FloatConVar aimStickDualZoneSmoothing = new FloatConVar("aim_stick_smoothing", ConVarFlags.None, "0.05", "The smoothing value for stick aiming.");

	public static FloatConVar aimStickGlobalScale = new FloatConVar("aim_stick_global_scale", ConVarFlags.Archive, "1.00", "The global sensitivity scale for stick aiming.");

	public static FloatConVar aimStickAssistMinSlowdownScale = new FloatConVar("aim_stick_assist_min_slowdown_scale", ConVarFlags.None, "1", "The MIN amount the sensitivity scales down when passing over an enemy.");

	public static FloatConVar aimStickAssistMaxSlowdownScale = new FloatConVar("aim_stick_assist_max_slowdown_scale", ConVarFlags.None, "0.4", "The MAX amount the sensitivity scales down when passing over an enemy. UNUSED");

	public static FloatConVar aimStickAssistMinDelta = new FloatConVar("aim_stick_assist_min_delta", ConVarFlags.None, "0", "The MIN amount in radians the aim assist will turn towards");

	public static FloatConVar aimStickAssistMaxDelta = new FloatConVar("aim_stick_assist_max_delta", ConVarFlags.None, "1.57", "The MAX amount in radians the aim assist will turn towards");

	public static FloatConVar aimStickAssistMaxInputHelp = new FloatConVar("aim_stick_assist_max_input_help", ConVarFlags.None, "0.2", "The amount, from 0-1, that the aim assist will actually ADD magnitude towards. Helps you keep target while strafing. CURRENTLY UNUSED.");

	public static FloatConVar aimStickAssistMaxSize = new FloatConVar("aim_stick_assist_max_size", ConVarFlags.None, "3", "The size, as a coefficient, of the aim assist 'white' zone.");

	public static FloatConVar aimStickAssistMinSize = new FloatConVar("aim_stick_assist_min_size", ConVarFlags.None, "1", "The minimum size, as a percentage of the GUI, of the aim assist 'red' zone.");

	public static BoolConVar enableSprintSensitivitySlowdown = new BoolConVar("enable_sprint_sensitivity_slowdown", ConVarFlags.Archive, "1", "Enables sensitivity reduction while sprinting.");

	private float hitmarkerAlpha;

	private float hitmarkerTimer;

	public bool disableSpectating { get; set; }

	public CameraModeBase cameraMode
	{
		get
		{
			return _cameraMode;
		}
		set
		{
			if (_cameraMode != value)
			{
				_cameraMode?.OnUninstall(this);
				_cameraMode = value;
				_cameraMode?.OnInstall(this);
			}
		}
	}

	public HUD hud { get; private set; }

	public GameObject firstPersonTarget { get; private set; }

	public TeamIndex targetTeamIndex { get; private set; } = TeamIndex.None;

	public CharacterBody targetBody { get; private set; }

	public Vector3 rawScreenShakeDisplacement { get; private set; }

	public Vector3 crosshairWorldPosition { get; private set; }

	public bool hasOverride => overrideCam != null;

	public bool isControlAllowed
	{
		get
		{
			if (hasOverride)
			{
				return overrideCam.IsUserControlAllowed(this);
			}
			return true;
		}
	}

	public bool isHudAllowed
	{
		get
		{
			if ((bool)target)
			{
				if (hasOverride)
				{
					return overrideCam.IsHudAllowed(this);
				}
				return true;
			}
			return false;
		}
	}

	public GameObject target
	{
		get
		{
			return _target;
		}
		private set
		{
			if ((object)_target != value)
			{
				GameObject oldTarget = _target;
				_target = value;
				bool flag = _target;
				targetParams = (flag ? target.GetComponent<CameraTargetParams>() : null);
				targetBody = (flag ? target.GetComponent<CharacterBody>() : null);
				cameraMode?.OnTargetChanged(this, new CameraModeBase.OnTargetChangedArgs
				{
					oldTarget = oldTarget,
					newTarget = _target
				});
				CameraRigController.onCameraTargetChanged?.Invoke(this, target);
			}
		}
	}

	public NetworkUser viewer
	{
		get
		{
			return _viewer;
		}
		set
		{
			_viewer = value;
			localUserViewer = (_viewer ? _viewer.localUser : null);
		}
	}

	public LocalUser localUserViewer
	{
		get
		{
			return _localUserViewer;
		}
		private set
		{
			if (_localUserViewer != value)
			{
				if (_localUserViewer != null)
				{
					_localUserViewer.cameraRigController = null;
				}
				_localUserViewer = value;
				if (_localUserViewer != null)
				{
					_localUserViewer.cameraRigController = this;
				}
				if ((bool)hud)
				{
					hud.localUserViewer = _localUserViewer;
				}
			}
		}
	}

	public HurtBox lastCrosshairHurtBox { get; private set; }

	public static event Action<CameraRigController, GameObject> onCameraTargetChanged;

	public static event Action<CameraRigController> onCameraEnableGlobal;

	public static event Action<CameraRigController> onCameraDisableGlobal;

	private void StartStateLerp(float lerpDuration)
	{
		lerpCameraState = currentCameraState;
		if (lerpDuration > 0f)
		{
			lerpCameraTime = 0f;
			lerpCameraTimeScale = 1f / lerpDuration;
		}
		else
		{
			lerpCameraTime = 1f;
			lerpCameraTimeScale = 0f;
		}
	}

	public void SetOverrideCam(ICameraStateProvider newOverrideCam, float lerpDuration = 1f)
	{
		if (newOverrideCam != overrideCam)
		{
			if (overrideCam != null && newOverrideCam == null)
			{
				cameraMode?.MatchState(in cameraModeContext, in currentCameraState);
			}
			overrideCam = newOverrideCam;
			StartStateLerp(lerpDuration);
		}
	}

	public bool IsOverrideCam(ICameraStateProvider testOverrideCam)
	{
		return overrideCam == testOverrideCam;
	}

	public static IEnumerator InitializeReferences()
	{
		AsyncOperationHandle<GameObject> handle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/DamageNumberManager");
		while (!handle.IsDone)
		{
			yield return null;
		}
		DamageNumberManagerPrefab = handle.Result;
		previousHudPath = GetHUDPath();
		handle = LegacyResourcesAPI.LoadAsync<GameObject>(previousHudPath);
		while (!handle.IsDone)
		{
			yield return null;
		}
		HudPrefab = handle.Result;
	}

	private void Start()
	{
		RoR2Application.onUpdate += EarlyUpdate;
		OnChangeHUD = (Action)Delegate.Combine(OnChangeHUD, new Action(UpdateHUD));
		UpdateHUD();
		if ((bool)uiCam)
		{
			uiCam.transform.parent = null;
			uiCam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		}
		if (!DamageNumberManager.instance)
		{
			if (DamageNumberManagerPrefab != null)
			{
				UnityEngine.Object.Instantiate(DamageNumberManagerPrefab);
			}
			else
			{
				AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/DamageNumberManager");
				asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
				{
					UnityEngine.Object.Instantiate(x.Result);
				};
			}
		}
		currentCameraState = new CameraState
		{
			position = base.transform.position,
			rotation = base.transform.rotation,
			fov = baseFov
		};
		cameraMode = CameraModePlayerBasic.playerBasic;
		if (enableMusic && (!RoR2Application.isInLocalMultiPlayer || _viewer.localUser.inputPlayer.name == "PlayerMain"))
		{
			EnableMusicForCamera();
		}
	}

	private void UpdateHUD()
	{
		if (createHud)
		{
			if ((bool)hud)
			{
				UnityEngine.Object.Destroy(hud.gameObject);
			}
			string hUDPath = GetHUDPath();
			if (HudPrefab == null || hUDPath != previousHudPath)
			{
				HudPrefab = LegacyResourcesAPI.Load<GameObject>(hUDPath);
				previousHudPath = hUDPath;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(HudPrefab);
			hud = gameObject.GetComponent<HUD>();
			hud.cameraRigController = this;
			SetPostProcessVolumeExclusive(hud.scoreboardPostProcessVolume);
			hud.GetComponent<Canvas>().worldCamera = uiCam;
			hud.GetComponent<CrosshairManager>().cameraRigController = this;
			hud.localUserViewer = localUserViewer;
			OnHUDUpdated?.Invoke();
		}
	}

	[CanBeNull]
	private static string GetHUDPath()
	{
		if (NetworkUser.readOnlyLocalPlayersList == null || NetworkUser.readOnlyLocalPlayersList.Count < 1)
		{
			return "Prefabs/HUDSimple";
		}
		UserProfile userProfile = NetworkUser.readOnlyLocalPlayersList[0]?.localUser?.userProfile;
		if (userProfile != null)
		{
			switch ((HudType)userProfile.hudSizeMode)
			{
			case HudType.Default:
				return "Prefabs/HUDSimple";
			case HudType.Large:
				return "Prefabs/HUDSimple_Big";
			case HudType.Auto:
				return "Prefabs/HUDSimple_Big";
			default:
				Debug.LogWarning(string.Format("Undefined {0}. Value : {1}. Resorting to default HUD.", "HudType", userProfile.hudSizeMode));
				return "Prefabs/HUDSimple";
			}
		}
		return "Prefabs/HUDSimple";
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		if ((bool)uiCam)
		{
			uiCam.gameObject.SetActive(value: true);
		}
		if ((bool)hud)
		{
			hud.gameObject.SetActive(value: true);
		}
		CameraRigController.onCameraEnableGlobal?.Invoke(this);
	}

	private void OnDisable()
	{
		CameraRigController.onCameraDisableGlobal?.Invoke(this);
		if ((bool)uiCam)
		{
			uiCam.gameObject.SetActive(value: false);
		}
		if ((bool)hud)
		{
			hud.gameObject.SetActive(value: false);
		}
		instancesList.Remove(this);
	}

	private void OnDestroy()
	{
		RoR2Application.onUpdate -= EarlyUpdate;
		OnChangeHUD = (Action)Delegate.Remove(OnChangeHUD, new Action(UpdateHUD));
		cameraMode = null;
		if ((bool)uiCam)
		{
			UnityEngine.Object.Destroy(uiCam.gameObject);
		}
		if ((bool)hud)
		{
			UnityEngine.Object.Destroy(hud.gameObject);
		}
		DisableMusicForCamera();
	}

	private void EarlyUpdate()
	{
		target = nextTarget;
		if ((bool)targetBody)
		{
			targetTeamIndex = targetBody.teamComponent.teamIndex;
		}
		if (Time.deltaTime != 0f && !isCutscene)
		{
			lerpCameraTime += Time.deltaTime * lerpCameraTimeScale;
			firstPersonTarget = null;
			sceneCam.rect = viewport;
			GenerateCameraModeContext(out cameraModeContext);
			if (cameraMode != null)
			{
				cameraMode.CollectLookInput(in cameraModeContext, out var result);
				CameraModeBase cameraModeBase = cameraMode;
				ref CameraModeBase.CameraModeContext context = ref cameraModeContext;
				CameraModeBase.ApplyLookInputArgs args = new CameraModeBase.ApplyLookInputArgs
				{
					lookInput = result.lookInput
				};
				cameraModeBase.ApplyLookInput(in context, in args);
			}
		}
	}

	private void LateUpdate()
	{
		if (Time.deltaTime == 0f || isCutscene)
		{
			return;
		}
		CameraState cameraState = currentCameraState;
		if (cameraMode != null && cameraModeContext.cameraInfo.cameraRigController != null && !doNotOverrideCameraState)
		{
			cameraMode.Update(in cameraModeContext, out var result);
			cameraState = result.cameraState;
			firstPersonTarget = result.firstPersonTarget;
			crosshairWorldPosition = result.crosshairWorldPosition;
			SetParticleSystemActive(result.showSprintParticles, sprintingParticleSystem);
			SetParticleSystemActive(result.showDisabledSkillsParticles, disabledSkillsParticleSystem);
		}
		if ((bool)hud)
		{
			CharacterMaster targetMaster = (targetBody ? targetBody.master : null);
			hud.targetMaster = targetMaster;
		}
		CameraState cameraState2 = cameraState;
		if (overrideCam != null)
		{
			if (!(overrideCam is UnityEngine.Object @object) || (bool)@object)
			{
				overrideCam.GetCameraState(this, ref cameraState2);
			}
			else
			{
				overrideCam = null;
			}
		}
		if (lerpCameraTime >= 1f)
		{
			currentCameraState = cameraState2;
		}
		else
		{
			currentCameraState = CameraState.Lerp(ref lerpCameraState, ref cameraState2, RemapLerpTime(lerpCameraTime));
		}
		SetCameraState(currentCameraState);
	}

	public void UpdatePreviousCameraState(Vector3 position, Quaternion rotation)
	{
		cameraModeContext.cameraInfo.previousCameraState.position = position;
		cameraModeContext.cameraInfo.previousCameraState.rotation = rotation;
	}

	private void GenerateCameraModeContext(out CameraModeBase.CameraModeContext result)
	{
		result.cameraInfo = default(CameraModeBase.CameraInfo);
		result.targetInfo = default(CameraModeBase.TargetInfo);
		result.viewerInfo = default(CameraModeBase.ViewerInfo);
		ref CameraModeBase.CameraInfo cameraInfo = ref result.cameraInfo;
		ref CameraModeBase.TargetInfo targetInfo = ref result.targetInfo;
		ref CameraModeBase.ViewerInfo viewerInfo = ref result.viewerInfo;
		cameraInfo.cameraRigController = this;
		cameraInfo.sceneCam = sceneCam;
		cameraInfo.overrideCam = overrideCam;
		cameraInfo.previousCameraState = currentCameraState;
		cameraInfo.baseFov = baseFov;
		targetInfo.target = (target ? target : null);
		targetInfo.body = targetBody;
		targetInfo.inputBank = targetInfo.target?.GetComponent<InputBankTest>();
		if (targetBody != null && targetBody.currentVehicle != null && targetBody.currentVehicle.targetParams != null)
		{
			targetInfo.targetParams = targetBody.currentVehicle.targetParams;
		}
		else
		{
			targetInfo.targetParams = targetParams;
		}
		targetInfo.teamIndex = targetInfo.target?.GetComponent<TeamComponent>()?.teamIndex ?? TeamIndex.None;
		targetInfo.isSprinting = (bool)targetInfo.body && targetInfo.body.isSprinting;
		targetInfo.skillsAreDisabled = (bool)targetInfo.body && targetInfo.body.allSkillsDisabled;
		targetInfo.master = targetInfo.body?.master;
		targetInfo.networkUser = targetInfo.master?.playerCharacterMasterController?.networkUser;
		targetInfo.networkedViewAngles = targetInfo.networkUser?.GetComponent<NetworkedViewAngles>();
		targetInfo.isViewerControlled = (bool)targetInfo.networkUser && (object)targetInfo.networkUser == localUserViewer?.currentNetworkUser;
		viewerInfo.localUser = localUserViewer;
		viewerInfo.userProfile = localUserViewer?.userProfile;
		viewerInfo.inputPlayer = localUserViewer?.inputPlayer;
		viewerInfo.eventSystem = localUserViewer?.eventSystem;
		viewerInfo.hasCursor = (bool)viewerInfo.eventSystem && viewerInfo.eventSystem.isCursorVisible;
		viewerInfo.isUIFocused = localUserViewer?.isUIFocused ?? false;
	}

	public void CloneCameraContext(CameraRigController cloneThisCamera)
	{
		if (!(cloneThisCamera == null))
		{
			cameraModeContext = cloneThisCamera.cameraModeContext;
		}
	}

	public float Raycast(Ray ray, float maxDistance, float wallCushion)
	{
		int num = 0;
		num = HGPhysics.SphereCastAll(out var hits, ray.origin, wallCushion, ray.direction, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
		float num2 = maxDistance;
		for (int i = 0; i < num; i++)
		{
			float distance = hits[i].distance;
			if (distance < num2)
			{
				Collider collider = hits[i].collider;
				if ((bool)collider && !collider.GetComponent<NonSolidToCamera>())
				{
					num2 = distance;
				}
			}
		}
		HGPhysics.ReturnResults(hits);
		return num2;
	}

	private static float RemapLerpTime(float t)
	{
		float num = 1f;
		float num2 = 0f;
		float num3 = 1f;
		if ((t /= num / 2f) < 1f)
		{
			return num3 / 2f * t * t + num2;
		}
		return (0f - num3) / 2f * ((t -= 1f) * (t - 2f) - 1f) + num2;
	}

	public void SetCameraState(CameraState cameraState)
	{
		currentCameraState = cameraState;
		float num = ((localUserViewer == null) ? 1f : localUserViewer.userProfile.screenShakeScale);
		Vector3 position = cameraState.position;
		rawScreenShakeDisplacement = ShakeEmitter.ComputeTotalShakeAtPoint(cameraState.position);
		Vector3 vector = rawScreenShakeDisplacement * num;
		Vector3 position2 = position + vector;
		if (vector != Vector3.zero && Physics.SphereCast(position, direction: vector, radius: sceneCam.nearClipPlane, hitInfo: out var hitInfo, maxDistance: vector.magnitude, layerMask: LayerIndex.world.mask, queryTriggerInteraction: QueryTriggerInteraction.Ignore))
		{
			position2 = position + vector.normalized * hitInfo.distance;
		}
		base.transform.SetPositionAndRotation(position2, cameraState.rotation);
		if ((bool)sceneCam)
		{
			sceneCam.fieldOfView = cameraState.fov;
		}
	}

	public static Ray ModifyAimRayIfApplicable(Ray originalAimRay, GameObject target, out float extraRaycastDistance)
	{
		CameraRigController cameraRigController = null;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			CameraRigController cameraRigController2 = readOnlyInstancesList[i];
			if (cameraRigController2.target == target && cameraRigController2._localUserViewer.cachedBodyObject == target && !cameraRigController2.hasOverride)
			{
				cameraRigController = cameraRigController2;
				break;
			}
		}
		if ((bool)cameraRigController)
		{
			Camera camera = cameraRigController.sceneCam;
			extraRaycastDistance = (originalAimRay.origin - camera.transform.position).magnitude;
			return camera.ViewportPointToRay(new Vector2(0.5f, 0.5f));
		}
		extraRaycastDistance = 0f;
		return originalAimRay;
	}

	private void SetSprintParticlesActive(bool newSprintParticlesActive)
	{
		if (!sprintingParticleSystem)
		{
			return;
		}
		ParticleSystem.MainModule main = sprintingParticleSystem.main;
		if (newSprintParticlesActive)
		{
			main.loop = true;
			if (!sprintingParticleSystem.isPlaying)
			{
				sprintingParticleSystem.Play();
			}
		}
		else
		{
			main.loop = false;
		}
	}

	private void SetParticleSystemActive(bool newParticlesActive, ParticleSystem particleSystem)
	{
		if (!particleSystem)
		{
			return;
		}
		ParticleSystem.MainModule main = particleSystem.main;
		if (newParticlesActive)
		{
			main.loop = true;
			if (!particleSystem.isPlaying)
			{
				particleSystem.Play();
			}
		}
		else
		{
			main.loop = false;
		}
	}

	public void SetPostProcessVolumeExclusive(PostProcessVolume volume)
	{
		if (!(volume == null) && !(sceneCam == null))
		{
			volume.targetLayer = sceneCam.GetComponent<PostProcessLayer>();
		}
	}

	[InitDuringStartup]
	private static void Init()
	{
		SceneCamera.onSceneCameraPreCull += delegate(SceneCamera sceneCam)
		{
			sceneCam.cameraRigController.sprintingParticleSystem.gameObject.layer = LayerIndex.defaultLayer.intVal;
		};
		SceneCamera.onSceneCameraPostRender += delegate(SceneCamera sceneCam)
		{
			sceneCam.cameraRigController.sprintingParticleSystem.gameObject.layer = LayerIndex.noDraw.intVal;
		};
	}

	public static bool IsObjectSpectatedByAnyCamera(GameObject gameObject)
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			if (instancesList[i].target == gameObject)
			{
				return true;
			}
		}
		return false;
	}

	[ContextMenu("Print Debug Info")]
	private void PrintDebugInfo()
	{
		Debug.LogFormat("hasOverride={0} overrideCam={1} isControlAllowed={2}", hasOverride, overrideCam, isControlAllowed);
	}

	public void EnableMusicForCamera(string deviceShareSet = "Music_Audio_Device")
	{
		GameObject gameObject = new GameObject("MusicListener", typeof(AkGameObj), typeof(AkAudioListener));
		Transform obj = gameObject.transform;
		obj.localPosition = Vector3.zero;
		obj.localRotation = Quaternion.identity;
		obj.localScale = Vector3.one;
		obj.SetParent(sceneCam.transform, worldPositionStays: false);
		AkOutputSettings in_Settings = new AkOutputSettings(deviceShareSet);
		ulong[] array = new ulong[1] { AkSoundEngine.GetAkGameObjectID(gameObject) };
		AkSoundEngine.AddOutput(in_Settings, out var out_pDeviceID, array, (uint)array.Length);
		musicListenerId = out_pDeviceID;
		musicEnabled = true;
	}

	public void DisableMusicForCamera()
	{
		if (musicEnabled)
		{
			AkSoundEngine.RemoveOutput(musicListenerId);
		}
	}
}
