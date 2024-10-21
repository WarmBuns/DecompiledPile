using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.UI;
using RoR2.WwiseUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace RoR2;

public class CharacterModel : MonoBehaviour
{
	[Serializable]
	public struct RendererInfo : IEquatable<RendererInfo>
	{
		[PrefabReference]
		public Renderer renderer;

		public Material defaultMaterial;

		public ShadowCastingMode defaultShadowCastingMode;

		public bool ignoreOverlays;

		public bool hideOnDeath;

		public bool Equals(RendererInfo other)
		{
			if ((object)renderer == other.renderer && (object)defaultMaterial == other.defaultMaterial && object.Equals(defaultShadowCastingMode, other.defaultShadowCastingMode) && object.Equals(ignoreOverlays, other.ignoreOverlays))
			{
				return object.Equals(hideOnDeath, other.hideOnDeath);
			}
			return false;
		}
	}

	[Serializable]
	public struct LightInfo
	{
		public Light light;

		public Color defaultColor;

		public LightInfo(Light light)
		{
			this.light = light;
			defaultColor = light.color;
		}
	}

	private struct HurtBoxInfo
	{
		public readonly Transform transform;

		public readonly float estimatedRadius;

		public HurtBoxInfo(HurtBox hurtBox)
		{
			transform = hurtBox.transform;
			estimatedRadius = Util.SphereVolumeToRadius(hurtBox.volume);
		}
	}

	private struct ParentedPrefabDisplay
	{
		public ItemIndex itemIndex;

		public EquipmentIndex equipmentIndex;

		public GameObject instance { get; private set; }

		public ItemDisplay itemDisplay { get; private set; }

		public void Apply(CharacterModel characterModel, GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			instance = UnityEngine.Object.Instantiate(prefab.gameObject, parent);
			instance.transform.localPosition = localPosition;
			instance.transform.localRotation = localRotation;
			instance.transform.localScale = localScale;
			LimbMatcher component = instance.GetComponent<LimbMatcher>();
			if ((bool)component && (bool)characterModel.childLocator)
			{
				component.SetChildLocator(characterModel.childLocator);
			}
			itemDisplay = instance.GetComponent<ItemDisplay>();
		}

		public void Undo()
		{
			if ((bool)instance)
			{
				UnityEngine.Object.Destroy(instance);
				instance = null;
			}
		}
	}

	private struct LimbMaskDisplay
	{
		public ItemIndex itemIndex;

		public EquipmentIndex equipmentIndex;

		public LimbFlags maskValue;

		public void Apply(CharacterModel characterModel, LimbFlags mask)
		{
			maskValue = mask;
			characterModel.limbFlagSet.AddFlags(mask);
		}

		public void Undo(CharacterModel characterModel)
		{
			characterModel.limbFlagSet.RemoveFlags(maskValue);
		}
	}

	[Serializable]
	private class LimbFlagSet
	{
		private readonly byte[] flagCounts = new byte[5];

		private LimbFlags flags;

		private static readonly int[] primeConversionTable;

		public int materialMaskValue { get; private set; }

		public LimbFlagSet()
		{
			materialMaskValue = 1;
		}

		static LimbFlagSet()
		{
			int[] array = new int[5] { 2, 3, 5, 11, 17 };
			primeConversionTable = new int[31];
			for (int i = 0; i < primeConversionTable.Length; i++)
			{
				int num = 1;
				for (int j = 0; j < 5; j++)
				{
					if ((i & (1 << j)) != 0)
					{
						num *= array[j];
					}
				}
				primeConversionTable[i] = num;
			}
		}

		private static int ConvertLimbFlagsToMaterialMask(LimbFlags limbFlags)
		{
			return primeConversionTable[(int)limbFlags];
		}

		public void AddFlags(LimbFlags addedFlags)
		{
			LimbFlags limbFlags = flags;
			flags |= addedFlags;
			for (int i = 0; i < 5; i++)
			{
				if (((uint)addedFlags & (uint)(1 << i)) != 0)
				{
					flagCounts[i]++;
				}
			}
			if (flags != limbFlags)
			{
				materialMaskValue = ConvertLimbFlagsToMaterialMask(flags);
			}
		}

		public void RemoveFlags(LimbFlags removedFlags)
		{
			LimbFlags limbFlags = flags;
			for (int i = 0; i < 5; i++)
			{
				if (((uint)removedFlags & (uint)(1 << i)) != 0 && --flagCounts[i] == 0)
				{
					flags &= (LimbFlags)(~(1 << i));
				}
			}
			if (flags != limbFlags)
			{
				materialMaskValue = ConvertLimbFlagsToMaterialMask(flags);
			}
		}
	}

	public CharacterBody body;

	public ItemDisplayRuleSet itemDisplayRuleSet;

	public bool autoPopulateLightInfos = true;

	public List<Transform> gameObjectActivationTransforms = new List<Transform>();

	[FormerlySerializedAs("rendererInfos")]
	public RendererInfo[] baseRendererInfos = Array.Empty<RendererInfo>();

	public LightInfo[] baseLightInfos = Array.Empty<LightInfo>();

	private ChildLocator childLocator;

	private GameObject goldAffixEffect;

	private HurtBoxInfo[] hurtBoxInfos = Array.Empty<HurtBoxInfo>();

	private Transform coreTransform;

	private static readonly Color hitFlashBaseColor;

	private static readonly Color hitFlashShieldColor;

	private static readonly Color healFlashColor;

	private static readonly float hitFlashDuration;

	private static readonly float healFlashDuration;

	private VisibilityLevel _visibility = VisibilityLevel.Visible;

	private bool _isGhost;

	private bool _isDoppelganger;

	private bool _isEcho;

	[HideInInspector]
	[NonSerialized]
	public int invisibilityCount;

	[NonSerialized]
	public List<TemporaryOverlayInstance> temporaryOverlays = new List<TemporaryOverlayInstance>();

	private bool materialsDirty = true;

	private MaterialPropertyBlock propertyStorage;

	private EquipmentIndex inventoryEquipmentIndex = EquipmentIndex.None;

	private EliteIndex myEliteIndex = EliteIndex.None;

	private float fade = 1f;

	private float firstPersonFade = 1f;

	public float corpseFade = 1f;

	public bool CharacterOnScreen;

	private SkinnedMeshRenderer mainSkinnedMeshRenderer;

	public RendererVisiblity visibilityChecker;

	private static readonly Color poisonEliteLightColor;

	private static readonly Color hauntedEliteLightColor;

	private static readonly Color lunarEliteLightColor;

	private static readonly Color voidEliteLightColor;

	private Color? lightColorOverride;

	private Material particleMaterialOverride;

	private GameObject poisonAffixEffect;

	private GameObject hauntedAffixEffect;

	private GameObject voidAffixEffect;

	private float affixHauntedCloakLockoutDuration = 3f;

	private EquipmentIndex currentEquipmentDisplayIndex = EquipmentIndex.None;

	private ItemMask enabledItemDisplays;

	private List<ParentedPrefabDisplay> parentedPrefabDisplays = new List<ParentedPrefabDisplay>();

	private List<LimbMaskDisplay> limbMaskDisplays = new List<LimbMaskDisplay>();

	private LimbFlagSet limbFlagSet = new LimbFlagSet();

	public static Material revealedMaterial;

	public static Material cloakedMaterial;

	public static Material ghostMaterial;

	public static Material bellBuffMaterial;

	public static Material wolfhatMaterial;

	public static Material energyShieldMaterial;

	public static Material fullCritMaterial;

	public static Material beetleJuiceMaterial;

	public static Material brittleMaterial;

	public static Material clayGooMaterial;

	public static Material slow80Material;

	public static Material immuneMaterial;

	public static Material elitePoisonOverlayMaterial;

	public static Material elitePoisonParticleReplacementMaterial;

	public static Material eliteHauntedOverlayMaterial;

	public static Material eliteJustHauntedOverlayMaterial;

	public static Material eliteHauntedParticleReplacementMaterial;

	public static Material eliteLunarParticleReplacementMaterial;

	public static Material eliteVoidParticleReplacementMaterial;

	public static Material eliteVoidOverlayMaterial;

	public static Material weakMaterial;

	public static Material pulverizedMaterial;

	public static Material doppelgangerMaterial;

	public static Material ghostParticleReplacementMaterial;

	public static Material lunarGolemShieldMaterial;

	public static Material echoMaterial;

	public static Material gummyCloneMaterial;

	public static Material voidSurvivorCorruptMaterial;

	public static Material voidShieldMaterial;

	public static Material growthNectarMaterial;

	public static Material eliteAurelioniteAffixOverlay;

	public static Material yesChefHeatMaterial;

	private static readonly int maxOverlays;

	private Material[] currentOverlays = new Material[maxOverlays];

	private int activeOverlayCount;

	private bool wasPreviouslyClayGooed;

	private bool wasPreviouslyHaunted;

	private RtpcSetter rtpcEliteEnemy;

	private int shaderEliteRampIndex = -1;

	private bool eliteChanged;

	public int activeOverlays;

	public int oldOverlays;

	public float hitFlashValue;

	public float healFlashValue;

	public float oldHit;

	public float oldHeal;

	public float oldFade;

	private EliteIndex oldEliteIndex = EliteIndex.None;

	public bool forceUpdate;

	private static Material[][] sharedMaterialArrays;

	private static readonly int maxMaterials;

	public VisibilityLevel visibility
	{
		get
		{
			return _visibility;
		}
		set
		{
			if (_visibility != value)
			{
				_visibility = value;
				materialsDirty = true;
			}
		}
	}

	public bool isGhost
	{
		get
		{
			return _isGhost;
		}
		set
		{
			if (_isGhost != value)
			{
				_isGhost = value;
				materialsDirty = true;
			}
		}
	}

	public bool isDoppelganger
	{
		get
		{
			return _isDoppelganger;
		}
		set
		{
			if (_isDoppelganger != value)
			{
				_isDoppelganger = value;
				materialsDirty = true;
			}
		}
	}

	public bool isEcho
	{
		get
		{
			return _isEcho;
		}
		set
		{
			if (_isEcho != value)
			{
				_isEcho = value;
				materialsDirty = true;
			}
		}
	}

	private void Awake()
	{
		enabledItemDisplays = ItemMask.Rent();
		childLocator = GetComponent<ChildLocator>();
		HurtBoxGroup component = GetComponent<HurtBoxGroup>();
		coreTransform = base.transform;
		if ((bool)component)
		{
			coreTransform = component.mainHurtBox?.transform ?? coreTransform;
			HurtBox[] hurtBoxes = component.hurtBoxes;
			if (hurtBoxes.Length != 0)
			{
				hurtBoxInfos = new HurtBoxInfo[hurtBoxes.Length];
				for (int i = 0; i < hurtBoxes.Length; i++)
				{
					hurtBoxInfos[i] = new HurtBoxInfo(hurtBoxes[i]);
				}
			}
		}
		propertyStorage = new MaterialPropertyBlock();
		RendererInfo[] array = baseRendererInfos;
		for (int j = 0; j < array.Length; j++)
		{
			RendererInfo rendererInfo = array[j];
			if (rendererInfo.renderer is SkinnedMeshRenderer)
			{
				mainSkinnedMeshRenderer = (SkinnedMeshRenderer)rendererInfo.renderer;
				break;
			}
		}
		if ((bool)body && Util.IsPrefab(body.gameObject) && !Util.IsPrefab(base.gameObject))
		{
			body = null;
		}
	}

	private void Start()
	{
		forceUpdate = true;
		visibility = VisibilityLevel.Invisible;
		UpdateMaterials();
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		if ((object)body != null)
		{
			rtpcEliteEnemy = new RtpcSetter("eliteEnemy", body.gameObject);
			body.onInventoryChanged += OnInventoryChanged;
		}
	}

	private void OnDisable()
	{
		InstanceUpdate();
		if ((object)body != null)
		{
			body.onInventoryChanged -= OnInventoryChanged;
		}
		InstanceTracker.Remove(this);
	}

	public void SetVisible(bool visible)
	{
		if ((bool)body)
		{
			body.CharacterIsVisible = (CharacterOnScreen = visible);
		}
	}

	private void OnDestroy()
	{
		ItemMask.Return(enabledItemDisplays);
	}

	private void OnInventoryChanged()
	{
		if ((bool)body)
		{
			Inventory inventory = body.inventory;
			if ((bool)inventory)
			{
				UpdateItemDisplay(inventory);
				inventoryEquipmentIndex = inventory.GetEquipmentIndex();
				SetEquipmentDisplay(inventoryEquipmentIndex);
				forceUpdate = true;
			}
		}
	}

	private void InstanceUpdate()
	{
		if (isGhost)
		{
			particleMaterialOverride = ghostParticleReplacementMaterial;
		}
		else if (myEliteIndex == RoR2Content.Elites.Poison.eliteIndex)
		{
			lightColorOverride = poisonEliteLightColor;
			particleMaterialOverride = elitePoisonParticleReplacementMaterial;
		}
		else if (myEliteIndex == RoR2Content.Elites.Haunted.eliteIndex)
		{
			lightColorOverride = hauntedEliteLightColor;
			particleMaterialOverride = eliteHauntedParticleReplacementMaterial;
		}
		else if (myEliteIndex == RoR2Content.Elites.Lunar.eliteIndex)
		{
			lightColorOverride = lunarEliteLightColor;
			particleMaterialOverride = eliteLunarParticleReplacementMaterial;
		}
		else if (myEliteIndex == DLC1Content.Elites.Void.eliteIndex && (bool)body && body.healthComponent.alive)
		{
			lightColorOverride = voidEliteLightColor;
			particleMaterialOverride = eliteVoidParticleReplacementMaterial;
		}
		else
		{
			lightColorOverride = null;
			particleMaterialOverride = null;
		}
		UpdateGoldAffix();
		UpdatePoisonAffix();
		UpdateHauntedAffix();
		UpdateVoidAffix();
		UpdateLights();
	}

	private void UpdateLights()
	{
		LightInfo[] array = baseLightInfos;
		if (array.Length == 0)
		{
			return;
		}
		if (lightColorOverride.HasValue)
		{
			Color value = lightColorOverride.Value;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].light.color = value;
			}
		}
		else
		{
			for (int j = 0; j < array.Length; j++)
			{
				ref LightInfo reference = ref array[j];
				reference.light.color = reference.defaultColor;
			}
		}
	}

	[InitDuringStartup]
	private static void Init()
	{
		RoR2Application.onLateUpdate += StaticUpdate;
	}

	private static void StaticUpdate()
	{
		foreach (CharacterModel instances in InstanceTracker.GetInstancesList<CharacterModel>())
		{
			instances.InstanceUpdate();
		}
	}

	public void AddTempOverlay(TemporaryOverlayInstance overlay)
	{
		temporaryOverlays.Add(overlay);
		materialsDirty = true;
	}

	public void RemoveTempOverlay(TemporaryOverlayInstance overlay)
	{
		temporaryOverlays.Remove(overlay);
		materialsDirty = true;
	}

	private bool IsCurrentEliteType(EliteDef eliteDef)
	{
		if ((object)eliteDef == null || eliteDef.eliteIndex == EliteIndex.None)
		{
			return false;
		}
		return eliteDef.eliteIndex == myEliteIndex;
	}

	private void UpdateGoldAffix()
	{
		if (IsCurrentEliteType(JunkContent.Elites.Gold) == (bool)goldAffixEffect)
		{
			return;
		}
		if (!goldAffixEffect)
		{
			goldAffixEffect = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/GoldAffixEffect"), base.transform);
			ParticleSystem.ShapeModule shape = goldAffixEffect.GetComponent<ParticleSystem>().shape;
			if ((bool)mainSkinnedMeshRenderer)
			{
				shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
				shape.skinnedMeshRenderer = mainSkinnedMeshRenderer;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(goldAffixEffect);
			goldAffixEffect = null;
		}
	}

	private void UpdatePoisonAffix()
	{
		if ((myEliteIndex == RoR2Content.Elites.Poison.eliteIndex && body.healthComponent.alive) == (bool)poisonAffixEffect)
		{
			return;
		}
		if (!poisonAffixEffect)
		{
			poisonAffixEffect = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/PoisonAffixEffect"), base.transform);
			if ((bool)mainSkinnedMeshRenderer)
			{
				JitterBones[] components = poisonAffixEffect.GetComponents<JitterBones>();
				for (int i = 0; i < components.Length; i++)
				{
					components[i].skinnedMeshRenderer = mainSkinnedMeshRenderer;
				}
			}
		}
		else
		{
			UnityEngine.Object.Destroy(poisonAffixEffect);
			poisonAffixEffect = null;
		}
	}

	private void UpdateHauntedAffix()
	{
		if ((myEliteIndex == RoR2Content.Elites.Haunted.eliteIndex && body.healthComponent.alive) == (bool)hauntedAffixEffect)
		{
			return;
		}
		if (!hauntedAffixEffect)
		{
			hauntedAffixEffect = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/HauntedAffixEffect"), base.transform);
			if ((bool)mainSkinnedMeshRenderer)
			{
				JitterBones[] components = hauntedAffixEffect.GetComponents<JitterBones>();
				for (int i = 0; i < components.Length; i++)
				{
					components[i].skinnedMeshRenderer = mainSkinnedMeshRenderer;
				}
			}
		}
		else
		{
			UnityEngine.Object.Destroy(hauntedAffixEffect);
			hauntedAffixEffect = null;
		}
	}

	private void UpdateVoidAffix()
	{
		if ((myEliteIndex == DLC1Content.Elites.Void.eliteIndex && body.healthComponent.alive) == (bool)voidAffixEffect)
		{
			return;
		}
		if (!voidAffixEffect)
		{
			voidAffixEffect = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteVoid/VoidAffixEffect.prefab").WaitForCompletion(), base.transform);
			if ((bool)mainSkinnedMeshRenderer)
			{
				JitterBones[] components = voidAffixEffect.GetComponents<JitterBones>();
				for (int i = 0; i < components.Length; i++)
				{
					components[i].skinnedMeshRenderer = mainSkinnedMeshRenderer;
				}
			}
		}
		else
		{
			UnityEngine.Object.Destroy(voidAffixEffect);
			voidAffixEffect = null;
		}
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			return;
		}
		for (int i = 0; i < baseLightInfos.Length; i++)
		{
			ref LightInfo reference = ref baseLightInfos[i];
			if ((bool)reference.light)
			{
				reference.defaultColor = reference.light.color;
			}
		}
		_ = (bool)itemDisplayRuleSet;
		if (autoPopulateLightInfos)
		{
			LightInfo[] first = (from light in GetComponentsInChildren<Light>()
				select new LightInfo(light)).ToArray();
			if (!first.SequenceEqual(baseLightInfos))
			{
				baseLightInfos = first;
			}
		}
	}

	private static void RefreshObstructorsForCamera(CameraRigController cameraRigController)
	{
		Vector3 position = cameraRigController.transform.position;
		foreach (CharacterModel instances in InstanceTracker.GetInstancesList<CharacterModel>())
		{
			if (cameraRigController.enableFading)
			{
				float nearestHurtBoxDistance = instances.GetNearestHurtBoxDistance(position);
				instances.fade = Mathf.Clamp01(Util.Remap(nearestHurtBoxDistance, cameraRigController.fadeStartDistance, cameraRigController.fadeEndDistance, 0f, 1f));
			}
			else
			{
				instances.fade = 1f;
			}
		}
	}

	private float GetNearestHurtBoxDistance(Vector3 cameraPosition)
	{
		float num = float.PositiveInfinity;
		for (int i = 0; i < hurtBoxInfos.Length; i++)
		{
			float num2 = Vector3.Distance(hurtBoxInfos[i].transform.position, cameraPosition) - hurtBoxInfos[i].estimatedRadius;
			if (num2 < num)
			{
				num = Mathf.Min(num2, num);
			}
		}
		return num;
	}

	private void UpdateForCamera(CameraRigController cameraRigController)
	{
		VisibilityLevel num = visibility;
		visibility = VisibilityLevel.Visible;
		float target = 1f;
		if ((bool)body)
		{
			if ((object)cameraRigController.firstPersonTarget == body.gameObject)
			{
				target = 0f;
			}
			visibility = body.GetVisibilityLevel(cameraRigController.targetTeamIndex);
		}
		float num2 = 0.25f;
		if ((bool)cameraRigController.targetParams && cameraRigController.targetParams.currentCameraParamsData.overrideFirstPersonFadeDuration > 0f)
		{
			num2 = cameraRigController.targetParams.currentCameraParamsData.overrideFirstPersonFadeDuration;
		}
		firstPersonFade = Mathf.MoveTowards(firstPersonFade, target, Time.deltaTime / num2);
		fade *= firstPersonFade * corpseFade;
		if (fade <= 0f || invisibilityCount > 0)
		{
			visibility = VisibilityLevel.Invisible;
		}
		if (num != visibility)
		{
			forceUpdate = true;
		}
		bool flag = forceUpdate;
		if (!flag)
		{
			for (int i = 0; i < baseRendererInfos.Length; i++)
			{
				if ((bool)baseRendererInfos[i].renderer && baseRendererInfos[i].renderer.isVisible)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			UpdateOverlays();
			if (materialsDirty)
			{
				UpdateMaterials();
				materialsDirty = false;
			}
		}
	}

	static CharacterModel()
	{
		hitFlashBaseColor = new Color32(193, 108, 51, byte.MaxValue);
		hitFlashShieldColor = new Color32(132, 159, byte.MaxValue, byte.MaxValue);
		healFlashColor = new Color32(104, 196, 49, byte.MaxValue);
		hitFlashDuration = 0.15f;
		healFlashDuration = 0.35f;
		poisonEliteLightColor = new Color32(90, byte.MaxValue, 193, 204);
		hauntedEliteLightColor = new Color32(152, 228, 217, 204);
		lunarEliteLightColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 127);
		voidEliteLightColor = new Color32(151, 78, 132, 204);
		maxOverlays = 6;
		maxMaterials = 1 + maxOverlays;
		SceneCamera.onSceneCameraPreRender += OnSceneCameraPreRender;
	}

	private static void OnSceneCameraPreRender(SceneCamera sceneCamera)
	{
		if ((bool)sceneCamera.cameraRigController)
		{
			RefreshObstructorsForCamera(sceneCamera.cameraRigController);
		}
		if (!sceneCamera.cameraRigController)
		{
			return;
		}
		foreach (CharacterModel instances in InstanceTracker.GetInstancesList<CharacterModel>())
		{
			instances.UpdateForCamera(sceneCamera.cameraRigController);
		}
	}

	private void InstantiateDisplayRuleGroup(DisplayRuleGroup displayRuleGroup, ItemIndex itemIndex, EquipmentIndex equipmentIndex)
	{
		if (displayRuleGroup.rules == null)
		{
			return;
		}
		for (int i = 0; i < displayRuleGroup.rules.Length; i++)
		{
			ItemDisplayRule itemDisplayRule = displayRuleGroup.rules[i];
			switch (itemDisplayRule.ruleType)
			{
			case ItemDisplayRuleType.ParentedPrefab:
				if ((bool)childLocator)
				{
					Transform transform = childLocator.FindChild(itemDisplayRule.childName);
					if ((bool)transform)
					{
						ParentedPrefabDisplay parentedPrefabDisplay = default(ParentedPrefabDisplay);
						parentedPrefabDisplay.itemIndex = itemIndex;
						parentedPrefabDisplay.equipmentIndex = equipmentIndex;
						ParentedPrefabDisplay item2 = parentedPrefabDisplay;
						item2.Apply(this, itemDisplayRule.followerPrefab, transform, itemDisplayRule.localPos, Quaternion.Euler(itemDisplayRule.localAngles), itemDisplayRule.localScale);
						parentedPrefabDisplays.Add(item2);
					}
				}
				break;
			case ItemDisplayRuleType.LimbMask:
			{
				LimbMaskDisplay limbMaskDisplay = default(LimbMaskDisplay);
				limbMaskDisplay.itemIndex = itemIndex;
				limbMaskDisplay.equipmentIndex = equipmentIndex;
				LimbMaskDisplay item = limbMaskDisplay;
				item.Apply(this, itemDisplayRule.limbMask);
				limbMaskDisplays.Add(item);
				break;
			}
			}
		}
	}

	private void SetEquipmentDisplay(EquipmentIndex newEquipmentIndex)
	{
		if (newEquipmentIndex == currentEquipmentDisplayIndex)
		{
			return;
		}
		for (int num = parentedPrefabDisplays.Count - 1; num >= 0; num--)
		{
			if (parentedPrefabDisplays[num].equipmentIndex != EquipmentIndex.None)
			{
				parentedPrefabDisplays[num].Undo();
				parentedPrefabDisplays.RemoveAt(num);
			}
		}
		for (int num2 = limbMaskDisplays.Count - 1; num2 >= 0; num2--)
		{
			if (limbMaskDisplays[num2].equipmentIndex != EquipmentIndex.None)
			{
				limbMaskDisplays[num2].Undo(this);
				limbMaskDisplays.RemoveAt(num2);
			}
		}
		currentEquipmentDisplayIndex = newEquipmentIndex;
		if ((bool)itemDisplayRuleSet)
		{
			DisplayRuleGroup equipmentDisplayRuleGroup = itemDisplayRuleSet.GetEquipmentDisplayRuleGroup(newEquipmentIndex);
			InstantiateDisplayRuleGroup(equipmentDisplayRuleGroup, ItemIndex.None, newEquipmentIndex);
		}
	}

	private void EnableItemDisplay(ItemIndex itemIndex)
	{
		if (!enabledItemDisplays.Contains(itemIndex))
		{
			enabledItemDisplays.Add(itemIndex);
			if ((bool)itemDisplayRuleSet)
			{
				DisplayRuleGroup itemDisplayRuleGroup = itemDisplayRuleSet.GetItemDisplayRuleGroup(itemIndex);
				InstantiateDisplayRuleGroup(itemDisplayRuleGroup, itemIndex, EquipmentIndex.None);
			}
		}
	}

	public void DisableAllItemDisplays()
	{
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
		{
			DisableItemDisplay(itemIndex);
		}
	}

	private void DisableItemDisplay(ItemIndex itemIndex)
	{
		if (!enabledItemDisplays.Contains(itemIndex))
		{
			return;
		}
		enabledItemDisplays.Remove(itemIndex);
		for (int num = parentedPrefabDisplays.Count - 1; num >= 0; num--)
		{
			if (parentedPrefabDisplays[num].itemIndex == itemIndex)
			{
				parentedPrefabDisplays[num].Undo();
				parentedPrefabDisplays.RemoveAt(num);
			}
		}
		for (int num2 = limbMaskDisplays.Count - 1; num2 >= 0; num2--)
		{
			if (limbMaskDisplays[num2].itemIndex == itemIndex)
			{
				limbMaskDisplays[num2].Undo(this);
				limbMaskDisplays.RemoveAt(num2);
			}
		}
	}

	public void UpdateItemDisplay(Inventory inventory)
	{
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
		{
			if (inventory.GetItemCount(itemIndex) > 0)
			{
				EnableItemDisplay(itemIndex);
			}
			else
			{
				DisableItemDisplay(itemIndex);
			}
		}
	}

	public void HighlightItemDisplay(ItemIndex itemIndex)
	{
		if (!enabledItemDisplays.Contains(itemIndex))
		{
			return;
		}
		ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(ItemCatalog.GetItemDef(itemIndex).tier);
		GameObject gameObject = null;
		gameObject = ((!itemTierDef || !itemTierDef.highlightPrefab) ? LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/HighlightTier1Item") : itemTierDef.highlightPrefab);
		for (int num = parentedPrefabDisplays.Count - 1; num >= 0; num--)
		{
			if (parentedPrefabDisplays[num].itemIndex == itemIndex)
			{
				GameObject instance = parentedPrefabDisplays[num].instance;
				if ((bool)instance)
				{
					Renderer componentInChildren = instance.GetComponentInChildren<Renderer>();
					if ((bool)componentInChildren && (bool)body)
					{
						HighlightRect.CreateHighlight(body.gameObject, componentInChildren, gameObject);
					}
				}
			}
		}
	}

	public List<GameObject> GetEquipmentDisplayObjects(EquipmentIndex equipmentIndex)
	{
		List<GameObject> list = new List<GameObject>();
		for (int num = parentedPrefabDisplays.Count - 1; num >= 0; num--)
		{
			if (parentedPrefabDisplays[num].equipmentIndex == equipmentIndex)
			{
				GameObject instance = parentedPrefabDisplays[num].instance;
				list.Add(instance);
			}
		}
		return list;
	}

	public List<GameObject> GetItemDisplayObjects(ItemIndex itemIndex)
	{
		List<GameObject> list = new List<GameObject>();
		for (int num = parentedPrefabDisplays.Count - 1; num >= 0; num--)
		{
			if (parentedPrefabDisplays[num].itemIndex == itemIndex)
			{
				GameObject instance = parentedPrefabDisplays[num].instance;
				list.Add(instance);
			}
		}
		return list;
	}

	[InitDuringStartup]
	private static void InitMaterials()
	{
		AsyncOperationHandle<Material> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEliteAurelioniteAffixOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteAurelioniteAffixOverlay = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matRevealedEffect");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			revealedMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matCloakedEffect");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			cloakedMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matGhostEffect");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			ghostMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matGhostParticleReplacement");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			ghostParticleReplacementMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matWolfhatOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			wolfhatMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEnergyShield");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			energyShieldMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matBeetleJuice");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			beetleJuiceMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matBrittle");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			brittleMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matFullCrit");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			fullCritMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matClayGooDebuff");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			clayGooMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matSlow80Debuff");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			slow80Material = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matImmune");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			immuneMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matBellBuff");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			bellBuffMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matElitePoisonOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			elitePoisonOverlayMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matElitePoisonParticleReplacement");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			elitePoisonParticleReplacementMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEliteHauntedOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteHauntedOverlayMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEliteHauntedParticleReplacement");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteHauntedParticleReplacementMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEliteJustHauntedOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteJustHauntedOverlayMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEliteLunarParticleReplacement");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteLunarParticleReplacementMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matDoppelganger");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			doppelgangerMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matWeakOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			weakMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matPulverizedOverlay");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			pulverizedMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matLunarGolemShield");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			lunarGolemShieldMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matEcho");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			echoMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matGummyClone");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			gummyCloneMaterial = x.Result;
		};
		asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Material>("Materials/matGrowthNectarGlow");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			growthNectarMaterial = x.Result;
		};
		asyncOperationHandle = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/EliteVoid/matEliteVoidParticleReplacement.mat");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteVoidParticleReplacementMaterial = x.Result;
		};
		asyncOperationHandle = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/EliteVoid/matEliteVoidOverlay.mat");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			eliteVoidOverlayMaterial = x.Result;
		};
		asyncOperationHandle = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorCorruptOverlay.mat");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			voidSurvivorCorruptMaterial = x.Result;
		};
		asyncOperationHandle = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/MissileVoid/matEnergyShieldVoid.mat");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			voidShieldMaterial = x.Result;
		};
		asyncOperationHandle = Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Chef/matYesChefOverlay.mat");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Material> x)
		{
			yesChefHeatMaterial = x.Result;
		};
	}

	private bool UpdateOverlayStates()
	{
		oldOverlays = activeOverlays;
		activeOverlays = 0;
		if ((bool)body)
		{
			bool condition2 = body.HasBuff(RoR2Content.Buffs.ClayGoo);
			Inventory inventory = body.inventory;
			isGhost = (object)inventory != null && inventory.GetItemCount(RoR2Content.Items.Ghost) > 0;
			myEliteIndex = EquipmentCatalog.GetEquipmentDef(inventoryEquipmentIndex)?.passiveBuffDef?.eliteDef?.eliteIndex ?? EliteIndex.None;
			oldHit = hitFlashValue;
			oldHeal = healFlashValue;
			if ((bool)body.healthComponent)
			{
				hitFlashValue = Mathf.Clamp01(1f - body.healthComponent.timeSinceLastHit / hitFlashDuration);
				healFlashValue = Mathf.Pow(Mathf.Clamp01(1f - body.healthComponent.timeSinceLastHeal / healFlashDuration), 0.5f);
			}
			eliteChanged = myEliteIndex != oldEliteIndex;
			int num = 0;
			SetOverlayFlag(num++, eliteChanged);
			SetOverlayFlag(num++, hitFlashValue != oldHit || healFlashValue != oldHeal);
			SetOverlayFlag(num++, isGhost);
			SetOverlayFlag(num++, condition2);
			SetOverlayFlag(num++, myEliteIndex == RoR2Content.Elites.Poison.eliteIndex || body.HasBuff(RoR2Content.Buffs.HealingDisabled));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.Weak));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.FullCrit));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.AttackSpeedOnCrit));
			SetOverlayFlag(num++, (bool)body.healthComponent && body.healthComponent.shield > 0f);
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.BeetleJuice));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.Immune));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.Slow80));
			SetOverlayFlag(num++, (bool)body.inventory && body.inventory.GetItemCount(RoR2Content.Items.LunarDagger) > 0);
			SetOverlayFlag(num++, (bool)body.inventory && body.inventory.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0);
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.AffixHaunted));
			SetOverlayFlag(num++, body.HasBuff(DLC1Content.Buffs.EliteVoid) && (bool)body.healthComponent && body.healthComponent.alive);
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.Pulverized));
			SetOverlayFlag(num++, body.HasBuff(RoR2Content.Buffs.LunarShell));
			SetOverlayFlag(num++, (bool)body.inventory && body.inventory.GetItemCount(RoR2Content.Items.SummonedEcho) > 0);
			SetOverlayFlag(num++, IsGummyClone());
			SetOverlayFlag(num++, body.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode));
			SetOverlayFlag(num++, IsAurelioniteAffix());
			SetOverlayFlag(num++, body.HasBuff(DLC2Content.Buffs.BoostAllStatsBuff));
			SetOverlayFlag(num++, body.HasBuff(DLC2Content.Buffs.boostedFireEffect));
			for (int i = 0; i < baseRendererInfos.Length; i++)
			{
				SetOverlayFlag(num + i, baseRendererInfos[i].ignoreOverlays);
			}
		}
		bool flag = fade != oldFade;
		oldEliteIndex = myEliteIndex;
		oldFade = fade;
		return oldOverlays != activeOverlays || flag;
		void SetOverlayFlag(int index, bool condition)
		{
			if (condition)
			{
				activeOverlays |= 1 << index;
			}
		}
	}

	private void UpdateOverlays()
	{
		if (visibility == VisibilityLevel.Invisible || (!UpdateOverlayStates() && !forceUpdate))
		{
			return;
		}
		forceUpdate = false;
		for (int i = 0; i < activeOverlayCount; i++)
		{
			currentOverlays[i] = null;
		}
		activeOverlayCount = 0;
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(inventoryEquipmentIndex);
		myEliteIndex = equipmentDef?.passiveBuffDef?.eliteDef?.eliteIndex ?? EliteIndex.None;
		shaderEliteRampIndex = equipmentDef?.passiveBuffDef?.eliteDef?.shaderEliteRampIndex ?? (-1);
		bool flag = false;
		bool flag2 = false;
		if ((bool)body)
		{
			flag = body.HasBuff(RoR2Content.Buffs.ClayGoo);
			flag2 = body.HasBuff(RoR2Content.Buffs.AffixHauntedRecipient);
			rtpcEliteEnemy.value = ((myEliteIndex != EliteIndex.None) ? 1f : 0f);
			rtpcEliteEnemy.FlushIfChanged();
			Inventory inventory = body.inventory;
			isGhost = (object)inventory != null && inventory.GetItemCount(RoR2Content.Items.Ghost) > 0;
			Inventory inventory2 = body.inventory;
			isDoppelganger = (object)inventory2 != null && inventory2.GetItemCount(RoR2Content.Items.InvadingDoppelganger) > 0;
			Inventory inventory3 = body.inventory;
			isEcho = (object)inventory3 != null && inventory3.GetItemCount(RoR2Content.Items.SummonedEcho) > 0;
			Inventory inventory4 = body.inventory;
			bool flag3 = (object)inventory4 != null && inventory4.GetItemCount(DLC1Content.Items.MissileVoid) > 0;
			AddOverlay(ghostMaterial, isGhost);
			AddOverlay(doppelgangerMaterial, isDoppelganger);
			AddOverlay(clayGooMaterial, flag);
			AddOverlay(elitePoisonOverlayMaterial, myEliteIndex == RoR2Content.Elites.Poison.eliteIndex || body.HasBuff(RoR2Content.Buffs.HealingDisabled));
			AddOverlay(eliteHauntedOverlayMaterial, body.HasBuff(RoR2Content.Buffs.AffixHaunted));
			AddOverlay(eliteVoidOverlayMaterial, body.HasBuff(DLC1Content.Buffs.EliteVoid) && (bool)body.healthComponent && body.healthComponent.alive);
			AddOverlay(pulverizedMaterial, body.HasBuff(RoR2Content.Buffs.Pulverized));
			AddOverlay(weakMaterial, body.HasBuff(RoR2Content.Buffs.Weak));
			AddOverlay(fullCritMaterial, body.HasBuff(RoR2Content.Buffs.FullCrit));
			AddOverlay(wolfhatMaterial, body.HasBuff(RoR2Content.Buffs.AttackSpeedOnCrit));
			AddOverlay(flag3 ? voidShieldMaterial : energyShieldMaterial, (bool)body.healthComponent && body.healthComponent.shield > 0f);
			AddOverlay(beetleJuiceMaterial, body.HasBuff(RoR2Content.Buffs.BeetleJuice));
			AddOverlay(immuneMaterial, body.HasBuff(RoR2Content.Buffs.Immune));
			AddOverlay(slow80Material, body.HasBuff(RoR2Content.Buffs.Slow80));
			AddOverlay(brittleMaterial, (bool)body.inventory && body.inventory.GetItemCount(RoR2Content.Items.LunarDagger) > 0);
			AddOverlay(lunarGolemShieldMaterial, body.HasBuff(RoR2Content.Buffs.LunarShell));
			AddOverlay(echoMaterial, isEcho);
			AddOverlay(gummyCloneMaterial, IsGummyClone());
			AddOverlay(voidSurvivorCorruptMaterial, body.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode));
			AddOverlay(growthNectarMaterial, body.HasBuff(DLC2Content.Buffs.BoostAllStatsBuff));
			AddOverlay(eliteAurelioniteAffixOverlay, IsAurelioniteAffix());
			AddOverlay(lunarGolemShieldMaterial, body.HasBuff(DLC2Content.Buffs.EliteBead));
			AddOverlay(brittleMaterial, body.HasBuff(DLC2Content.Buffs.EliteBeadCorruption));
			AddOverlay(yesChefHeatMaterial, body.HasBuff(DLC2Content.Buffs.boostedFireEffect));
		}
		if (wasPreviouslyClayGooed && !flag)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(base.gameObject);
			temporaryOverlayInstance.duration = 0.6f;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = clayGooMaterial;
			temporaryOverlayInstance.AddToCharacterModel(this);
		}
		if (wasPreviouslyHaunted != flag2)
		{
			TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(base.gameObject);
			temporaryOverlayInstance2.duration = 0.5f;
			temporaryOverlayInstance2.animateShaderAlpha = true;
			temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance2.destroyComponentOnEnd = true;
			temporaryOverlayInstance2.originalMaterial = eliteJustHauntedOverlayMaterial;
			temporaryOverlayInstance2.AddToCharacterModel(this);
		}
		wasPreviouslyClayGooed = flag;
		wasPreviouslyHaunted = flag2;
		for (int j = 0; j < temporaryOverlays.Count; j++)
		{
			if (activeOverlayCount >= maxOverlays)
			{
				break;
			}
			currentOverlays[activeOverlayCount++] = temporaryOverlays[j].materialInstance;
		}
		wasPreviouslyClayGooed = flag;
		wasPreviouslyHaunted = flag2;
		materialsDirty = true;
		void AddOverlay(Material overlayMaterial, bool condition)
		{
			if (activeOverlayCount < maxOverlays && condition)
			{
				currentOverlays[activeOverlayCount++] = overlayMaterial;
			}
		}
	}

	[InitDuringStartup]
	private static void InitSharedMaterialsArrays()
	{
		sharedMaterialArrays = new Material[maxMaterials + 1][];
		if (maxMaterials > 0)
		{
			sharedMaterialArrays[0] = Array.Empty<Material>();
			for (int i = 1; i < sharedMaterialArrays.Length; i++)
			{
				sharedMaterialArrays[i] = new Material[i];
			}
		}
	}

	private void UpdateRendererMaterials(Renderer renderer, Material defaultMaterial, bool ignoreOverlays)
	{
		Material material = null;
		switch (visibility)
		{
		case VisibilityLevel.Invisible:
			renderer.sharedMaterial = null;
			return;
		case VisibilityLevel.Cloaked:
			if (!ignoreOverlays)
			{
				ignoreOverlays = true;
				material = cloakedMaterial;
			}
			break;
		case VisibilityLevel.Revealed:
			if (!ignoreOverlays)
			{
				material = revealedMaterial;
			}
			break;
		case VisibilityLevel.Visible:
			material = (ignoreOverlays ? (particleMaterialOverride ? particleMaterialOverride : defaultMaterial) : ((!isDoppelganger) ? ((!isGhost) ? ((!IsGummyClone()) ? ((!IsAurelioniteAffix()) ? defaultMaterial : eliteAurelioniteAffixOverlay) : gummyCloneMaterial) : ghostMaterial) : doppelgangerMaterial));
			break;
		}
		int num = ((!ignoreOverlays) ? activeOverlayCount : 0);
		if ((bool)material)
		{
			num++;
		}
		Material material2 = null;
		Material[] array = sharedMaterialArrays[num];
		int num2 = 0;
		if ((bool)material)
		{
			array[num2++] = material;
		}
		if (!ignoreOverlays)
		{
			for (int i = 0; i < activeOverlayCount; i++)
			{
				array[num2++] = currentOverlays[i];
			}
		}
		if (material2 != null)
		{
			array[num2++] = material2;
		}
		if (num2 != array.Length)
		{
			Debug.LogError("Materials are going to be pink or something!?");
		}
		renderer.sharedMaterials = array;
	}

	private void UpdateMaterials()
	{
		Color value = Color.black;
		if ((bool)body && (bool)body.healthComponent)
		{
			float num = Mathf.Clamp01(1f - body.healthComponent.timeSinceLastHit / hitFlashDuration);
			float num2 = Mathf.Pow(Mathf.Clamp01(1f - body.healthComponent.timeSinceLastHeal / healFlashDuration), 0.5f);
			value = ((!(num2 > num)) ? (((body.healthComponent.shield > 0f) ? hitFlashShieldColor : hitFlashBaseColor) * num) : (healFlashColor * num2));
		}
		if (visibility == VisibilityLevel.Invisible)
		{
			for (int num3 = baseRendererInfos.Length - 1; num3 >= 0; num3--)
			{
				RendererInfo rendererInfo = baseRendererInfos[num3];
				rendererInfo.renderer.shadowCastingMode = ShadowCastingMode.Off;
				rendererInfo.renderer.enabled = false;
			}
		}
		else
		{
			for (int num4 = baseRendererInfos.Length - 1; num4 >= 0; num4--)
			{
				RendererInfo rendererInfo2 = baseRendererInfos[num4];
				Renderer renderer = rendererInfo2.renderer;
				UpdateRendererMaterials(renderer, baseRendererInfos[num4].defaultMaterial, baseRendererInfos[num4].ignoreOverlays);
				renderer.shadowCastingMode = rendererInfo2.defaultShadowCastingMode;
				renderer.enabled = true;
				renderer.GetPropertyBlock(propertyStorage);
				propertyStorage.SetColor(CommonShaderProperties._FlashColor, value);
				propertyStorage.SetFloat(CommonShaderProperties._EliteIndex, shaderEliteRampIndex + 1);
				propertyStorage.SetInt(CommonShaderProperties._LimbPrimeMask, limbFlagSet.materialMaskValue);
				propertyStorage.SetFloat(CommonShaderProperties._Fade, fade);
				renderer.SetPropertyBlock(propertyStorage);
			}
		}
		for (int i = 0; i < parentedPrefabDisplays.Count; i++)
		{
			ItemDisplay itemDisplay = parentedPrefabDisplays[i].itemDisplay;
			itemDisplay.SetVisibilityLevel(visibility);
			for (int j = 0; j < itemDisplay.rendererInfos.Length; j++)
			{
				Renderer renderer2 = itemDisplay.rendererInfos[j].renderer;
				renderer2.GetPropertyBlock(propertyStorage);
				propertyStorage.SetColor(CommonShaderProperties._FlashColor, value);
				propertyStorage.SetFloat(CommonShaderProperties._Fade, fade);
				renderer2.SetPropertyBlock(propertyStorage);
			}
		}
	}

	private bool IsGummyClone()
	{
		CharacterBody characterBody = body;
		if ((object)characterBody == null)
		{
			return false;
		}
		return characterBody.inventory?.GetItemCount(DLC1Content.Items.GummyCloneIdentifier) > 0;
	}

	private bool IsAurelioniteAffix()
	{
		bool result = false;
		if ((bool)body)
		{
			result = body.HasBuff(DLC2Content.Buffs.EliteAurelionite);
		}
		return result;
	}

	public void OnDeath()
	{
		for (int i = 0; i < parentedPrefabDisplays.Count; i++)
		{
			parentedPrefabDisplays[i].itemDisplay.OnDeath();
		}
		InstanceUpdate();
		UpdateOverlays();
		UpdateMaterials();
	}
}
