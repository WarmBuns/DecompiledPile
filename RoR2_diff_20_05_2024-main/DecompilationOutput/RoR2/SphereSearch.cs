using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HG;
using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public class SphereSearch
{
	private struct Candidate
	{
		public Collider collider;

		public HurtBox hurtBox;

		public Vector3 position;

		public Vector3 difference;

		public float distanceSqr;

		public Transform root;

		public ProjectileController projectileController;

		public EntityLocator entityLocator;

		public static bool HurtBoxHealthComponentIsValid(Candidate candidate)
		{
			return candidate.hurtBox.healthComponent;
		}
	}

	private struct SearchData
	{
		private Candidate[] candidatesBuffer;

		private int[] candidatesMapping;

		private int candidatesCount;

		private bool hurtBoxesLoaded;

		private bool rootsLoaded;

		private bool projectileControllersLoaded;

		private bool entityLocatorsLoaded;

		private bool filteredByHurtBoxes;

		private bool filteredByHurtBoxHealthComponents;

		private bool filteredByProjectileControllers;

		private bool filteredByEntityLocators;

		public static readonly SearchData empty = new SearchData
		{
			candidatesBuffer = Array.Empty<Candidate>(),
			candidatesMapping = Array.Empty<int>(),
			candidatesCount = 0,
			hurtBoxesLoaded = false
		};

		public SearchData(Candidate[] candidatesBuffer)
		{
			this.candidatesBuffer = candidatesBuffer;
			candidatesMapping = new int[candidatesBuffer.Length];
			candidatesCount = candidatesBuffer.Length;
			for (int i = 0; i < candidatesBuffer.Length; i++)
			{
				candidatesMapping[i] = i;
			}
			hurtBoxesLoaded = false;
			rootsLoaded = false;
			projectileControllersLoaded = false;
			entityLocatorsLoaded = false;
			filteredByHurtBoxes = false;
			filteredByHurtBoxHealthComponents = false;
			filteredByProjectileControllers = false;
			filteredByEntityLocators = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ref Candidate GetCandidate(int i)
		{
			return ref candidatesBuffer[candidatesMapping[i]];
		}

		private void RemoveCandidate(int i)
		{
			ArrayUtils.ArrayRemoveAt(candidatesMapping, ref candidatesCount, i);
		}

		public void LoadHurtBoxes()
		{
			if (!hurtBoxesLoaded)
			{
				for (int i = 0; i < candidatesCount; i++)
				{
					ref Candidate candidate = ref GetCandidate(i);
					candidate.hurtBox = candidate.collider.GetComponent<HurtBox>();
				}
				hurtBoxesLoaded = true;
			}
		}

		public void LoadRoots()
		{
			if (!rootsLoaded)
			{
				for (int i = 0; i < candidatesCount; i++)
				{
					ref Candidate candidate = ref GetCandidate(i);
					candidate.root = candidate.collider.transform.root;
				}
				rootsLoaded = true;
			}
		}

		public void LoadProjectileControllers()
		{
			if (!projectileControllersLoaded)
			{
				LoadRoots();
				for (int i = 0; i < candidatesCount; i++)
				{
					ref Candidate candidate = ref GetCandidate(i);
					candidate.projectileController = (candidate.root ? candidate.root.GetComponent<ProjectileController>() : null);
				}
				projectileControllersLoaded = true;
			}
		}

		public void LoadColliderEntityLocators()
		{
			if (!entityLocatorsLoaded)
			{
				for (int i = 0; i < candidatesCount; i++)
				{
					ref Candidate candidate = ref GetCandidate(i);
					candidate.entityLocator = candidate.collider.GetComponent<EntityLocator>();
				}
				entityLocatorsLoaded = true;
			}
		}

		public void FilterByProjectileControllers()
		{
			if (filteredByProjectileControllers)
			{
				return;
			}
			LoadProjectileControllers();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				if (!GetCandidate(num).projectileController)
				{
					RemoveCandidate(num);
				}
			}
			filteredByProjectileControllers = true;
		}

		public void FilterByHurtBoxes()
		{
			if (filteredByHurtBoxes)
			{
				return;
			}
			LoadHurtBoxes();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				if (!GetCandidate(num).hurtBox)
				{
					RemoveCandidate(num);
				}
			}
			filteredByHurtBoxes = true;
		}

		public void FilterByHurtBoxHealthComponents()
		{
			if (filteredByHurtBoxHealthComponents)
			{
				return;
			}
			FilterByHurtBoxes();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				if (!GetCandidate(num).hurtBox.healthComponent)
				{
					RemoveCandidate(num);
				}
			}
			filteredByHurtBoxHealthComponents = true;
		}

		public void FilterByHurtBoxTeam(TeamMask teamMask)
		{
			FilterByHurtBoxes();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				if (!teamMask.HasTeam(GetCandidate(num).hurtBox.teamIndex))
				{
					RemoveCandidate(num);
				}
			}
		}

		public void FilterByHurtBoxEntitiesDistinct()
		{
			FilterByHurtBoxHealthComponents();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				ref Candidate candidate = ref GetCandidate(num);
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					ref Candidate candidate2 = ref GetCandidate(num2);
					if ((object)candidate.hurtBox.healthComponent == candidate2.hurtBox.healthComponent)
					{
						RemoveCandidate(num);
						break;
					}
				}
			}
		}

		public void FilterByColliderEntities()
		{
			if (filteredByEntityLocators)
			{
				return;
			}
			LoadColliderEntityLocators();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				ref Candidate candidate = ref GetCandidate(num);
				if (!candidate.entityLocator || !candidate.entityLocator.entity)
				{
					RemoveCandidate(num);
				}
			}
			filteredByEntityLocators = true;
		}

		public void FilterByColliderEntitiesDistinct()
		{
			FilterByColliderEntities();
			for (int num = candidatesCount - 1; num >= 0; num--)
			{
				ref Candidate candidate = ref GetCandidate(num);
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					ref Candidate candidate2 = ref GetCandidate(num2);
					if ((object)candidate.entityLocator.entity == candidate2.entityLocator.entity)
					{
						RemoveCandidate(num);
						break;
					}
				}
			}
		}

		public void OrderByDistance()
		{
			if (candidatesCount == 0)
			{
				return;
			}
			bool flag = true;
			while (flag)
			{
				flag = false;
				ref Candidate reference = ref GetCandidate(0);
				int i = 1;
				for (int num = candidatesCount - 1; i < num; i++)
				{
					ref Candidate candidate = ref GetCandidate(i);
					if (reference.distanceSqr > candidate.distanceSqr)
					{
						Util.Swap(ref candidatesMapping[i - 1], ref candidatesMapping[i]);
						flag = true;
					}
					else
					{
						reference = ref candidate;
					}
				}
			}
		}

		public HurtBox[] GetHurtBoxes()
		{
			FilterByHurtBoxes();
			HurtBox[] array = new HurtBox[candidatesCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = GetCandidate(i).hurtBox;
			}
			return array;
		}

		public void GetHurtBoxes(List<HurtBox> dest)
		{
			int num = dest.Count + candidatesCount;
			if (dest.Capacity < num)
			{
				dest.Capacity = num;
			}
			for (int i = 0; i < candidatesCount; i++)
			{
				dest.Add(GetCandidate(i).hurtBox);
			}
		}

		public void GetProjectileControllers(List<ProjectileController> dest)
		{
			int num = dest.Count + candidatesCount;
			if (dest.Capacity < num)
			{
				dest.Capacity = num;
			}
			for (int i = 0; i < candidatesCount; i++)
			{
				dest.Add(GetCandidate(i).projectileController);
			}
		}

		public void GetColliders(List<Collider> dest)
		{
			int num = dest.Count + candidatesCount;
			if (dest.Capacity < num)
			{
				dest.Capacity = num;
			}
			for (int i = 0; i < candidatesCount; i++)
			{
				dest.Add(GetCandidate(i).collider);
			}
		}
	}

	public float radius;

	public Vector3 origin;

	public LayerMask mask;

	public QueryTriggerInteraction queryTriggerInteraction;

	private SearchData searchData = SearchData.empty;

	public SphereSearch ClearCandidates()
	{
		searchData = SearchData.empty;
		return this;
	}

	public SphereSearch RefreshCandidates()
	{
		Collider[] colliders;
		int num = HGPhysics.OverlapSphere(out colliders, origin, radius, mask, queryTriggerInteraction);
		Candidate[] array = new Candidate[num];
		for (int i = 0; i < num; i++)
		{
			Collider collider = colliders[i];
			ref Candidate reference = ref array[i];
			reference.collider = collider;
			if (collider is MeshCollider { convex: false })
			{
				reference.position = collider.ClosestPointOnBounds(origin);
			}
			else
			{
				reference.position = collider.ClosestPoint(origin);
			}
			reference.difference = reference.position - origin;
			reference.distanceSqr = reference.difference.sqrMagnitude;
		}
		HGPhysics.ReturnResults(colliders);
		searchData = new SearchData(array);
		return this;
	}

	public SphereSearch OrderCandidatesByDistance()
	{
		searchData.OrderByDistance();
		return this;
	}

	public SphereSearch FilterCandidatesByHurtBoxTeam(TeamMask mask)
	{
		searchData.FilterByHurtBoxTeam(mask);
		return this;
	}

	public SphereSearch FilterCandidatesByColliderEntities()
	{
		searchData.FilterByColliderEntities();
		return this;
	}

	public SphereSearch FilterCandidatesByDistinctColliderEntities()
	{
		searchData.FilterByColliderEntitiesDistinct();
		return this;
	}

	public SphereSearch FilterCandidatesByDistinctHurtBoxEntities()
	{
		searchData.FilterByHurtBoxEntitiesDistinct();
		return this;
	}

	public SphereSearch FilterCandidatesByProjectileControllers()
	{
		searchData.FilterByProjectileControllers();
		return this;
	}

	public HurtBox[] GetHurtBoxes()
	{
		return searchData.GetHurtBoxes();
	}

	public void GetHurtBoxes(List<HurtBox> dest)
	{
		searchData.GetHurtBoxes(dest);
	}

	public void GetProjectileControllers(List<ProjectileController> dest)
	{
		searchData.GetProjectileControllers(dest);
	}

	public void GetColliders(List<Collider> dest)
	{
		searchData.GetColliders(dest);
	}
}
