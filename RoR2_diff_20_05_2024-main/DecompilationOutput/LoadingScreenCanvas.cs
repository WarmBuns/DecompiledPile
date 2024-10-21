using System;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenCanvas : MonoBehaviour
{
	public static LoadingScreenCanvas Instance;

	public SpriteAsNumberManager percentage;

	public GameObject background;

	public LoadingScreenCanvas()
	{
		Instance = this;
	}

	public void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(OnLoadComplete));
		SceneManager.sceneLoaded += OnSceneChange;
	}

	private void OnLoadComplete()
	{
		SceneManager.sceneLoaded -= OnSceneChange;
		Instance = null;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnSceneChange(Scene a, LoadSceneMode b)
	{
		Instance.background.SetActive(a.name == "loadingbasic");
	}
}
