using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using HG;
using JetBrains.Annotations;
using UnityEngine.Networking;

namespace RoR2;

public class RuleBook : IEquatable<RuleBook>
{
	public struct ChoicesEnumerable : IEnumerable<RuleChoiceDef>, IEnumerable
	{
		public struct Enumerator : IEnumerator<RuleChoiceDef>, IEnumerator, IDisposable
		{
			private int ruleIndex;

			private readonly byte[] localChoiceIndicesArray;

			public RuleChoiceDef Current => RuleCatalog.GetRuleDef(ruleIndex)?.choices[localChoiceIndicesArray[ruleIndex]];

			object IEnumerator.Current => Current;

			public Enumerator(byte[] localChoiceIndicesArray)
			{
				ruleIndex = -1;
				this.localChoiceIndicesArray = localChoiceIndicesArray;
			}

			public bool MoveNext()
			{
				ruleIndex++;
				return ruleIndex < localChoiceIndicesArray.Length;
			}

			public void Reset()
			{
				ruleIndex = -1;
			}

			public void Dispose()
			{
			}
		}

		public readonly RuleBook target;

		public ChoicesEnumerable(RuleBook target)
		{
			this.target = target;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(target.ruleValues);
		}

		IEnumerator<RuleChoiceDef> IEnumerable<RuleChoiceDef>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	private readonly byte[] ruleValues = new byte[RuleCatalog.ruleCount];

	protected static readonly RuleDef startingMoneyRule = RuleCatalog.FindRuleDef("Misc.StartingMoney");

	protected static readonly RuleDef stageOrderRule = RuleCatalog.FindRuleDef("Misc.StageOrder");

	protected static readonly RuleDef keepMoneyBetweenStagesRule = RuleCatalog.FindRuleDef("Misc.KeepMoneyBetweenStages");

	private static byte[] defaultValues;

	public uint startingMoney => (uint)GetRuleChoice(startingMoneyRule).extraData;

	public StageOrder stageOrder => (StageOrder)GetRuleChoice(stageOrderRule).extraData;

	public bool keepMoneyBetweenStages => (bool)GetRuleChoice(keepMoneyBetweenStagesRule).extraData;

	public ChoicesEnumerable choices => new ChoicesEnumerable(this);

	[SystemInitializer(new Type[] { typeof(RuleCatalog) })]
	private static void Init()
	{
		defaultValues = new byte[RuleCatalog.ruleCount];
		for (int i = 0; i < RuleCatalog.ruleCount; i++)
		{
			defaultValues[i] = (byte)RuleCatalog.GetRuleDef(i).defaultChoiceIndex;
		}
		HGXml.Register<RuleBook>(ToXml, FromXml);
	}

	public RuleBook()
	{
		SetToDefaults();
	}

	public void SetToDefaults()
	{
		Array.Copy(defaultValues, 0, ruleValues, 0, ruleValues.Length);
	}

	public void ApplyChoice(RuleChoiceDef choiceDef)
	{
		ruleValues[choiceDef.ruleDef.globalIndex] = (byte)choiceDef.localIndex;
	}

	public bool IsChoiceActive(RuleChoiceDef choiceDef)
	{
		return ruleValues[choiceDef.ruleDef.globalIndex] == choiceDef.localIndex;
	}

	public int GetRuleChoiceIndex(int ruleIndex)
	{
		return ruleValues[ruleIndex];
	}

	public int GetRuleChoiceIndex(RuleDef ruleDef)
	{
		return ruleValues[ruleDef.globalIndex];
	}

	public RuleChoiceDef GetRuleChoice(int ruleIndex)
	{
		return RuleCatalog.GetRuleDef(ruleIndex).choices[ruleValues[ruleIndex]];
	}

	public RuleChoiceDef GetRuleChoice(RuleDef ruleDef)
	{
		return ruleDef.choices[ruleValues[ruleDef.globalIndex]];
	}

	public void Serialize(NetworkWriter writer)
	{
		for (int i = 0; i < ruleValues.Length; i++)
		{
			writer.Write(ruleValues[i]);
		}
	}

	public void Deserialize(NetworkReader reader)
	{
		for (int i = 0; i < ruleValues.Length; i++)
		{
			ruleValues[i] = reader.ReadByte();
		}
	}

	public bool Equals(RuleBook other)
	{
		if (other == null)
		{
			return false;
		}
		return ArrayUtils.SequenceEquals(ruleValues, other.ruleValues);
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as RuleBook);
	}

	public override int GetHashCode()
	{
		int num = 0;
		for (int i = 0; i < ruleValues.Length; i++)
		{
			num += ruleValues[i];
		}
		return num;
	}

	public void Copy([NotNull] RuleBook src)
	{
		if (src == null)
		{
			throw new ArgumentException("Argument cannot be null.", "src");
		}
		Array.Copy(src.ruleValues, ruleValues, ruleValues.Length);
	}

	public DifficultyIndex FindDifficulty()
	{
		for (int i = 0; i < ruleValues.Length; i++)
		{
			RuleChoiceDef ruleChoiceDef = RuleCatalog.GetRuleDef(i).choices[ruleValues[i]];
			if (ruleChoiceDef.difficultyIndex != DifficultyIndex.Invalid)
			{
				return ruleChoiceDef.difficultyIndex;
			}
		}
		return DifficultyIndex.Invalid;
	}

	public ArtifactMask GenerateArtifactMask()
	{
		ArtifactMask result = default(ArtifactMask);
		for (int i = 0; i < ruleValues.Length; i++)
		{
			RuleChoiceDef ruleChoiceDef = RuleCatalog.GetRuleDef(i).choices[ruleValues[i]];
			if (ruleChoiceDef.artifactIndex != ArtifactIndex.None)
			{
				result.AddArtifact(ruleChoiceDef.artifactIndex);
			}
		}
		return result;
	}

	public void GenerateItemMask([NotNull] ItemMask dest)
	{
		dest.Clear();
		for (int i = 0; i < ruleValues.Length; i++)
		{
			RuleChoiceDef ruleChoiceDef = RuleCatalog.GetRuleDef(i).choices[ruleValues[i]];
			if (ruleChoiceDef.itemIndex != ItemIndex.None)
			{
				dest.Add(ruleChoiceDef.itemIndex);
			}
		}
	}

	public void GenerateEquipmentMask([NotNull] EquipmentMask dest)
	{
		dest.Clear();
		for (int i = 0; i < ruleValues.Length; i++)
		{
			RuleChoiceDef ruleChoiceDef = RuleCatalog.GetRuleDef(i).choices[ruleValues[i]];
			if (ruleChoiceDef.equipmentIndex != EquipmentIndex.None)
			{
				dest.Add(ruleChoiceDef.equipmentIndex);
			}
		}
	}

	public static void ToXml(XElement element, RuleBook src)
	{
		byte[] array = src.ruleValues;
		string[] choiceNamesBuffer = new string[array.Length];
		int choiceNamesCount = 0;
		for (int i = 0; i < array.Length; i++)
		{
			RuleDef ruleDef = RuleCatalog.GetRuleDef(i);
			byte b = array[i];
			if ((long)b < (long)ruleDef.choices.Count)
			{
				AddChoice(ruleDef.choices[b].globalName);
			}
		}
		element.Value = string.Join(" ", choiceNamesBuffer, 0, choiceNamesCount);
		void AddChoice(string globalChoiceName)
		{
			choiceNamesBuffer[choiceNamesCount++] = globalChoiceName;
		}
	}

	public static bool FromXml(XElement element, ref RuleBook dest)
	{
		dest.SetToDefaults();
		string[] array = element.Value.Split(' ');
		for (int i = 0; i < array.Length; i++)
		{
			RuleChoiceDef ruleChoiceDef = RuleCatalog.FindChoiceDef(array[i]);
			if (ruleChoiceDef != null)
			{
				dest.ApplyChoice(ruleChoiceDef);
			}
		}
		return true;
	}

	public static void WriteBase64ToStringBuilder(RuleBook src, StringBuilder dest)
	{
		byte[] array = src.ruleValues;
		int inputWriteIndex = 0;
		byte[] inputBytes = new byte[3];
		char[] outputChars = new char[4];
		int bytesPerRuleChoiceGlobalIndex = 1;
		if (RuleCatalog.choiceCount > 256)
		{
			bytesPerRuleChoiceGlobalIndex = 2;
			if (RuleCatalog.choiceCount > 65536)
			{
				bytesPerRuleChoiceGlobalIndex = 3;
				if (RuleCatalog.choiceCount > 16777216)
				{
					bytesPerRuleChoiceGlobalIndex = 4;
				}
			}
		}
		PushByte((byte)bytesPerRuleChoiceGlobalIndex);
		for (int i = 0; i < array.Length; i++)
		{
			byte index = array[i];
			RuleChoiceDef ruleChoiceDef = RuleCatalog.GetRuleDef(i).choices[index];
			if (!ruleChoiceDef.isDefaultChoice)
			{
				PushRuleChoiceGlobalIndex((ushort)ruleChoiceDef.globalIndex);
			}
		}
		PushCurrentInputToDest();
		void PushByte(byte value)
		{
			inputBytes[inputWriteIndex++] = value;
			if (inputWriteIndex == 3)
			{
				PushCurrentInputToDest();
			}
		}
		void PushCurrentInputToDest()
		{
			if (inputWriteIndex != 0)
			{
				System.Convert.ToBase64CharArray(inputBytes, 0, inputWriteIndex, outputChars, 0);
				dest.Append(outputChars);
				inputWriteIndex = 0;
			}
		}
		void PushRuleChoiceGlobalIndex(int id)
		{
			for (int j = 0; j < bytesPerRuleChoiceGlobalIndex; j++)
			{
				int num = (bytesPerRuleChoiceGlobalIndex - 1 - j) * 8;
				PushByte((byte)(id >> num));
			}
		}
	}

	public static void ReadBase64(string src, RuleBook dest)
	{
		dest.SetToDefaults();
		byte[] array = System.Convert.FromBase64String(src);
		if (array.Length == 0)
		{
			return;
		}
		int num = array[0];
		if (num > 4)
		{
			return;
		}
		for (int i = 1; i < array.Length; i += num)
		{
			int num2 = 0;
			for (int j = 0; j < num; j++)
			{
				num2 <<= 8;
				num2 |= array[i + j];
			}
			RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(num2);
			if (choiceDef != null)
			{
				dest.ApplyChoice(choiceDef);
			}
		}
	}
}
