using System.Collections.ObjectModel;
using Rewired;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(CameraRigController))]
public class CameraRigControllerSpectateControls : MonoBehaviour
{
	private CameraRigController cameraRigController;

	private void Awake()
	{
		cameraRigController = GetComponent<CameraRigController>();
	}

	private static bool CanUserSpectateBody(NetworkUser viewer, CharacterBody body)
	{
		return Util.LookUpBodyNetworkUser(body.gameObject);
	}

	public static GameObject GetNextSpectateGameObject(NetworkUser viewer, GameObject currentGameObject)
	{
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		if (readOnlyInstancesList.Count == 0)
		{
			return null;
		}
		CharacterBody characterBody = (currentGameObject ? currentGameObject.GetComponent<CharacterBody>() : null);
		int num = (characterBody ? readOnlyInstancesList.IndexOf(characterBody) : 0);
		for (int i = num + 1; i < readOnlyInstancesList.Count; i++)
		{
			if (CanUserSpectateBody(viewer, readOnlyInstancesList[i]))
			{
				return readOnlyInstancesList[i].gameObject;
			}
		}
		for (int j = 0; j <= num; j++)
		{
			if (CanUserSpectateBody(viewer, readOnlyInstancesList[j]))
			{
				return readOnlyInstancesList[j].gameObject;
			}
		}
		return null;
	}

	public static GameObject GetPreviousSpectateGameObject(NetworkUser viewer, GameObject currentGameObject)
	{
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		if (readOnlyInstancesList.Count == 0)
		{
			return null;
		}
		CharacterBody characterBody = (currentGameObject ? currentGameObject.GetComponent<CharacterBody>() : null);
		int num = (characterBody ? readOnlyInstancesList.IndexOf(characterBody) : 0);
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (CanUserSpectateBody(viewer, readOnlyInstancesList[num2]))
			{
				return readOnlyInstancesList[num2].gameObject;
			}
		}
		for (int num3 = readOnlyInstancesList.Count - 1; num3 >= num; num3--)
		{
			if (CanUserSpectateBody(viewer, readOnlyInstancesList[num3]))
			{
				return readOnlyInstancesList[num3].gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		Player player = cameraRigController.localUserViewer?.inputPlayer;
		if (cameraRigController.cameraMode != null && cameraRigController.cameraMode.IsSpectating(cameraRigController) && player != null)
		{
			if (player.GetButtonDown(7))
			{
				cameraRigController.nextTarget = GetNextSpectateGameObject(cameraRigController.viewer, cameraRigController.target);
			}
			if (player.GetButtonDown(8))
			{
				cameraRigController.nextTarget = GetPreviousSpectateGameObject(cameraRigController.viewer, cameraRigController.target);
			}
		}
	}
}
