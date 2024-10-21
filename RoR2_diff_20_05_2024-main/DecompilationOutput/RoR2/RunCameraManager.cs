using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2.CameraModes;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

public class RunCameraManager : MonoBehaviour
{
	public static RunCameraManager instance;

	private readonly Dictionary<string, CameraRigController> cameras = new Dictionary<string, CameraRigController>();

	private readonly Dictionary<string, Canvas> canvases = new Dictionary<string, Canvas>();

	public bool isLobby;

	private bool isLocalMultiplayer = RoR2Application.isInLocalMultiPlayer;

	private bool leavingLobby;

	public GameObject CharacterSelectUIMain;

	public GameObject CharacterSelectUILocal;

	public GameObject MainCamera;

	public GameObject PreGameController;

	private GameObject popOutContainer;

	private int planeDistanceMultiplier = 100;

	private float popOutScaleValue = 1.85f;

	[Header("----------------Splitscreen Lobby----------------")]
	[Tooltip("A relative offset to stagger the camera for each successive splitscreen lobby player viewport.  Defaults roughly match SurvivorMannequinDiorama slots.")]
	public Vector3 SplitscreenCameraOffset = new Vector3(-0.9f, 0f, 3f);

	public static readonly Rect[][] ScreenLayouts = new Rect[5][]
	{
		new Rect[0],
		new Rect[1]
		{
			new Rect(0f, 0f, 1f, 1f)
		},
		new Rect[2]
		{
			new Rect(0f, 0.5f, 1f, 0.5f),
			new Rect(0f, 0f, 1f, 0.5f)
		},
		new Rect[3]
		{
			new Rect(0f, 0.5f, 1f, 0.5f),
			new Rect(0f, 0f, 0.5f, 0.5f),
			new Rect(0.5f, 0f, 0.5f, 0.5f)
		},
		new Rect[4]
		{
			new Rect(0f, 0.5f, 0.5f, 0.5f),
			new Rect(0.5f, 0.5f, 0.5f, 0.5f),
			new Rect(0f, 0f, 0.5f, 0.5f),
			new Rect(0.5f, 0f, 0.5f, 0.5f)
		}
	};

	private List<CameraRigController> activeCameras = new List<CameraRigController>();

	public IReadOnlyList<CameraRigController> ReadOnlyActiveCameras => activeCameras;

	private void Start()
	{
		instance = this;
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private static GameObject GetNetworkUserBodyObject(NetworkUser networkUser)
	{
		if ((bool)networkUser.masterObject)
		{
			CharacterMaster component = networkUser.masterObject.GetComponent<CharacterMaster>();
			if ((bool)component)
			{
				return component.GetBodyObject();
			}
		}
		return null;
	}

	private static TeamIndex GetNetworkUserTeamIndex(NetworkUser networkUser)
	{
		if ((bool)networkUser.masterObject)
		{
			CharacterMaster component = networkUser.masterObject.GetComponent<CharacterMaster>();
			if ((bool)component)
			{
				return component.teamIndex;
			}
		}
		return TeamIndex.Neutral;
	}

	private void Update()
	{
		if (TransitionCommand.requestPending || leavingLobby)
		{
			leavingLobby = true;
			return;
		}
		bool flag = Stage.instance;
		if (isLobby)
		{
			flag = true;
		}
		if (flag)
		{
			int i = 0;
			for (int count = CameraRigController.readOnlyInstancesList.Count; i < count; i++)
			{
				if (CameraRigController.readOnlyInstancesList[i].suppressPlayerCameras)
				{
					flag = false;
					return;
				}
			}
		}
		if (flag)
		{
			int num = 0;
			ReadOnlyCollection<NetworkUser> readOnlyLocalPlayersList = NetworkUser.readOnlyLocalPlayersList;
			for (int j = 0; j < readOnlyLocalPlayersList.Count; j++)
			{
				NetworkUser networkUser = readOnlyLocalPlayersList[j];
				LocalUser localUser = networkUser?.localUser ?? null;
				if (localUser == null || localUser.inputPlayer == null)
				{
					continue;
				}
				CameraRigController cameraRigController = null;
				if (!cameras.ContainsKey(localUser.inputPlayer.name) || cameras[localUser.inputPlayer.name] == null)
				{
					if (isLobby && localUser.inputPlayer.name == "PlayerMain")
					{
						cameraRigController = MainCamera.GetComponent<CameraRigController>();
						cameras[localUser.inputPlayer.name] = cameraRigController;
						cameraRigController.sceneCam.cullingMask |= 1 << LayerMask.NameToLayer("Splitscreen" + localUser.inputPlayer.name);
					}
					else if (isLobby && isLocalMultiplayer)
					{
						if (!RoR2Application.IsAllUsersEntitlementsUpdated)
						{
							continue;
						}
						cameraRigController = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Main Camera")).GetComponent<CameraRigController>();
						cameras[localUser.inputPlayer.name] = cameraRigController;
						if (cameraRigController.sceneCam.GetComponent<AkAudioListener>() != null)
						{
							Object.Destroy(cameraRigController.sceneCam.GetComponent<AkAudioListener>());
						}
						cameraRigController.CloneCameraContext(MainCamera.GetComponent<CameraRigController>());
						Vector3 zero = Vector3.zero;
						zero.x = SplitscreenCameraOffset.x * (float)j;
						zero.y = SplitscreenCameraOffset.y * (float)j;
						zero.z = SplitscreenCameraOffset.z * (float)j;
						cameraRigController.sceneCam.transform.localPosition = zero;
						cameraRigController.sceneCam.cullingMask |= 1 << LayerMask.NameToLayer("Splitscreen" + localUser.inputPlayer.name);
					}
					else if (!isLobby)
					{
						cameraRigController = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Main Camera")).GetComponent<CameraRigController>();
						cameras[localUser.inputPlayer.name] = cameraRigController;
					}
				}
				else
				{
					cameraRigController = cameras[localUser.inputPlayer.name];
					cameraRigController.enabled = true;
				}
				if (!isLobby)
				{
					cameraRigController.viewer = networkUser;
					networkUser.cameraRigController = cameraRigController;
					GameObject networkUserBodyObject = GetNetworkUserBodyObject(networkUser);
					ForceSpectate forceSpectate = InstanceTracker.FirstOrNull<ForceSpectate>();
					if ((bool)forceSpectate)
					{
						cameraRigController.nextTarget = forceSpectate.target;
						cameraRigController.cameraMode = CameraModePlayerBasic.spectator;
					}
					else if ((bool)networkUserBodyObject)
					{
						cameraRigController.nextTarget = networkUserBodyObject;
						cameraRigController.cameraMode = CameraModePlayerBasic.playerBasic;
					}
					else if (!cameraRigController.disableSpectating)
					{
						cameraRigController.cameraMode = CameraModePlayerBasic.spectator;
						if (!cameraRigController.target)
						{
							cameraRigController.nextTarget = CameraRigControllerSpectateControls.GetNextSpectateGameObject(networkUser, null);
						}
					}
					else
					{
						cameraRigController.cameraMode = CameraModeNone.instance;
					}
				}
				num++;
			}
			if (isLobby)
			{
				if (popOutContainer == null)
				{
					popOutContainer = GameObject.Find("PopoutPanelContainer");
				}
				else
				{
					RectTransform component = popOutContainer.GetComponent<RectTransform>();
					if (1 < readOnlyLocalPlayersList.Count && readOnlyLocalPlayersList.Count < 4 && component != null)
					{
						component.localScale = new Vector3(popOutScaleValue, popOutScaleValue, popOutScaleValue);
					}
					else if (component != null)
					{
						component.localScale = new Vector3(1f, 1f, 1f);
					}
				}
				for (int k = 0; k < readOnlyLocalPlayersList.Count; k++)
				{
					LocalUser localUser2 = readOnlyLocalPlayersList[k]?.localUser ?? null;
					if (localUser2 == null)
					{
						continue;
					}
					if (!canvases.ContainsKey(localUser2.inputPlayer.name) || canvases[localUser2.inputPlayer.name] == null)
					{
						GameObject gameObject = null;
						if (localUser2.inputPlayer.name == "PlayerMain")
						{
							gameObject = Object.Instantiate(CharacterSelectUIMain);
						}
						else if (localUser2.inputPlayer.name == "Player2" && isLocalMultiplayer)
						{
							gameObject = Object.Instantiate(CharacterSelectUILocal);
							gameObject.GetComponent<MPEventSystemProvider>().fallBackToPlayer2EventSystem = true;
						}
						else if (localUser2.inputPlayer.name == "Player3" && isLocalMultiplayer)
						{
							gameObject = Object.Instantiate(CharacterSelectUILocal);
							gameObject.GetComponent<MPEventSystemProvider>().fallBackToPlayer3EventSystem = true;
						}
						else if (localUser2.inputPlayer.name == "Player4" && isLocalMultiplayer)
						{
							gameObject = Object.Instantiate(CharacterSelectUILocal);
							gameObject.GetComponent<MPEventSystemProvider>().fallBackToPlayer4EventSystem = true;
						}
						if (gameObject != null)
						{
							VoteInfoPanelController componentInChildren = gameObject.GetComponentInChildren<VoteInfoPanelController>();
							if (componentInChildren != null)
							{
								componentInChildren.voteController = PreGameController.GetComponent<VoteController>();
							}
						}
						Canvas component2 = gameObject.GetComponent<Canvas>();
						component2.renderMode = RenderMode.ScreenSpaceCamera;
						component2.planeDistance = planeDistanceMultiplier * (k + 1);
						component2.worldCamera = cameras[localUser2.inputPlayer.name].uiCam;
						canvases[localUser2.inputPlayer.name] = component2;
					}
					else if (!canvases[localUser2.inputPlayer.name].enabled)
					{
						RefreshCharacterSelectUI(localUser2.inputPlayer.name);
						canvases[localUser2.inputPlayer.name].enabled = true;
					}
				}
			}
			int num2 = num;
			Rect[] array = ScreenLayouts[num2];
			activeCameras.Clear();
			for (int l = 0; l < num2; l++)
			{
				CameraRigController cameraRigController2 = cameras[readOnlyLocalPlayersList[l].localUser.inputPlayer.name];
				activeCameras.Add(cameraRigController2);
				if (cameraRigController2.enabled)
				{
					cameraRigController2.viewport = array[l];
				}
			}
			return;
		}
		foreach (string key in cameras.Keys)
		{
			if ((bool)cameras[key])
			{
				Object.Destroy(cameras[key].gameObject);
				cameras.Remove(key);
			}
		}
	}

	public void RefreshAllCharacterSelectUI()
	{
		if (!isLobby)
		{
			return;
		}
		foreach (string key in canvases.Keys)
		{
			RefreshCharacterSelectUI(key);
		}
	}

	private void RefreshCharacterSelectUI(string playerName)
	{
		if ((bool)canvases[playerName])
		{
			canvases[playerName].gameObject.GetComponent<CharacterSelectController>()?.RebuildOptions();
		}
	}

	public void OnUserRemoved(NetworkUser user)
	{
	}
}
