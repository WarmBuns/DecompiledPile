using System;
using System.Runtime.InteropServices;
using RoR2;
using RoR2.Networking;
using UnityEngine.SceneManagement;

public static class FlashWindow
{
	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	private struct FLASHWINFO
	{
		public uint cbSize;

		public IntPtr hwnd;

		public uint dwFlags;

		public uint uCount;

		public uint dwTimeout;
	}

	private static IntPtr myWindow;

	private static readonly IntPtr myProcessId = GetCurrentProcessId();

	public const uint FLASHW_STOP = 0u;

	public const uint FLASHW_CAPTION = 1u;

	public const uint FLASHW_TRAY = 2u;

	public const uint FLASHW_ALL = 3u;

	public const uint FLASHW_TIMER = 4u;

	public const uint FLASHW_TIMERNOFG = 12u;

	private static bool Win2000OrLater => Environment.OSVersion.Version.Major >= 5;

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetCurrentProcessId();

	private static bool GetWindowEnum(IntPtr hWnd, IntPtr lParam)
	{
		GetWindowThreadProcessId(hWnd, out var lpdwProcessId);
		if (lpdwProcessId == myProcessId)
		{
			myWindow = hWnd;
			return false;
		}
		return true;
	}

	private static void UpdateCurrentWindow()
	{
		EnumWindows(GetWindowEnum, IntPtr.Zero);
	}

	private static bool IsCurrentWindowValid()
	{
		GetWindowThreadProcessId(myWindow, out var lpdwProcessId);
		if (lpdwProcessId != myProcessId)
		{
			myWindow = IntPtr.Zero;
		}
		return myWindow != IntPtr.Zero;
	}

	public static IntPtr GetWindowHandle()
	{
		if (!IsCurrentWindowValid())
		{
			UpdateCurrentWindow();
		}
		return myWindow;
	}

	public static bool Flash(IntPtr formHandle)
	{
		if (Win2000OrLater)
		{
			FLASHWINFO pwfi = Create_FLASHWINFO(formHandle, 15u, uint.MaxValue, 0u);
			return FlashWindowEx(ref pwfi);
		}
		return false;
	}

	private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
	{
		FLASHWINFO fLASHWINFO = default(FLASHWINFO);
		fLASHWINFO.cbSize = Convert.ToUInt32(Marshal.SizeOf(fLASHWINFO));
		fLASHWINFO.hwnd = handle;
		fLASHWINFO.dwFlags = flags;
		fLASHWINFO.uCount = count;
		fLASHWINFO.dwTimeout = timeout;
		return fLASHWINFO;
	}

	public static bool Flash(IntPtr formHandle, uint count)
	{
		if (Win2000OrLater)
		{
			FLASHWINFO pwfi = Create_FLASHWINFO(formHandle, 3u, count, 0u);
			return FlashWindowEx(ref pwfi);
		}
		return false;
	}

	public static bool Flash()
	{
		return Flash(GetWindowHandle());
	}

	public static bool Start(IntPtr formHandle)
	{
		if (Win2000OrLater)
		{
			FLASHWINFO pwfi = Create_FLASHWINFO(formHandle, 3u, uint.MaxValue, 0u);
			return FlashWindowEx(ref pwfi);
		}
		return false;
	}

	public static bool Stop(IntPtr formHandle)
	{
		if (Win2000OrLater)
		{
			FLASHWINFO pwfi = Create_FLASHWINFO(formHandle, 0u, uint.MaxValue, 0u);
			return FlashWindowEx(ref pwfi);
		}
		return false;
	}

	[InitDuringStartup]
	private static void Init()
	{
		SceneManager.activeSceneChanged += delegate(Scene previousScene, Scene newScene)
		{
			if (newScene.name == "lobby")
			{
				Flash();
			}
		};
		NetworkManagerSystem.onClientConnectGlobal += delegate
		{
			Flash();
		};
	}
}
