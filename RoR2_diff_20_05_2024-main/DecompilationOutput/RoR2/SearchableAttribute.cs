using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace RoR2;

[MeansImplicitUse]
public abstract class SearchableAttribute : Attribute
{
	private static readonly Dictionary<Type, List<SearchableAttribute>> instancesListsByType;

	private static HashSet<string> assemblyBlacklist;

	public object target { get; private set; }

	public static List<SearchableAttribute> GetInstances<T>() where T : SearchableAttribute
	{
		if (instancesListsByType.TryGetValue(typeof(T), out var value))
		{
			return value;
		}
		return null;
	}

	public static void GetInstances<T>(List<T> dest) where T : SearchableAttribute
	{
		List<SearchableAttribute> instances = GetInstances<T>();
		if (instances == null)
		{
			return;
		}
		foreach (SearchableAttribute item in instances)
		{
			dest.Add((T)item);
		}
	}

	private static IEnumerable<Assembly> GetScannableAssemblies()
	{
		return from a in AppDomain.CurrentDomain.GetAssemblies()
			where !assemblyBlacklist.Contains(a.GetName().Name)
			select a;
	}

	private static void Scan()
	{
		IEnumerable<Type> enumerable = GetScannableAssemblies().SelectMany((Assembly a) => a.GetTypes());
		List<SearchableAttribute> allInstancesList = new List<SearchableAttribute>();
		Type memoizedInput = null;
		List<SearchableAttribute> memoizedOutput = null;
		foreach (Type item in enumerable)
		{
			foreach (SearchableAttribute customAttribute in CustomAttributeExtensions.GetCustomAttributes<SearchableAttribute>(item, inherit: false))
			{
				Register(customAttribute, item);
			}
			MemberInfo[] members = item.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MemberInfo element in members)
			{
				foreach (SearchableAttribute customAttribute2 in CustomAttributeExtensions.GetCustomAttributes<SearchableAttribute>(element, inherit: false))
				{
					Register(customAttribute2, element);
				}
			}
		}
		List<SearchableAttribute> GetInstancesListForType(Type attributeType)
		{
			if (attributeType.Equals(memoizedInput))
			{
				return memoizedOutput;
			}
			if (!instancesListsByType.TryGetValue(attributeType, out var value))
			{
				value = new List<SearchableAttribute>();
				instancesListsByType.Add(attributeType, value);
			}
			memoizedInput = attributeType;
			memoizedOutput = value;
			return value;
		}
		void Register(SearchableAttribute attribute, object target)
		{
			attribute.target = target;
			allInstancesList.Add(attribute);
			GetInstancesListForType(attribute.GetType()).Add(attribute);
		}
	}

	static SearchableAttribute()
	{
		instancesListsByType = new Dictionary<Type, List<SearchableAttribute>>();
		assemblyBlacklist = new HashSet<string>
		{
			"mscorlib", "UnityEngine", "UnityEngine.AIModule", "UnityEngine.ARModule", "UnityEngine.AccessibilityModule", "UnityEngine.AnimationModule", "UnityEngine.AssetBundleModule", "UnityEngine.AudioModule", "UnityEngine.BaselibModule", "UnityEngine.ClothModule",
			"UnityEngine.ClusterInputModule", "UnityEngine.ClusterRendererModule", "UnityEngine.CoreModule", "UnityEngine.CrashReportingModule", "UnityEngine.DirectorModule", "UnityEngine.FileSystemHttpModule", "UnityEngine.GameCenterModule", "UnityEngine.GridModule", "UnityEngine.HotReloadModule", "UnityEngine.IMGUIModule",
			"UnityEngine.ImageConversionModule", "UnityEngine.InputModule", "UnityEngine.JSONSerializeModule", "UnityEngine.LocalizationModule", "UnityEngine.ParticleSystemModule", "UnityEngine.PerformanceReportingModule", "UnityEngine.PhysicsModule", "UnityEngine.Physics2DModule", "UnityEngine.ProfilerModule", "UnityEngine.ScreenCaptureModule",
			"UnityEngine.SharedInternalsModule", "UnityEngine.SpatialTrackingModule", "UnityEngine.SpriteMaskModule", "UnityEngine.SpriteShapeModule", "UnityEngine.StreamingModule", "UnityEngine.StyleSheetsModule", "UnityEngine.SubstanceModule", "UnityEngine.TLSModule", "UnityEngine.TerrainModule", "UnityEngine.TerrainPhysicsModule",
			"UnityEngine.TextCoreModule", "UnityEngine.TextRenderingModule", "UnityEngine.TilemapModule", "UnityEngine.TimelineModule", "UnityEngine.UIModule", "UnityEngine.UIElementsModule", "UnityEngine.UNETModule", "UnityEngine.UmbraModule", "UnityEngine.UnityAnalyticsModule", "UnityEngine.UnityConnectModule",
			"UnityEngine.UnityTestProtocolModule", "UnityEngine.UnityWebRequestModule", "UnityEngine.UnityWebRequestAssetBundleModule", "UnityEngine.UnityWebRequestAudioModule", "UnityEngine.UnityWebRequestTextureModule", "UnityEngine.UnityWebRequestWWWModule", "UnityEngine.VFXModule", "UnityEngine.VRModule", "UnityEngine.VehiclesModule", "UnityEngine.VideoModule",
			"UnityEngine.WindModule", "UnityEngine.XRModule", "UnityEditor", "Unity.Locator", "System.Core", "System", "Mono.Security", "System.Configuration", "System.Xml", "Unity.DataContract",
			"Unity.PackageManager", "UnityEngine.UI", "UnityEditor.UI", "UnityEditor.TestRunner", "UnityEngine.TestRunner", "nunit.framework", "UnityEngine.Timeline", "UnityEditor.Timeline", "UnityEngine.Networking", "UnityEditor.Networking",
			"UnityEditor.GoogleAudioSpatializer", "UnityEngine.GoogleAudioSpatializer", "UnityEditor.SpatialTracking", "UnityEngine.SpatialTracking", "UnityEditor.VR", "UnityEditor.Graphs", "UnityEditor.WindowsStandalone.Extensions", "SyntaxTree.VisualStudio.Unity.Bridge", "Rewired_ControlMapper_CSharp_Editor", "Rewired_CSharp_Editor",
			"Unity.ProBuilder.AddOns.Editor", "Wwise-Editor", "Unity.RenderPipelines.Core.Editor", "Unity.RenderPipelines.Core.Runtime", "Unity.TextMeshPro.Editor", "Unity.PackageManagerUI.Editor", "Rewired_NintendoSwitch_CSharp", "Unity.Postprocessing.Editor", "Rewired_CSharp", "Unity.Postprocessing.Runtime",
			"Rewired_NintendoSwitch_CSharp_Editor", "Wwise", "Unity.RenderPipelines.Core.ShaderLibrary", "Unity.TextMeshPro", "Rewired_UnityUI_CSharp_Editor", "Facepunch.Steamworks", "Rewired_Editor", "Rewired_Core", "Rewired_Windows_Lib", "Rewired_NintendoSwitch_Editor",
			"Rewired_NintendoSwitch_EditorRuntime", "Zio", "AssetIdRemapUtility", "ProBuilderCore", "ProBuilderMeshOps", "KdTreeLib", "pb_Stl", "Poly2Tri", "ProBuilderEditor", "netstandard",
			"System.Xml.Linq", "Unity.Cecil", "Unity.SerializationLogic", "Unity.Legacy.NRefactory", "ExCSS.Unity", "Unity.IvyParser", "UnityEditor.iOS.Extensions.Xcode", "SyntaxTree.VisualStudio.Unity.Messaging", "Microsoft.GeneratedCode", "Anonymously",
			"Hosted", "DynamicMethods", "Assembly", "UnityEditor.Switch.Extensions"
		};
		try
		{
			Scan();
		}
		catch (Exception ex)
		{
			System.Console.WriteLine(ex.Message);
		}
	}
}
