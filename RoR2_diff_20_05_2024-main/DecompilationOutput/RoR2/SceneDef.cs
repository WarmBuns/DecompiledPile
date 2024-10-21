using System;
using System.Collections.Generic;
using HG;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SceneDef")]
public class SceneDef : ScriptableObject
{
	[Header("Scene")]
	[Tooltip("The address of the associated scene. There is a name-based fallback systems for mods to use if the address isn't provided, but all official scenes must provide this.")]
	public AssetReferenceScene sceneAddress;

	[Tooltip("The \"base\" name used for things like unlockables and stat tracking associated with this scene. If empty, the name of this asset will be used instead.")]
	public string baseSceneNameOverride;

	[Header("Classification")]
	public SceneType sceneType;

	public bool isOfflineScene;

	public int stageOrder;

	public ExpansionDef requiredExpansion;

	[Header("User-Facing Name")]
	public string nameToken;

	public string subtitleToken;

	public Texture previewTexture;

	[Header("Bazaar")]
	public Material portalMaterial;

	public string portalSelectionMessageString;

	[Header("Logbook")]
	public bool shouldIncludeInLogbook = true;

	[Tooltip("The logbook text for this scene. If empty, this scene will not be represented in the logbook.")]
	public string loreToken;

	public GameObject dioramaPrefab;

	[FormerlySerializedAs("song")]
	[Header("Music")]
	public MusicTrackDef mainTrack;

	[FormerlySerializedAs("bossSong")]
	public MusicTrackDef bossTrack;

	[Header("Color")]
	[Tooltip("Representative color of the environment, to be used for things such as hardware lights.")]
	public Color environmentColor;

	[Header("Combat Message")]
	[Tooltip("scene has safe start or not.")]
	public bool hasSafeStart;

	[Header("Behavior")]
	[Tooltip("Prevents players from spawning into the scene. This is usually for cutscenes.")]
	public bool suppressPlayerEntry;

	[Tooltip("Prevents persistent NPCs (like drones) from spawning into the scene. This is usually for cutscenes, or areas that get them killed them due to hazards they're not smart enough to avoid.")]
	public bool suppressNpcEntry;

	[Tooltip("Prevents Captain from using orbital skills.")]
	public bool blockOrbitalSkills;

	[Tooltip("Is this stage allowed to be selected when using a random stage order (e.g., in Prismatic Trials?)")]
	public bool validForRandomSelection = true;

	[Tooltip("Allow objects from items to spawn in this level. Overrides the need to have the SceneType = Stage.")]
	public bool allowItemsToSpawnObjects;

	[Header("Artifact")]
	[Tooltip("Devotion lemurians should respawn later when find a valid node graph")]
	public bool needSkipDevotionRespawn;

	[Header("Destinations")]
	[Tooltip("A collection of stages that can be destinations of the teleporter.")]
	public SceneCollection destinationsGroup;

	[Tooltip("A collection of stages that can be destinations of the teleporter after looping.")]
	public SceneCollection loopedDestinationsGroup;

	[SerializeField]
	[Tooltip("A collection of stages that can be destinations of the teleporter after looping.")]
	private bool shouldUpdateSceneCollectionAfterLooping;

	[Tooltip("A bool that tracks if this stage is available before looping.")]
	public bool isLockedBeforeLooping;

	[Tooltip("For specific portal transitions that references the original and needs the looped version.")]
	public SceneDef loopedSceneDef;

	[Obsolete("Use destinationsGroup instead.")]
	[ShowFieldObsolete]
	[Tooltip("Stages that can be destinations of the teleporter.")]
	public SceneDef[] destinations = Array.Empty<SceneDef>();

	[Header("Portal Appearance")]
	public GameObject preferredPortalPrefab;

	private string _cachedName;

	[HideInInspector]
	[NonSerialized]
	[Obsolete]
	public List<string> sceneNameOverrides;

	public SceneIndex sceneDefIndex { get; set; }

	public string baseSceneName
	{
		get
		{
			if (string.IsNullOrEmpty(baseSceneNameOverride))
			{
				return cachedName;
			}
			return baseSceneNameOverride;
		}
	}

	public bool isFinalStage
	{
		get
		{
			if (sceneType == SceneType.Stage)
			{
				return !hasAnyDestinations;
			}
			return false;
		}
	}

	public bool hasAnyDestinations
	{
		get
		{
			if (destinations.Length == 0)
			{
				if ((bool)destinationsGroup)
				{
					return !destinationsGroup.isEmpty;
				}
				return false;
			}
			return true;
		}
	}

	[Obsolete(".name should not be used. Use .cachedName instead. If retrieving the value from the engine is absolutely necessary, cast to ScriptableObject first.", true)]
	public new string name
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string cachedName
	{
		get
		{
			return _cachedName;
		}
		set
		{
			base.name = value;
			_cachedName = value;
		}
	}

	private void Awake()
	{
		_cachedName = base.name;
	}

	private void OnValidate()
	{
		_cachedName = base.name;
		if (string.IsNullOrEmpty(sceneAddress?.AssetGUID))
		{
			AutoAssignSceneAddress();
		}
	}

	[ContextMenu("Auto-assign scene address")]
	private void AutoAssignSceneAddress()
	{
	}

	public void AddDestinationsToWeightedSelection(WeightedSelection<SceneDef> dest, Func<SceneDef, bool> canAdd = null)
	{
		if ((bool)destinationsGroup)
		{
			if (Run.instance.stageClearCount >= 4 && shouldUpdateSceneCollectionAfterLooping)
			{
				if ((bool)loopedDestinationsGroup)
				{
					loopedDestinationsGroup.AddToWeightedSelection(dest, canAdd);
				}
				else
				{
					Debug.LogError("Should be looping but looped scene collection is missing.");
				}
			}
			else
			{
				destinationsGroup.AddToWeightedSelection(dest, canAdd);
			}
		}
		SceneDef[] array = destinations;
		foreach (SceneDef value in array)
		{
			dest.AddChoice(value, 1f);
		}
	}
}
