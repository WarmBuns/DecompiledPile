using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using EntityStates;
using EntityStates.Commando.CommandoWeapon;
using Microsoft.CodeAnalysis;
using On.EntityStates;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName = ".NET Standard 2.0")]
[assembly: AssemblyCompany("StickyGrenades")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("StickyGrenades")]
[assembly: AssemblyTitle("StickyGrenades")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Microsoft.CodeAnalysis.Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public readonly int Version;

		public RefSafetyRulesAttribute(int P_0)
		{
			Version = P_0;
		}
	}
}
namespace StickyGrenades
{
	[BepInPlugin("com.Nuxlar.StickyGrenades", "StickyGrenades", "1.0.1")]
	public class StickyGrenades : BaseUnityPlugin
	{
		private GameObject commando = Addressables.LoadAssetAsync<GameObject>((object)"RoR2/Base/Commando/CommandoBody.prefab").WaitForCompletion();

		private GameObject stickyGrenade = Addressables.LoadAssetAsync<GameObject>((object)"RoR2/Junk/Commando/CommandoStickyGrenadeProjectile.prefab").WaitForCompletion();

		private GameObject grenadeVFX = Addressables.LoadAssetAsync<GameObject>((object)"RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();

		private SkillDef stickyGrenadeSkill = Addressables.LoadAssetAsync<SkillDef>((object)"RoR2/Junk/Commando/ThrowStickyGrenade.asset").WaitForCompletion();

		public AssetBundleCreateRequest stickyGrenadeAssets;

		private Sprite newIconSprite;

		public void Awake()
		{
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Expected O, but got Unknown
			stickyGrenadeAssets = AssetBundle.LoadFromFileAsync(Path.Combine(Path.GetDirectoryName(((BaseUnityPlugin)this).Info.Location), "stickygrenades.bundle"));
			ref Sprite reference = ref newIconSprite;
			Object asset = stickyGrenadeAssets.assetBundle.LoadAssetAsync<Sprite>("assets/texcommandoskilliconssticky.png").asset;
			reference = (Sprite)(object)((asset is Sprite) ? asset : null);
			ProjectileImpactExplosion component = stickyGrenade.GetComponent<ProjectileImpactExplosion>();
			component.lifetimeAfterImpact = 1f;
			((ProjectileExplosion)component).falloffModel = (FalloffModel)0;
			stickyGrenade.GetComponent<ProjectileImpactExplosion>().impactEffect = grenadeVFX;
			stickyGrenadeSkill.icon = newIconSprite;
			stickyGrenadeSkill.skillNameToken = "Sticky Grenade";
			stickyGrenadeSkill.skillDescriptionToken = "Throw a grenade that <style=cIsUtility>sticks to enemies</style> and explodes for <style=cIsDamage>600% damage</style>. Can hold up to 2.";
			AddSkill();
			GenericProjectileBaseState.OnEnter += new hook_OnEnter(AlterSticky);
		}

		private void AddSkill()
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Expected O, but got Unknown
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			SkillFamily skillFamily = commando.GetComponent<SkillLocator>().special.skillFamily;
			Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
			Variant[] variants = skillFamily.variants;
			int num = skillFamily.variants.Length - 1;
			Variant val = new Variant
			{
				skillDef = stickyGrenadeSkill
			};
			((Variant)(ref val)).viewableNode = new Node(stickyGrenadeSkill.skillNameToken, false, (Node)null);
			variants[num] = val;
		}

		private void AlterSticky(orig_OnEnter orig, GenericProjectileBaseState self)
		{
			if (self is ThrowStickyGrenade)
			{
				self.damageCoefficient = 6f;
			}
			orig.Invoke(self);
		}
	}
}