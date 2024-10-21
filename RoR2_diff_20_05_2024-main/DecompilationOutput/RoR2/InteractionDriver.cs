using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(InputBankTest))]
[RequireComponent(typeof(Interactor))]
public class InteractionDriver : MonoBehaviour, ILifeBehavior
{
	public bool highlightInteractor;

	public Vector3 aimOffset;

	public bool useAimOffset;

	public bool drawDebugRay;

	public float debugRayLength = 5f;

	private bool inputReceived;

	private NetworkIdentity networkIdentity;

	private InputBankTest inputBank;

	private CharacterBody characterBody;

	private EquipmentSlot equipmentSlot;

	[NonSerialized]
	public GameObject interactableOverride;

	private const float interactableCooldownDuration = 0.25f;

	private float interactableCooldown;

	private const float interactableCheckCooldownDuration = 0f;

	private float interactableCheckCooldown;

	private GameObject saleStarEffect;

	[NonSerialized]
	public GameObject currentInteractable;

	public Interactor interactor { get; private set; }

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		interactor = GetComponent<Interactor>();
		inputBank = GetComponent<InputBankTest>();
		characterBody = GetComponent<CharacterBody>();
		equipmentSlot = (characterBody ? characterBody.equipmentSlot : null);
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.deltaTime);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		if (networkIdentity.hasAuthority)
		{
			interactableCooldown -= deltaTime;
			interactableCheckCooldown -= deltaTime;
			inputReceived = inputBank.interact.justPressed || (inputBank.interact.down && interactableCooldown <= 0f);
			if (inputBank.interact.justReleased)
			{
				inputReceived = false;
				interactableCooldown = 0f;
			}
			currentInteractable = FindBestInteractableObject();
		}
		if (inputReceived && (bool)currentInteractable)
		{
			interactor.AttemptInteraction(currentInteractable);
			interactableCooldown = 0.25f;
		}
		if ((bool)currentInteractable)
		{
			if (characterBody.inventory.GetItemCount(DLC2Content.Items.LowerPricedChests) > 0)
			{
				PurchaseInteraction component = currentInteractable.GetComponent<PurchaseInteraction>();
				if ((object)component != null && component.saleStarCompatible)
				{
					if ((bool)currentInteractable.GetComponent<PurchaseInteraction>() && currentInteractable.GetComponent<PurchaseInteraction>().CanBeAffordedByInteractor(GetComponent<Interactor>()) && !saleStarEffect)
					{
						saleStarEffect = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LowerPricedChestsGlow"), currentInteractable.transform.position, Quaternion.identity, currentInteractable.transform);
					}
					return;
				}
			}
			if ((bool)saleStarEffect)
			{
				UnityEngine.Object.Destroy(saleStarEffect);
			}
		}
		else if ((bool)saleStarEffect)
		{
			UnityEngine.Object.Destroy(saleStarEffect);
		}
	}

	public GameObject FindBestInteractableObject()
	{
		if ((bool)interactableOverride)
		{
			return interactableOverride;
		}
		if (interactableCheckCooldown > 0f)
		{
			return currentInteractable;
		}
		interactableCheckCooldown = 0f;
		float extraRaycastDistance = 0f;
		float num = interactor.maxInteractionDistance;
		if ((bool)equipmentSlot && equipmentSlot.equipmentIndex == RoR2Content.Equipment.Recycle.equipmentIndex)
		{
			num *= 2f;
		}
		Vector3 vector = inputBank.aimOrigin;
		Vector3 vector2 = inputBank.aimDirection;
		if (useAimOffset)
		{
			Vector3 vector3 = vector + vector2 * num;
			vector = inputBank.aimOrigin + aimOffset;
			vector2 = (vector3 - vector) / num;
		}
		Ray originalAimRay = new Ray(vector, vector2);
		if (drawDebugRay)
		{
			Debug.DrawLine(vector, vector + vector2 * debugRayLength, Color.green, 0.5f);
		}
		Ray raycastRay = CameraRigController.ModifyAimRayIfApplicable(originalAimRay, base.gameObject, out extraRaycastDistance);
		return interactor.FindBestInteractableObject(raycastRay, num + extraRaycastDistance, originalAimRay.origin, num);
	}

	static InteractionDriver()
	{
		OutlineHighlight.onPreRenderOutlineHighlight = (Action<OutlineHighlight>)Delegate.Combine(OutlineHighlight.onPreRenderOutlineHighlight, new Action<OutlineHighlight>(OnPreRenderOutlineHighlight));
	}

	private static void OnPreRenderOutlineHighlight(OutlineHighlight outlineHighlight)
	{
		if (!outlineHighlight.sceneCamera || !outlineHighlight.sceneCamera.cameraRigController)
		{
			return;
		}
		GameObject target = outlineHighlight.sceneCamera.cameraRigController.target;
		if (!target)
		{
			return;
		}
		InteractionDriver component = target.GetComponent<InteractionDriver>();
		if (!component)
		{
			return;
		}
		GameObject gameObject = component.currentInteractable;
		if (!gameObject)
		{
			return;
		}
		IInteractable component2 = gameObject.GetComponent<IInteractable>();
		Highlight component3 = gameObject.GetComponent<Highlight>();
		if ((bool)component3)
		{
			Color color = component3.GetColor();
			if (component2 != null && ((MonoBehaviour)component2).isActiveAndEnabled && component2.GetInteractability(component.interactor) == Interactability.ConditionsNotMet)
			{
				color = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unaffordable);
			}
			outlineHighlight.highlightQueue.Enqueue(new OutlineHighlight.HighlightInfo
			{
				renderer = component3.targetRenderer,
				color = color * component3.strength
			});
		}
	}

	public void OnDeathStart()
	{
		base.enabled = false;
	}
}
