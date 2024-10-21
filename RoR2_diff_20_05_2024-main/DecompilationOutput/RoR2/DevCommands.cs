using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public static class DevCommands
{
	private class CvSvNetLogObjectIds : ToggleVirtualConVar
	{
		private static readonly CvSvNetLogObjectIds instance = new CvSvNetLogObjectIds("sv_net_log_object_ids", ConVarFlags.None, "0", "Logs objects associated with each network id to net_id_log.txt as encountered by the server.");

		private uint highestObservedId;

		private FieldInfo monitoredField;

		private TextWriter writer;

		public CvSvNetLogObjectIds(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
			monitoredField = typeof(NetworkIdentity).GetField("s_NextNetworkId", BindingFlags.Static | BindingFlags.NonPublic);
		}

		protected override void OnEnable()
		{
			RoR2Application.onFixedUpdate += Update;
			RoR2Application.onUpdate += Update;
			writer = new StreamWriter("net_id_log.txt", append: false);
			highestObservedId = GetCurrentHighestID();
		}

		protected override void OnDisable()
		{
			writer.Dispose();
			writer = null;
			RoR2Application.onUpdate -= Update;
			RoR2Application.onFixedUpdate -= Update;
		}

		private uint GetCurrentHighestID()
		{
			return (uint)monitoredField.GetValue(null);
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				uint currentHighestID = GetCurrentHighestID();
				while (highestObservedId < currentHighestID)
				{
					GameObject gameObject = NetworkServer.FindLocalObject(new NetworkInstanceId(highestObservedId));
					writer.WriteLine(string.Format("[{0, 0:D10}]={1}", highestObservedId, gameObject ? gameObject.name : "null"));
					highestObservedId++;
				}
			}
		}
	}

	private static void AddTokenIfDefault(List<string> lines, string token)
	{
		if (!string.IsNullOrEmpty(token) && (object)Language.GetString(token) == token)
		{
			lines.Add(string.Format("\t\t\"{0}\": \"{0}\",", token));
		}
	}

	[ConCommand(commandName = "language_generate_tokens", flags = ConVarFlags.None, helpText = "Generates default token definitions to be inserted into a JSON language file.")]
	private static void CCLanguageGenerateTokens(ConCommandArgs args)
	{
		List<string> list = new List<string>();
		foreach (ItemDef item in ItemCatalog.allItems.Select(ItemCatalog.GetItemDef))
		{
			AddTokenIfDefault(list, item.nameToken);
			AddTokenIfDefault(list, item.pickupToken);
			AddTokenIfDefault(list, item.descriptionToken);
		}
		list.Add("\r\n");
		foreach (EquipmentDef item2 in EquipmentCatalog.allEquipment.Select(EquipmentCatalog.GetEquipmentDef))
		{
			AddTokenIfDefault(list, item2.nameToken);
			AddTokenIfDefault(list, item2.pickupToken);
			AddTokenIfDefault(list, item2.descriptionToken);
		}
	}

	[ConCommand(commandName = "rng_test_roll", flags = ConVarFlags.None, helpText = "Tests the RNG. First argument is a percent chance, second argument is a number of rolls to perform. Result is the average number of rolls that passed.")]
	private static void CCTestRng(ConCommandArgs args)
	{
		float argFloat = args.GetArgFloat(0);
		ulong argULong = args.GetArgULong(1);
		ulong num = 0uL;
		for (ulong num2 = 0uL; num2 < argULong; num2++)
		{
			if (RoR2Application.rng.RangeFloat(0f, 100f) < argFloat)
			{
				num++;
			}
		}
	}

	[ConCommand(commandName = "getpos", flags = ConVarFlags.None, helpText = "Prints the current position of the sender's body.")]
	private static void CCGetPos(ConCommandArgs args)
	{
		Vector3 position = args.GetSenderBody().transform.position;
		Debug.LogFormat("{0} {1} {2}", position.x, position.y, position.z);
	}

	[ConCommand(commandName = "setpos", flags = ConVarFlags.Cheat, helpText = "Teleports the sender's body to the specified position.")]
	private static void CCSetPos(ConCommandArgs args)
	{
		CharacterBody senderBody = args.GetSenderBody();
		TeleportHelper.TeleportGameObject(newPosition: new Vector3(args.GetArgFloat(0), args.GetArgFloat(1), args.GetArgFloat(2)), gameObject: senderBody.gameObject);
	}

	[ConCommand(commandName = "create_object_from_resources", flags = (ConVarFlags.ExecuteOnServer | ConVarFlags.Cheat), helpText = "Instantiates an object from the Resources folder where the sender is looking.")]
	private static void CreateObjectFromResources(ConCommandArgs args)
	{
		CharacterBody senderBody = args.GetSenderBody();
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>(args.GetArgString(0));
		if (!gameObject)
		{
			throw new ConCommandException("Prefab could not be found at the specified path. Argument must be a Resources/-relative path to a prefab.");
		}
		if (senderBody.GetComponent<InputBankTest>().GetAimRaycast(float.PositiveInfinity, out var hitInfo))
		{
			Vector3 point = hitInfo.point;
			Quaternion identity = Quaternion.identity;
			NetworkServer.Spawn(UnityEngine.Object.Instantiate(gameObject, point, identity));
		}
	}

	[ConCommand(commandName = "resources_load_async_test", flags = ConVarFlags.None, helpText = "Tests Resources.LoadAsync. Loads the asset at the specified path and prints out the results of the operation.")]
	private static void CCResourcesLoadAsyncTest(ConCommandArgs args)
	{
		Resources.LoadAsync(args.GetArgString(0)).completed += Check;
		static void Check(AsyncOperation asyncOperation)
		{
		}
	}

	private static UnityEngine.Object FindObjectFromInstanceID(int instanceId)
	{
		return (UnityEngine.Object)typeof(UnityEngine.Object).GetMethod("FindObjectFromInstanceID", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[1] { instanceId });
	}

	[ConCommand(commandName = "dump_object_info", flags = ConVarFlags.None, helpText = "Prints debug info about the object with the provided instance ID.")]
	private static void CCDumpObjectInfo(ConCommandArgs args)
	{
		int argInt = args.GetArgInt(0);
		UnityEngine.Object @object = FindObjectFromInstanceID(argInt);
		if (!@object)
		{
			throw new Exception($"Object is not valid. objectInstanceId={argInt}");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(@object.name);
		stringBuilder.AppendLine($"  instanceId={@object.GetInstanceID()}");
		stringBuilder.AppendLine("  type=" + @object.GetType().FullName);
		GameObject gameObject = null;
		if (@object is GameObject gameObject2)
		{
			gameObject = gameObject2;
		}
		else if (@object is Component component)
		{
			gameObject = component.gameObject;
		}
		if ((bool)gameObject)
		{
			stringBuilder.Append("  scene=\"" + gameObject.scene.name + "\"");
			stringBuilder.Append("  transformPath=" + Util.BuildPrefabTransformPath(gameObject.transform.root, gameObject.transform, appendCloneSuffix: false, includeRoot: true));
		}
		args.Log(stringBuilder.ToString());
	}
}
