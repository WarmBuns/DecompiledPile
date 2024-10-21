using UnityEngine;

namespace EntityStates.AurelioniteHeart;

public class AurelioniteHeartFalseSonSurvivorUnlockState : AurelioniteHeartEntityState
{
	[SerializeField]
	public float animationDuration = 4f;

	[SerializeField]
	public float initialDelay = 2f;

	[SerializeField]
	public string falseSonUnlockAnimationState;

	[SerializeField]
	public float upwardMovement = 0.2f;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!(base.fixedAge >= initialDelay))
		{
			return;
		}
		heartMovementTimer -= GetDeltaTime();
		base.gameObject.transform.position += new Vector3(0f, upwardMovement, 0f);
		if (heartMovementTimer <= 0f)
		{
			heartMovementTimer += 0.02f;
			upwardMovement += upwardMovement / 10f;
			if (base.fixedAge >= animationDuration)
			{
				heartController.rebirthShrine.SetActive(value: true);
				EntityState.Destroy(base.gameObject);
			}
		}
	}
}
