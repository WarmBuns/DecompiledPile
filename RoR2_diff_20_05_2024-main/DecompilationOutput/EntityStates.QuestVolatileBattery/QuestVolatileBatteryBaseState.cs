using System;
using RoR2;
using UnityEngine;

namespace EntityStates.QuestVolatileBattery;

public class QuestVolatileBatteryBaseState : BaseState
{
	protected NetworkedBodyAttachment networkedBodyAttachment { get; private set; }

	protected HealthComponent attachedHealthComponent { get; private set; }

	protected CharacterModel attachedCharacterModel { get; private set; }

	protected Transform[] displays { get; private set; } = Array.Empty<Transform>();

	public override void OnEnter()
	{
		base.OnEnter();
		networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
		if (!networkedBodyAttachment || !networkedBodyAttachment.attachedBody)
		{
			return;
		}
		attachedHealthComponent = networkedBodyAttachment.attachedBody.healthComponent;
		ModelLocator modelLocator = networkedBodyAttachment.attachedBody.modelLocator;
		if ((bool)modelLocator)
		{
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				attachedCharacterModel = modelTransform.GetComponent<CharacterModel>();
			}
		}
	}
}
