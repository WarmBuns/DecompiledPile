using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(TeamFilter))]
public class DeskPlantController : MonoBehaviour
{
	private abstract class DeskPlantBaseState : BaseState
	{
		protected DeskPlantController controller;

		protected abstract bool showSeedObject { get; }

		protected abstract bool showPlantObject { get; }

		protected abstract float CalcScale();

		public override void OnEnter()
		{
			base.OnEnter();
			controller = GetComponent<DeskPlantController>();
			controller.seedObject.SetActive(showSeedObject);
			controller.plantObject.SetActive(showPlantObject);
		}

		public override void Update()
		{
			base.Update();
			if (showPlantObject)
			{
				float num = CalcScale();
				Vector3 vector = new Vector3(num, num, num);
				Transform transform = controller.plantObject.transform;
				if (transform.localScale != vector)
				{
					transform.localScale = vector;
				}
			}
		}
	}

	private class SeedState : DeskPlantBaseState
	{
		protected override bool showSeedObject => true;

		protected override bool showPlantObject => false;

		protected override float CalcScale()
		{
			return 1f;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			Util.PlaySound("Play_item_proc_interstellarDeskPlant_grow", base.gameObject);
			GetComponent<Animation>().Play();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= seedDuration)
			{
				outer.SetNextState(new SproutState());
			}
		}
	}

	private class SproutState : DeskPlantBaseState
	{
		protected override bool showSeedObject => false;

		protected override bool showPlantObject => true;

		protected override float CalcScale()
		{
			float time = Mathf.Clamp01(base.age / scaleDuration);
			return linearRampUp.Evaluate(time) * easeUp.Evaluate(time);
		}

		public override void OnEnter()
		{
			base.OnEnter();
			Util.PlaySound("Play_item_proc_interstellarDeskPlant_bloom", base.gameObject);
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= scaleDuration)
			{
				outer.SetNextState(new MainState());
			}
		}
	}

	private class MainState : DeskPlantBaseState
	{
		private GameObject deskplantWard;

		protected override bool showSeedObject => false;

		protected override bool showPlantObject => true;

		protected override float CalcScale()
		{
			return 1f;
		}

		public override void OnEnter()
		{
			base.OnEnter();
			if (NetworkServer.active && !deskplantWard)
			{
				deskplantWard = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DeskplantWard"), controller.plantObject.transform.position, Quaternion.identity);
				deskplantWard.GetComponent<TeamFilter>().teamIndex = controller.teamFilter.teamIndex;
				if ((bool)deskplantWard)
				{
					HealingWard component = deskplantWard.GetComponent<HealingWard>();
					component.healFraction = 0.05f;
					component.healPoints = 0f;
					component.Networkradius = controller.healingRadius + controller.radiusIncreasePerStack * (float)controller.itemCount;
					component.enabled = true;
				}
				NetworkServer.Spawn(deskplantWard);
			}
		}

		public override void OnExit()
		{
			if ((bool)deskplantWard)
			{
				EntityState.Destroy(deskplantWard);
			}
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= mainDuration)
			{
				outer.SetNextState(new DeathState());
			}
		}
	}

	private class DeathState : DeskPlantBaseState
	{
		protected override bool showSeedObject => false;

		protected override bool showPlantObject => true;

		protected override float CalcScale()
		{
			float time = Mathf.Clamp01(base.age / scaleDuration);
			return linearRampDown.Evaluate(time) * easeDown.Evaluate(time);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge >= scaleDuration)
			{
				EntityState.Destroy(base.gameObject);
			}
		}
	}

	public GameObject plantObject;

	public GameObject seedObject;

	public GameObject groundFX;

	public float healingRadius;

	public float radiusIncreasePerStack = 5f;

	private static readonly float seedDuration = 5f;

	private static readonly float mainDuration = 10f;

	private static readonly float scaleDuration = 0.5f;

	private static readonly AnimationCurve linearRampUp = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	private static readonly AnimationCurve linearRampDown = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	private static readonly AnimationCurve easeUp = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	private static readonly AnimationCurve easeDown = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

	public TeamFilter teamFilter { get; private set; }

	public int itemCount { get; set; }

	private void Awake()
	{
		teamFilter = GetComponent<TeamFilter>();
		plantObject.SetActive(value: false);
		seedObject.SetActive(value: false);
		if (NetworkServer.active && Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 500f, LayerIndex.world.mask))
		{
			base.transform.position = hitInfo.point;
			base.transform.up = hitInfo.normal;
		}
	}

	public void SeedPlanting()
	{
		EffectManager.SimpleEffect(groundFX, base.transform.position, Quaternion.identity, transmit: false);
	}
}
