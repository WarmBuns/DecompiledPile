using System.Linq;
using System.Xml;
using System.Xml.Linq;
using RoR2.Stats;

namespace RoR2;

public static class XmlUtility
{
	private static XElement CreateStringField(string name, string value)
	{
		return new XElement(name, value);
	}

	private static XElement CreateUintField(string name, uint value)
	{
		return new XElement(name, TextSerialization.ToStringInvariant(value));
	}

	private static XElement CreateStatsField(string name, StatSheet statSheet)
	{
		XElement xElement = new XElement(name);
		for (int i = 0; i < statSheet.fields.Length; i++)
		{
			XElement xElement2 = new XElement("stat", new XText(statSheet.fields[i].ToString()));
			xElement2.SetAttributeValue("name", statSheet.fields[i].name);
			xElement.Add(xElement2);
		}
		int unlockableCount = statSheet.GetUnlockableCount();
		for (int j = 0; j < unlockableCount; j++)
		{
			UnlockableDef unlockable = statSheet.GetUnlockable(j);
			XElement content = new XElement("unlock", new XText(unlockable.cachedName));
			xElement.Add(content);
		}
		return xElement;
	}

	private static XElement CreateLoadoutField(string name, Loadout loadout)
	{
		return loadout.ToXml(name);
	}

	private static uint GetUintField(XElement container, string fieldName, uint defaultValue)
	{
		XElement xElement = container.Element(fieldName);
		if (xElement != null)
		{
			XNode firstNode = xElement.FirstNode;
			if (firstNode != null && firstNode.NodeType == XmlNodeType.Text)
			{
				if (!TextSerialization.TryParseInvariant(((XText)firstNode).Value, out uint result))
				{
					return defaultValue;
				}
				return result;
			}
		}
		return defaultValue;
	}

	private static string GetStringField(XElement container, string fieldName, string defaultValue)
	{
		XElement xElement = container.Element(fieldName);
		if (xElement != null)
		{
			XNode firstNode = xElement.FirstNode;
			if (firstNode != null && firstNode.NodeType == XmlNodeType.Text)
			{
				return ((XText)firstNode).Value;
			}
		}
		return defaultValue;
	}

	private static void GetStatsField(XElement container, string fieldName, StatSheet dest)
	{
		XElement xElement = container.Element(fieldName);
		if (xElement == null)
		{
			return;
		}
		foreach (XElement item in from element in xElement.Elements()
			where element.Name == "stat"
			select element)
		{
			string statName = item.Attributes().FirstOrDefault((XAttribute attribute) => attribute.Name == "name")?.Value;
			string value = (item.Nodes().FirstOrDefault((XNode node) => node.NodeType == XmlNodeType.Text) as XText)?.Value;
			dest.SetStatValueFromString(StatDef.Find(statName), value);
		}
		foreach (XElement item2 in from element in xElement.Elements()
			where element.Name == "unlock"
			select element)
		{
			UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef((item2.Nodes().FirstOrDefault((XNode node) => node.NodeType == XmlNodeType.Text) as XText)?.Value);
			if (unlockableDef != null)
			{
				dest.AddUnlockable(unlockableDef);
			}
		}
	}

	private static void GetLoadoutField(XElement container, string fieldName, Loadout dest)
	{
		XElement xElement = container.Element(fieldName);
		if (xElement != null)
		{
			Loadout loadout = new Loadout();
			if (loadout.FromXml(xElement))
			{
				loadout.Copy(dest);
			}
		}
	}

	public static XDocument ToXml(UserProfile userProfile)
	{
		object[] array = new object[UserProfile.saveFields.Length];
		for (int i = 0; i < UserProfile.saveFields.Length; i++)
		{
			SaveFieldAttribute saveFieldAttribute = UserProfile.saveFields[i];
			array[i] = CreateStringField(saveFieldAttribute.fieldName, saveFieldAttribute.getter(userProfile));
		}
		object[] element = new object[5]
		{
			CreateStatsField("stats", userProfile.statSheet),
			CreateUintField("tutorialDifficulty", userProfile.tutorialDifficulty.showCount),
			CreateUintField("tutorialEquipment", userProfile.tutorialEquipment.showCount),
			CreateUintField("tutorialSprint", userProfile.tutorialSprint.showCount),
			CreateLoadoutField("loadout", userProfile.loadout)
		};
		return new XDocument(new XElement("UserProfile", array.Append(element).ToArray()));
	}

	public static UserProfile FromXml(XDocument doc)
	{
		UserProfile userProfile = new UserProfile();
		XElement root = doc.Root;
		SaveFieldAttribute[] saveFields = UserProfile.saveFields;
		foreach (SaveFieldAttribute saveFieldAttribute in saveFields)
		{
			string stringField = GetStringField(root, saveFieldAttribute.fieldName, null);
			if (stringField != null)
			{
				saveFieldAttribute.setter(userProfile, stringField);
			}
		}
		GetStatsField(root, "stats", userProfile.statSheet);
		GetLoadoutField(root, "loadout", userProfile.loadout);
		userProfile.tutorialDifficulty.showCount = GetUintField(root, "tutorialDifficulty", userProfile.tutorialDifficulty.showCount);
		userProfile.tutorialEquipment.showCount = GetUintField(root, "tutorialEquipment", userProfile.tutorialEquipment.showCount);
		userProfile.tutorialSprint.showCount = GetUintField(root, "tutorialSprint", userProfile.tutorialSprint.showCount);
		return userProfile;
	}
}
