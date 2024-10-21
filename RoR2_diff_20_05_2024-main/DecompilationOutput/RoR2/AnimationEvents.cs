using System.Collections;
using Generics.Dynamics;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class AnimationEvents : MonoBehaviour
{
	public GameObject soundCenter;

	private GameObject bodyObject;

	private CharacterModel characterModel;

	private ChildLocator childLocator;

	private EntityLocator entityLocator;

	private Renderer meshRenderer;

	private ModelLocator modelLocator;

	private float printHeight;

	private float printTime;

	private void Start()
	{
		childLocator = GetComponent<ChildLocator>();
		entityLocator = GetComponent<EntityLocator>();
		meshRenderer = GetComponentInChildren<Renderer>();
		characterModel = GetComponent<CharacterModel>();
		if ((bool)characterModel && (bool)characterModel.body)
		{
			bodyObject = characterModel.body.gameObject;
			modelLocator = bodyObject.GetComponent<ModelLocator>();
		}
	}

	public void UpdateIKState(AnimationEvent animationEvent)
	{
		childLocator.FindChild(animationEvent.stringParameter).GetComponent<IIKTargetBehavior>()?.UpdateIKState(animationEvent.intParameter);
	}

	public void PlaySound(string soundString)
	{
		Util.PlaySound(soundString, soundCenter ? soundCenter : bodyObject);
	}

	public void NormalizeToFloor()
	{
		if ((bool)modelLocator)
		{
			modelLocator.normalizeToFloor = true;
		}
	}

	public void SetIK(AnimationEvent animationEvent)
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			bool flag = ((animationEvent.intParameter != 0) ? true : false);
			InverseKinematics component = modelLocator.modelTransform.GetComponent<InverseKinematics>();
			StriderLegController component2 = modelLocator.modelTransform.GetComponent<StriderLegController>();
			if ((bool)component)
			{
				component.enabled = flag;
			}
			if ((bool)component2)
			{
				component2.enabled = flag;
			}
		}
	}

	public void RefreshAllIK()
	{
		IKTargetPassive[] componentsInChildren = GetComponentsInChildren<IKTargetPassive>();
		foreach (IKTargetPassive obj in componentsInChildren)
		{
			obj.UpdateIKTargetPosition();
			obj.UpdateYOffset();
		}
	}

	public void CreateEffect(AnimationEvent animationEvent)
	{
		Transform transform = base.transform;
		int num = -1;
		if (!string.IsNullOrEmpty(animationEvent.stringParameter))
		{
			num = childLocator.FindChildIndex(animationEvent.stringParameter);
			if (num != -1)
			{
				transform = childLocator.FindChild(num);
			}
		}
		bool transmit = animationEvent.intParameter != 0;
		EffectData effectData = new EffectData();
		effectData.origin = transform.position;
		effectData.SetChildLocatorTransformReference(bodyObject, num);
		EffectManager.SpawnEffect((GameObject)animationEvent.objectReferenceParameter, effectData, transmit);
	}

	public void CreatePrefab(AnimationEvent animationEvent)
	{
		GameObject gameObject = (GameObject)animationEvent.objectReferenceParameter;
		bool flag = EffectManager.ShouldUsePooledEffect(gameObject);
		string stringParameter = animationEvent.stringParameter;
		Transform transform = base.transform;
		int intParameter = animationEvent.intParameter;
		if ((bool)childLocator)
		{
			Transform transform2 = childLocator.FindChild(stringParameter);
			if ((bool)transform2)
			{
				if (intParameter == 0)
				{
					if (!flag)
					{
						Object.Instantiate(gameObject, transform2.position, Quaternion.identity);
					}
					else
					{
						EffectManager.GetAndActivatePooledEffect(gameObject, transform2.position, Quaternion.identity);
					}
				}
				else if (!flag)
				{
					Object.Instantiate(gameObject, transform2.position, transform2.rotation).transform.parent = transform2;
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(gameObject, transform2.position, transform2.rotation, transform2);
				}
			}
			else if ((bool)gameObject)
			{
				if (!flag)
				{
					Object.Instantiate(gameObject, transform.position, transform.rotation);
				}
				else
				{
					EffectManager.GetAndActivatePooledEffect(gameObject, transform.position, transform.rotation);
				}
			}
		}
		else if ((bool)gameObject)
		{
			if (!flag)
			{
				Object.Instantiate(gameObject, transform.position, transform.rotation);
			}
			else
			{
				EffectManager.GetAndActivatePooledEffect(gameObject, transform.position, transform.rotation);
			}
		}
	}

	public void ItemDrop()
	{
		if (NetworkServer.active && (bool)entityLocator)
		{
			IChestBehavior component = entityLocator.entity.GetComponent<IChestBehavior>();
			if ((bool)(Component)component)
			{
				component.ItemDrop();
			}
		}
	}

	public void BeginPrint(AnimationEvent animationEvent)
	{
		if ((bool)meshRenderer)
		{
			Material material = (Material)animationEvent.objectReferenceParameter;
			float floatParameter = animationEvent.floatParameter;
			float maxPrintHeight = animationEvent.intParameter;
			meshRenderer.material = material;
			printTime = 0f;
			MaterialPropertyBlock printPropertyBlock = new MaterialPropertyBlock();
			StartCoroutine(startPrint(floatParameter, maxPrintHeight, printPropertyBlock));
		}
	}

	private IEnumerator startPrint(float maxPrintTime, float maxPrintHeight, MaterialPropertyBlock printPropertyBlock)
	{
		if ((bool)meshRenderer)
		{
			while (printHeight < maxPrintHeight)
			{
				printTime += Time.deltaTime;
				printHeight = printTime / maxPrintTime * maxPrintHeight;
				meshRenderer.GetPropertyBlock(printPropertyBlock);
				printPropertyBlock.Clear();
				printPropertyBlock.SetFloat("_SliceHeight", printHeight);
				meshRenderer.SetPropertyBlock(printPropertyBlock);
				yield return new WaitForEndOfFrame();
			}
		}
	}

	public void SetChildEnable(AnimationEvent animationEvent)
	{
		string stringParameter = animationEvent.stringParameter;
		bool active = animationEvent.intParameter > 0;
		if ((bool)childLocator)
		{
			Transform transform = childLocator.FindChild(stringParameter);
			if ((bool)transform)
			{
				transform.gameObject.SetActive(active);
			}
		}
	}

	public void SwapMaterial(AnimationEvent animationEvent)
	{
		Material material = (Material)animationEvent.objectReferenceParameter;
		if ((bool)meshRenderer)
		{
			meshRenderer.material = material;
		}
	}
}
