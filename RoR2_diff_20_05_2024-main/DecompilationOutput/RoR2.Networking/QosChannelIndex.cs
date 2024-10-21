namespace RoR2.Networking;

public struct QosChannelIndex
{
	public int intVal;

	public static QosChannelIndex defaultReliable = new QosChannelIndex
	{
		intVal = 0
	};

	public static QosChannelIndex defaultUnreliable = new QosChannelIndex
	{
		intVal = 1
	};

	public static QosChannelIndex characterTransformUnreliable = new QosChannelIndex
	{
		intVal = 2
	};

	public static QosChannelIndex time = new QosChannelIndex
	{
		intVal = 3
	};

	public static QosChannelIndex chat = new QosChannelIndex
	{
		intVal = 4
	};

	public const int viewAnglesChannel = 5;

	public static QosChannelIndex viewAngles = new QosChannelIndex
	{
		intVal = 5
	};

	public static QosChannelIndex ping = new QosChannelIndex
	{
		intVal = 6
	};

	public static QosChannelIndex effects = new QosChannelIndex
	{
		intVal = 7
	};
}
