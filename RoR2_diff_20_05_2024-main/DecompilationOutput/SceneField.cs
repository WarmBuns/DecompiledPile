using System;
using UnityEngine;

[Serializable]
public class SceneField
{
	[SerializeField]
	private UnityEngine.Object sceneAsset;

	[SerializeField]
	private string sceneName = "";

	public string SceneName => sceneName;

	public SceneField(string sceneName)
	{
		this.sceneName = sceneName;
	}

	public static implicit operator string(SceneField sceneField)
	{
		return sceneField.sceneName;
	}

	public static implicit operator bool(SceneField sceneField)
	{
		return !string.IsNullOrEmpty(sceneField.sceneName);
	}
}
