using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class PurchaseAvailabilityIndicator : MonoBehaviour
{
	public GameObject indicatorObject;

	public GameObject disabledIndicatorObject;

	public Animator animator;

	public string mecanimBool;

	private int mecanimBoolHash;

	private PurchaseInteraction purchaseInteraction;

	private void Awake()
	{
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		mecanimBoolHash = Animator.StringToHash(mecanimBool);
	}

	private void FixedUpdate()
	{
		if ((bool)indicatorObject)
		{
			indicatorObject.SetActive(purchaseInteraction.available);
		}
		if ((bool)disabledIndicatorObject)
		{
			disabledIndicatorObject.SetActive(!purchaseInteraction.available);
		}
		if ((bool)animator)
		{
			animator.SetBool(mecanimBoolHash, purchaseInteraction.available);
		}
	}
}
