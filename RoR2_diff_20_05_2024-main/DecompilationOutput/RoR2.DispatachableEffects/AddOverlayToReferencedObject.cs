using UnityEngine;

namespace RoR2.DispatachableEffects;

[RequireComponent(typeof(EffectComponent))]
[RequireComponent(typeof(TemporaryOverlay))]
public class AddOverlayToReferencedObject : MonoBehaviour
{
	public bool effectDataGenericFloatOverridesDuration = true;

	protected void Start()
	{
		EffectComponent component = GetComponent<EffectComponent>();
		ApplyOverlay(component.GetReferencedObject(), component.effectData.genericFloat);
	}

	protected void ApplyOverlay(GameObject targetObject, float duration)
	{
		if (!targetObject)
		{
			return;
		}
		ModelLocator component = targetObject.GetComponent<ModelLocator>();
		if (!component)
		{
			return;
		}
		Transform modelTransform = component.modelTransform;
		if (!modelTransform)
		{
			return;
		}
		CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
		if ((bool)component2)
		{
			TemporaryOverlay component3 = GetComponent<TemporaryOverlay>();
			if ((bool)component3)
			{
				component3.AddToCharacerModel(component2);
			}
			if (effectDataGenericFloatOverridesDuration)
			{
				component3.durationInstance = duration;
			}
		}
	}
}
