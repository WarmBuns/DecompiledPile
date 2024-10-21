using UnityEngine;

namespace RoR2.UI;

public class SpectatorLabel : MonoBehaviour
{
	public HUD hud;

	public HGTextMeshProUGUI label;

	public GameObject labelRoot;

	private GameObject cachedTarget;

	private HudElement hudElement;

	private void Awake()
	{
		labelRoot.SetActive(value: false);
	}

	private void Update()
	{
		UpdateLabel();
	}

	private void UpdateLabel()
	{
		CameraRigController cameraRigController = hud.cameraRigController;
		GameObject gameObject = null;
		GameObject gameObject2 = null;
		if ((bool)cameraRigController)
		{
			gameObject = cameraRigController.target;
			gameObject2 = cameraRigController.localUserViewer.cachedBodyObject;
		}
		if ((object)gameObject == gameObject2 || !(gameObject != null))
		{
			labelRoot.SetActive(value: false);
			cachedTarget = null;
			return;
		}
		labelRoot.SetActive(value: true);
		if ((object)cachedTarget != gameObject)
		{
			string text = (gameObject ? Util.GetBestBodyName(gameObject) : "");
			label.SetText(Language.GetStringFormatted("SPECTATING_NAME_FORMAT", text));
			cachedTarget = gameObject;
		}
	}
}
