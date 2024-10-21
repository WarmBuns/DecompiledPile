using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

public class SplashScreenController : MonoBehaviour
{
	private static BoolConVar cvSplashSkip = new BoolConVar("splash_skip", ConVarFlags.Archive, "0", "Whether or not to skip startup splash screens.");

	private void Start()
	{
		if (cvSplashSkip.value)
		{
			Finish();
		}
	}

	public void Finish()
	{
		if (IntroCutsceneController.shouldSkip)
		{
			PlatformSystems.networkManager.ServerChangeScene("dafb193bd5aedc74abd11b22f8dce1c2");
		}
		else
		{
			StartCoroutine(LoadHelper.LoadSceneAdditivelyAsync("330d792ae1727574e969e68ce8e966d2"));
		}
	}
}
