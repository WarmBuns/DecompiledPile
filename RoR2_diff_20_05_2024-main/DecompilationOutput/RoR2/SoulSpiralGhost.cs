using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class SoulSpiralGhost : MonoBehaviour
{
	[SerializeField]
	[Tooltip("Triggered at beginning, to make sure the ghost is cleaned up.")]
	private UnityEvent OnEnableAction;

	[SerializeField]
	[Tooltip("Triggered when the orbs are 'boosted'.")]
	private UnityEvent OnOrbBoost;

	[SerializeField]
	[Tooltip("Triggered when the orbs' boost ends.")]
	private UnityEvent OnOrbBoostEnd;

	[SerializeField]
	[Tooltip("Triggered when a given orb has only one hit left.")]
	private UnityEvent OnOrbLastHit;

	public void OnEnable()
	{
		OnEnableAction?.Invoke();
	}

	public void Boost()
	{
		OnOrbBoost?.Invoke();
	}

	public void EndBoost()
	{
		OnOrbBoostEnd?.Invoke();
	}

	public void OnLastHit()
	{
		OnOrbLastHit?.Invoke();
	}
}
