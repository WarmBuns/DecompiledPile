using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EventFunctions : MonoBehaviour
{
	public void DestroySelf()
	{
		Object.Destroy(base.gameObject);
	}

	public void DestroyGameObject(GameObject obj)
	{
		Object.Destroy(obj);
	}

	public void UnparentTransform(Transform transform)
	{
		if ((bool)transform)
		{
			transform.SetParent(null);
		}
	}

	public void ToggleGameObjectActive(GameObject obj)
	{
		obj.SetActive(!obj.activeSelf);
	}

	public void CreateLocalEffect(GameObject effectObj)
	{
		EffectData effectData = new EffectData();
		effectData.origin = base.transform.position;
		EffectManager.SpawnEffect(effectObj, effectData, transmit: false);
	}

	public void CreateNetworkedEffect(GameObject effectObj)
	{
		EffectData effectData = new EffectData();
		effectData.origin = base.transform.position;
		EffectManager.SpawnEffect(effectObj, effectData, transmit: true);
	}

	public void OpenURL(string url)
	{
		Application.OpenURL(url);
	}

	public void PlaySound(string soundString)
	{
		Util.PlaySound(soundString, base.gameObject);
	}

	public void PlayUISound(string soundString)
	{
		Util.PlaySound(soundString, RoR2Application.instance.gameObject);
	}

	public void PlayNetworkedUISound(NetworkSoundEventDef nseDef)
	{
		if ((bool)nseDef)
		{
			EffectManager.SimpleSoundEffect(nseDef.index, Vector3.zero, transmit: true);
		}
	}

	public void RunSetFlag(string flagName)
	{
		if (NetworkServer.active)
		{
			Run.instance?.SetEventFlag(flagName);
		}
	}

	public void RunResetFlag(string flagName)
	{
		if (NetworkServer.active)
		{
			Run.instance?.ResetEventFlag(flagName);
		}
	}

	public void DisableAllChildren()
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			base.transform.GetChild(num).gameObject.SetActive(value: false);
		}
	}

	public void DisableAllChildrenExcept(GameObject objectToEnable)
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			GameObject gameObject = base.transform.GetChild(num).gameObject;
			if (!(gameObject == objectToEnable))
			{
				gameObject.SetActive(value: false);
			}
		}
		objectToEnable.SetActive(value: true);
	}

	public void BeginEnding(GameEndingDef gameEndingDef)
	{
		if (NetworkServer.active)
		{
			Run.instance.BeginGameOver(gameEndingDef);
		}
	}
}
