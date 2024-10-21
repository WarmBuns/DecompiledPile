using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(ItemFollower))]
public class GravCubeController : MonoBehaviour
{
	private ItemFollower itemFollower;

	private float activeTimer;

	private Animator itemFollowerAnimator;

	private static int activeParamHash = Animator.StringToHash("active");

	private void Start()
	{
		itemFollower = GetComponent<ItemFollower>();
	}

	public void ActivateCube(float duration)
	{
		activeTimer = duration;
	}

	private void Update()
	{
		if ((bool)itemFollower && (bool)itemFollower.followerInstance)
		{
			if (!itemFollowerAnimator)
			{
				itemFollowerAnimator = itemFollower.followerInstance.GetComponentInChildren<Animator>();
			}
			activeTimer -= Time.deltaTime;
			itemFollowerAnimator.SetBool(activeParamHash, activeTimer > 0f);
		}
	}
}
