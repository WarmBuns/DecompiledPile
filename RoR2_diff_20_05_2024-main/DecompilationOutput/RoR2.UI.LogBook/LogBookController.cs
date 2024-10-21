using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using EntityStates;
using HG;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoR2.UI.LogBook;

public class LogBookController : MonoBehaviour
{
	private class NavigationPageInfo
	{
		public CategoryDef categoryDef;

		public Entry[] entries;

		public int index;

		public int indexInCategory;
	}

	private class LogBookState : EntityState
	{
		protected LogBookController logBookController;

		protected float unscaledAge;

		public override void OnEnter()
		{
			base.OnEnter();
			logBookController = GetComponent<LogBookController>();
		}

		public override void Update()
		{
			base.Update();
			unscaledAge += Time.unscaledDeltaTime;
		}
	}

	private class FadeState : LogBookState
	{
		private CanvasGroup canvasGroup;

		public float duration = 0.5f;

		public float endValue;

		public override void OnEnter()
		{
			base.OnEnter();
			canvasGroup = GetComponent<CanvasGroup>();
			if ((bool)canvasGroup)
			{
				canvasGroup.alpha = 0f;
			}
		}

		public override void OnExit()
		{
			if ((bool)canvasGroup)
			{
				canvasGroup.alpha = endValue;
			}
			base.OnExit();
		}

		public override void Update()
		{
			if ((bool)canvasGroup)
			{
				canvasGroup.alpha = unscaledAge / duration;
				if (canvasGroup.alpha >= 1f)
				{
					outer.SetNextState(new Idle());
				}
			}
			base.Update();
		}
	}

	private class ChangeEntriesPageState : LogBookState
	{
		private int oldPageIndex;

		public NavigationPageInfo newNavigationPageInfo;

		public float duration = 0.1f;

		public Vector2 moveDirection;

		private GameObject oldPage;

		private GameObject newPage;

		private Vector2 oldPageTargetPosition;

		private Vector2 newPageTargetPosition;

		private Vector2 containerSize = Vector2.zero;

		public override void OnEnter()
		{
			base.OnEnter();
			if ((bool)logBookController)
			{
				oldPageIndex = logBookController.currentPageIndex;
				oldPage = logBookController.currentEntriesPageObject;
				newPage = logBookController.BuildEntriesPage(newNavigationPageInfo);
				containerSize = logBookController.entryPageContainer.rect.size;
			}
			SetPagePositions(0f);
		}

		public override void OnExit()
		{
			base.OnExit();
			EntityState.Destroy(oldPage);
			if ((bool)logBookController)
			{
				logBookController.currentEntriesPageObject = newPage;
				logBookController.currentPageIndex = newNavigationPageInfo.indexInCategory;
			}
		}

		private void SetPagePositions(float t)
		{
			Vector2 vector = new Vector2(containerSize.x * (0f - moveDirection.x), containerSize.y * moveDirection.y);
			Vector2 vector2 = vector * t;
			if ((bool)oldPage)
			{
				oldPage.transform.localPosition = vector2;
			}
			if ((bool)newPage)
			{
				newPage.transform.localPosition = vector2 - vector;
			}
		}

		public override void Update()
		{
			base.Update();
			float num = Mathf.Clamp01(unscaledAge / duration);
			SetPagePositions(num);
			if (num == 1f)
			{
				outer.SetNextState(new Idle());
			}
		}
	}

	private class ChangeCategoryState : LogBookState
	{
		private int oldCategoryIndex;

		public int newCategoryIndex;

		public bool goToLastPage;

		public float duration = 0.1f;

		private GameObject oldPage;

		private GameObject newPage;

		private Vector2 oldPageTargetPosition;

		private Vector2 newPageTargetPosition;

		private Vector2 moveDirection;

		private Vector2 containerSize = Vector2.zero;

		private NavigationPageInfo[] newNavigationPages;

		private int destinationPageIndex;

		private NavigationPageInfo newNavigationPageInfo;

		private int frame;

		public override void OnEnter()
		{
			base.OnEnter();
			if ((bool)logBookController)
			{
				oldCategoryIndex = logBookController.currentCategoryIndex;
				oldPage = logBookController.currentEntriesPageObject;
				newNavigationPages = logBookController.GetCategoryPages(newCategoryIndex);
				destinationPageIndex = newNavigationPages[0].index;
				if (goToLastPage)
				{
					destinationPageIndex = newNavigationPages[newNavigationPages.Length - 1].index;
					Debug.LogFormat("goToLastPage=true destinationPageIndex={0}", destinationPageIndex);
				}
				newNavigationPageInfo = logBookController.allNavigationPages[destinationPageIndex];
				newPage = logBookController.BuildEntriesPage(newNavigationPageInfo);
				containerSize = logBookController.entryPageContainer.rect.size;
				moveDirection = new Vector2(Mathf.Sign(newCategoryIndex - oldCategoryIndex), 0f);
			}
			SetPagePositions(0f);
		}

		public override void OnExit()
		{
			EntityState.Destroy(oldPage);
			if ((bool)logBookController)
			{
				logBookController.currentEntriesPageObject = newPage;
				logBookController.currentPageIndex = newNavigationPageInfo.indexInCategory;
				logBookController.desiredPageIndex = newNavigationPageInfo.indexInCategory;
				logBookController.currentCategoryIndex = newCategoryIndex;
				logBookController.availableNavigationPages = newNavigationPages;
				logBookController.currentCategoryLabel.token = categories[newCategoryIndex].nameToken;
				logBookController.categoryHightlightRect.SetParent(logBookController.navigationCategoryButtonAllocator.elements[newCategoryIndex].transform, worldPositionStays: false);
				logBookController.categoryHightlightRect.gameObject.SetActive(value: false);
				logBookController.categoryHightlightRect.gameObject.SetActive(value: true);
				if (logBookController.moveNavigationPageIndicatorContainerToCategoryButton)
				{
					logBookController.navigationPageIndicatorContainer.SetParent(logBookController.navigationCategoryButtonAllocator.elements[newCategoryIndex].transform, worldPositionStays: false);
				}
			}
			base.OnExit();
		}

		private void SetPagePositions(float t)
		{
			Vector2 vector = new Vector2(containerSize.x * (0f - moveDirection.x), containerSize.y * moveDirection.y);
			Vector2 vector2 = vector * t;
			if ((bool)oldPage)
			{
				oldPage.transform.localPosition = vector2;
			}
			if ((bool)newPage)
			{
				newPage.transform.localPosition = vector2 - vector;
				if (frame == 4)
				{
					newPage.GetComponent<GridLayoutGroup>().enabled = false;
				}
			}
		}

		public override void Update()
		{
			base.Update();
			frame++;
			float num = Mathf.Clamp01(unscaledAge / duration);
			SetPagePositions(num);
			if (num == 1f)
			{
				outer.SetNextState(new Idle());
			}
		}
	}

	private class EnterLogViewState : LogBookState
	{
		public Texture iconTexture;

		public RectTransform startRectTransform;

		public RectTransform endRectTransform;

		public Entry entry;

		private GameObject flyingIcon;

		private RectTransform flyingIconTransform;

		private RawImage flyingIconImage;

		private float duration = 0.75f;

		private Rect startRect;

		private Rect midRect;

		private Rect endRect;

		private bool submittedViewEntry;

		public override void OnEnter()
		{
			base.OnEnter();
			flyingIcon = new GameObject("FlyingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
			flyingIconTransform = (RectTransform)flyingIcon.transform;
			flyingIconTransform.SetParent(logBookController.transform, worldPositionStays: false);
			flyingIconTransform.localScale = Vector3.one;
			flyingIconImage = flyingIconTransform.GetComponent<RawImage>();
			flyingIconImage.texture = iconTexture;
			Vector3[] array = new Vector3[4];
			startRectTransform.GetWorldCorners(array);
			startRect = GetRectRelativeToParent(array);
			midRect = new Rect(((RectTransform)logBookController.transform).rect.center, startRect.size);
			endRectTransform.GetWorldCorners(array);
			endRect = GetRectRelativeToParent(array);
			SetIconRect(startRect);
		}

		private void SetIconRect(Rect rect)
		{
			flyingIconTransform.position = rect.position;
			flyingIconTransform.offsetMin = rect.min;
			flyingIconTransform.offsetMax = rect.max;
		}

		private Rect GetRectRelativeToParent(Vector3[] corners)
		{
			for (int i = 0; i < 4; i++)
			{
				corners[i] = logBookController.transform.InverseTransformPoint(corners[i]);
			}
			Rect result = default(Rect);
			result.xMin = corners[0].x;
			result.xMax = corners[2].x;
			result.yMin = corners[0].y;
			result.yMax = corners[2].y;
			return result;
		}

		private static Rect RectFromWorldCorners(Vector3[] corners)
		{
			Rect result = default(Rect);
			result.xMin = corners[0].x;
			result.xMax = corners[2].x;
			result.yMin = corners[0].y;
			result.yMax = corners[2].y;
			return result;
		}

		private static Rect LerpRect(Rect a, Rect b, float t)
		{
			Rect result = default(Rect);
			result.min = Vector2.LerpUnclamped(a.min, b.min, t);
			result.max = Vector2.LerpUnclamped(a.max, b.max, t);
			return result;
		}

		public override void OnExit()
		{
			EntityState.Destroy(flyingIcon);
			base.OnExit();
		}

		public override void Update()
		{
			base.Update();
			float num = Mathf.Min(unscaledAge / duration, 1f);
			if (num < 0.1f)
			{
				Util.Remap(num, 0f, 0.1f, 0f, 1f);
				SetIconRect(startRect);
			}
			if (num < 0.2f)
			{
				float t = Util.Remap(num, 0.1f, 0.2f, 0f, 1f);
				SetIconRect(LerpRect(startRect, midRect, t));
			}
			else if (num < 0.4f)
			{
				Util.Remap(num, 0.2f, 0.4f, 0f, 1f);
				SetIconRect(midRect);
			}
			else if (num < 0.6f)
			{
				float t2 = Util.Remap(num, 0.4f, 0.6f, 0f, 1f);
				SetIconRect(LerpRect(midRect, endRect, t2));
			}
			else if (num < 1f)
			{
				float num2 = Util.Remap(num, 0.6f, 1f, 0f, 1f);
				flyingIconImage.color = new Color(1f, 1f, 1f, 1f - num2);
				SetIconRect(endRect);
				if (!submittedViewEntry)
				{
					submittedViewEntry = true;
					logBookController.ViewEntry(entry);
				}
			}
			else
			{
				outer.SetNextState(new Idle());
			}
		}
	}

	[Header("Navigation")]
	public GameObject navigationPanel;

	public RectTransform categoryContainer;

	public GameObject categorySpaceFiller;

	public int categorySpaceFillerCount;

	public Color spaceFillerColor;

	private UIElementAllocator<MPButton> navigationCategoryButtonAllocator;

	public RectTransform entryPageContainer;

	public GameObject entryPagePrefab;

	public RectTransform navigationPageIndicatorContainer;

	public GameObject navigationPageIndicatorPrefab;

	public bool moveNavigationPageIndicatorContainerToCategoryButton;

	private UIElementAllocator<MPButton> navigationPageIndicatorAllocator;

	public MPButton previousPageButton;

	public MPButton nextPageButton;

	public LanguageTextMeshController currentCategoryLabel;

	private RectTransform categoryHightlightRect;

	[Header("PageViewer")]
	public UnityEvent OnViewEntry;

	public GameObject pageViewerPanel;

	public MPButton pageViewerBackButton;

	[Header("Misc")]
	public GameObject categoryButtonPrefab;

	public GameObject headerHighlightPrefab;

	public LanguageTextMeshController hoverLanguageTextMeshController;

	public string hoverDescriptionFormatString;

	private EntityStateMachine stateMachine;

	private UILayerKey uiLayerKey;

	public static CategoryDef[] categories;

	public static ResourceAvailability availability;

	private static bool IsInitialized;

	public static bool IsStaticDataReady;

	private NavigationPageInfo[] _availableNavigationPages = Array.Empty<NavigationPageInfo>();

	private GameObject currentEntriesPageObject;

	private int currentCategoryIndex;

	private int desiredCategoryIndex;

	private int currentPageIndex;

	private int desiredPageIndex;

	private bool goToEndOfNextCategory;

	private NavigationPageInfo[] allNavigationPages;

	private NavigationPageInfo[][] navigationPagesByCategory;

	private static List<EntitlementDef> lastBuiltEntitlements;

	private NavigationPageInfo[] availableNavigationPages
	{
		get
		{
			return _availableNavigationPages;
		}
		set
		{
			int num = _availableNavigationPages.Length;
			_availableNavigationPages = value;
			if (num != availableNavigationPages.Length)
			{
				navigationPageIndicatorAllocator.AllocateElements(availableNavigationPages.Length);
			}
		}
	}

	public static event Action OnViewablesRegistered;

	private void Awake()
	{
		EntitlementManager.UpdateLocalUsersEntitlements();
		navigationCategoryButtonAllocator = new UIElementAllocator<MPButton>(categoryContainer, categoryButtonPrefab);
		navigationPageIndicatorAllocator = new UIElementAllocator<MPButton>(navigationPageIndicatorContainer, navigationPageIndicatorPrefab);
		navigationPageIndicatorAllocator.onCreateElement = delegate(int index, MPButton button)
		{
			button.onClick.AddListener(delegate
			{
				desiredPageIndex = index;
			});
		};
		previousPageButton.onClick.AddListener(OnLeftButton);
		nextPageButton.onClick.AddListener(OnRightButton);
		pageViewerBackButton.onClick.AddListener(ReturnToNavigation);
		stateMachine = base.gameObject.AddComponent<EntityStateMachine>();
		uiLayerKey = base.gameObject.GetComponent<UILayerKey>();
		stateMachine.initialStateType = default(SerializableEntityStateType);
		stateMachine.AllowStartWithoutNetworker = true;
		stateMachine.ShouldStateTransitionOnUpdate = true;
		categoryHightlightRect = (RectTransform)UnityEngine.Object.Instantiate(headerHighlightPrefab, base.transform.parent).transform;
		categoryHightlightRect.gameObject.SetActive(value: false);
	}

	private void Start()
	{
		GeneratePages(LocalUserManager.GetFirstLocalUser()?.userProfile);
		BuildCategoriesButtons();
		stateMachine.SetNextState(new ChangeCategoryState
		{
			newCategoryIndex = 0
		});
	}

	private void BuildCategoriesButtons()
	{
		navigationCategoryButtonAllocator.AllocateElements(categories.Length);
		ReadOnlyCollection<MPButton> elements = navigationCategoryButtonAllocator.elements;
		for (int i = 0; i < categories.Length; i++)
		{
			int categoryIndex = i;
			MPButton mPButton = elements[i];
			CategoryDef categoryDef = categories[i];
			mPButton.GetComponentInChildren<TextMeshProUGUI>().text = Language.GetString(categoryDef.nameToken);
			mPButton.onClick.RemoveAllListeners();
			mPButton.onClick.AddListener(delegate
			{
				OnCategoryClicked(categoryIndex);
			});
			mPButton.requiredTopLayer = uiLayerKey;
			ViewableTag viewableTag = mPButton.gameObject.GetComponent<ViewableTag>();
			if (!viewableTag)
			{
				viewableTag = mPButton.gameObject.AddComponent<ViewableTag>();
			}
			viewableTag.viewableName = categoryDef.viewableNode.fullName;
		}
		if ((bool)categorySpaceFiller)
		{
			for (int j = 0; j < categorySpaceFillerCount; j++)
			{
				UnityEngine.Object.Instantiate(categorySpaceFiller, categoryContainer).gameObject.SetActive(value: true);
			}
		}
	}

	public static IEnumerator Init()
	{
		if (LocalUserManager.isAnyUserSignedIn)
		{
			BuildStaticData();
		}
		yield return null;
		LocalUserManager.onUserSignIn += OnUserSignIn;
		BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated = (Action)Delegate.Combine(BaseUserEntitlementTracker<LocalUser>.OnUserEntitlementsUpdated, new Action(BuildStaticData));
		IsInitialized = true;
	}

	private static void OnUserSignIn(LocalUser obj)
	{
		BuildStaticData();
	}

	private static EntryStatus GetPickupStatus(in Entry entry, UserProfile viewerProfile)
	{
		UnlockableDef unlockableDef = null;
		PickupIndex pickupIndex = (PickupIndex)entry.extraData;
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
		EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
		if (itemIndex != ItemIndex.None)
		{
			unlockableDef = ItemCatalog.GetItemDef(itemIndex).unlockableDef;
		}
		else
		{
			if (equipmentIndex == EquipmentIndex.None)
			{
				return EntryStatus.Unimplemented;
			}
			unlockableDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex).unlockableDef;
		}
		if (!viewerProfile.HasUnlockable(unlockableDef))
		{
			return EntryStatus.Locked;
		}
		if (!viewerProfile.HasDiscoveredPickup(pickupIndex))
		{
			return EntryStatus.Unencountered;
		}
		return EntryStatus.Available;
	}

	private static TooltipContent GetPickupTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		UnlockableDef unlockableDef = PickupCatalog.GetPickupDef((PickupIndex)entry.extraData).unlockableDef;
		TooltipContent result = default(TooltipContent);
		if (status >= EntryStatus.Available)
		{
			result.overrideTitleText = entry.GetDisplayName(userProfile);
			result.titleColor = entry.color;
			if (unlockableDef != null)
			{
				result.overrideBodyText = unlockableDef.getUnlockedString();
			}
			result.bodyToken = "";
			result.bodyColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable);
		}
		else
		{
			result.titleToken = "UNIDENTIFIED";
			result.titleColor = Color.gray;
			result.bodyToken = "";
			switch (status)
			{
			case EntryStatus.Unimplemented:
				result.titleToken = "TOOLTIP_WIP_CONTENT_NAME";
				result.bodyToken = "TOOLTIP_WIP_CONTENT_DESCRIPTION";
				break;
			case EntryStatus.Unencountered:
				result.overrideBodyText = Language.GetString("LOGBOOK_UNLOCK_ITEM_LOG");
				break;
			case EntryStatus.Locked:
				result.overrideBodyText = unlockableDef.getHowToUnlockString();
				break;
			}
			result.bodyColor = Color.white;
		}
		return result;
	}

	private static TooltipContent GetMonsterTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		TooltipContent result = default(TooltipContent);
		result.titleColor = entry.color;
		if (status >= EntryStatus.Available)
		{
			result.overrideTitleText = entry.GetDisplayName(userProfile);
			result.titleColor = entry.color;
			result.bodyToken = "";
		}
		else
		{
			result.titleToken = "UNIDENTIFIED";
			result.titleColor = Color.gray;
			result.bodyToken = "LOGBOOK_UNLOCK_ITEM_MONSTER";
		}
		return result;
	}

	private static TooltipContent GetStageTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		TooltipContent result = default(TooltipContent);
		result.titleColor = entry.color;
		if (status >= EntryStatus.Available)
		{
			result.overrideTitleText = entry.GetDisplayName(userProfile);
			result.titleColor = entry.color;
			result.bodyToken = "";
		}
		else
		{
			result.titleToken = "UNIDENTIFIED";
			result.titleColor = Color.gray;
			result.bodyToken = "LOGBOOK_UNLOCK_ITEM_STAGE";
		}
		return result;
	}

	private static TooltipContent GetSurvivorTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		TooltipContent result = default(TooltipContent);
		UnlockableDef unlockableDef = SurvivorCatalog.FindSurvivorDefFromBody(((CharacterBody)entry.extraData).gameObject).unlockableDef;
		if (status >= EntryStatus.Available)
		{
			result.overrideTitleText = entry.GetDisplayName(userProfile);
			result.titleColor = entry.color;
			result.bodyToken = "";
		}
		else
		{
			result.titleToken = "UNIDENTIFIED";
			result.bodyToken = "";
			result.titleColor = Color.gray;
			switch (status)
			{
			case EntryStatus.Unencountered:
				result.overrideBodyText = Language.GetString("LOGBOOK_UNLOCK_ITEM_SURVIVOR");
				break;
			case EntryStatus.Locked:
				result.overrideBodyText = unlockableDef.getHowToUnlockString();
				break;
			}
		}
		return result;
	}

	private static TooltipContent GetAchievementTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		TooltipContent result = default(TooltipContent);
		UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(((AchievementDef)entry.extraData).unlockableRewardIdentifier);
		result.titleColor = entry.color;
		result.bodyToken = "";
		if (unlockableDef == null)
		{
			result.overrideTitleText = entry.GetDisplayName(userProfile);
			result.titleColor = Color.gray;
			result.overrideBodyText = "ACHIEVEMENT HAS NO UNLOCKABLE DEFINED";
			result.bodyColor = Color.white;
			return result;
		}
		if (status >= EntryStatus.Available)
		{
			result.titleToken = entry.GetDisplayName(userProfile);
			result.titleColor = entry.color;
			result.overrideBodyText = unlockableDef.getUnlockedString();
		}
		else
		{
			result.titleToken = "UNIDENTIFIED";
			result.titleColor = Color.gray;
			if (status == EntryStatus.Locked)
			{
				result.overrideBodyText = Language.GetString("UNIDENTIFIED_DESCRIPTION");
			}
			else
			{
				result.overrideBodyText = unlockableDef.getHowToUnlockString();
			}
		}
		return result;
	}

	private static TooltipContent GetWIPTooltipContent(in Entry entry, UserProfile userProfile, EntryStatus status)
	{
		TooltipContent result = default(TooltipContent);
		result.titleColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.WIP);
		result.titleToken = "TOOLTIP_WIP_CONTENT_NAME";
		result.bodyToken = "TOOLTIP_WIP_CONTENT_DESCRIPTION";
		return result;
	}

	private static EntryStatus GetAlwaysAvailable(UserProfile userProfile, Entry entry)
	{
		return EntryStatus.Available;
	}

	private static EntryStatus GetUnimplemented(in Entry entry, UserProfile viewerProfile)
	{
		return EntryStatus.Unimplemented;
	}

	private static EntryStatus GetStageStatus(in Entry entry, UserProfile viewerProfile)
	{
		UnlockableDef unlockableLogFromBaseSceneName = SceneCatalog.GetUnlockableLogFromBaseSceneName((entry.extraData as SceneDef).baseSceneName);
		if (unlockableLogFromBaseSceneName != null && viewerProfile.HasUnlockable(unlockableLogFromBaseSceneName))
		{
			return EntryStatus.Available;
		}
		return EntryStatus.Unencountered;
	}

	private static EntryStatus GetMonsterStatus(in Entry entry, UserProfile viewerProfile)
	{
		CharacterBody characterBody = (CharacterBody)entry.extraData;
		UnlockableDef unlockableDef = characterBody.GetComponent<DeathRewards>()?.logUnlockableDef;
		if (!unlockableDef)
		{
			return EntryStatus.None;
		}
		if (viewerProfile.HasUnlockable(unlockableDef))
		{
			return EntryStatus.Available;
		}
		if (viewerProfile.statSheet.GetStatValueULong(PerBodyStatDef.killsAgainst, characterBody.gameObject.name) != 0)
		{
			return EntryStatus.Unencountered;
		}
		return EntryStatus.Locked;
	}

	private static EntryStatus GetSurvivorStatus(in Entry entry, UserProfile viewerProfile)
	{
		CharacterBody characterBody = (CharacterBody)entry.extraData;
		SurvivorDef survivorDef = SurvivorCatalog.FindSurvivorDefFromBody(characterBody.gameObject);
		if (!viewerProfile.HasUnlockable(survivorDef.unlockableDef))
		{
			return EntryStatus.Locked;
		}
		if (viewerProfile.statSheet.GetStatValueULong(PerBodyStatDef.totalWins, characterBody.gameObject.name) == 0L)
		{
			return EntryStatus.Unencountered;
		}
		return EntryStatus.Available;
	}

	private static EntryStatus GetAchievementStatus(in Entry entry, UserProfile viewerProfile)
	{
		string identifier = ((AchievementDef)entry.extraData).identifier;
		bool flag = viewerProfile.HasAchievement(identifier);
		if (!viewerProfile.CanSeeAchievement(identifier))
		{
			return EntryStatus.Locked;
		}
		if (!flag)
		{
			return EntryStatus.Unencountered;
		}
		return EntryStatus.Available;
	}

	private static void BuildStaticData()
	{
		RoR2Application.instance.StartCoroutine(WaitForBuildingStaticData());
	}

	static LogBookController()
	{
		categories = Array.Empty<CategoryDef>();
		availability = default(ResourceAvailability);
		IsInitialized = false;
		IsStaticDataReady = false;
		lastBuiltEntitlements = null;
	}

	private NavigationPageInfo[] GetCategoryPages(int categoryIndex)
	{
		if (navigationPagesByCategory.GetLength(0) <= categoryIndex)
		{
			return new NavigationPageInfo[0];
		}
		return navigationPagesByCategory[categoryIndex];
	}

	public void OnLeftButton()
	{
		desiredPageIndex--;
	}

	public void OnRightButton()
	{
		desiredPageIndex++;
	}

	private void OnCategoryClicked(int categoryIndex)
	{
		desiredCategoryIndex = categoryIndex;
		goToEndOfNextCategory = false;
	}

	public void OnCategoryLeftButton()
	{
		desiredCategoryIndex--;
		goToEndOfNextCategory = false;
	}

	public void OnCategoryRightButton()
	{
		desiredCategoryIndex++;
		goToEndOfNextCategory = false;
	}

	private void GeneratePages(UserProfile viewerProfile)
	{
		if (!IsInitialized)
		{
			Init();
		}
		navigationPagesByCategory = new NavigationPageInfo[categories.Length][];
		IEnumerable<NavigationPageInfo> enumerable = Array.Empty<NavigationPageInfo>();
		int num = 0;
		for (int i = 0; i < categories.Length; i++)
		{
			CategoryDef categoryDef = categories[i];
			Entry[] array = categoryDef.BuildEntries(viewerProfile);
			bool fullWidth = categoryDef.fullWidth;
			Vector2 size = entryPageContainer.rect.size;
			if (fullWidth)
			{
				categoryDef.iconSize.x = size.x;
			}
			int num2 = Mathf.FloorToInt(Mathf.Max(size.x / categoryDef.iconSize.x, 1f));
			int num3 = Mathf.FloorToInt(Mathf.Max(size.y / categoryDef.iconSize.y, 1f));
			int num4 = num2 * num3;
			int num5 = Mathf.CeilToInt((float)array.Length / (float)num4);
			if (num5 <= 0)
			{
				num5 = 1;
			}
			NavigationPageInfo[] array2 = new NavigationPageInfo[num5];
			for (int j = 0; j < num5; j++)
			{
				int num6 = j * num4;
				int num7 = array.Length - num6;
				int num8 = num4;
				if (num8 > num7)
				{
					num8 = num7;
				}
				Entry[] array3 = new Entry[num4];
				Array.Copy(array, num6, array3, 0, num8);
				NavigationPageInfo navigationPageInfo = new NavigationPageInfo();
				navigationPageInfo.categoryDef = categoryDef;
				navigationPageInfo.entries = array3;
				navigationPageInfo.index = num++;
				navigationPageInfo.indexInCategory = j;
				array2[j] = navigationPageInfo;
			}
			navigationPagesByCategory[i] = array2;
			enumerable = enumerable.Concat(array2);
		}
		allNavigationPages = enumerable.ToArray();
	}

	private void Update()
	{
		if (desiredPageIndex > availableNavigationPages.Length - 1)
		{
			desiredPageIndex = availableNavigationPages.Length - 1;
			desiredCategoryIndex++;
			goToEndOfNextCategory = false;
		}
		if (desiredPageIndex < 0)
		{
			desiredCategoryIndex--;
			desiredPageIndex = 0;
			goToEndOfNextCategory = true;
		}
		if (desiredCategoryIndex > categories.Length - 1)
		{
			desiredCategoryIndex = 0;
			goToEndOfNextCategory = false;
		}
		if (desiredCategoryIndex < 0)
		{
			desiredCategoryIndex = categories.Length - 1;
			goToEndOfNextCategory = true;
		}
		foreach (MPButton element in navigationPageIndicatorAllocator.elements)
		{
			ColorBlock colors = element.colors;
			colors.colorMultiplier = 1f;
			element.colors = colors;
		}
		if (currentPageIndex < navigationPageIndicatorAllocator.elements.Count)
		{
			MPButton mPButton = navigationPageIndicatorAllocator.elements[currentPageIndex];
			ColorBlock colors2 = mPButton.colors;
			colors2.colorMultiplier = 2f;
			mPButton.colors = colors2;
		}
		if (desiredCategoryIndex != currentCategoryIndex)
		{
			if (stateMachine.state is Idle)
			{
				int num = ((desiredCategoryIndex > currentCategoryIndex) ? 1 : (-1));
				stateMachine.SetNextState(new ChangeCategoryState
				{
					newCategoryIndex = currentCategoryIndex + num,
					goToLastPage = goToEndOfNextCategory
				});
			}
		}
		else if (desiredPageIndex != currentPageIndex && stateMachine.state is Idle)
		{
			int num2 = ((desiredPageIndex > currentPageIndex) ? 1 : (-1));
			stateMachine.SetNextState(new ChangeEntriesPageState
			{
				newNavigationPageInfo = GetCategoryPages(currentCategoryIndex)[currentPageIndex + num2],
				moveDirection = new Vector2(num2, 0f)
			});
		}
	}

	private UserProfile LookUpUserProfile()
	{
		return LocalUserManager.readOnlyLocalUsersList.FirstOrDefault((LocalUser v) => v != null)?.userProfile;
	}

	private GameObject BuildEntriesPage(NavigationPageInfo navigationPageInfo)
	{
		Entry[] entries = navigationPageInfo.entries;
		CategoryDef categoryDef = navigationPageInfo.categoryDef;
		GameObject gameObject = UnityEngine.Object.Instantiate(entryPagePrefab, entryPageContainer);
		GridLayoutGroup component = gameObject.GetComponent<GridLayoutGroup>();
		component.cellSize = categoryDef.iconSize;
		component.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		float num = entryPageContainer.rect.width - (float)component.padding.left - (float)component.padding.right;
		component.constraintCount = (int)(num / (component.cellSize.x + component.spacing.x));
		UIElementAllocator<RectTransform> uIElementAllocator = new UIElementAllocator<RectTransform>((RectTransform)gameObject.transform, categoryDef.iconPrefab);
		uIElementAllocator.AllocateElements(entries.Length);
		UserProfile userProfile = LookUpUserProfile();
		ReadOnlyCollection<RectTransform> elements = uIElementAllocator.elements;
		for (int i = 0; i < elements.Count; i++)
		{
			RectTransform rectTransform = elements[i];
			HGButton component2 = rectTransform.GetComponent<HGButton>();
			Entry entry = ((i < entries.Length) ? entries[i] : null);
			EntryStatus entryStatus = entry?.GetStatus(userProfile) ?? EntryStatus.None;
			if (entryStatus != 0)
			{
				TooltipContent tooltipContent = entry.GetTooltipContent(userProfile, entryStatus);
				categoryDef.initializeElementGraphics?.Invoke(rectTransform.gameObject, entry, entryStatus, userProfile);
				if ((bool)component2)
				{
					UnityEngine.UI.Navigation navigation = component2.navigation;
					navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
					int index = (i + elements.Count - 1) % elements.Count;
					navigation.selectOnLeft = elements[index].GetComponent<HGButton>();
					int index2 = (i + elements.Count + 1) % elements.Count;
					navigation.selectOnRight = elements[index2].GetComponent<HGButton>();
					if (i - component.constraintCount >= 0)
					{
						navigation.selectOnUp = elements[i - component.constraintCount].GetComponent<HGButton>();
					}
					if (i + component.constraintCount < elements.Count)
					{
						navigation.selectOnDown = elements[i + component.constraintCount].GetComponent<HGButton>();
					}
					component2.navigation = navigation;
					bool flag = entryStatus >= EntryStatus.Available;
					component2.interactable = true;
					component2.disableGamepadClick = component2.disableGamepadClick || !flag;
					component2.disablePointerClick = component2.disablePointerClick || !flag;
					component2.imageOnInteractable = (flag ? component2.imageOnInteractable : null);
					component2.requiredTopLayer = uiLayerKey;
					component2.updateTextOnHover = true;
					component2.hoverLanguageTextMeshController = hoverLanguageTextMeshController;
					string titleText = tooltipContent.GetTitleText();
					string bodyText = tooltipContent.GetBodyText();
					Color titleColor = tooltipContent.titleColor;
					titleColor.a = 0.2f;
					component2.hoverToken = Language.GetStringFormatted("LOGBOOK_HOVER_DESCRIPTION_FORMAT", titleText, bodyText, ColorUtility.ToHtmlStringRGBA(titleColor));
				}
				if (entry.viewableNode != null)
				{
					ViewableTag viewableTag = rectTransform.gameObject.GetComponent<ViewableTag>();
					if (!viewableTag)
					{
						viewableTag = rectTransform.gameObject.AddComponent<ViewableTag>();
						viewableTag.viewableVisualStyle = ViewableTag.ViewableVisualStyle.Icon;
					}
					viewableTag.viewableName = entry.viewableNode.fullName;
				}
			}
			if (entryStatus >= EntryStatus.Available && (bool)component2)
			{
				component2.onClick.AddListener(delegate
				{
					ViewEntry(entry);
				});
			}
			if (entryStatus == EntryStatus.None)
			{
				if ((bool)component2)
				{
					component2.enabled = false;
					component2.targetGraphic.color = spaceFillerColor;
				}
				else
				{
					rectTransform.GetComponent<Image>().color = spaceFillerColor;
				}
				for (int num2 = rectTransform.childCount - 1; num2 >= 0; num2--)
				{
					UnityEngine.Object.Destroy(rectTransform.GetChild(num2).gameObject);
				}
			}
			if ((bool)component2 && i == 0)
			{
				component2.defaultFallbackButton = true;
			}
		}
		gameObject.gameObject.SetActive(value: true);
		GridLayoutGroup gridLayoutGroup = gameObject.GetComponent<GridLayoutGroup>();
		Action destroyLayoutGroup = null;
		int frameTimer = 2;
		destroyLayoutGroup = delegate
		{
			int num3 = frameTimer - 1;
			frameTimer = num3;
			if (frameTimer <= 0)
			{
				gridLayoutGroup.enabled = false;
				RoR2Application.onLateUpdate -= destroyLayoutGroup;
			}
		};
		RoR2Application.onLateUpdate += destroyLayoutGroup;
		return gameObject;
	}

	private void ViewEntry(Entry entry)
	{
		OnViewEntry.Invoke();
		LogBookPage component = pageViewerPanel.GetComponent<LogBookPage>();
		component.SetEntry(LookUpUserProfile(), entry);
		component.modelPanel.SetAnglesForCharacterThumbnailForSeconds(0.5f);
		ViewableTrigger.TriggerView(entry.viewableNode?.fullName);
	}

	private void ReturnToNavigation()
	{
		navigationPanel.SetActive(value: true);
		pageViewerPanel.SetActive(value: false);
	}

	private static bool UnlockableExists(UnlockableDef unlockableDef)
	{
		return unlockableDef;
	}

	private static bool IsEntryBodyWithoutLore(in Entry entry)
	{
		CharacterBody obj = (CharacterBody)entry.extraData;
		bool flag = false;
		string text = "";
		string baseNameToken = obj.baseNameToken;
		if (!string.IsNullOrEmpty(baseNameToken))
		{
			text = baseNameToken.Replace("_NAME", "_LORE");
			if (Language.english.TokenIsRegistered(text))
			{
				flag = true;
			}
		}
		return !flag;
	}

	private static bool IsEntryPickupItemWithoutLore(in Entry entry)
	{
		ItemDef itemDef = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef((PickupIndex)entry.extraData).itemIndex);
		return !Language.english.TokenIsRegistered(itemDef.loreToken);
	}

	private static bool IsEntryPickupEquipmentWithoutLore(in Entry entry)
	{
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(PickupCatalog.GetPickupDef((PickupIndex)entry.extraData).equipmentIndex);
		return !Language.english.TokenIsRegistered(equipmentDef.loreToken);
	}

	private static bool CanSelectItemEntry(ItemDef itemDef, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if (itemDef != null)
		{
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
			if ((bool)itemTierDef && itemTierDef.isDroppable)
			{
				if (!(itemDef.requiredExpansion == null) && expansionAvailability.ContainsKey(itemDef.requiredExpansion))
				{
					return expansionAvailability[itemDef.requiredExpansion];
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private static bool CanSelectEquipmentEntry(EquipmentDef equipmentDef, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if (equipmentDef != null)
		{
			if (equipmentDef.canDrop)
			{
				if (!(equipmentDef.requiredExpansion == null) && expansionAvailability.ContainsKey(equipmentDef.requiredExpansion))
				{
					return expansionAvailability[equipmentDef.requiredExpansion];
				}
				return true;
			}
			return false;
		}
		return false;
	}

	private static Entry[] BuildPickupEntries(Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		new Entry
		{
			nameToken = "TOOLTIP_WIP_CONTENT_NAME",
			color = Color.white,
			iconTexture = LegacyResourcesAPI.Load<Texture>("Textures/MiscIcons/texWIPIcon"),
			getStatusImplementation = GetUnimplemented,
			getTooltipContentImplementation = GetWIPTooltipContent
		};
		IEnumerable<Entry> first = from pickupDef in PickupCatalog.allPickups
			select ItemCatalog.GetItemDef(pickupDef.itemIndex) into itemDef
			where CanSelectItemEntry(itemDef, expansionAvailability)
			orderby (int)(itemDef.tier + ((itemDef.tier == ItemTier.Lunar) ? 100 : 0))
			select new Entry
			{
				nameToken = itemDef.nameToken,
				color = ColorCatalog.GetColor(itemDef.darkColorIndex),
				iconTexture = itemDef.pickupIconTexture,
				bgTexture = itemDef.bgIconTexture,
				extraData = PickupCatalog.FindPickupIndex(itemDef.itemIndex),
				modelPrefab = itemDef.pickupModelPrefab,
				getStatusImplementation = GetPickupStatus,
				getTooltipContentImplementation = GetPickupTooltipContent,
				pageBuilderMethod = PageBuilder.SimplePickup,
				isWIPImplementation = IsEntryPickupItemWithoutLore
			};
		IEnumerable<Entry> second = from pickupDef in PickupCatalog.allPickups
			select EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex) into equipmentDef
			where CanSelectEquipmentEntry(equipmentDef, expansionAvailability)
			orderby !equipmentDef.isLunar
			select new Entry
			{
				nameToken = equipmentDef.nameToken,
				color = ColorCatalog.GetColor(equipmentDef.colorIndex),
				iconTexture = equipmentDef.pickupIconTexture,
				bgTexture = equipmentDef.bgIconTexture,
				extraData = PickupCatalog.FindPickupIndex(equipmentDef.equipmentIndex),
				modelPrefab = equipmentDef.pickupModelPrefab,
				getStatusImplementation = GetPickupStatus,
				getTooltipContentImplementation = GetPickupTooltipContent,
				pageBuilderMethod = PageBuilder.SimplePickup,
				isWIPImplementation = IsEntryPickupEquipmentWithoutLore
			};
		return first.Concat(second).ToArray();
	}

	private static bool CanSelectMonsterEntry(CharacterBody characterBody, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if ((bool)characterBody)
		{
			ExpansionRequirementComponent component = characterBody.GetComponent<ExpansionRequirementComponent>();
			if (!component || !component.requiredExpansion || !expansionAvailability.ContainsKey(component.requiredExpansion) || expansionAvailability[component.requiredExpansion])
			{
				return UnlockableExists(characterBody.GetComponent<DeathRewards>()?.logUnlockableDef);
			}
			return false;
		}
		return false;
	}

	private static Entry[] BuildMonsterEntries(Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		Entry[] array = (from characterBody in BodyCatalog.allBodyPrefabBodyBodyComponents
			where CanSelectMonsterEntry(characterBody, expansionAvailability)
			orderby characterBody.baseMaxHealth
			select new Entry
			{
				nameToken = characterBody.baseNameToken,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.HardDifficulty),
				iconTexture = characterBody.portraitIcon,
				extraData = characterBody,
				modelPrefab = characterBody.GetComponent<ModelLocator>()?.modelTransform?.gameObject,
				getStatusImplementation = GetMonsterStatus,
				getTooltipContentImplementation = GetMonsterTooltipContent,
				pageBuilderMethod = PageBuilder.MonsterBody,
				bgTexture = (characterBody.isChampion ? LegacyResourcesAPI.Load<Texture>("Textures/ItemIcons/BG/texTier3BGIcon") : LegacyResourcesAPI.Load<Texture>("Textures/ItemIcons/BG/texTier1BGIcon")),
				isWIPImplementation = IsEntryBodyWithoutLore
			}).ToArray();
		Dictionary<string, Entry> dictionary = new Dictionary<string, Entry>();
		Entry[] array2 = array;
		foreach (Entry entry in array2)
		{
			if (!dictionary.ContainsKey(entry.nameToken))
			{
				dictionary.Add(entry.nameToken, entry);
			}
		}
		return dictionary.Values.ToArray();
	}

	private static bool CanSelectStageEntry(SceneDef sceneDef, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if ((bool)sceneDef)
		{
			ExpansionDef requiredExpansion = sceneDef.requiredExpansion;
			if (!requiredExpansion || !expansionAvailability.ContainsKey(requiredExpansion) || expansionAvailability[requiredExpansion])
			{
				return sceneDef.shouldIncludeInLogbook;
			}
			return false;
		}
		return false;
	}

	private static Entry[] BuildStageEntries(Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		return (from sceneDef in SceneCatalog.allSceneDefs
			where CanSelectStageEntry(sceneDef, expansionAvailability)
			orderby sceneDef.stageOrder
			select new Entry
			{
				nameToken = sceneDef.nameToken,
				iconTexture = sceneDef.previewTexture,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier2ItemDark),
				getStatusImplementation = GetStageStatus,
				modelPrefab = sceneDef.dioramaPrefab,
				getTooltipContentImplementation = GetStageTooltipContent,
				pageBuilderMethod = PageBuilder.Stage,
				extraData = sceneDef,
				isWIPImplementation = delegate(in Entry entry)
				{
					return !Language.english.TokenIsRegistered(((SceneDef)entry.extraData).loreToken);
				}
			}).ToArray();
	}

	private static bool CanSelectSurvivorBodyEntry(CharacterBody body, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if ((bool)body)
		{
			ExpansionRequirementComponent component = body.GetComponent<ExpansionRequirementComponent>();
			if ((bool)component && (bool)component.requiredExpansion && expansionAvailability.ContainsKey(component.requiredExpansion))
			{
				return expansionAvailability[component.requiredExpansion];
			}
			return true;
		}
		return false;
	}

	private static Entry[] BuildSurvivorEntries(Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		return (from survivorDef in SurvivorCatalog.orderedSurvivorDefs
			select BodyCatalog.GetBodyPrefabBodyComponent(SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorDef.survivorIndex)) into body
			where CanSelectSurvivorBodyEntry(body, expansionAvailability)
			select body into characterBody
			select new Entry
			{
				nameToken = characterBody.baseNameToken,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.NormalDifficulty),
				iconTexture = characterBody.portraitIcon,
				bgTexture = LegacyResourcesAPI.Load<Texture>("Textures/ItemIcons/BG/texSurvivorBGIcon"),
				extraData = characterBody,
				modelPrefab = characterBody.GetComponent<ModelLocator>()?.modelTransform?.gameObject,
				getStatusImplementation = GetSurvivorStatus,
				getTooltipContentImplementation = GetSurvivorTooltipContent,
				pageBuilderMethod = PageBuilder.SurvivorBody,
				isWIPImplementation = IsEntryBodyWithoutLore
			}).ToArray();
	}

	private static bool CanSelectAchievementEntry(AchievementDef achievementDef, Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		if (achievementDef != null)
		{
			ExpansionDef expansionDefForUnlockable = UnlockableCatalog.GetExpansionDefForUnlockable(UnlockableCatalog.GetUnlockableDef(achievementDef.unlockableRewardIdentifier)?.index ?? UnlockableIndex.None);
			if ((bool)expansionDefForUnlockable && expansionAvailability.ContainsKey(expansionDefForUnlockable))
			{
				return expansionAvailability[expansionDefForUnlockable];
			}
			return true;
		}
		return false;
	}

	private static Entry[] BuildAchievementEntries(Dictionary<ExpansionDef, bool> expansionAvailability)
	{
		return (from achievementDef in AchievementManager.allAchievementDefs
			where CanSelectAchievementEntry(achievementDef, expansionAvailability)
			select new Entry
			{
				nameToken = achievementDef.nameToken,
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.NormalDifficulty),
				iconTexture = achievementDef.GetAchievedIcon()?.texture,
				extraData = achievementDef,
				modelPrefab = null,
				getStatusImplementation = GetAchievementStatus,
				getTooltipContentImplementation = GetAchievementTooltipContent
			}).ToArray();
	}

	private static Entry[] BuildProfileEntries(UserProfile viewerProfile)
	{
		List<Entry> entries = new List<Entry>();
		if (true)
		{
			foreach (string availableProfileName in PlatformSystems.saveSystem.GetAvailableProfileNames())
			{
				AddProfileStatsEntry(PlatformSystems.saveSystem.GetProfile(availableProfileName));
			}
		}
		else if (viewerProfile != null)
		{
			AddProfileStatsEntry(viewerProfile);
		}
		return entries.ToArray();
		void AddProfileStatsEntry(UserProfile userProfile)
		{
			Entry item = new Entry
			{
				pageBuilderMethod = PageBuilder.StatsPanel,
				getStatusImplementation = delegate
				{
					return EntryStatus.Available;
				},
				extraData = userProfile,
				getDisplayNameImplementation = delegate(in Entry entry, UserProfile _viewerProfile)
				{
					return ((UserProfile)entry.extraData).name;
				},
				iconTexture = userProfile.portraitTexture
			};
			entries.Add(item);
		}
	}

	private static Entry[] BuildMorgueEntries(UserProfile viewerProfile)
	{
		List<Entry> entries = CollectionPool<Entry, List<Entry>>.RentCollection();
		List<RunReport> list = CollectionPool<RunReport, List<RunReport>>.RentCollection();
		MorgueManager.LoadHistoryRunReports(list);
		foreach (RunReport item2 in list)
		{
			AddRunReportEntry(item2);
		}
		CollectionPool<RunReport, List<RunReport>>.ReturnCollection(list);
		Entry[] result = entries.ToArray();
		CollectionPool<Entry, List<Entry>>.ReturnCollection(entries);
		return result;
		void AddRunReportEntry(RunReport runReport)
		{
			GetPrimaryPlayerInfo(runReport, out var _, out var primaryPlayerBody2);
			Entry item = new Entry
			{
				pageBuilderMethod = PageBuilder.RunReportPanel,
				getStatusImplementation = GetEntryStatus,
				extraData = runReport,
				getDisplayNameImplementation = GetDisplayName,
				getTooltipContentImplementation = GetTooltipContent,
				iconTexture = primaryPlayerBody2?.portraitIcon
			};
			entries.Add(item);
		}
		static string GetDisplayName(in Entry entry, UserProfile _viewerProfile)
		{
			return ((RunReport)entry.extraData).snapshotTimeUtc.ToLocalTime().ToString("G", CultureInfo.CurrentCulture);
		}
		static EntryStatus GetEntryStatus(in Entry entry, UserProfile _viewerProfile)
		{
			return EntryStatus.Available;
		}
		void GetPrimaryPlayerInfo(RunReport _runReport, out RunReport.PlayerInfo primaryPlayerInfo, out CharacterBody primaryPlayerBody)
		{
			primaryPlayerInfo = _runReport.FindPlayerInfo(viewerProfile) ?? _runReport.FindFirstPlayerInfo();
			primaryPlayerBody = BodyCatalog.GetBodyPrefabBodyComponent(primaryPlayerInfo?.bodyIndex ?? BodyIndex.None);
		}
		TooltipContent GetTooltipContent(in Entry entry, UserProfile _viewerProfile, EntryStatus entryStatus)
		{
			RunReport runReport2 = (RunReport)entry.extraData;
			GetPrimaryPlayerInfo(runReport2, out var _, out var primaryPlayerBody3);
			TooltipContent result2 = default(TooltipContent);
			result2.overrideTitleText = Language.GetStringFormatted("LOGBOOK_ENTRY_RUNREPORT_TOOLTIP_TITLE_FORMAT", runReport2.snapshotTimeUtc.ToLocalTime().ToString("G", CultureInfo.CurrentCulture));
			result2.overrideBodyText = Language.GetStringFormatted("LOGBOOK_ENTRY_RUNREPORT_TOOLTIP_BODY_FORMAT", Language.GetString(primaryPlayerBody3?.baseNameToken ?? string.Empty), Language.GetString(runReport2.gameMode?.nameToken ?? string.Empty), Language.GetString(runReport2.gameEnding?.endingTextToken ?? string.Empty));
			return result2;
		}
	}

	private static IEnumerator WaitForBuildingStaticData()
	{
		IsStaticDataReady = false;
		Dictionary<ExpansionDef, bool> expansionAvailability = new Dictionary<ExpansionDef, bool>();
		List<EntitlementDef> list = new List<EntitlementDef>();
		foreach (ExpansionDef expansionDef in ExpansionCatalog.expansionDefs)
		{
			expansionAvailability.Add(expansionDef, !expansionDef.requiredEntitlement || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(expansionDef.requiredEntitlement));
			if (expansionAvailability[expansionDef])
			{
				list.Add(expansionDef.requiredEntitlement);
			}
		}
		if (lastBuiltEntitlements != null && lastBuiltEntitlements.SequenceEqual(list))
		{
			lastBuiltEntitlements = list;
			IsStaticDataReady = true;
			yield break;
		}
		lastBuiltEntitlements = list;
		yield return null;
		Entry[] pickupEntries = BuildPickupEntries(expansionAvailability);
		yield return null;
		Entry[] monsterEntries = BuildMonsterEntries(expansionAvailability);
		yield return null;
		Entry[] stageEntries = BuildStageEntries(expansionAvailability);
		yield return null;
		Entry[] survivorEntries = BuildSurvivorEntries(expansionAvailability);
		yield return null;
		Entry[] achievementEntries = BuildAchievementEntries(expansionAvailability);
		yield return null;
		categories = new CategoryDef[7]
		{
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_ITEMANDEQUIPMENT",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/ItemEntryIcon"),
				buildEntries = (UserProfile viewerProfile) => pickupEntries,
				navButtons = new CategoryDef.NavButtons
				{
					navigate = "LOGBOOK_ZOOMINOUT"
				}
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_MONSTER",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/MonsterEntryIcon"),
				buildEntries = (UserProfile viewerProfile) => monsterEntries,
				navButtons = new CategoryDef.NavButtons
				{
					navigate = "LOGBOOK_ZOOMINOUT"
				}
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_STAGE",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/StageEntryIcon"),
				buildEntries = (UserProfile viewerProfile) => stageEntries
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_SURVIVOR",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/SurvivorEntryIcon"),
				buildEntries = (UserProfile viewerProfile) => survivorEntries
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_ACHIEVEMENTS",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/AchievementEntryIcon"),
				buildEntries = (UserProfile viewerProfile) => achievementEntries,
				initializeElementGraphics = CategoryDef.InitializeChallenge
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_STATS",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/StatsEntryIcon"),
				buildEntries = BuildProfileEntries,
				initializeElementGraphics = CategoryDef.InitializeStats,
				navButtons = new CategoryDef.NavButtons
				{
					scroll = "",
					rotate = "LOGBOOK_PAN"
				}
			},
			new CategoryDef
			{
				nameToken = "LOGBOOK_CATEGORY_MORGUE",
				iconPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/Logbook/MorgueEntryIcon"),
				buildEntries = BuildMorgueEntries,
				initializeElementGraphics = CategoryDef.InitializeMorgue,
				navButtons = new CategoryDef.NavButtons
				{
					rotate = "",
					navigate = ""
				}
			}
		};
		RegisterViewables(categories);
		availability.MakeAvailable();
		IsStaticDataReady = true;
		EntitlementManager.UpdateLocalUsersEntitlements();
	}

	[ConCommand(commandName = "logbook_list_unfinished_lore", flags = ConVarFlags.None, helpText = "Prints all logbook entries which still have undefined lore.")]
	private static void CCLogbookListUnfinishedLore(ConCommandArgs args)
	{
		List<string> list = new List<string>();
		CategoryDef[] array = categories;
		for (int i = 0; i < array.Length; i++)
		{
			Entry[] array2 = array[i].BuildEntries(args.GetSenderLocalUser()?.userProfile);
			foreach (Entry entry in array2)
			{
				string text = "";
				if (entry.extraData is UnityEngine.Object @object)
				{
					text = @object.name;
				}
				if (entry.isWip)
				{
					list.Add(entry.extraData?.GetType()?.Name + " \"" + text + "\" \"" + Language.GetString(entry.nameToken) + "\"");
				}
			}
		}
		args.Log(string.Join("\n", list));
	}

	private static void RegisterViewables(CategoryDef[] categoriesToGenerateFrom)
	{
		ViewablesCatalog.Node node = new ViewablesCatalog.Node("Logbook", isFolder: true);
		CategoryDef[] array = categories;
		foreach (CategoryDef obj in array)
		{
			ViewablesCatalog.Node parent = (obj.viewableNode = new ViewablesCatalog.Node(obj.nameToken, isFolder: true, node));
			Entry[] array2 = obj.BuildEntries(null);
			foreach (Entry entry in array2)
			{
				string nameToken = entry.nameToken;
				ViewablesCatalog.Node entryNode;
				AchievementDef achievementDef;
				bool hasPrereq;
				if (!entry.isWip && !(nameToken == "TOOLTIP_WIP_CONTENT_NAME") && !string.IsNullOrEmpty(nameToken))
				{
					entryNode = new ViewablesCatalog.Node(nameToken, isFolder: false, parent);
					object extraData = entry.extraData;
					achievementDef = extraData as AchievementDef;
					if (achievementDef != null)
					{
						hasPrereq = !string.IsNullOrEmpty(achievementDef.prerequisiteAchievementIdentifier);
						entryNode.shouldShowUnviewed = Check;
					}
					else
					{
						entryNode.shouldShowUnviewed = Check;
					}
					entry.viewableNode = entryNode;
				}
				bool Check(UserProfile userProfile)
				{
					if (userProfile.HasViewedViewable(entryNode.fullName))
					{
						return false;
					}
					if (userProfile.HasAchievement(achievementDef.identifier))
					{
						return false;
					}
					if (hasPrereq)
					{
						return userProfile.HasAchievement(achievementDef.prerequisiteAchievementIdentifier);
					}
					return true;
				}
				bool Check(UserProfile viewerProfile)
				{
					if (viewerProfile.HasViewedViewable(entryNode.fullName))
					{
						return false;
					}
					return entry.GetStatus(viewerProfile) == EntryStatus.Available;
				}
			}
		}
		ViewablesCatalog.AddNodeToRoot(node);
		LogBookController.OnViewablesRegistered?.Invoke();
	}
}
