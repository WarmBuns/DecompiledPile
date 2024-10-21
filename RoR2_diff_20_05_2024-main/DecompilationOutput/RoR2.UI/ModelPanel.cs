using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HG;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RawImage))]
[RequireComponent(typeof(RectTransform))]
public class ModelPanel : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IScrollHandler, IEndDragHandler
{
	private class CameraFramingCalculator
	{
		private GameObject modelInstance;

		private Transform root;

		private readonly List<Transform> boneList = new List<Transform>();

		private HurtBoxGroup hurtBoxGroup;

		private HurtBox[] hurtBoxes = Array.Empty<HurtBox>();

		public Vector3 outputPivotPoint;

		public Vector3 outputCameraPosition;

		public float outputMinDistance;

		public float outputMaxDistance;

		public Quaternion outputCameraRotation;

		private static void GenerateBoneList(Transform rootBone, List<Transform> boneList)
		{
			boneList.AddRange(rootBone.gameObject.GetComponentsInChildren<Transform>());
		}

		public CameraFramingCalculator(GameObject modelInstance)
		{
			this.modelInstance = modelInstance;
			root = modelInstance.transform;
			GenerateBoneList(root, boneList);
			hurtBoxGroup = modelInstance.GetComponent<HurtBoxGroup>();
			if ((bool)hurtBoxGroup)
			{
				hurtBoxes = hurtBoxGroup.hurtBoxes;
			}
		}

		private bool FindBestEyePoint(out Vector3 result, out float approximateEyeRadius)
		{
			approximateEyeRadius = 1f;
			IEnumerable<Transform> source = boneList.Where(FirstChoice);
			if (!source.Any())
			{
				source = boneList.Where(SecondChoice);
			}
			Vector3[] array = source.Select((Transform bone) => bone.position).ToArray();
			result = Vector3Utils.AveragePrecise(array);
			for (int i = 0; i < array.Length; i++)
			{
				float magnitude = (array[i] - result).magnitude;
				if (magnitude > approximateEyeRadius)
				{
					approximateEyeRadius = magnitude;
				}
			}
			return array.Length != 0;
			static bool FirstChoice(Transform bone)
			{
				if ((bool)bone.GetComponent<SkinnedMeshRenderer>())
				{
					return false;
				}
				string name = bone.name;
				if (name.Equals("eye", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (name.Equals("eyeball.1", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				return false;
			}
			static bool SecondChoice(Transform bone)
			{
				if ((bool)bone.GetComponent<SkinnedMeshRenderer>())
				{
					return false;
				}
				return bone.name.ToLower().Contains("eye");
			}
		}

		private bool FindBestHeadPoint(string searchName, out Vector3 result, out float approximateRadius)
		{
			Transform[] array = boneList.Where((Transform bone) => string.Equals(bone.name, searchName, StringComparison.OrdinalIgnoreCase)).ToArray();
			if (array.Length == 0)
			{
				array = boneList.Where((Transform bone) => bone.name.ToLower(CultureInfo.InvariantCulture).Contains(searchName)).ToArray();
			}
			if (array.Length != 0)
			{
				foreach (Transform bone2 in array)
				{
					if (TryCalcBoneBounds(bone2, 0.2f, out var bounds, out approximateRadius))
					{
						result = bounds.center;
						return true;
					}
				}
			}
			result = Vector3.zero;
			approximateRadius = 0f;
			return false;
		}

		private static float CalcMagnitudeToFrameSphere(float sphereRadius, float fieldOfView)
		{
			float num = fieldOfView * 0.5f;
			float num2 = 90f;
			return Mathf.Tan((180f - num2 - num) * (MathF.PI / 180f)) * sphereRadius;
		}

		private bool FindBestCenterOfMass(out Vector3 result, out float approximateRadius)
		{
			_ = from bone in boneList
				select bone.GetComponent<HurtBox>() into hurtBox
				where hurtBox
				select hurtBox;
			if ((bool)hurtBoxGroup && (bool)hurtBoxGroup.mainHurtBox)
			{
				result = hurtBoxGroup.mainHurtBox.transform.position;
				approximateRadius = Util.SphereVolumeToRadius(hurtBoxGroup.mainHurtBox.volume);
				return true;
			}
			result = Vector3.zero;
			approximateRadius = 1f;
			return false;
		}

		private static float GetWeightForBone(ref BoneWeight boneWeight, int boneIndex)
		{
			if (boneWeight.boneIndex0 == boneIndex)
			{
				return boneWeight.weight0;
			}
			if (boneWeight.boneIndex1 == boneIndex)
			{
				return boneWeight.weight1;
			}
			if (boneWeight.boneIndex2 == boneIndex)
			{
				return boneWeight.weight2;
			}
			if (boneWeight.boneIndex3 == boneIndex)
			{
				return boneWeight.weight3;
			}
			return 0f;
		}

		private static int FindBoneIndex(SkinnedMeshRenderer _skinnedMeshRenderer, Transform _bone)
		{
			Transform[] bones = _skinnedMeshRenderer.bones;
			for (int i = 0; i < bones.Length; i++)
			{
				if (bones[i] == _bone)
				{
					return i;
				}
			}
			return -1;
		}

		private bool TryCalcBoneBounds(Transform bone, float weightThreshold, out Bounds bounds, out float approximateRadius)
		{
			SkinnedMeshRenderer[] componentsInChildren = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
			SkinnedMeshRenderer skinnedMeshRenderer = null;
			Mesh mesh = null;
			int num = -1;
			List<int> list = new List<int>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				skinnedMeshRenderer = componentsInChildren[i];
				mesh = skinnedMeshRenderer.sharedMesh;
				if (!mesh)
				{
					continue;
				}
				num = FindBoneIndex(skinnedMeshRenderer, bone);
				if (num != -1)
				{
					BoneWeight[] boneWeights = mesh.boneWeights;
					for (int j = 0; j < boneWeights.Length; j++)
					{
						if (GetWeightForBone(ref boneWeights[j], num) > weightThreshold)
						{
							list.Add(j);
						}
					}
					if (list.Count == 0)
					{
						num = -1;
					}
				}
				if (num != -1)
				{
					break;
				}
			}
			if (num == -1)
			{
				bounds = default(Bounds);
				approximateRadius = 0f;
				return false;
			}
			Mesh mesh2 = new Mesh();
			skinnedMeshRenderer.BakeMesh(mesh2);
			Vector3[] vertices = mesh2.vertices;
			UnityEngine.Object.Destroy(mesh2);
			if (mesh2.vertexCount != mesh.vertexCount)
			{
				Debug.LogWarningFormat("Baked mesh vertex count differs from the original mesh vertex count! baked={0} original={1}", mesh2.vertexCount, mesh.vertexCount);
				vertices = mesh.vertices;
			}
			Vector3[] array = new Vector3[list.Count];
			Transform transform = skinnedMeshRenderer.transform;
			Vector3 position = transform.position;
			Quaternion rotation = transform.rotation;
			for (int k = 0; k < list.Count; k++)
			{
				int num2 = list[k];
				Vector3 vector = vertices[num2];
				Vector3 vector2 = position + rotation * vector;
				array[k] = vector2;
			}
			bounds = new Bounds(Vector3Utils.AveragePrecise(array), Vector3.zero);
			float num3 = 0f;
			for (int l = 0; l < array.Length; l++)
			{
				bounds.Encapsulate(array[l]);
				float num4 = Vector3.Distance(bounds.center, array[l]);
				if (num4 > num3)
				{
					num3 = num4;
				}
			}
			approximateRadius = num3;
			return true;
		}

		public void GetCharacterThumbnailPosition(float fov)
		{
			ModelPanelParameters component = modelInstance.GetComponent<ModelPanelParameters>();
			if ((bool)component)
			{
				if ((bool)component.focusPointTransform)
				{
					outputPivotPoint = component.focusPointTransform.position;
				}
				if ((bool)component.cameraPositionTransform)
				{
					outputCameraPosition = component.cameraPositionTransform.position;
				}
				outputCameraRotation = Util.QuaternionSafeLookRotation(component.cameraDirection);
				outputMinDistance = component.minDistance;
				outputMaxDistance = component.maxDistance;
				return;
			}
			Vector3 result = Vector3.zero;
			float approximateRadius = 1f;
			bool flag = FindBestHeadPoint("head", out result, out approximateRadius);
			if (!flag)
			{
				flag = FindBestHeadPoint("chest", out result, out approximateRadius);
			}
			bool flag2 = false;
			bool flag3 = false;
			float num = 1f;
			float approximateEyeRadius = 1f;
			flag2 = FindBestEyePoint(out var result2, out approximateEyeRadius);
			if (!flag)
			{
				approximateRadius = approximateEyeRadius;
			}
			if (flag2)
			{
				result = result2;
			}
			if (!flag && !flag2)
			{
				flag3 = FindBestCenterOfMass(out result, out approximateRadius);
			}
			float num2 = 1f;
			if (Util.GuessRenderBoundsMeshOnly(modelInstance, out var bounds))
			{
				if (flag3)
				{
					approximateRadius = Util.SphereVolumeToRadius(bounds.size.x * bounds.size.y * bounds.size.z);
				}
				Mathf.Max((result.y - bounds.min.y) / bounds.size.y - 0.5f - 0.2f, 0f);
				_ = bounds.center;
				num2 = bounds.size.z / bounds.size.x;
				outputMinDistance = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) * 1f;
				outputMaxDistance = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 2f;
			}
			Vector3 vector = -root.forward;
			for (int i = 0; i < boneList.Count; i++)
			{
				if (boneList[i].name.Equals("muzzle", StringComparison.OrdinalIgnoreCase))
				{
					Vector3 vector2 = root.position - boneList[i].position;
					vector2.y = 0f;
					float magnitude = vector2.magnitude;
					if (magnitude > 0.2f)
					{
						vector2 /= magnitude;
						vector = vector2;
						break;
					}
				}
			}
			vector = Quaternion.Euler(0f, 57.29578f * Mathf.Atan(num2 - 1f) * 1f, 0f) * vector;
			Vector3 vector3 = -vector * (CalcMagnitudeToFrameSphere(approximateRadius, fov) + num);
			Vector3 vector4 = result + vector3;
			outputPivotPoint = result;
			outputCameraPosition = vector4;
			outputCameraRotation = Util.QuaternionSafeLookRotation(result - vector4);
		}
	}

	private GameObject _modelPrefab;

	public GameObject modelPostProcessVolumePrefab;

	public Vector3 modelPostProcessVolumePosition = Vector3.zero;

	public RenderSettingsState renderSettings;

	public Color camBackgroundColor = Color.clear;

	public bool disablePostProcessLayer = true;

	public bool useUnscaledTime;

	private static int isGroundedParamHash = Animator.StringToHash("isGrounded");

	private static int aimPitchCycleParamHash = Animator.StringToHash("aimPitchCycle");

	private static int aimYawCycleParamHash = Animator.StringToHash("aimYawCycle");

	private static int IdleStateHash = Animator.StringToHash("Idle");

	private RectTransform rectTransform;

	private RawImage rawImage;

	private GameObject modelInstance;

	private CameraRigController cameraRigController;

	private ModelCamera modelCamera;

	public GameObject headlightPrefab;

	public GameObject[] lightPrefabs;

	private GameObject modelPostProcessVolumeInstance;

	private Vector3 postProcessVolumeLocation;

	private Light headlight;

	public float fov = 60f;

	public bool enableGamepadControls;

	public float gamepadZoomSensitivity;

	public float gamepadRotateSensitivity;

	public UILayerKey requiredTopLayer;

	private MPEventSystemLocator mpEventSystemLocator;

	private float zoom = 0.5f;

	private float desiredZoom = 0.5f;

	private float zoomVelocity;

	private float minDistance = 0.5f;

	private float maxDistance = 10f;

	private float orbitPitch;

	private float orbitYaw = 180f;

	private Vector3 orbitalVelocity = Vector3.zero;

	private Vector3 orbitalVelocitySmoothDampVelocity = Vector3.zero;

	private Vector2 pan;

	private Vector2 panVelocity;

	private Vector2 panVelocitySmoothDampVelocity;

	private Vector3 pivotPoint = Vector3.zero;

	private List<Light> lights = new List<Light>();

	private Vector2 orbitDragPoint;

	private Vector2 panDragPoint;

	private int orbitDragCount;

	private int panDragCount;

	public GameObject modelPrefab
	{
		get
		{
			return _modelPrefab;
		}
		set
		{
			if (!(_modelPrefab == value))
			{
				DestroyModelInstance();
				_modelPrefab = value;
				BuildModelInstance();
			}
		}
	}

	public RenderTexture renderTexture { get; private set; }

	private void DestroyModelInstance()
	{
		UnityEngine.Object.Destroy(modelInstance);
		modelInstance = null;
	}

	private void BuildModelInstance()
	{
		if (!_modelPrefab || !base.enabled || (bool)modelInstance)
		{
			return;
		}
		modelInstance = UnityEngine.Object.Instantiate(_modelPrefab, modelPostProcessVolumePosition, Quaternion.identity);
		ModelPanelParameters component = _modelPrefab.GetComponent<ModelPanelParameters>();
		if ((bool)component)
		{
			modelInstance.transform.rotation = component.modelRotation;
		}
		Util.GuessRenderBoundsMeshOnly(modelInstance, out var bounds);
		pivotPoint = bounds.center;
		minDistance = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) * 1f;
		maxDistance = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 2f;
		Renderer[] componentsInChildren = modelInstance.GetComponentsInChildren<Renderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = LayerIndex.noDraw.intVal;
		}
		AimAnimator[] componentsInChildren2 = modelInstance.GetComponentsInChildren<AimAnimator>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].inputBank = null;
			componentsInChildren2[j].directionComponent = null;
			componentsInChildren2[j].enabled = false;
		}
		Animator[] componentsInChildren3 = modelInstance.GetComponentsInChildren<Animator>();
		foreach (Animator obj in componentsInChildren3)
		{
			obj.SetBool(isGroundedParamHash, value: true);
			obj.SetFloat(aimPitchCycleParamHash, 0.5f);
			obj.SetFloat(aimYawCycleParamHash, 0.5f);
			obj.Play(IdleStateHash);
			obj.Update(0f);
			obj.updateMode = AnimatorUpdateMode.UnscaledTime;
		}
		IKSimpleChain[] componentsInChildren4 = modelInstance.GetComponentsInChildren<IKSimpleChain>();
		for (int l = 0; l < componentsInChildren4.Length; l++)
		{
			componentsInChildren4[l].enabled = false;
		}
		DitherModel[] componentsInChildren5 = modelInstance.GetComponentsInChildren<DitherModel>();
		for (int m = 0; m < componentsInChildren5.Length; m++)
		{
			componentsInChildren5[m].enabled = false;
		}
		PrintController[] componentsInChildren6 = modelInstance.GetComponentsInChildren<PrintController>();
		for (int m = 0; m < componentsInChildren6.Length; m++)
		{
			componentsInChildren6[m].enabled = false;
		}
		LightIntensityCurve[] componentsInChildren7 = modelInstance.GetComponentsInChildren<LightIntensityCurve>();
		foreach (LightIntensityCurve lightIntensityCurve in componentsInChildren7)
		{
			if (!lightIntensityCurve.loop)
			{
				lightIntensityCurve.enabled = false;
			}
		}
		AkEvent[] componentsInChildren8 = modelInstance.GetComponentsInChildren<AkEvent>();
		for (int m = 0; m < componentsInChildren8.Length; m++)
		{
			componentsInChildren8[m].enabled = false;
		}
		TemporaryOverlay[] componentsInChildren9 = modelInstance.GetComponentsInChildren<TemporaryOverlay>();
		for (int m = 0; m < componentsInChildren9.Length; m++)
		{
			UnityEngine.Object.Destroy(componentsInChildren9[m]);
		}
		ParticleSystem[] componentsInChildren10 = modelInstance.GetComponentsInChildren<ParticleSystem>();
		for (int m = 0; m < componentsInChildren10.Length; m++)
		{
			ParticleSystem.MainModule main = componentsInChildren10[m].main;
			main.useUnscaledTime = true;
		}
		desiredZoom = 0.5f;
		zoom = desiredZoom;
		zoomVelocity = 0f;
		ResetOrbitAndPan();
	}

	private void ResetOrbitAndPan()
	{
		orbitPitch = 0f;
		orbitYaw = 0f;
		orbitalVelocity = Vector3.zero;
		orbitalVelocitySmoothDampVelocity = Vector3.zero;
		pan = Vector2.zero;
		panVelocity = Vector2.zero;
		panVelocitySmoothDampVelocity = Vector2.zero;
	}

	private void Awake()
	{
		modelPostProcessVolumeInstance = UnityEngine.Object.Instantiate(modelPostProcessVolumePrefab, modelPostProcessVolumePosition, Quaternion.identity);
		rectTransform = GetComponent<RectTransform>();
		rawImage = GetComponent<RawImage>();
		mpEventSystemLocator = GetComponent<MPEventSystemLocator>();
		cameraRigController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Main Camera")).GetComponent<CameraRigController>();
		cameraRigController.gameObject.name = "ModelCamera";
		cameraRigController.uiCam.gameObject.SetActive(value: false);
		cameraRigController.createHud = false;
		cameraRigController.enableFading = false;
		cameraRigController.enableMusic = false;
		GameObject gameObject = cameraRigController.sceneCam.gameObject;
		modelCamera = gameObject.AddComponent<ModelCamera>();
		cameraRigController.transform.position = modelPostProcessVolumeInstance.transform.position - Vector3.forward * 10f;
		cameraRigController.transform.forward = Vector3.forward;
		CameraResolutionScaler component = gameObject.GetComponent<CameraResolutionScaler>();
		if ((bool)component)
		{
			component.enabled = false;
		}
		Camera sceneCam = cameraRigController.sceneCam;
		sceneCam.backgroundColor = Color.clear;
		sceneCam.clearFlags = CameraClearFlags.Color;
		if (disablePostProcessLayer)
		{
			PostProcessLayer component2 = sceneCam.GetComponent<PostProcessLayer>();
			if ((bool)component2)
			{
				component2.enabled = false;
			}
		}
		Vector3 eulerAngles = cameraRigController.transform.eulerAngles;
		orbitPitch = eulerAngles.x;
		orbitYaw = eulerAngles.y;
		modelCamera.attachedCamera.backgroundColor = camBackgroundColor;
		modelCamera.attachedCamera.clearFlags = CameraClearFlags.Color;
		modelCamera.attachedCamera.cullingMask = LayerIndex.manualRender.mask;
		if ((bool)headlightPrefab)
		{
			headlight = UnityEngine.Object.Instantiate(headlightPrefab, modelCamera.transform).GetComponent<Light>();
			if ((bool)headlight)
			{
				headlight.gameObject.SetActive(value: true);
				modelCamera.AddLight(headlight);
			}
		}
		for (int i = 0; i < lightPrefabs.Length; i++)
		{
			GameObject obj = UnityEngine.Object.Instantiate(lightPrefabs[i]);
			Light component3 = obj.GetComponent<Light>();
			obj.SetActive(value: true);
			lights.Add(component3);
			modelCamera.AddLight(component3);
		}
	}

	public void Start()
	{
		BuildRenderTexture();
		desiredZoom = 0.5f;
		zoom = desiredZoom;
		zoomVelocity = 0f;
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(renderTexture);
		if ((bool)modelPostProcessVolumeInstance)
		{
			UnityEngine.Object.Destroy(modelPostProcessVolumeInstance);
		}
		if ((bool)cameraRigController)
		{
			UnityEngine.Object.Destroy(cameraRigController.gameObject);
		}
		foreach (Light light in lights)
		{
			UnityEngine.Object.Destroy(light.gameObject);
		}
	}

	private void OnDisable()
	{
		DestroyModelInstance();
	}

	private void OnEnable()
	{
		BuildModelInstance();
	}

	public void Update()
	{
		UpdateForModelViewer(Time.unscaledDeltaTime);
		if (enableGamepadControls && (!requiredTopLayer || requiredTopLayer.representsTopLayer) && mpEventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad && (bool)mpEventSystemLocator.eventSystem)
		{
			float axis = mpEventSystemLocator.eventSystem.player.GetAxis(16);
			float axis2 = mpEventSystemLocator.eventSystem.player.GetAxis(17);
			bool button = mpEventSystemLocator.eventSystem.player.GetButton(29);
			bool button2 = mpEventSystemLocator.eventSystem.player.GetButton(30);
			Vector3 zero = Vector3.zero;
			zero.y = (0f - axis) * gamepadRotateSensitivity;
			zero.x = axis2 * gamepadRotateSensitivity;
			orbitalVelocity = zero;
			if (button != button2)
			{
				desiredZoom = Mathf.Clamp01(desiredZoom + (button ? 0.1f : (-0.1f)) * Time.deltaTime * gamepadZoomSensitivity);
			}
		}
	}

	public void LateUpdate()
	{
		modelCamera.attachedCamera.aspect = (float)renderTexture.width / (float)renderTexture.height;
		cameraRigController.baseFov = fov;
		modelCamera.renderSettings = renderSettings;
		modelCamera.RenderItem(modelInstance, renderTexture);
	}

	private void OnRectTransformDimensionsChange()
	{
		BuildRenderTexture();
	}

	private void BuildRenderTexture()
	{
		if (!rectTransform)
		{
			return;
		}
		Vector3[] fourCornersArray = new Vector3[4];
		rectTransform.GetLocalCorners(fourCornersArray);
		Vector2 size = rectTransform.rect.size;
		int num = Mathf.FloorToInt(size.x);
		int num2 = Mathf.FloorToInt(size.y);
		if (!renderTexture || renderTexture.width != num || renderTexture.height != num2)
		{
			UnityEngine.Object.Destroy(renderTexture);
			renderTexture = null;
			if (num > 0 && num2 > 0)
			{
				RenderTextureDescriptor desc = new RenderTextureDescriptor(num, num2, RenderTextureFormat.ARGB32);
				desc.sRGB = true;
				renderTexture = new RenderTexture(desc);
				renderTexture.useMipMap = false;
				renderTexture.filterMode = FilterMode.Bilinear;
			}
			rawImage.texture = renderTexture;
		}
	}

	private void UpdateForModelViewer(float deltaTime)
	{
		zoom = Mathf.SmoothDamp(zoom, desiredZoom, ref zoomVelocity, 0.1f, 10f, deltaTime);
		orbitPitch %= 360f;
		if (orbitPitch < -180f)
		{
			orbitPitch += 360f;
		}
		else if (orbitPitch > 180f)
		{
			orbitPitch -= 360f;
		}
		orbitPitch = Mathf.Clamp(orbitPitch + orbitalVelocity.x * deltaTime, -89f, 89f);
		orbitYaw += orbitalVelocity.y * deltaTime;
		orbitalVelocity = Vector3.SmoothDamp(orbitalVelocity, Vector3.zero, ref orbitalVelocitySmoothDampVelocity, 0.25f, 2880f, deltaTime);
		if (orbitDragCount > 0)
		{
			orbitalVelocity = Vector3.zero;
			orbitalVelocitySmoothDampVelocity = Vector3.zero;
		}
		pan += panVelocity * deltaTime;
		panVelocity = Vector2.SmoothDamp(panVelocity, Vector2.zero, ref panVelocitySmoothDampVelocity, 0.25f, 100f, deltaTime);
		if (panDragCount > 0)
		{
			panVelocity = Vector2.zero;
			panVelocitySmoothDampVelocity = Vector2.zero;
		}
		Quaternion quaternion = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
		cameraRigController.transform.forward = quaternion * Vector3.forward;
		Vector3 forward = cameraRigController.transform.forward;
		Vector3 position = pivotPoint + forward * (0f - Mathf.LerpUnclamped(minDistance, maxDistance, zoom)) + cameraRigController.transform.up * pan.y + cameraRigController.transform.right * pan.x;
		cameraRigController.transform.position = position;
	}

	public void SetAnglesForCharacterThumbnailForSeconds(float time, bool setZoom = false)
	{
		SetAnglesForCharacterThumbnail(setZoom);
		float t = time;
		Action func = null;
		func = delegate
		{
			t -= (useUnscaledTime ? Time.fixedUnscaledDeltaTime : Time.deltaTime);
			if ((bool)this)
			{
				SetAnglesForCharacterThumbnail(setZoom);
			}
			if (t <= 0f)
			{
				RoR2Application.onUpdate -= func;
			}
		};
		RoR2Application.onUpdate += func;
	}

	public void SetAnglesForCharacterThumbnail(bool setZoom = false)
	{
		if ((bool)modelInstance)
		{
			CameraFramingCalculator cameraFramingCalculator = new CameraFramingCalculator(modelInstance);
			cameraFramingCalculator.GetCharacterThumbnailPosition(fov);
			pivotPoint = cameraFramingCalculator.outputPivotPoint;
			minDistance = cameraFramingCalculator.outputMinDistance;
			maxDistance = cameraFramingCalculator.outputMaxDistance;
			ResetOrbitAndPan();
			Vector3 eulerAngles = cameraFramingCalculator.outputCameraRotation.eulerAngles;
			orbitPitch = eulerAngles.x;
			orbitYaw = eulerAngles.y;
			if (setZoom)
			{
				zoom = Util.Remap(Vector3.Distance(cameraFramingCalculator.outputCameraPosition, cameraFramingCalculator.outputPivotPoint), minDistance, maxDistance, 0f, 1f);
				desiredZoom = zoom;
			}
			zoomVelocity = 0f;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			orbitDragCount++;
			if (orbitDragCount == 1)
			{
				orbitDragPoint = eventData.pressPosition;
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			panDragCount++;
			if (panDragCount == 1)
			{
				panDragPoint = eventData.pressPosition;
			}
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			orbitDragCount--;
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			panDragCount--;
		}
		OnDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			Vector2 vector = eventData.position - orbitDragPoint;
			orbitDragPoint = eventData.position;
			float num = 0.5f / unscaledDeltaTime;
			orbitalVelocity = new Vector3((0f - vector.y) * num * 0.5f, vector.x * num, 0f);
		}
		else
		{
			Vector2 vector2 = eventData.position - panDragPoint;
			panDragPoint = eventData.position;
			float num2 = -0.01f;
			panVelocity = vector2 * num2 / unscaledDeltaTime;
		}
	}

	public void OnScroll(PointerEventData eventData)
	{
		desiredZoom = Mathf.Clamp01(desiredZoom + eventData.scrollDelta.y * -0.05f);
	}
}
