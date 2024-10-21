using UnityEngine;

public static class AkGameObjUtil
{
	public static void SetAkGameObjectStatic(GameObject gameObj, bool staticSetting)
	{
		if (gameObj.TryGetComponent<AkGameObj>(out var component))
		{
			component.isStaticAfterDelay = staticSetting;
		}
	}
}
