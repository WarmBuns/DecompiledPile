using Rewired;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2;

[RequireComponent(typeof(MPEventSystemLocator))]
public class InputStickVisualizer : MonoBehaviour
{
	[Header("Move")]
	public Scrollbar moveXBar;

	public Scrollbar moveYBar;

	public TextMeshProUGUI moveXLabel;

	public TextMeshProUGUI moveYLabel;

	[Header("Aim")]
	public Scrollbar aimXBar;

	public Scrollbar aimYBar;

	public TextMeshProUGUI aimXLabel;

	public TextMeshProUGUI aimYLabel;

	public Scrollbar aimStickPostSmoothingXBar;

	public Scrollbar aimStickPostSmoothingYBar;

	public Scrollbar aimStickPostDualZoneXBar;

	public Scrollbar aimStickPostDualZoneYBar;

	public Scrollbar aimStickPostExponentXBar;

	public Scrollbar aimStickPostExponentYBar;

	private MPEventSystemLocator eventSystemLocator;

	private void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	private Player GetPlayer()
	{
		return eventSystemLocator.eventSystem?.player;
	}

	private CameraRigController GetCameraRigController()
	{
		if (CameraRigController.readOnlyInstancesList.Count <= 0)
		{
			return null;
		}
		return CameraRigController.readOnlyInstancesList[0];
	}

	private void SetBarValues(Vector2 vector, Scrollbar scrollbarX, Scrollbar scrollbarY)
	{
		if ((bool)scrollbarX)
		{
			scrollbarX.value = Util.Remap(vector.x, -1f, 1f, 0f, 1f);
		}
		if ((bool)scrollbarY)
		{
			scrollbarY.value = Util.Remap(vector.y, -1f, 1f, 0f, 1f);
		}
	}

	private void Update()
	{
		Player player = GetPlayer();
		if ((bool)GetCameraRigController() && player != null)
		{
			Vector2 vector = new Vector2(player.GetAxis(0), player.GetAxis(1));
			Vector2 vector2 = new Vector2(player.GetAxis(16), player.GetAxis(17));
			SetBarValues(vector, moveXBar, moveYBar);
			SetBarValues(vector2, aimXBar, aimYBar);
			moveXLabel.text = $"move.x={vector.x:0.0000}";
			moveYLabel.text = $"move.y={vector.y:0.0000}";
			aimXLabel.text = $"aim.x={vector2.x:0.0000}";
			aimYLabel.text = $"aim.y={vector2.y:0.0000}";
		}
	}
}
