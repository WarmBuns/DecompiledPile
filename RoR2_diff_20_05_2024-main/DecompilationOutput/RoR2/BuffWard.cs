using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(TeamFilter))]
public class BuffWard : NetworkBehaviour
{
	public enum BuffWardShape
	{
		Sphere,
		VerticalTube,
		Count
	}

	[Tooltip("The shape of the area")]
	public BuffWardShape shape;

	[SyncVar]
	[Tooltip("The area of effect.")]
	public float radius;

	[Tooltip("How long between buff pulses in the area of effect.")]
	public float interval = 1f;

	[Tooltip("The child range indicator object. Will be scaled to the radius.")]
	public Transform rangeIndicator;

	[Tooltip("The buff type to grant")]
	public BuffDef buffDef;

	[Tooltip("The buff duration")]
	public float buffDuration;

	[Tooltip("Should the ward be floored on start")]
	public bool floorWard;

	[Tooltip("Does the ward disappear over time?")]
	public bool expires;

	[Tooltip("If set, applies to all teams BUT the one selected.")]
	public bool invertTeamFilter;

	public float expireDuration;

	public bool animateRadius;

	public AnimationCurve radiusCoefficientCurve;

	[Tooltip("If set, the ward will give you this amount of time to play removal effects.")]
	public float removalTime;

	private bool needsRemovalTime;

	public string removalSoundString = "";

	public UnityEvent onRemoval;

	public bool requireGrounded;

	private TeamFilter teamFilter;

	private float buffTimer;

	private float rangeIndicatorScaleVelocity;

	private float stopwatch;

	private float calculatedRadius;

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
	}

	private void OnEnable()
	{
		if ((bool)rangeIndicator)
		{
			rangeIndicator.gameObject.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		if ((bool)rangeIndicator)
		{
			rangeIndicator.gameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		if (removalTime > 0f)
		{
			needsRemovalTime = true;
		}
		if (floorWard && Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 500f, LayerIndex.world.mask))
		{
			base.transform.position = hitInfo.point;
			base.transform.up = hitInfo.normal;
		}
		if ((bool)rangeIndicator && expires)
		{
			ScaleParticleSystemDuration component = rangeIndicator.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = expireDuration;
			}
		}
	}

	private void Update()
	{
		calculatedRadius = (animateRadius ? (radius * radiusCoefficientCurve.Evaluate(stopwatch / expireDuration)) : radius);
		stopwatch += Time.deltaTime;
		if (expires && NetworkServer.active)
		{
			if (needsRemovalTime)
			{
				if (stopwatch >= expireDuration - removalTime)
				{
					needsRemovalTime = false;
					Util.PlaySound(removalSoundString, base.gameObject);
					onRemoval.Invoke();
				}
			}
			else if (expireDuration <= stopwatch)
			{
				Object.Destroy(base.gameObject);
			}
		}
		if ((bool)rangeIndicator)
		{
			float num = Mathf.SmoothDamp(rangeIndicator.localScale.x, calculatedRadius, ref rangeIndicatorScaleVelocity, 0.2f);
			rangeIndicator.localScale = new Vector3(num, num, num);
		}
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		buffTimer -= Time.fixedDeltaTime;
		if (!(buffTimer <= 0f))
		{
			return;
		}
		buffTimer = interval;
		float radiusSqr = calculatedRadius * calculatedRadius;
		Vector3 position = base.transform.position;
		if (invertTeamFilter)
		{
			for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex++)
			{
				if (teamIndex != teamFilter.teamIndex)
				{
					BuffTeam(TeamComponent.GetTeamMembers(teamIndex), radiusSqr, position);
				}
			}
		}
		else
		{
			BuffTeam(TeamComponent.GetTeamMembers(teamFilter.teamIndex), radiusSqr, position);
		}
	}

	private void BuffTeam(IEnumerable<TeamComponent> recipients, float radiusSqr, Vector3 currentPosition)
	{
		if (!NetworkServer.active || !buffDef)
		{
			return;
		}
		foreach (TeamComponent recipient in recipients)
		{
			Vector3 vector = recipient.transform.position - currentPosition;
			if (shape == BuffWardShape.VerticalTube)
			{
				vector.y = 0f;
			}
			if (vector.sqrMagnitude <= radiusSqr)
			{
				CharacterBody component = recipient.GetComponent<CharacterBody>();
				if ((bool)component && (!requireGrounded || !component.characterMotor || component.characterMotor.isGrounded))
				{
					component.AddTimedBuff(buffDef.buffIndex, buffDuration);
				}
			}
		}
	}

	private void OnValidate()
	{
		if (!buffDef)
		{
			Debug.LogWarningFormat(this, "BuffWard {0} has no buff specified.", this);
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
