using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(ChildLocator))]
public class FootstepHandler : MonoBehaviour
{
	public string baseFootstepString;

	public string baseFootliftString;

	public string sprintFootstepOverrideString;

	public string sprintFootliftOverrideString;

	public bool enableFootstepDust;

	public GameObject footstepDustPrefab;

	private ChildLocator childLocator;

	private Inventory bodyInventory;

	private Animator animator;

	private Transform footstepDustInstanceTransform;

	private ParticleSystem footstepDustInstanceParticleSystem;

	private ShakeEmitter footstepDustInstanceShakeEmitter;

	private CharacterBody body;

	public bool inLava;

	private void Start()
	{
		childLocator = GetComponent<ChildLocator>();
		if ((bool)GetComponent<CharacterModel>())
		{
			body = GetComponent<CharacterModel>().body;
			bodyInventory = (body ? body.inventory : null);
		}
		animator = GetComponent<Animator>();
		if (enableFootstepDust)
		{
			footstepDustInstanceTransform = Object.Instantiate(footstepDustPrefab, base.transform).transform;
			footstepDustInstanceParticleSystem = footstepDustInstanceTransform.GetComponent<ParticleSystem>();
			footstepDustInstanceShakeEmitter = footstepDustInstanceTransform.GetComponent<ShakeEmitter>();
		}
	}

	public void Update()
	{
	}

	public void Footstep(AnimationEvent animationEvent)
	{
		if ((double)animationEvent.animatorClipInfo.weight > 0.5)
		{
			Footstep(animationEvent.stringParameter, (GameObject)animationEvent.objectReferenceParameter);
		}
	}

	public void Footstep(string childName, GameObject footstepEffect)
	{
		if (!body)
		{
			return;
		}
		Transform transform = childLocator.FindChild(childName);
		int childIndex = childLocator.FindChildIndex(childName);
		if ((bool)transform)
		{
			Color color = Color.gray;
			RaycastHit hitInfo = default(RaycastHit);
			Vector3 position = transform.position;
			position.y += 1.5f;
			if (Physics.Raycast(new Ray(position, Vector3.down), out hitInfo, 4f, (int)LayerIndex.world.mask | (int)LayerIndex.water.mask, QueryTriggerInteraction.Collide))
			{
				if ((bool)bodyInventory && bodyInventory.GetItemCount(RoR2Content.Items.Hoof) > 0 && childName == "FootR")
				{
					Util.PlaySound("Play_item_proc_hoof", body.gameObject);
				}
				if ((bool)footstepEffect)
				{
					EffectData effectData = new EffectData();
					effectData.origin = hitInfo.point;
					effectData.rotation = Util.QuaternionSafeLookRotation(hitInfo.normal);
					effectData.SetChildLocatorTransformReference(body.gameObject, childIndex);
					EffectManager.SpawnEffect(footstepEffect, effectData, transmit: false);
				}
				SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitInfo.collider, hitInfo.point);
				bool flag = false;
				if ((bool)objectSurfaceDef)
				{
					body.surfaceSpeedBoost = objectSurfaceDef.speedBoost;
					color = objectSurfaceDef.approximateColor;
					if ((bool)objectSurfaceDef.footstepEffectPrefab)
					{
						EffectManager.SpawnEffect(objectSurfaceDef.footstepEffectPrefab, new EffectData
						{
							origin = hitInfo.point,
							scale = body.radius
						}, transmit: false);
						flag = true;
					}
					if (!string.IsNullOrEmpty(objectSurfaceDef.materialSwitchString))
					{
						AkSoundEngine.SetSwitch("material", objectSurfaceDef.materialSwitchString, body.gameObject);
					}
				}
				else
				{
					inLava = false;
				}
				if ((bool)footstepDustInstanceTransform && !flag)
				{
					footstepDustInstanceTransform.position = hitInfo.point;
					ParticleSystem.MainModule main = footstepDustInstanceParticleSystem.main;
					main.startColor = color;
					footstepDustInstanceParticleSystem.Play();
					if ((bool)footstepDustInstanceShakeEmitter)
					{
						footstepDustInstanceShakeEmitter.StartShake();
					}
				}
			}
			Util.PlaySound((!string.IsNullOrEmpty(sprintFootstepOverrideString) && body.isSprinting) ? sprintFootstepOverrideString : baseFootstepString, body.gameObject);
		}
		else
		{
			Debug.LogWarningFormat("Object {0} lacks ChildLocator entry \"{1}\" to handle Footstep event!", base.gameObject.name, childName);
		}
	}

	public void Footlift(AnimationEvent animationEvent)
	{
		if ((double)animationEvent.animatorClipInfo.weight > 0.5)
		{
			Footlift();
		}
	}

	public void Footlift()
	{
		Util.PlaySound((!string.IsNullOrEmpty(sprintFootliftOverrideString) && body.isSprinting) ? sprintFootliftOverrideString : baseFootliftString, body.gameObject);
	}
}
