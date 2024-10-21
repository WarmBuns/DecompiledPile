using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.SiphonItem;

public class BaseSiphonItemState : BaseBodyAttachmentState
{
	private List<ParticleSystem> FXParticles = new List<ParticleSystem>();

	protected int GetItemStack()
	{
		if (!base.attachedBody || !base.attachedBody.inventory)
		{
			return 1;
		}
		return base.attachedBody.inventory.GetItemCount(RoR2Content.Items.SiphonOnLowHealth);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		FXParticles = base.gameObject.GetComponentsInChildren<ParticleSystem>().ToList();
	}

	public void TurnOffHealingFX()
	{
		for (int i = 0; i < FXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = false;
		}
	}

	public void TurnOnHealingFX()
	{
		for (int i = 0; i < FXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = true;
		}
	}
}
