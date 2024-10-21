using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoR2;

public static class FadeToBlackManager
{
	private static Image image;

	public static int fadeCount;

	private static float alpha;

	public static bool fadePaused;

	private const float fadeDuration = 0.25f;

	private const float inversefadeDuration = 4f;

	public static bool fullyFaded => alpha == 2f;

	[InitDuringStartup]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback<GameObject>("Prefabs/UI/ScreenTintCanvas", InitCanvas);
		static void InitCanvas(GameObject prefab)
		{
			GameObject gameObject = Object.Instantiate(prefab, RoR2Application.instance.mainCanvas.transform);
			alpha = 0f;
			image = gameObject.transform.GetChild(0).GetComponent<Image>();
			UpdateImageAlpha(alpha);
			RoR2Application.onUpdate += Update;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
		}
	}

	public static void OnSceneUnloaded(Scene scene)
	{
		ForceFullBlack();
	}

	public static void ForceFullBlack()
	{
		alpha = 2f;
	}

	private static void Update()
	{
		if (!fadePaused)
		{
			float target = 2f;
			float num = 4f;
			if (fadeCount <= 0)
			{
				target = 0f;
				num *= 0.25f;
			}
			alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime * num);
			float num2 = 0f;
			List<FadeToBlackOffset> instancesList = InstanceTracker.GetInstancesList<FadeToBlackOffset>();
			for (int i = 0; i < instancesList.Count; i++)
			{
				FadeToBlackOffset fadeToBlackOffset = instancesList[i];
				num2 += fadeToBlackOffset.value;
			}
			UpdateImageAlpha(alpha + num2);
		}
	}

	private static void UpdateImageAlpha(float finalAlpha)
	{
		Color color = image.color;
		Color color2 = color;
		color2.a = finalAlpha;
		image.color = color2;
		image.raycastTarget = color2.a > color.a;
	}

	public static void ForceClear()
	{
		fadeCount = 0;
		alpha = 0f;
		TransitionCommand.ForceClearFadeToBlack();
	}

	public static bool IsFading()
	{
		return alpha > 0f;
	}
}
