using System.Linq;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace EntityStates.ImpBossMonster;

public class BlinkState : BaseState
{
	private Transform modelTransform;

	[SerializeField]
	public bool disappearWhileBlinking;

	[SerializeField]
	public GameObject blinkPrefab;

	[SerializeField]
	public GameObject blinkDestinationPrefab;

	[SerializeField]
	public Material destealthMaterial;

	private Vector3 blinkDestination = Vector3.zero;

	private Vector3 blinkStart = Vector3.zero;

	[SerializeField]
	public float duration = 0.3f;

	[SerializeField]
	public float exitDuration;

	[SerializeField]
	public float destinationAlertDuration;

	[SerializeField]
	public float blinkDistance = 25f;

	[SerializeField]
	public string beginSoundString;

	[SerializeField]
	public string endSoundString;

	[SerializeField]
	public float blastAttackRadius;

	[SerializeField]
	public float blastAttackDamageCoefficient;

	[SerializeField]
	public float blastAttackForce;

	[SerializeField]
	public float blastAttackProcCoefficient;

	private Animator animator;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private ChildLocator childLocator;

	private GameObject blinkDestinationInstance;

	private bool isExiting;

	private bool hasBlinked;

	private BullseyeSearch search;

	private BlastAttack attack;

	private EffectData _effectData;

	private EffectManagerHelper _emh_blinkDestinationInstance;

	private int originalLayer;

	private static int BlinkEndStateHash = Animator.StringToHash("BlinkEnd");

	private static int BlinkEndParamHash = Animator.StringToHash("BlinkEnd.playbackRate");

	public override void Reset()
	{
		base.Reset();
		modelTransform = null;
		blinkDestination = Vector3.zero;
		blinkStart = Vector3.zero;
		animator = null;
		characterModel = null;
		hurtboxGroup = null;
		childLocator = null;
		blinkDestinationInstance = null;
		isExiting = false;
		hasBlinked = false;
		if (search != null)
		{
			search.Reset();
		}
		if (attack != null)
		{
			attack.Reset();
		}
		if (_effectData != null)
		{
			_effectData.Reset();
		}
		_emh_blinkDestinationInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			animator = modelTransform.GetComponent<Animator>();
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
		if (disappearWhileBlinking)
		{
			if ((bool)characterModel)
			{
				characterModel.invisibilityCount++;
			}
			if ((bool)hurtboxGroup)
			{
				HurtBoxGroup hurtBoxGroup = hurtboxGroup;
				int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
				hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			if ((bool)childLocator)
			{
				childLocator.FindChild("DustCenter").gameObject.SetActive(value: false);
			}
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = false;
		}
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
		base.characterMotor.Motor.RebuildCollidableLayers();
		CalculateBlinkDestination();
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
	}

	private void CalculateBlinkDestination()
	{
		Vector3 vector = Vector3.zero;
		Ray aimRay = GetAimRay();
		if (search == null)
		{
			search = new BullseyeSearch();
		}
		search.searchOrigin = aimRay.origin;
		search.searchDirection = aimRay.direction;
		search.maxDistanceFilter = blinkDistance;
		search.teamMaskFilter = TeamMask.allButNeutral;
		search.filterByLoS = false;
		search.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(base.gameObject));
		search.sortMode = BullseyeSearch.SortMode.Angle;
		search.RefreshCandidates();
		HurtBox hurtBox = search.GetResults().FirstOrDefault();
		if ((bool)hurtBox)
		{
			vector = hurtBox.transform.position - base.transform.position;
		}
		blinkDestination = base.transform.position;
		blinkStart = base.transform.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		NodeGraph.NodeIndex nodeIndex = groundNodes.FindClosestNode(base.transform.position + vector, base.characterBody.hullClassification);
		groundNodes.GetNodePosition(nodeIndex, out blinkDestination);
		blinkDestination += base.transform.position - base.characterBody.footPosition;
		base.characterDirection.forward = vector;
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		if ((bool)blinkPrefab)
		{
			if (_effectData == null)
			{
				_effectData = new EffectData();
			}
			_effectData.rotation = Util.QuaternionSafeLookRotation(blinkDestination - blinkStart);
			_effectData.origin = origin;
			EffectManager.SpawnEffect(blinkPrefab, _effectData, transmit: false);
		}
	}

	private void SetPosition(Vector3 newPosition)
	{
		if ((bool)base.characterMotor)
		{
			base.characterMotor.Motor.SetPositionAndRotation(newPosition, Quaternion.identity);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if (!hasBlinked)
		{
			SetPosition(Vector3.Lerp(blinkStart, blinkDestination, base.fixedAge / duration));
		}
		if (base.fixedAge >= duration - destinationAlertDuration && !hasBlinked)
		{
			hasBlinked = true;
			if ((bool)blinkDestinationPrefab)
			{
				if (!EffectManager.ShouldUsePooledEffect(blinkDestinationPrefab))
				{
					blinkDestinationInstance = Object.Instantiate(blinkDestinationPrefab, blinkDestination, Quaternion.identity);
				}
				else
				{
					_emh_blinkDestinationInstance = EffectManager.GetAndActivatePooledEffect(blinkDestinationPrefab, blinkDestination, Quaternion.identity);
					blinkDestinationInstance = _emh_blinkDestinationInstance.gameObject;
				}
				blinkDestinationInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = destinationAlertDuration;
			}
			SetPosition(blinkDestination);
		}
		if (base.fixedAge >= duration)
		{
			ExitCleanup();
		}
		if (base.fixedAge >= duration + exitDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void ExitCleanup()
	{
		if (isExiting)
		{
			return;
		}
		isExiting = true;
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		Util.PlaySound(endSoundString, base.gameObject);
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
		modelTransform = GetModelTransform();
		if (blastAttackDamageCoefficient > 0f)
		{
			if (attack == null)
			{
				attack = new BlastAttack();
			}
			attack.attacker = base.gameObject;
			attack.inflictor = base.gameObject;
			attack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			attack.baseDamage = damageStat * blastAttackDamageCoefficient;
			attack.baseForce = blastAttackForce;
			attack.position = blinkDestination;
			attack.radius = blastAttackRadius;
			attack.falloffModel = BlastAttack.FalloffModel.Linear;
			attack.attackerFiltering = AttackerFiltering.NeverHitSelf;
			attack.Fire();
		}
		if (disappearWhileBlinking)
		{
			if ((bool)modelTransform && (bool)destealthMaterial)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(animator.gameObject);
				temporaryOverlayInstance.duration = 1f;
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = destealthMaterial;
				temporaryOverlayInstance.inspectorCharacterModel = animator.gameObject.GetComponent<CharacterModel>();
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.animateShaderAlpha = true;
			}
			if ((bool)characterModel)
			{
				characterModel.invisibilityCount--;
			}
			if ((bool)hurtboxGroup)
			{
				HurtBoxGroup hurtBoxGroup = hurtboxGroup;
				int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
				hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			if ((bool)childLocator)
			{
				childLocator.FindChild("DustCenter").gameObject.SetActive(value: true);
			}
			PlayAnimation("Gesture, Additive", BlinkEndStateHash, BlinkEndParamHash, exitDuration);
		}
		if ((bool)blinkDestinationInstance)
		{
			if (_emh_blinkDestinationInstance != null && _emh_blinkDestinationInstance.OwningPool != null)
			{
				_emh_blinkDestinationInstance.OwningPool.ReturnObject(_emh_blinkDestinationInstance);
			}
			else
			{
				EntityState.Destroy(blinkDestinationInstance);
			}
			_emh_blinkDestinationInstance = null;
			blinkDestinationInstance = null;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = true;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		ExitCleanup();
	}
}
