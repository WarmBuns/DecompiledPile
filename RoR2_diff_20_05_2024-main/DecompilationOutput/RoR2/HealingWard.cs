using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(TeamFilter))]
public class HealingWard : NetworkBehaviour
{
	[SyncVar]
	[Tooltip("The area of effect.")]
	public float radius;

	[Tooltip("How long between heal pulses in the area of effect.")]
	public float interval = 1f;

	[Tooltip("How many hit points to restore each pulse.")]
	public float healPoints;

	[Tooltip("What fraction of the healee max health to restore each pulse.")]
	public float healFraction;

	[Tooltip("The child range indicator object. Will be scaled to the radius.")]
	public Transform rangeIndicator;

	[Tooltip("Should the ward be floored on start")]
	public bool floorWard;

	private TeamFilter teamFilter;

	private float healTimer;

	private float rangeIndicatorScaleVelocity;

	public float Networkradius
	{
		get
		{
			return radius;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref radius, 1u);
		}
	}

	private void Awake()
	{
		teamFilter = GetComponent<TeamFilter>();
		if (NetworkServer.active && floorWard && Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 500f, LayerIndex.world.mask))
		{
			base.transform.position = hitInfo.point;
			base.transform.up = hitInfo.normal;
		}
	}

	private void Update()
	{
		if ((bool)rangeIndicator)
		{
			float num = Mathf.SmoothDamp(rangeIndicator.localScale.x, radius, ref rangeIndicatorScaleVelocity, 0.2f);
			rangeIndicator.localScale = new Vector3(num, num, num);
		}
	}

	private void FixedUpdate()
	{
		healTimer -= Time.fixedDeltaTime;
		if (healTimer <= 0f && NetworkServer.active)
		{
			healTimer = interval;
			HealOccupants();
		}
	}

	private void HealOccupants()
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(teamFilter.teamIndex);
		float num = radius * radius;
		Vector3 position = base.transform.position;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			if (!((teamMembers[i].transform.position - position).sqrMagnitude <= num))
			{
				continue;
			}
			HealthComponent component = teamMembers[i].GetComponent<HealthComponent>();
			if ((bool)component)
			{
				float num2 = healPoints + component.fullHealth * healFraction;
				if (num2 > 0f)
				{
					component.Heal(num2, default(ProcChainMask));
				}
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(radius);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(radius);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			radius = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			radius = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
