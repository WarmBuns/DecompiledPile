using System.Collections.Generic;
using RoR2.ConVar;
using RoR2.UI.LogBook;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class ViewableTag : MonoBehaviour
{
	public enum ViewableVisualStyle
	{
		Button,
		Icon
	}

	private static readonly List<ViewableTag> instancesList = new List<ViewableTag>();

	[FormerlySerializedAs("viewableName")]
	[SerializeField]
	[Tooltip("The path of the viewable that determines whether or not the \"NEW\" tag is activated.")]
	private string _viewableName;

	[Tooltip("Marks the named viewable as viewed when this component is disabled.")]
	public bool markAsViewedOnDisable;

	public bool markAsViewedOnHover;

	public bool markAsViewedOnClick;

	public ViewableVisualStyle viewableVisualStyle;

	public static readonly BoolConVar viewablesWarnUndefined = new BoolConVar("viewables_warn_undefined", ConVarFlags.None, "0", "Issues a warning in the console if a viewable is not defined.");

	private static GameObject tagPrefab;

	private GameObject tagInstance;

	private static bool pendingRefreshAll = false;

	public string viewableName
	{
		get
		{
			return _viewableName;
		}
		set
		{
			if (!(_viewableName == value))
			{
				_viewableName = value;
				Refresh();
			}
		}
	}

	private bool Check()
	{
		if (LocalUserManager.readOnlyLocalUsersList.Count == 0)
		{
			return false;
		}
		UserProfile userProfile = LocalUserManager.readOnlyLocalUsersList[0].userProfile;
		ViewablesCatalog.Node node = ViewablesCatalog.FindNode(viewableName ?? "");
		if (node == null)
		{
			if (viewablesWarnUndefined.value)
			{
				Debug.LogWarningFormat("Viewable {0} is not defined.", viewableName);
			}
			return false;
		}
		return node.shouldShowUnviewed(userProfile);
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		RoR2Application.onNextUpdate += CallRefreshIfStillValid;
		LogBookController.OnViewablesRegistered += CallRefreshIfStillValid;
		if (markAsViewedOnHover && TryGetComponent<HGButton>(out var component))
		{
			component.onSelect.AddListener(OnControllerSelect);
		}
	}

	private void CallRefreshIfStillValid()
	{
		if ((bool)this)
		{
			Refresh();
		}
	}

	public void Refresh()
	{
		bool flag = base.enabled && Check();
		if ((bool)tagInstance != flag)
		{
			if ((bool)tagInstance)
			{
				Object.Destroy(tagInstance);
				tagInstance = null;
			}
			else
			{
				string childName = viewableVisualStyle.ToString();
				tagInstance = Object.Instantiate(tagPrefab, base.transform);
				tagInstance.GetComponent<ChildLocator>().FindChild(childName).gameObject.SetActive(value: true);
			}
		}
	}

	private void OnDisable()
	{
		LogBookController.OnViewablesRegistered -= CallRefreshIfStillValid;
		instancesList.Remove(this);
		Refresh();
		if (markAsViewedOnDisable)
		{
			TriggerView();
		}
	}

	private void TriggerView()
	{
		ViewableTrigger.TriggerView(viewableName);
	}

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/UI/NewViewableTag");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			tagPrefab = x.Result;
		};
		UserProfile.onUserProfileViewedViewablesChanged += delegate
		{
			if (!pendingRefreshAll)
			{
				pendingRefreshAll = true;
				RoR2Application.onNextUpdate += delegate
				{
					foreach (ViewableTag instances in instancesList)
					{
						instances.Refresh();
					}
					pendingRefreshAll = false;
				};
			}
		};
	}

	private void OnControllerSelect()
	{
		if (markAsViewedOnHover)
		{
			TriggerView();
		}
	}
}
