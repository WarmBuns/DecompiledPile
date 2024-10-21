using System;
using RoR2;
using RoR2.Projectile;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace EntityStates;

public abstract class AimThrowableBase : BaseSkillState
{
	private struct CalculateArcPointsJob : IJobParallelFor, IDisposable
	{
		[ReadOnly]
		private Vector3 origin;

		[ReadOnly]
		private Vector3 velocity;

		[ReadOnly]
		private float indexMultiplier;

		[ReadOnly]
		private float gravity;

		[WriteOnly]
		public NativeArray<Vector3> outputPositions;

		public void SetParameters(Vector3 origin, Vector3 velocity, float totalTravelTime, int positionCount, float gravity)
		{
			this.origin = origin;
			this.velocity = velocity;
			if (outputPositions.Length != positionCount)
			{
				if (outputPositions.IsCreated)
				{
					outputPositions.Dispose();
				}
				outputPositions = new NativeArray<Vector3>(positionCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			}
			indexMultiplier = totalTravelTime / (float)(positionCount - 1);
			this.gravity = gravity;
		}

		public void Dispose()
		{
			if (outputPositions.IsCreated)
			{
				outputPositions.Dispose();
			}
		}

		public void Execute(int index)
		{
			float t = (float)index * indexMultiplier;
			outputPositions[index] = Trajectory.CalculatePositionAtTime(origin, velocity, t, gravity);
		}
	}

	protected struct TrajectoryInfo
	{
		public Ray finalRay;

		public Vector3 hitPoint;

		public Vector3 hitNormal;

		public float travelTime;

		public float speedOverride;
	}

	[SerializeField]
	public float maxDistance;

	[SerializeField]
	public float rayRadius;

	[SerializeField]
	public GameObject arcVisualizerPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject endpointVisualizerPrefab;

	[SerializeField]
	public float endpointVisualizerRadiusScale;

	[SerializeField]
	public bool setFuse;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float baseMinimumDuration;

	protected LineRenderer arcVisualizerLineRenderer;

	protected Transform endpointVisualizerTransform;

	protected float projectileBaseSpeed;

	protected float detonationRadius;

	protected float minimumDuration;

	protected bool useGravity;

	private CalculateArcPointsJob calculateArcPointsJob;

	private JobHandle calculateArcPointsJobHandle;

	private Vector3[] pointsBuffer = Array.Empty<Vector3>();

	private Action completeArcPointsVisualizerJobMethod;

	protected TrajectoryInfo currentTrajectoryInfo;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)arcVisualizerPrefab)
		{
			arcVisualizerLineRenderer = UnityEngine.Object.Instantiate(arcVisualizerPrefab, base.transform.position, Quaternion.identity).GetComponent<LineRenderer>();
			calculateArcPointsJob = default(CalculateArcPointsJob);
			completeArcPointsVisualizerJobMethod = CompleteArcVisualizerJob;
			RoR2Application.onLateUpdate += completeArcPointsVisualizerJobMethod;
		}
		if ((bool)endpointVisualizerPrefab)
		{
			endpointVisualizerTransform = UnityEngine.Object.Instantiate(endpointVisualizerPrefab, base.transform.position, Quaternion.identity).transform;
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.hideCrosshair = true;
		}
		ProjectileSimple component = projectilePrefab.GetComponent<ProjectileSimple>();
		if ((bool)component)
		{
			projectileBaseSpeed = component.velocity;
		}
		Rigidbody component2 = projectilePrefab.GetComponent<Rigidbody>();
		if ((bool)component2)
		{
			useGravity = component2.useGravity;
		}
		minimumDuration = baseMinimumDuration / attackSpeedStat;
		ProjectileImpactExplosion component3 = projectilePrefab.GetComponent<ProjectileImpactExplosion>();
		if ((bool)component3)
		{
			detonationRadius = component3.blastRadius;
			if ((bool)endpointVisualizerTransform)
			{
				endpointVisualizerTransform.localScale = new Vector3(detonationRadius, detonationRadius, detonationRadius);
			}
		}
		UpdateVisualizers(currentTrajectoryInfo);
		SceneCamera.onSceneCameraPreRender += OnPreRenderSceneCam;
	}

	public override void OnExit()
	{
		SceneCamera.onSceneCameraPreRender -= OnPreRenderSceneCam;
		if (!outer.destroying)
		{
			if (base.isAuthority)
			{
				FireProjectile();
			}
			OnProjectileFiredLocal();
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.hideCrosshair = false;
		}
		calculateArcPointsJobHandle.Complete();
		if ((bool)arcVisualizerLineRenderer)
		{
			EntityState.Destroy(arcVisualizerLineRenderer.gameObject);
			arcVisualizerLineRenderer = null;
		}
		if (completeArcPointsVisualizerJobMethod != null)
		{
			RoR2Application.onLateUpdate -= completeArcPointsVisualizerJobMethod;
			completeArcPointsVisualizerJobMethod = null;
		}
		calculateArcPointsJob.Dispose();
		pointsBuffer = Array.Empty<Vector3>();
		if ((bool)endpointVisualizerTransform)
		{
			EntityState.Destroy(endpointVisualizerTransform.gameObject);
			endpointVisualizerTransform = null;
		}
		base.OnExit();
	}

	protected virtual bool KeyIsDown()
	{
		return IsKeyDownAuthority();
	}

	protected virtual void OnProjectileFiredLocal()
	{
	}

	protected virtual void FireProjectile()
	{
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.crit = RollCrit();
		fireProjectileInfo.owner = base.gameObject;
		fireProjectileInfo.position = currentTrajectoryInfo.finalRay.origin;
		fireProjectileInfo.projectilePrefab = projectilePrefab;
		fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(currentTrajectoryInfo.finalRay.direction, Vector3.up);
		fireProjectileInfo.speedOverride = currentTrajectoryInfo.speedOverride;
		fireProjectileInfo.damage = damageCoefficient * damageStat;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		if (setFuse)
		{
			fireProjectileInfo2.fuseOverride = currentTrajectoryInfo.travelTime;
		}
		ModifyProjectile(ref fireProjectileInfo2);
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
	}

	protected virtual void ModifyProjectile(ref FireProjectileInfo fireProjectileInfo)
	{
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && !KeyIsDown() && base.fixedAge >= minimumDuration)
		{
			UpdateTrajectoryInfo(out currentTrajectoryInfo);
			EntityState entityState = PickNextState();
			if (entityState != null)
			{
				outer.SetNextState(entityState);
			}
			else
			{
				outer.SetNextStateToMain();
			}
		}
	}

	protected virtual EntityState PickNextState()
	{
		return null;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public override void Update()
	{
		base.Update();
		UpdateTrajectoryInfo(out currentTrajectoryInfo);
		UpdateVisualizers(currentTrajectoryInfo);
	}

	protected virtual void UpdateTrajectoryInfo(out TrajectoryInfo dest)
	{
		dest = default(TrajectoryInfo);
		Ray aimRay = GetAimRay();
		RaycastHit hitInfo = default(RaycastHit);
		bool flag = false;
		if (rayRadius > 0f && Util.CharacterSpherecast(base.gameObject, aimRay, rayRadius, out hitInfo, maxDistance, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal) && (bool)hitInfo.collider.GetComponent<HurtBox>())
		{
			flag = true;
		}
		if (!flag)
		{
			flag = Util.CharacterRaycast(base.gameObject, aimRay, out hitInfo, maxDistance, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal);
		}
		if (flag)
		{
			dest.hitPoint = hitInfo.point;
			dest.hitNormal = hitInfo.normal;
		}
		else
		{
			dest.hitPoint = aimRay.GetPoint(maxDistance);
			dest.hitNormal = -aimRay.direction;
		}
		Vector3 vector = dest.hitPoint - aimRay.origin;
		if (useGravity)
		{
			float num = projectileBaseSpeed;
			Vector2 vector2 = new Vector2(vector.x, vector.z);
			float magnitude = vector2.magnitude;
			float y = Trajectory.CalculateInitialYSpeed(magnitude / num, vector.y);
			Vector3 vector3 = new Vector3(vector2.x / magnitude * num, y, vector2.y / magnitude * num);
			dest.speedOverride = vector3.magnitude;
			dest.finalRay = new Ray(aimRay.origin, vector3 / dest.speedOverride);
			dest.travelTime = Trajectory.CalculateGroundTravelTime(num, magnitude);
		}
		else
		{
			dest.speedOverride = projectileBaseSpeed;
			dest.finalRay = aimRay;
			dest.travelTime = projectileBaseSpeed / vector.magnitude;
		}
	}

	private void CompleteArcVisualizerJob()
	{
		calculateArcPointsJobHandle.Complete();
		if ((bool)arcVisualizerLineRenderer)
		{
			Array.Resize(ref pointsBuffer, calculateArcPointsJob.outputPositions.Length);
			calculateArcPointsJob.outputPositions.CopyTo(pointsBuffer);
			arcVisualizerLineRenderer.SetPositions(pointsBuffer);
		}
	}

	private void UpdateVisualizers(TrajectoryInfo trajectoryInfo)
	{
		if ((bool)arcVisualizerLineRenderer && calculateArcPointsJobHandle.IsCompleted)
		{
			calculateArcPointsJob.SetParameters(trajectoryInfo.finalRay.origin, trajectoryInfo.finalRay.direction * trajectoryInfo.speedOverride, trajectoryInfo.travelTime, arcVisualizerLineRenderer.positionCount, useGravity ? Physics.gravity.y : 0f);
			calculateArcPointsJobHandle = calculateArcPointsJob.Schedule(calculateArcPointsJob.outputPositions.Length, 32);
		}
		if ((bool)endpointVisualizerTransform)
		{
			endpointVisualizerTransform.SetPositionAndRotation(trajectoryInfo.hitPoint, Util.QuaternionSafeLookRotation(trajectoryInfo.hitNormal));
			if (!endpointVisualizerRadiusScale.Equals(0f))
			{
				endpointVisualizerTransform.localScale = new Vector3(endpointVisualizerRadiusScale, endpointVisualizerRadiusScale, endpointVisualizerRadiusScale);
			}
		}
	}

	private void OnPreRenderSceneCam(SceneCamera sceneCam)
	{
		if ((bool)arcVisualizerLineRenderer)
		{
			arcVisualizerLineRenderer.renderingLayerMask = ((sceneCam.cameraRigController.target == base.gameObject) ? 1u : 0u);
		}
		if ((bool)endpointVisualizerTransform)
		{
			endpointVisualizerTransform.gameObject.layer = ((sceneCam.cameraRigController.target == base.gameObject) ? LayerIndex.defaultLayer.intVal : LayerIndex.noDraw.intVal);
		}
	}
}
