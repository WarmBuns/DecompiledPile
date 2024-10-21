using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SteamAPIValidator;

public static class SteamApiValidator
{
	private const int MAX_PATH_SIZE = 32767;

	[DllImport("kernel32.dll")]
	private static extern IntPtr LoadLibrary(string dllToLoad);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr GetModuleHandle(string lpModuleName);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint GetModuleFileName([In] IntPtr hModule, [Out] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);

	public static bool IsValidSteamApiDll()
	{
		string text = (Environment.Is64BitProcess ? "steam_api64.dll" : "steam_api.dll");
		IntPtr intPtr = GetModuleHandle(text);
		if (intPtr == IntPtr.Zero)
		{
			intPtr = LoadLibrary(text);
		}
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		if (intPtr != IntPtr.Zero)
		{
			StringBuilder stringBuilder = new StringBuilder(32767);
			if (GetModuleFileName(intPtr, stringBuilder, 32767) != 0)
			{
				return CheckIfValveSigned(stringBuilder.ToString());
			}
		}
		return false;
	}

	public static bool IsValidSteamClientDll()
	{
		IntPtr moduleHandle = GetModuleHandle(Environment.Is64BitProcess ? "steamclient64.dll" : "steamclient.dll");
		if (moduleHandle != IntPtr.Zero)
		{
			StringBuilder stringBuilder = new StringBuilder(32767);
			if (GetModuleFileName(moduleHandle, stringBuilder, 32767) != 0)
			{
				return CheckIfValveSigned(stringBuilder.ToString());
			}
		}
		return false;
	}

	private static bool CheckIfValveSigned(string filePath)
	{
		try
		{
			IntPtr phCertStore = IntPtr.Zero;
			IntPtr phMsg = IntPtr.Zero;
			IntPtr ppvContext = IntPtr.Zero;
			if (!WinCrypt.CryptQueryObject(1, Marshal.StringToHGlobalUni(filePath), 16382, 14, 0, out var _, out var pdwContentType, out var _, ref phCertStore, ref phMsg, ref ppvContext))
			{
				return false;
			}
			return pdwContentType == 10;
		}
		catch
		{
			return false;
		}
	}
}
