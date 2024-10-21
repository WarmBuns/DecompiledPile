using System;
using System.Collections.Generic;
using EntityStates.Missions.Goldshores;
using JetBrains.Annotations;
using RoR2.Scripts.GameBehaviors.UI;
using RoR2.Scripts.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

public class ObjectivePanelController : MonoBehaviour
{
	public struct ObjectiveSourceDescriptor : IEquatable<ObjectiveSourceDescriptor>
	{
		public UnityEngine.Object source;

		public CharacterMaster master;

		public Type objectiveType;

		public override int GetHashCode()
		{
			return (((((source != null) ? source.GetHashCode() : 0) * 397) ^ ((master != null) ? master.GetHashCode() : 0)) * 397) ^ ((objectiveType != null) ? objectiveType.GetHashCode() : 0);
		}

		public static bool Equals(ObjectiveSourceDescriptor a, ObjectiveSourceDescriptor b)
		{
			if (a.source == b.source && a.master == b.master)
			{
				return a.objectiveType == b.objectiveType;
			}
			return false;
		}

		public bool Equals(ObjectiveSourceDescriptor other)
		{
			if (source == other.source && master == other.master)
			{
				return objectiveType == other.objectiveType;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (obj is ObjectiveSourceDescriptor)
			{
				return Equals((ObjectiveSourceDescriptor)obj);
			}
			return false;
		}
	}

	public class ObjectiveTracker
	{
		public ObjectiveSourceDescriptor sourceDescriptor;

		public ObjectivePanelController owner;

		public bool isRelevant;

		public bool isPrimary;

		public bool isTutorialCrossfading;

		public bool useLayoutAnimation;

		protected Image checkbox;

		protected TextMeshProUGUI label;

		protected string cachedString;

		protected string baseToken = "";

		protected bool retired;

		protected bool lastRetiredState;

		public GameObject stripObject { get; private set; }

		protected virtual bool shouldConsiderComplete => retired;

		public void SetStrip(GameObject stripObject)
		{
			this.stripObject = stripObject;
			label = stripObject.transform.Find("Label").GetComponent<TextMeshProUGUI>();
			checkbox = label.transform.Find("Checkbox").GetComponent<Image>();
			UpdateStrip();
		}

		public string GetString()
		{
			if (IsDirty())
			{
				cachedString = GenerateString();
			}
			return cachedString;
		}

		protected virtual string GenerateString()
		{
			return Language.GetString(baseToken);
		}

		protected virtual bool IsDirty()
		{
			return cachedString == null;
		}

		public void Retire()
		{
			retired = true;
			OnRetired();
			UpdateStrip();
		}

		protected virtual void OnRetired()
		{
		}

		public virtual void UpdateStrip()
		{
			if ((bool)label && GetString() != label.text)
			{
				label.text = GetString();
				label.color = (retired ? Color.gray : Color.white);
				if (retired)
				{
					label.fontStyle |= FontStyles.Strikethrough;
				}
			}
			if ((bool)checkbox)
			{
				bool flag = shouldConsiderComplete;
				if (flag != lastRetiredState)
				{
					checkbox.sprite = (flag ? owner.checkboxSuccessSprite : owner.checkboxActiveSprite);
					checkbox.color = (flag ? Color.yellow : Color.white);
					lastRetiredState = flag;
				}
			}
		}

		public static ObjectiveTracker Instantiate(ObjectiveSourceDescriptor sourceDescriptor)
		{
			if (sourceDescriptor.objectiveType != null && sourceDescriptor.objectiveType.IsSubclassOf(typeof(ObjectiveTracker)))
			{
				ObjectiveTracker obj = (ObjectiveTracker)Activator.CreateInstance(sourceDescriptor.objectiveType);
				obj.sourceDescriptor = sourceDescriptor;
				return obj;
			}
			Debug.LogFormat("Bad objectiveType {0}", sourceDescriptor.objectiveType?.FullName);
			return null;
		}
	}

	private class FindTeleporterObjectiveTracker : ObjectiveTracker
	{
		public FindTeleporterObjectiveTracker()
		{
			baseToken = "OBJECTIVE_FIND_TELEPORTER";
			isPrimary = true;
		}
	}

	private class ActivateGoldshoreBeaconTracker : ObjectiveTracker
	{
		private int cachedActiveBeaconCount = -1;

		private int cachedRequiredBeaconCount = -1;

		private GoldshoresMissionController missionController => sourceDescriptor.source as GoldshoresMissionController;

		public ActivateGoldshoreBeaconTracker()
		{
			baseToken = "OBJECTIVE_GOLDSHORES_ACTIVATE_BEACONS";
		}

		private bool UpdateCachedValues()
		{
			int beaconsActive = missionController.beaconsActive;
			int beaconCount = missionController.beaconCount;
			if (beaconsActive != cachedActiveBeaconCount || beaconCount != cachedRequiredBeaconCount)
			{
				cachedActiveBeaconCount = beaconsActive;
				cachedRequiredBeaconCount = beaconCount;
				return true;
			}
			return false;
		}

		protected override string GenerateString()
		{
			UpdateCachedValues();
			return string.Format(Language.GetString(baseToken), cachedActiveBeaconCount, cachedRequiredBeaconCount);
		}

		protected override bool IsDirty()
		{
			if (!(sourceDescriptor.source as GoldshoresMissionController))
			{
				return true;
			}
			return UpdateCachedValues();
		}
	}

	private class ClearArena : ObjectiveTracker
	{
		public ClearArena()
		{
			baseToken = "OBJECTIVE_CLEAR_ARENA";
		}

		protected override string GenerateString()
		{
			ArenaMissionController instance = ArenaMissionController.instance;
			return string.Format(Language.GetString(baseToken), instance.clearedRounds, instance.totalRoundsMax);
		}

		protected override bool IsDirty()
		{
			return true;
		}
	}

	private class DestroyTimeCrystals : ObjectiveTracker
	{
		public DestroyTimeCrystals()
		{
			baseToken = "OBJECTIVE_WEEKLYRUN_DESTROY_CRYSTALS";
		}

		protected override string GenerateString()
		{
			WeeklyRun weeklyRun = Run.instance as WeeklyRun;
			return string.Format(Language.GetString(baseToken), weeklyRun.crystalsKilled, weeklyRun.crystalsRequiredToKill);
		}

		protected override bool IsDirty()
		{
			return true;
		}
	}

	private class FinishTeleporterObjectiveTracker : ObjectiveTracker
	{
		public FinishTeleporterObjectiveTracker()
		{
			baseToken = "OBJECTIVE_FINISH_TELEPORTER";
		}
	}

	private class ObjectiveStripAnimation
	{
		public enum StripAnimationType
		{
			Enter,
			Exit
		}

		public float percentComplete;

		public readonly ObjectiveTracker objectiveTracker;

		protected readonly LayoutElement layoutElement;

		protected readonly CanvasGroup canvasGroup;

		protected readonly ObjectiveStripAnimationParams AnimParams;

		protected readonly StripAnimationType _animationType;

		public StripAnimationType AnimationType => _animationType;

		protected ObjectiveStripAnimation(ObjectiveTracker objectiveTracker, StripAnimationType animType)
		{
			_animationType = animType;
			if ((bool)objectiveTracker.stripObject)
			{
				this.objectiveTracker = objectiveTracker;
				layoutElement = objectiveTracker.stripObject.GetComponent<LayoutElement>();
				canvasGroup = objectiveTracker.stripObject.GetComponent<CanvasGroup>();
				AnimParams = objectiveTracker.stripObject.GetComponent<ObjectiveStripAnimationParams>();
			}
		}

		public virtual void SetPercentComplete(float newPercentComplete)
		{
			percentComplete = newPercentComplete;
		}
	}

	private class StripExitAnimation : ObjectiveStripAnimation
	{
		protected readonly float originalHeight;

		protected float alphaStartPercent;

		protected float alphaEndPercent;

		protected float heightStartPercent;

		protected float heightEndPercent;

		public StripExitAnimation(ObjectiveTracker objectiveTracker)
			: base(objectiveTracker, StripAnimationType.Exit)
		{
			alphaStartPercent = AnimParams.GetParam("exitAlphaStartPercent", 0.5f);
			alphaEndPercent = AnimParams.GetParam("exitAlphaEndPercent", 0.75f);
			heightStartPercent = AnimParams.GetParam("exitHeightStartPercent", 0.75f);
			heightEndPercent = AnimParams.GetParam("exitHeightEndPercent", 1f);
			if ((bool)objectiveTracker.stripObject)
			{
				originalHeight = layoutElement.minHeight;
			}
		}

		public override void SetPercentComplete(float newPercentComplete)
		{
			base.SetPercentComplete(newPercentComplete);
			if ((bool)objectiveTracker.stripObject)
			{
				float alpha = Mathf.Clamp01(Util.Remap(percentComplete, alphaStartPercent, alphaEndPercent, 1f, 0f));
				canvasGroup.alpha = alpha;
				float num = Mathf.Clamp01(Util.Remap(percentComplete, heightStartPercent, heightEndPercent, 1f, 0f));
				num *= num;
				layoutElement.minHeight = num * originalHeight;
				layoutElement.preferredHeight = layoutElement.minHeight;
				layoutElement.flexibleHeight = 0f;
			}
		}
	}

	private class StripEnterAnimation : ObjectiveStripAnimation
	{
		private readonly float finalHeight;

		private readonly bool useLayoutAnimation;

		protected float alphaStartPercent;

		protected float alphaEndPercent;

		protected float heightStartPercent;

		protected float heightEndPercent;

		public StripEnterAnimation(ObjectiveTracker objectiveTracker)
			: base(objectiveTracker, StripAnimationType.Enter)
		{
			alphaStartPercent = AnimParams.GetParam("enterAlphaStartPercent", 0.25f);
			alphaEndPercent = AnimParams.GetParam("enterAlphaEndPercent", 0.5f);
			heightStartPercent = AnimParams.GetParam("enterHeightStartPercent", 0f);
			heightEndPercent = AnimParams.GetParam("enterHeightEndPercent", 0.25f);
			if ((bool)objectiveTracker.stripObject)
			{
				useLayoutAnimation = objectiveTracker.useLayoutAnimation;
				finalHeight = layoutElement.minHeight;
			}
		}

		public override void SetPercentComplete(float newPercentComplete)
		{
			base.SetPercentComplete(newPercentComplete);
			if ((bool)objectiveTracker.stripObject)
			{
				float alpha = Mathf.Clamp01(Util.Remap(percentComplete, alphaStartPercent, alphaEndPercent, 0f, 1f));
				canvasGroup.alpha = alpha;
				if (useLayoutAnimation)
				{
					float num = Mathf.Clamp01(Util.Remap(percentComplete, heightStartPercent, heightEndPercent, 0f, 1f));
					num *= num;
					layoutElement.minHeight = num * finalHeight;
					layoutElement.preferredHeight = layoutElement.minHeight;
					layoutElement.flexibleHeight = 0f;
				}
			}
		}
	}

	private class StripTutorialCrossfade : StripEnterAnimation
	{
		private readonly float _targetX;

		private readonly RectTransform rect;

		private readonly float maxHorizontal;

		private readonly float crossfadeStartPercent;

		private readonly float crossfadeEndPercent;

		public StripTutorialCrossfade(ObjectiveTracker objectiveTracker)
			: base(objectiveTracker)
		{
			if ((bool)objectiveTracker.stripObject && objectiveTracker.stripObject.transform is RectTransform rectTransform)
			{
				rect = rectTransform;
				_targetX = rectTransform.localPosition.x;
				maxHorizontal = AnimParams.GetParam("maxHorizontalLerp", -50f);
				crossfadeStartPercent = AnimParams.GetParam("crossfadeStartPercent", 0.25f);
				crossfadeEndPercent = AnimParams.GetParam("crossfadeEndPercent", 0.5f);
				alphaStartPercent = AnimParams.GetParam("crossfadeAlphaStartPercent", 0f);
				alphaEndPercent = AnimParams.GetParam("crossfadeAlphaEndPercent", 0.25f);
			}
		}

		public override void SetPercentComplete(float newPercentComplete)
		{
			base.SetPercentComplete(newPercentComplete);
			if ((bool)rect)
			{
				float num = Mathf.Clamp01(Util.Remap(percentComplete, crossfadeStartPercent, crossfadeEndPercent, 1f, 0f));
				float x = _targetX + maxHorizontal * num;
				Vector3 localPosition = rect.localPosition;
				localPosition.x = x;
				rect.localPosition = localPosition;
			}
		}
	}

	public RectTransform objectiveTrackerContainer;

	public GameObject objectiveTrackerPrefab;

	public Sprite checkboxActiveSprite;

	public Sprite checkboxSuccessSprite;

	public Sprite checkboxFailSprite;

	private CharacterMaster currentMaster;

	private readonly List<ObjectiveTracker> objectiveTrackers = new List<ObjectiveTracker>();

	private Dictionary<ObjectiveSourceDescriptor, ObjectiveTracker> objectiveSourceToTrackerDictionary = new Dictionary<ObjectiveSourceDescriptor, ObjectiveTracker>(EqualityComparer<ObjectiveSourceDescriptor>.Default);

	private readonly List<ObjectiveSourceDescriptor> objectiveSourceDescriptors = new List<ObjectiveSourceDescriptor>();

	private readonly List<ObjectiveStripAnimation> objectiveStripAnimations = new List<ObjectiveStripAnimation>();

	public static event Action<CharacterMaster, List<ObjectiveSourceDescriptor>> collectObjectiveSources;

	public void SetCurrentMaster(CharacterMaster newMaster)
	{
		if (!(newMaster == currentMaster))
		{
			for (int num = objectiveTrackers.Count - 1; num >= 0; num--)
			{
				UnityEngine.Object.Destroy(objectiveTrackers[num].stripObject);
			}
			objectiveTrackers.Clear();
			objectiveSourceToTrackerDictionary.Clear();
			currentMaster = newMaster;
			RefreshObjectiveTrackers();
		}
	}

	public bool GetPrimaryObjectiveTracker(out ObjectiveTracker primaryObjectiveTracker)
	{
		int num = objectiveTrackers.FindIndex((ObjectiveTracker tracker) => tracker.isPrimary);
		if (num >= 0)
		{
			primaryObjectiveTracker = objectiveTrackers[num];
			return true;
		}
		primaryObjectiveTracker = null;
		return false;
	}

	private void AddObjectiveTracker(ObjectiveTracker objectiveTracker)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(objectiveTrackerPrefab, objectiveTrackerContainer);
		gameObject.SetActive(value: true);
		objectiveTracker.owner = this;
		objectiveTracker.SetStrip(gameObject);
		objectiveTrackers.Add(objectiveTracker);
		objectiveSourceToTrackerDictionary.Add(objectiveTracker.sourceDescriptor, objectiveTracker);
		if (objectiveTracker.isTutorialCrossfading)
		{
			TutorialObjectiveCrossfadeHelper component = gameObject.GetComponent<TutorialObjectiveCrossfadeHelper>();
			if ((object)component != null)
			{
				CanvasGroup component2 = gameObject.GetComponent<CanvasGroup>();
				if ((object)component2 != null)
				{
					component2.alpha = 0f;
				}
				component.FinishCrossfadeAction = delegate
				{
					AddAnimation(new StripTutorialCrossfade(objectiveTracker));
				};
			}
		}
		else
		{
			AddAnimation(new StripEnterAnimation(objectiveTracker));
		}
	}

	private void RemoveObjectiveTracker(ObjectiveTracker objectiveTracker)
	{
		objectiveTrackers.Remove(objectiveTracker);
		objectiveSourceToTrackerDictionary.Remove(objectiveTracker.sourceDescriptor);
		objectiveTracker.Retire();
		AddAnimation(new StripExitAnimation(objectiveTracker));
	}

	private void RefreshObjectiveTrackers()
	{
		foreach (ObjectiveTracker objectiveTracker2 in objectiveTrackers)
		{
			objectiveTracker2.isRelevant = false;
		}
		if ((bool)currentMaster)
		{
			GetObjectiveSources(currentMaster, objectiveSourceDescriptors);
			foreach (ObjectiveSourceDescriptor objectiveSourceDescriptor in objectiveSourceDescriptors)
			{
				if (objectiveSourceToTrackerDictionary.TryGetValue(objectiveSourceDescriptor, out var value))
				{
					value.isRelevant = true;
					continue;
				}
				ObjectiveTracker objectiveTracker = ObjectiveTracker.Instantiate(objectiveSourceDescriptor);
				objectiveTracker.isRelevant = true;
				bool isTutorialEnabled = TutorialManager.isTutorialEnabled;
				objectiveTracker.isTutorialCrossfading = objectiveTracker.isPrimary && isTutorialEnabled && (bool)Run.instance && Run.instance.stageClearCount == 0;
				AddObjectiveTracker(objectiveTracker);
			}
		}
		for (int num = objectiveTrackers.Count - 1; num >= 0; num--)
		{
			if (!objectiveTrackers[num].isRelevant)
			{
				RemoveObjectiveTracker(objectiveTrackers[num]);
			}
		}
		foreach (ObjectiveTracker objectiveTracker3 in objectiveTrackers)
		{
			objectiveTracker3.UpdateStrip();
		}
	}

	private void GetObjectiveSources(CharacterMaster master, [NotNull] List<ObjectiveSourceDescriptor> output)
	{
		output.Clear();
		WeeklyRun weeklyRun = Run.instance as WeeklyRun;
		if ((bool)weeklyRun && weeklyRun.crystalsRequiredToKill > weeklyRun.crystalsKilled)
		{
			output.Add(new ObjectiveSourceDescriptor
			{
				source = Run.instance,
				master = master,
				objectiveType = typeof(DestroyTimeCrystals)
			});
		}
		TeleporterInteraction instance = TeleporterInteraction.instance;
		if ((bool)instance)
		{
			Type type = null;
			if (instance.isCharged && !instance.isInFinalSequence)
			{
				type = typeof(FinishTeleporterObjectiveTracker);
			}
			else if (instance.isIdle)
			{
				type = typeof(FindTeleporterObjectiveTracker);
			}
			if (type != null)
			{
				output.Add(new ObjectiveSourceDescriptor
				{
					source = instance,
					master = master,
					objectiveType = type
				});
			}
		}
		if ((bool)GoldshoresMissionController.instance)
		{
			Type type2 = GoldshoresMissionController.instance.entityStateMachine.state.GetType();
			if ((type2 == typeof(ActivateBeacons) || type2 == typeof(GoldshoresBossfight)) && GoldshoresMissionController.instance.beaconsActive < GoldshoresMissionController.instance.beaconCount)
			{
				output.Add(new ObjectiveSourceDescriptor
				{
					source = GoldshoresMissionController.instance,
					master = master,
					objectiveType = typeof(ActivateGoldshoreBeaconTracker)
				});
			}
		}
		if ((bool)ArenaMissionController.instance && ArenaMissionController.instance.clearedRounds < ArenaMissionController.instance.totalRoundsMax)
		{
			output.Add(new ObjectiveSourceDescriptor
			{
				source = ArenaMissionController.instance,
				master = master,
				objectiveType = typeof(ClearArena)
			});
		}
		ObjectivePanelController.collectObjectiveSources?.Invoke(master, output);
	}

	private void Update()
	{
		RefreshObjectiveTrackers();
		RunObjectiveAnimations();
	}

	private void AddAnimation(ObjectiveStripAnimation newAnimation)
	{
		if (newAnimation.AnimationType == ObjectiveStripAnimation.StripAnimationType.Exit)
		{
			for (int num = objectiveStripAnimations.Count - 1; num >= 0; num--)
			{
				ObjectiveStripAnimation objectiveStripAnimation = objectiveStripAnimations[num];
				if (objectiveStripAnimation.AnimationType == ObjectiveStripAnimation.StripAnimationType.Enter && objectiveStripAnimation.objectiveTracker == newAnimation.objectiveTracker)
				{
					objectiveStripAnimations.RemoveAt(num);
				}
			}
		}
		objectiveStripAnimations.Add(newAnimation);
	}

	private void RunObjectiveAnimations()
	{
		float deltaTime = Time.deltaTime;
		float num = 7f;
		float num2 = deltaTime / num;
		for (int num3 = objectiveStripAnimations.Count - 1; num3 >= 0; num3--)
		{
			ObjectiveStripAnimation objectiveStripAnimation = objectiveStripAnimations[num3];
			float num4 = Mathf.Min(objectiveStripAnimation.percentComplete + num2, 1f);
			objectiveStripAnimation.SetPercentComplete(num4);
			if (num4 >= 1f)
			{
				if (objectiveStripAnimation.AnimationType == ObjectiveStripAnimation.StripAnimationType.Exit)
				{
					UnityEngine.Object.Destroy(objectiveStripAnimation.objectiveTracker.stripObject);
				}
				objectiveStripAnimations.RemoveAt(num3);
			}
		}
	}
}
