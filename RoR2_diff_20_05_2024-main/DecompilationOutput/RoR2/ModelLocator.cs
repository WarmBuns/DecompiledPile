using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[DisallowMultipleComponent]
public class ModelLocator : MonoBehaviour, ILifeBehavior
{
	private class DestructionNotifier : MonoBehaviour
	{
		public ModelLocator subscriber { private get; set; }

		private void OnDestroy()
		{
			if ((object)subscriber != null)
			{
				subscriber.modelTransform = null;
			}
		}
	}

	[FormerlySerializedAs("modelTransform")]
	[Header("Cached Model Values")]
	[SerializeField]
	[Tooltip("The transform of the child gameobject which acts as the model for this entity.")]
	private Transform _modelTransform;

	public RendererVisiblity modelVisibility;

	private DestructionNotifier modelDestructionNotifier;

	[Tooltip("The transform of the child gameobject which acts as the base for this entity's model. If provided, this will be detached from the hierarchy and positioned to match this object's position.")]
	public Transform modelBaseTransform;

	[Tooltip("Compensates for the irregular scaling from baseline human scale for attached VFX.")]
	public float modelScaleCompensation = 1f;

	[Header("Update Properties")]
	[Tooltip("Whether or not to update the model transforms automatically.")]
	public bool autoUpdateModelTransform = true;

	[Tooltip("Forces the model to remain in hierarchy, rather that detaching on start. You usually don't want this for anything that moves.")]
	public bool dontDetatchFromParent;

	private Transform modelParentTransform;

	[Header("Corpse Properties")]
	[Tooltip("Only matters if preserveModel=true. Prevents the addition of a Corpse component to the model when this object is destroyed.")]
	public bool noCorpse;

	[Tooltip("If true, ownership of the model will not be relinquished by death.")]
	public bool dontReleaseModelOnDeath;

	[Tooltip("Prevents the model from being destroyed when this object is destroyed. This is rarely used, as character death states are usually responsible for snatching the model away and leaving this ModelLocator with nothing to destroy.")]
	public bool preserveModel;

	public bool forceCulled;

	[Header("Normal Properties")]
	[Tooltip("Allows the model to align to the floor.")]
	public bool normalizeToFloor;

	public float normalSmoothdampTime = 0.1f;

	[Range(0f, 90f)]
	public float normalMaxAngleDelta = 90f;

	private Vector3 normalSmoothdampVelocity;

	private Vector3 targetNormal = Vector3.up;

	private Vector3 currentNormal = Vector3.up;

	private CharacterMotor characterMotor;

	public Transform modelTransform
	{
		get
		{
			return _modelTransform;
		}
		set
		{
			if ((object)_modelTransform != value)
			{
				if ((object)modelDestructionNotifier != null)
				{
					modelDestructionNotifier.subscriber = null;
					UnityEngine.Object.Destroy(modelDestructionNotifier);
					modelDestructionNotifier = null;
				}
				_modelTransform = value;
				if ((bool)_modelTransform)
				{
					modelDestructionNotifier = _modelTransform.gameObject.AddComponent<DestructionNotifier>();
					modelDestructionNotifier.subscriber = this;
				}
				this.onModelChanged?.Invoke(_modelTransform);
			}
		}
	}

	public event Action<Transform> onModelChanged;

	private void Awake()
	{
		characterMotor = GetComponent<CharacterMotor>();
	}

	public void Start()
	{
		if ((bool)modelTransform)
		{
			modelParentTransform = modelTransform.parent;
			if (!dontDetatchFromParent)
			{
				modelTransform.parent = null;
			}
		}
	}

	private void UpdateModelTransform(float deltaTime)
	{
		if ((bool)modelTransform && (bool)modelParentTransform)
		{
			Vector3 position = modelParentTransform.position;
			Quaternion rotation = modelParentTransform.rotation;
			UpdateTargetNormal();
			SmoothNormals(deltaTime);
			rotation = Quaternion.FromToRotation(Vector3.up, currentNormal) * rotation;
			modelTransform.SetPositionAndRotation(position, rotation);
		}
	}

	private void SmoothNormals(float deltaTime)
	{
		currentNormal = Vector3.SmoothDamp(currentNormal, targetNormal, ref normalSmoothdampVelocity, normalSmoothdampTime, float.PositiveInfinity, deltaTime);
	}

	private void UpdateTargetNormal()
	{
		if (normalizeToFloor && (bool)characterMotor)
		{
			targetNormal = (characterMotor.isGrounded ? characterMotor.estimatedGroundNormal : Vector3.up);
			targetNormal = Vector3.RotateTowards(Vector3.up, targetNormal, normalMaxAngleDelta * (MathF.PI / 180f), float.PositiveInfinity);
		}
		else
		{
			targetNormal = Vector3.up;
		}
	}

	public void LateUpdate()
	{
		if (autoUpdateModelTransform)
		{
			UpdateModelTransform(Time.deltaTime);
		}
	}

	private void OnDestroy()
	{
		if (!modelTransform)
		{
			return;
		}
		if (preserveModel)
		{
			if (!noCorpse)
			{
				modelTransform.gameObject.AddComponent<Corpse>();
			}
			modelTransform = null;
		}
		else
		{
			UnityEngine.Object.Destroy(modelTransform.gameObject);
		}
	}

	public void OnDeathStart()
	{
		if (!dontReleaseModelOnDeath)
		{
			preserveModel = true;
		}
	}
}
