using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI.MainMenu;

[RequireComponent(typeof(RectTransform))]
public class BaseMainMenuScreen : MonoBehaviour
{
	public Transform desiredCameraTransform;

	[HideInInspector]
	public bool shouldDisplay;

	protected MainMenuController myMainMenuController;

	protected FirstSelectedObjectProvider firstSelectedObjectProvider;

	public UnityEvent onEnter;

	public UnityEvent onExit;

	public void Awake()
	{
		firstSelectedObjectProvider = GetComponent<FirstSelectedObjectProvider>();
	}

	public void OnEnable()
	{
		MainMenuController.instance?.SetAllowTransition(value: true);
	}

	public virtual bool IsReadyToLeave()
	{
		return true;
	}

	public void Update()
	{
		if (SimpleDialogBox.instancesList.Count == 0)
		{
			firstSelectedObjectProvider?.EnsureSelectedObject();
		}
	}

	public virtual void OnEnter(MainMenuController mainMenuController)
	{
		Debug.LogFormat("BaseMainMenuScreen: OnEnter()");
		myMainMenuController = mainMenuController;
		if (SimpleDialogBox.instancesList.Count == 0)
		{
			firstSelectedObjectProvider?.EnsureSelectedObject();
		}
		onEnter.Invoke();
	}

	public virtual void OnExit(MainMenuController mainMenuController)
	{
		if (myMainMenuController == mainMenuController)
		{
			myMainMenuController = null;
		}
		onExit.Invoke();
	}
}
