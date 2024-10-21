using System;
using UnityEngine.Events;

namespace RoR2.Projectile;

[Serializable]
public class ProjectileImpactEvent : UnityEvent<ProjectileImpactInfo>
{
}
