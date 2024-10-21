using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace EntityStates.GameOver;

public class VoidEndingFadeToBlack : BaseGameOverControllerState
{
	[SerializeField]
	public float delay;

	[SerializeField]
	public float duration;

	[SerializeField]
	public GameObject screenPrefab;

	private Run.TimeStamp startTime;

	private Image image;

	private GameObject screenInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			startTime = Run.TimeStamp.now + delay;
		}
		screenInstance = Object.Instantiate(screenPrefab, RoR2Application.instance.mainCanvas.transform);
		image = screenInstance.GetComponentInChildren<Image>();
		image.raycastTarget = false;
		UpdateImageAlpha(0f);
	}

	public override void OnExit()
	{
		FadeToBlackManager.ForceFullBlack();
		EntityState.Destroy(screenInstance);
		screenInstance = null;
		image = null;
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(startTime);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		startTime = reader.ReadTimeStamp();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		UpdateImageAlpha(Mathf.Max(0f, Mathf.Min(1f, startTime.timeSince / duration)));
		if (NetworkServer.active && (startTime + duration).hasPassed)
		{
			outer.SetNextStateToMain();
		}
	}

	private void UpdateImageAlpha(float finalAlpha)
	{
		Color color = image.color;
		color.a = finalAlpha;
		image.color = color;
	}
}
