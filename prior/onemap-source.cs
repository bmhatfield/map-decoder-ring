using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Splatform;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: AssemblyTitle("One Map To Rule Them All")]
[assembly: AssemblyDescription("Valheim One Map To Rule Them All Mod by DrummerCraig")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("One Map To Rule Them All")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: Guid("6A5B7C8D-E9F0-1234-5678-9ABCDEF01234")]
[assembly: AssemblyFileVersion("1.2.1")]
[assembly: AssemblyVersion("1.2.1.0")]
namespace OneMapToRuleThemAll;

public class SharedPin
{
	public string Name;

	public PinType Type;

	public Vector3 Pos;

	public bool Checked;

	public string OwnerId = "";
}
public static class MapStateRepository
{
	public const int MapSize = 2048;

	public const int MapSizeSquared = 4194304;

	public const string DiscoveryOwnerId = "auto";

	private static List<SharedPin> ServerPins = new List<SharedPin>();

	public static List<SharedPin> ClientPins = new List<SharedPin>();

	public static bool[] Explored;

	public static bool InitialPinsReceived = false;

	public static ZPackage Default()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(1);
		val.Write(2048);
		for (int i = 0; i < 4194304; i++)
		{
			val.Write(false);
		}
		val.Write(0);
		val.SetPos(0);
		return val;
	}

	public static ZPackage GetMapData()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected I4, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(2);
		val.Write(2048);
		byte b = 0;
		int num = 0;
		for (int i = 0; i < Explored.Length; i++)
		{
			if (Explored[i])
			{
				b |= (byte)(1 << num);
			}
			num++;
			if (num >= 8)
			{
				val.Write(b);
				b = 0;
				num = 0;
			}
		}
		if (num > 0)
		{
			val.Write(b);
		}
		val.Write(ServerPins.Count);
		foreach (SharedPin serverPin in ServerPins)
		{
			val.Write(serverPin.Name);
			val.Write(serverPin.Pos);
			val.Write((int)serverPin.Type);
			val.Write(serverPin.Checked);
			val.Write(serverPin.OwnerId);
		}
		return val;
	}

	public static void SetMapData(ZPackage mapData)
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		ServerPins.Clear();
		mapData.SetPos(0);
		int num = mapData.ReadInt();
		int num2 = mapData.ReadInt();
		bool[] array = new bool[num2 * num2];
		if (num >= 2)
		{
			for (int i = 0; i < num2 * num2; i += 8)
			{
				byte b = mapData.ReadByte();
				for (int j = 0; j < 8 && i + j < array.Length; j++)
				{
					array[i + j] = (b & (1 << j)) != 0;
				}
			}
		}
		else
		{
			for (int k = 0; k < num2 * num2; k++)
			{
				array[k] = mapData.ReadBool();
			}
		}
		int num3 = mapData.ReadInt();
		for (int l = 0; l < num3; l++)
		{
			ServerPins.Add(new SharedPin
			{
				Name = mapData.ReadString(),
				Pos = mapData.ReadVector3(),
				Type = (PinType)mapData.ReadInt(),
				Checked = mapData.ReadBool(),
				OwnerId = mapData.ReadString()
			});
		}
		Explored = array;
	}

	public static void SetExplored(int x, int y)
	{
		Explored[y * 2048 + x] = true;
		MapFilePersistence.MapDirty = true;
	}

	public static bool[] GetExplorationArray()
	{
		return Explored;
	}

	public static void MergeExplorationArray(bool[] arr, int startIndex, int size)
	{
		for (int i = 0; i < size; i++)
		{
			Explored[startIndex + i] = arr[i] || Explored[startIndex + i];
		}
		MapFilePersistence.MapDirty = true;
	}

	public static ZPackage PackBoolArray(bool[] arr, int chunkId, int startIndex, int size)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(chunkId);
		byte b = 0;
		int num = 0;
		for (int i = startIndex; i < startIndex + size; i++)
		{
			if (arr[i])
			{
				b |= (byte)(1 << num);
			}
			num++;
			if (num >= 8)
			{
				val.Write(b);
				b = 0;
				num = 0;
			}
		}
		if (num > 0)
		{
			val.Write(b);
		}
		return val;
	}

	public static bool[] UnpackBoolArray(ZPackage z, int length)
	{
		bool[] array = new bool[length];
		for (int i = 0; i < length; i += 8)
		{
			byte b = z.ReadByte();
			array[i] = (b & 1) != 0;
			array[i + 1] = (b & 2) != 0;
			array[i + 2] = (b & 4) != 0;
			array[i + 3] = (b & 8) != 0;
			array[i + 4] = (b & 0x10) != 0;
			array[i + 5] = (b & 0x20) != 0;
			array[i + 6] = (b & 0x40) != 0;
			array[i + 7] = (b & 0x80) != 0;
		}
		return array;
	}

	public static ZPackage PackPin(SharedPin pin, bool skipSetPos = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected I4, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(pin.Name);
		val.Write(pin.Pos);
		val.Write((int)pin.Type);
		val.Write(pin.Checked);
		val.Write(pin.OwnerId);
		if (!skipSetPos)
		{
			val.SetPos(0);
		}
		return val;
	}

	public static SharedPin UnpackPin(ZPackage z)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		return new SharedPin
		{
			Name = z.ReadString(),
			Pos = z.ReadVector3(),
			Type = (PinType)z.ReadInt(),
			Checked = z.ReadBool(),
			OwnerId = z.ReadString()
		};
	}

	public static ZPackage PackPins(List<SharedPin> pins)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(pins.Count);
		foreach (SharedPin pin in pins)
		{
			val.Write(pin.Name);
			val.Write(pin.Pos);
			val.Write((int)pin.Type);
			val.Write(pin.Checked);
			val.Write(pin.OwnerId);
		}
		val.SetPos(0);
		return val;
	}

	public static List<SharedPin> UnpackPins(ZPackage z)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		List<SharedPin> list = new List<SharedPin>();
		int num = z.ReadInt();
		for (int i = 0; i < num; i++)
		{
			list.Add(new SharedPin
			{
				Name = z.ReadString(),
				Pos = z.ReadVector3(),
				Type = (PinType)z.ReadInt(),
				Checked = z.ReadBool(),
				OwnerId = z.ReadString()
			});
		}
		return list;
	}

	public static List<SharedPin> GetPins()
	{
		return ServerPins;
	}

	public static void AddPin(SharedPin pin)
	{
		ServerPins.Add(pin);
		MapFilePersistence.MapDirty = true;
	}

	public static void RemovePin(SharedPin needle)
	{
		ServerPins.RemoveAll((SharedPin p) => ArePinsEqual(p, needle));
		MapFilePersistence.MapDirty = true;
	}

	public static void SetPinState(SharedPin needle, bool state)
	{
		foreach (SharedPin serverPin in ServerPins)
		{
			if (ArePinsEqual(serverPin, needle))
			{
				serverPin.Checked = state;
			}
		}
		MapFilePersistence.MapDirty = true;
	}

	public static bool ArePinsEqual(SharedPin a, SharedPin b)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (a.Name == b.Name && a.Type == b.Type)
		{
			return ((Vector3)(ref a.Pos)).Equals(b.Pos);
		}
		return false;
	}

	public static bool ArePinsEqual(SharedPin a, PinData b)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (a.Name == b.m_name && a.Type == b.m_type)
		{
			return Utils.DistanceXZ(a.Pos, b.m_pos) < 1f;
		}
		return false;
	}
}
[HarmonyPatch]
public static class ZNetProxy
{
	[HarmonyPatch(typeof(ZNet), "Awake")]
	private static class ZNetAwake
	{
		private static void Postfix(ZNet __instance)
		{
			ZNetInstance = __instance;
			ModSettings.ModActive = true;
			if ((Object)(object)MinimapProxy.Instance != (Object)null && IsServer(__instance))
			{
				MapPinSynchronizer.SendPinsToClient(null);
			}
		}
	}

	public static ZNet ZNetInstance;

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(ZNet), "IsServer")]
	public static bool IsServer(ZNet instance)
	{
		throw new NotImplementedException();
	}

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(ZNet), "IsDedicated")]
	public static bool IsDedicated(ZNet instance)
	{
		throw new NotImplementedException();
	}

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(ZNet), "GetServerRPC")]
	public static ZRpc GetServerRPC(ZNet instance)
	{
		throw new NotImplementedException();
	}
}
[HarmonyPatch]
public static class MinimapProxy
{
	[HarmonyPatch(typeof(Minimap), "Awake")]
	private static class MinimapAwake
	{
		private static void Postfix(Minimap __instance)
		{
			Instance = __instance;
			object value = Traverse.Create((object)__instance).Field("m_fogTexture").GetValue();
			FogTexture = (Texture2D)((value is Texture2D) ? value : null);
		}
	}

	public static Minimap Instance;

	public static Texture2D FogTexture;

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(Minimap), "Explore", new Type[]
	{
		typeof(int),
		typeof(int)
	})]
	public static bool Explore(Minimap instance, int x, int y)
	{
		throw new NotImplementedException();
	}

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(Minimap), "AddPin", new Type[]
	{
		typeof(Vector3),
		typeof(PinType),
		typeof(string),
		typeof(bool),
		typeof(bool),
		typeof(long),
		typeof(PlatformUserID)
	})]
	public static PinData AddPin(Minimap instance, Vector3 pos, PinType type, string name, bool save, bool isChecked, long owner, PlatformUserID author)
	{
		throw new NotImplementedException();
	}

	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPatch(typeof(Minimap), "RemovePin", new Type[] { typeof(PinData) })]
	public static void RemovePin(Minimap instance, PinData pin)
	{
		throw new NotImplementedException();
	}
}
public static class ExplorationSynchronizer
{
	[HarmonyPatch(typeof(Minimap), "Explore", new Type[]
	{
		typeof(Vector3),
		typeof(float)
	})]
	private class MinimapPatchExploreInterval
	{
		private static void Postfix(Minimap __instance)
		{
			if (_fogDirty)
			{
				_fogDirty = false;
				object value = Traverse.Create((object)__instance).Field("m_fogTexture").GetValue();
				object obj = ((value is Texture2D) ? value : null);
				if (obj != null)
				{
					((Texture2D)obj).Apply();
				}
			}
			if (_pendingCells.Count == 0)
			{
				return;
			}
			if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
			{
				foreach (var (x, y) in _pendingCells)
				{
					OnClientExplore(null, x, y);
				}
			}
			else
			{
				FlushBatchToServer(_pendingCells);
			}
			_pendingCells.Clear();
		}
	}

	[HarmonyPatch(typeof(Minimap), "Explore", new Type[]
	{
		typeof(int),
		typeof(int)
	})]
	private class MinimapPatchExplore
	{
		private static void Postfix(int x, int y, bool __result)
		{
			if (__result && !_blockExplore && ModSettings.ModActive)
			{
				_pendingCells.Add((x, y));
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "SetMapData", new Type[] { typeof(byte[]) })]
	private class MinimapPatchSetMapData
	{
		private static void Prefix()
		{
			_blockExplore = true;
		}

		private static void Postfix()
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			_blockExplore = false;
			if (!ModSettings.ModActive)
			{
				return;
			}
			if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
			{
				SendChunkToClient(null, 0);
				return;
			}
			ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
			if (serverRPC != null)
			{
				serverRPC.Invoke("OM_ClientRequestInitialMap", new object[1] { (object)new ZPackage() });
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "ExploreAll")]
	private class MinimapPatchExploreAll
	{
		private static void Prefix()
		{
			_blockExplore = true;
		}

		private static void Postfix(Minimap __instance)
		{
			_pendingCells.Clear();
			if (ModSettings.ModActive && (Object)(object)ZNetProxy.ZNetInstance != (Object)null)
			{
				if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
				{
					if (Traverse.Create((object)__instance).Field("m_explored").GetValue() is bool[] array)
					{
						MapStateRepository.Explored = (bool[])array.Clone();
						MapFilePersistence.MapDirty = true;
					}
					if (Traverse.Create((object)ZNetProxy.ZNetInstance).Field("m_peers").GetValue() is List<ZNetPeer> list)
					{
						foreach (ZNetPeer item in list)
						{
							if (item.IsReady())
							{
								SendChunkToClient(item.m_rpc, 0);
							}
						}
					}
				}
				else
				{
					((MonoBehaviour)ZNetProxy.ZNetInstance).StartCoroutine(SendChunkToServer(ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance), 0));
				}
			}
			_blockExplore = false;
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	private class ZNetPatchRPCPeerInfo
	{
		private static void Postfix(ZRpc rpc, ZNet __instance)
		{
			if (ModSettings.ModActive && __instance.IsServer())
			{
				SendChunkToClient(rpc, 0);
				rpc.Invoke("OM_ServerDiscoveryConfig", new object[1] { AutoPinConfig.PackDiscoveryConfig() });
				rpc.Invoke("OM_ServerRadarConfig", new object[1] { RadarConfig.PackRadarConfig() });
			}
		}
	}

	private static bool _blockExplore = false;

	private static bool _fogDirty = false;

	private const int Chunks = 64;

	private static readonly List<(int x, int y)> _pendingCells = new List<(int, int)>(64);

	public static void OnClientExplore(ZRpc client, int x, int y)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		if (!ModSettings.ModActive)
		{
			return;
		}
		MapStateRepository.SetExplored(x, y);
		List<ZNetPeer> obj = Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>;
		int num = 0;
		foreach (ZNetPeer item in obj)
		{
			if (item.IsReady() && item.m_rpc != client)
			{
				ZPackage val = new ZPackage();
				val.Write(x);
				val.Write(y);
				item.m_rpc.Invoke("OM_ServerMapData", new object[1] { val });
				num++;
			}
		}
		if ((Object)(object)MinimapProxy.Instance != (Object)null)
		{
			MinimapProxy.Explore(MinimapProxy.Instance, x, y);
			_fogDirty = true;
		}
	}

	public static void OnClientRequestInitialMap(ZRpc client, ZPackage data)
	{
		if (ModSettings.ModActive)
		{
			SendChunkToClient(client, 0);
		}
	}

	public static void OnClientInitialMap(ZRpc client, ZPackage data)
	{
		if (ModSettings.ModActive)
		{
			data.SetPos(0);
			int num = data.ReadInt();
			int num2 = 65536;
			int startIndex = num * num2;
			MapStateRepository.MergeExplorationArray(MapStateRepository.UnpackBoolArray(data, num2), startIndex, num2);
			SendChunkToClient(client, num + 1);
		}
	}

	public static void OnServerMapData(ZRpc rpc, ZPackage data)
	{
		if (ModSettings.ModActive)
		{
			data.SetPos(0);
			int x = data.ReadInt();
			int y = data.ReadInt();
			if (!((Object)(object)MinimapProxy.Instance == (Object)null))
			{
				MinimapProxy.Explore(MinimapProxy.Instance, x, y);
				_fogDirty = true;
			}
		}
	}

	public static void OnServerMapInitial(ZRpc rpc, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		int num = data.ReadInt();
		int num2 = 65536;
		int num3 = num * num2;
		bool[] array = MapStateRepository.UnpackBoolArray(data, num2);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i])
			{
				MinimapProxy.Explore(MinimapProxy.Instance, (num3 + i) % 2048, (num3 + i) / 2048);
			}
		}
		Texture2D fogTexture = MinimapProxy.FogTexture;
		if (fogTexture != null)
		{
			fogTexture.Apply();
		}
		((MonoBehaviour)ZNetProxy.ZNetInstance).StartCoroutine(SendChunkToServer(rpc, num));
	}

	private static void FlushBatchToServer(List<(int x, int y)> cells)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
		if (serverRPC == null)
		{
			return;
		}
		ZPackage val = new ZPackage();
		val.Write(cells.Count);
		foreach (var (num, num2) in cells)
		{
			val.Write(num);
			val.Write(num2);
		}
		serverRPC.Invoke("OM_ClientExploreBatch", new object[1] { val });
	}

	public static void OnClientExploreBatch(ZRpc client, ZPackage data)
	{
		if (ModSettings.ModActive)
		{
			data.SetPos(0);
			int num = data.ReadInt();
			for (int i = 0; i < num; i++)
			{
				int x = data.ReadInt();
				int y = data.ReadInt();
				OnClientExplore(client, x, y);
			}
		}
	}

	private static void SendChunkToClient(ZRpc client, int chunk)
	{
		if (chunk < 64)
		{
			int num = 65536;
			int startIndex = chunk * num;
			ZPackage val = MapStateRepository.PackBoolArray(MapStateRepository.GetExplorationArray(), chunk, startIndex, num);
			if (client == null)
			{
				OnServerMapInitial(null, val);
				return;
			}
			client.Invoke("OM_ServerMapInitial", new object[1] { val });
		}
	}

	private static IEnumerator SendChunkToServer(ZRpc serverRpc, int chunk)
	{
		if (chunk >= 64)
		{
			yield break;
		}
		int num = 65536;
		int startIndex = chunk * num;
		bool[] arr = Traverse.Create((object)MinimapProxy.Instance).Field("m_explored").GetValue() as bool[];
		ZPackage z = MapStateRepository.PackBoolArray(arr, chunk, startIndex, num);
		if (serverRpc == null)
		{
			OnClientInitialMap(null, z);
			yield break;
		}
		yield return (object)new WaitUntil((Func<bool>)(() => ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance) != null));
		ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance).Invoke("OM_ClientInitialMap", new object[1] { z });
	}
}
public static class MapPinSynchronizer
{
	[HarmonyPatch(typeof(Minimap), "OnPinTextEntered")]
	private class MinimapPatchOnPinTextEntered
	{
		private static void Prefix(out PinData __state, PinData ___m_namePin)
		{
			__state = ___m_namePin;
		}

		private static void Postfix(PinData __state)
		{
			if (__state != null)
			{
				SendPinToServer(__state);
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "OnMapRightClick")]
	private class MinimapPatchOnMapRightClick
	{
		private static void Postfix()
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			if (LatestClosestPin == null)
			{
				return;
			}
			SharedPin clientPin = GetClientPin(LatestClosestPin);
			if (clientPin == null)
			{
				return;
			}
			if (clientPin.OwnerId == "auto")
			{
				if ((Object)(object)MinimapProxy.Instance != (Object)null)
				{
					string name = (AutoPinConfig.DiscoveryPinText.Value ? clientPin.Name : "");
					string keywordForLabel = AutoPinConfig.GetKeywordForLabel(clientPin.Name);
					PinData mmPin = MinimapProxy.AddPin(MinimapProxy.Instance, clientPin.Pos, clientPin.Type, name, save: false, clientPin.Checked, 0L, new PlatformUserID(""));
					if (keywordForLabel != null)
					{
						AutoPinConfig.ApplyDiscoveryIcon(mmPin, keywordForLabel);
					}
				}
			}
			else
			{
				RemovePinFromServer(clientPin);
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "OnMapLeftClick")]
	private class MinimapPatchOnMapLeftClick
	{
		private static void Postfix()
		{
			if (LatestClosestPin != null)
			{
				SharedPin clientPin = GetClientPin(LatestClosestPin);
				if (clientPin != null)
				{
					clientPin.Checked = LatestClosestPin.m_checked;
					CheckPinOnServer(clientPin, clientPin.Checked);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "GetClosestPin", new Type[]
	{
		typeof(Vector3),
		typeof(float),
		typeof(bool)
	})]
	private class MinimapPatchGetClosestPin
	{
		private static void Postfix(ref PinData __result, Vector3 pos, float radius)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			if (!ModSettings.ModActive)
			{
				return;
			}
			LatestClosestPin = __result;
			SharedPin sharedPin = null;
			float num = 999999f;
			foreach (SharedPin clientPin in MapStateRepository.ClientPins)
			{
				float num2 = Utils.DistanceXZ(pos, clientPin.Pos);
				if (num2 < radius && (sharedPin == null || num2 < num))
				{
					sharedPin = clientPin;
					num = num2;
				}
			}
			if (sharedPin != null)
			{
				PinData mapPin = GetMapPin(sharedPin);
				if (mapPin != null && (__result == null || Utils.DistanceXZ(pos, __result.m_pos) > num))
				{
					__result = mapPin;
					LatestClosestPin = mapPin;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Minimap), "ClearPins")]
	private class MinimapPatchClearPins
	{
		private static void Postfix()
		{
			_cachedMmPins = null;
			AddPinsAfterClear();
		}
	}

	[HarmonyPatch(typeof(Minimap), "UpdatePins")]
	private class MinimapPatchUpdateDiscoveryIcons
	{
		private static void Postfix(Minimap __instance)
		{
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			if (!AutoPinConfig.DiscoveryPinObjectIcon.Value || _iconLookup.Count == 0)
			{
				return;
			}
			_iconTimer += Time.deltaTime;
			if (_iconTimer < ModSettings.ActiveDiscoveryIconInterval)
			{
				return;
			}
			_iconTimer = 0f;
			if (_cachedMmPins == null)
			{
				_cachedMmPins = Traverse.Create((object)__instance).Field("m_pins").GetValue() as List<PinData>;
			}
			if (_cachedMmPins == null || _cachedMmPins.Count == 0)
			{
				return;
			}
			foreach (PinData cachedMmPin in _cachedMmPins)
			{
				if (_iconLookup.TryGetValue((cachedMmPin.m_name, cachedMmPin.m_type), out var value))
				{
					cachedMmPin.m_icon = value;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	private class ZNetPatchRPCPeerInfo
	{
		private static void Postfix(ZRpc rpc, ZNet __instance)
		{
			if (ModSettings.ModActive && __instance.IsServer())
			{
				SendPinsToClient(rpc);
				rpc.Invoke("OM_ServerDiscoveryConfig", new object[1] { AutoPinConfig.PackDiscoveryConfig() });
				rpc.Invoke("OM_ServerRadarConfig", new object[1] { RadarConfig.PackRadarConfig() });
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private class ZNetPatchShutdown
	{
		private static void Postfix()
		{
			_cachedMmPins = null;
			MapStateRepository.ClientPins.Clear();
			AutoPinPlacer.InvalidatePinIndex();
			MapStateRepository.InitialPinsReceived = false;
			AutoPinConfig.ResetToLocalConfig();
			RadarConfig.ResetToLocalConfig();
			RadarClusterManager.Clear();
		}
	}

	[HarmonyPatch(typeof(Minimap), "SetMapData", new Type[] { typeof(byte[]) })]
	private class MinimapPatchSetMapData
	{
		private static void Postfix()
		{
			_cachedMmPins = null;
			if (ModSettings.ModActive && !((Object)(object)ZNetProxy.ZNetInstance == (Object)null) && ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
			{
				SendPinsToClient(null);
			}
		}
	}

	private static PinData LatestClosestPin = null;

	private static readonly Dictionary<(string name, PinType type), Sprite> _iconLookup = new Dictionary<(string, PinType), Sprite>();

	private static float _iconTimer = 0f;

	private static List<PinData> _cachedMmPins = null;

	private static void RebuildIconLookup()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		_iconLookup.Clear();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (clientPin.OwnerId != "auto")
			{
				continue;
			}
			string keywordForLabel = AutoPinConfig.GetKeywordForLabel(clientPin.Name);
			if (keywordForLabel != null && RadarIconLoader.TryGetIcon(keywordForLabel, out var sprite))
			{
				(string, PinType) key = (clientPin.Name, clientPin.Type);
				if (!_iconLookup.ContainsKey(key))
				{
					_iconLookup[key] = sprite;
				}
			}
		}
		AutoPinPlacer.InvalidatePinIndex();
	}

	public static void OnClientAddPin(ZRpc client, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin pin = MapStateRepository.UnpackPin(data);
		MapStateRepository.AddPin(pin);
		foreach (ZNetPeer item in Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>)
		{
			if (item.IsReady() && item.m_rpc != client)
			{
				item.m_rpc.Invoke("OM_ServerAddPin", new object[1] { MapStateRepository.PackPin(pin) });
			}
		}
		if (client != null && (Object)(object)MinimapProxy.Instance != (Object)null)
		{
			OnServerAddPin(null, MapStateRepository.PackPin(pin));
		}
	}

	public static void OnClientRemovePin(ZRpc client, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin sharedPin = MapStateRepository.UnpackPin(data);
		if (client != null && !ClientCanRemovePin(client, sharedPin))
		{
			return;
		}
		MapStateRepository.RemovePin(sharedPin);
		List<ZNetPeer> obj = Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>;
		bool flag = sharedPin.OwnerId == "auto";
		foreach (ZNetPeer item in obj)
		{
			if (item.IsReady() && (flag || item.m_rpc != client))
			{
				item.m_rpc.Invoke("OM_ServerRemovePin", new object[1] { MapStateRepository.PackPin(sharedPin) });
			}
		}
		if (flag || client != null)
		{
			OnServerRemovePin(null, MapStateRepository.PackPin(sharedPin));
		}
	}

	public static void OnClientCheckPin(ZRpc client, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin sharedPin = MapStateRepository.UnpackPin(data);
		bool flag = data.ReadBool();
		MapStateRepository.SetPinState(sharedPin, flag);
		foreach (ZNetPeer item in Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>)
		{
			if (item.IsReady() && item.m_rpc != client)
			{
				ZPackage val = MapStateRepository.PackPin(sharedPin, skipSetPos: true);
				val.Write(flag);
				item.m_rpc.Invoke("OM_ServerCheckPin", new object[1] { val });
			}
		}
		if (client != null)
		{
			ZPackage val2 = MapStateRepository.PackPin(sharedPin, skipSetPos: true);
			val2.Write(flag);
			OnServerCheckPin(null, val2);
		}
	}

	private static bool ClientCanRemovePin(ZRpc client, SharedPin pin)
	{
		if (pin.OwnerId == "auto")
		{
			return false;
		}
		if (AdminCommands.IsAdmin(client))
		{
			return true;
		}
		ZNetPeer val = (Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>)?.FirstOrDefault((Func<ZNetPeer, bool>)((ZNetPeer p) => p.m_rpc == client));
		if (val == null)
		{
			return false;
		}
		string text = val.m_uid.ToString();
		if (!(pin.OwnerId == text))
		{
			return pin.OwnerId == string.Empty;
		}
		return true;
	}

	public static void OnServerInitialPins(ZRpc rpc, ZPackage data)
	{
		if (ModSettings.ModActive)
		{
			data.SetPos(0);
			MapStateRepository.ClientPins = MapStateRepository.UnpackPins(data);
			MapStateRepository.InitialPinsReceived = true;
			AppendPins();
		}
	}

	public static void OnServerAddPin(ZRpc rpc, ZPackage data)
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin sharedPin = MapStateRepository.UnpackPin(data);
		string text = ZNet.GetUID().ToString();
		MapStateRepository.ClientPins.Add(sharedPin);
		RebuildIconLookup();
		string text2 = null;
		if (sharedPin.OwnerId != "auto")
		{
			if (!ModSettings.ShowOtherPlayerPins.Value && sharedPin.OwnerId != "" && sharedPin.OwnerId != text)
			{
				return;
			}
		}
		else
		{
			text2 = AutoPinConfig.GetKeywordForLabel(sharedPin.Name);
			if (text2 != null && !WorldObjectConfig.IsKeywordDiscoveryVisible(text2))
			{
				return;
			}
		}
		string name = ((sharedPin.OwnerId == "auto" && !AutoPinConfig.DiscoveryPinText.Value) ? "" : sharedPin.Name);
		PinData mmPin = MinimapProxy.AddPin(MinimapProxy.Instance, sharedPin.Pos, sharedPin.Type, name, save: false, sharedPin.Checked, 0L, new PlatformUserID(""));
		if (text2 != null)
		{
			AutoPinConfig.ApplyDiscoveryIcon(mmPin, text2);
		}
	}

	public static void OnServerRemovePin(ZRpc rpc, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin pin = MapStateRepository.UnpackPin(data);
		MapStateRepository.ClientPins.RemoveAll((SharedPin p) => MapStateRepository.ArePinsEqual(p, pin));
		RebuildIconLookup();
		if (!((Object)(object)MinimapProxy.Instance == (Object)null) && Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> mmPins)
		{
			PinData mapPin = GetMapPin(pin);
			if (mapPin != null)
			{
				DestroyPinEntry(mmPins, mapPin);
			}
		}
	}

	public static void OnServerCheckPin(ZRpc rpc, ZPackage data)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		data.SetPos(0);
		SharedPin b = MapStateRepository.UnpackPin(data);
		bool @checked = data.ReadBool();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (MapStateRepository.ArePinsEqual(clientPin, b))
			{
				clientPin.Checked = @checked;
				PinData mapPin = GetMapPin(clientPin);
				if (mapPin != null)
				{
					mapPin.m_checked = @checked;
				}
			}
		}
	}

	public static void SendPinToServer(PinData pin, bool isDiscovery = false)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive)
		{
			return;
		}
		SharedPin sharedPin = new SharedPin
		{
			Name = pin.m_name,
			Pos = pin.m_pos,
			Type = pin.m_type,
			Checked = pin.m_checked,
			OwnerId = (isDiscovery ? "auto" : ZNet.GetUID().ToString())
		};
		MapStateRepository.ClientPins.Add(sharedPin);
		AutoPinPlacer.InvalidatePinIndex();
		ZPackage val = MapStateRepository.PackPin(sharedPin);
		if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
		{
			OnClientAddPin(null, val);
			return;
		}
		ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
		if (serverRPC != null)
		{
			serverRPC.Invoke("OM_ClientAddPin", new object[1] { val });
		}
	}

	public static void RemovePinFromServer(SharedPin pin)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		MapStateRepository.ClientPins.Remove(pin);
		AutoPinPlacer.InvalidatePinIndex();
		ZPackage val = MapStateRepository.PackPin(pin);
		if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
		{
			OnClientRemovePin(null, val);
			return;
		}
		ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
		if (serverRPC != null)
		{
			serverRPC.Invoke("OM_ClientRemovePin", new object[1] { val });
		}
	}

	public static void CheckPinOnServer(SharedPin pin, bool state)
	{
		if (!ModSettings.ModActive)
		{
			return;
		}
		ZPackage val = MapStateRepository.PackPin(pin, skipSetPos: true);
		val.Write(state);
		val.SetPos(0);
		if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
		{
			OnClientCheckPin(null, val);
			return;
		}
		ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
		if (serverRPC != null)
		{
			serverRPC.Invoke("OM_ClientCheckPin", new object[1] { val });
		}
	}

	public static void SendPinsToClient(ZRpc client)
	{
		if (ModSettings.ModActive)
		{
			ZPackage val = MapStateRepository.PackPins(MapStateRepository.GetPins());
			if (client == null)
			{
				OnServerInitialPins(null, val);
				return;
			}
			client.Invoke("OM_ServerInitialPins", new object[1] { val });
		}
	}

	public static void AppendPins()
	{
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive || (Object)(object)MinimapProxy.Instance == (Object)null || !(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list))
		{
			return;
		}
		string text = ZNet.GetUID().ToString();
		foreach (SharedPin pin in MapStateRepository.ClientPins)
		{
			PinData val = ((!(pin.OwnerId == "auto")) ? ((IEnumerable<PinData>)list).FirstOrDefault((Func<PinData, bool>)((PinData p) => MapStateRepository.ArePinsEqual(pin, p))) : ((IEnumerable<PinData>)list).FirstOrDefault((Func<PinData, bool>)((PinData p) => p.m_type == pin.Type && Utils.DistanceXZ(pin.Pos, p.m_pos) < 1f)));
			if (val != null)
			{
				DestroyPinEntry(list, val);
			}
			string text2 = null;
			bool flag;
			if (pin.OwnerId == "auto")
			{
				text2 = AutoPinConfig.GetKeywordForLabel(pin.Name);
				flag = text2 == null || WorldObjectConfig.IsKeywordDiscoveryVisible(text2);
			}
			else
			{
				flag = ModSettings.ShowOtherPlayerPins.Value || pin.OwnerId == "" || pin.OwnerId == text;
			}
			if (flag)
			{
				string name = ((pin.OwnerId == "auto" && !AutoPinConfig.DiscoveryPinText.Value) ? "" : pin.Name);
				PinData mmPin = MinimapProxy.AddPin(MinimapProxy.Instance, pin.Pos, pin.Type, name, save: false, pin.Checked, 0L, new PlatformUserID(""));
				if (text2 != null)
				{
					AutoPinConfig.ApplyDiscoveryIcon(mmPin, text2);
				}
			}
		}
		RebuildIconLookup();
	}

	private static void AddPinsAfterClear()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive || (Object)(object)MinimapProxy.Instance == (Object)null)
		{
			return;
		}
		string text = ZNet.GetUID().ToString();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			string text2 = null;
			bool flag;
			if (clientPin.OwnerId == "auto")
			{
				text2 = AutoPinConfig.GetKeywordForLabel(clientPin.Name);
				flag = text2 == null || WorldObjectConfig.IsKeywordDiscoveryVisible(text2);
			}
			else
			{
				flag = ModSettings.ShowOtherPlayerPins.Value || clientPin.OwnerId == "" || clientPin.OwnerId == text;
			}
			if (flag)
			{
				string name = ((clientPin.OwnerId == "auto" && !AutoPinConfig.DiscoveryPinText.Value) ? "" : clientPin.Name);
				PinData mmPin = MinimapProxy.AddPin(MinimapProxy.Instance, clientPin.Pos, clientPin.Type, name, save: false, clientPin.Checked, 0L, new PlatformUserID(""));
				if (text2 != null)
				{
					AutoPinConfig.ApplyDiscoveryIcon(mmPin, text2);
				}
			}
		}
		RebuildIconLookup();
	}

	private static void DestroyPinEntry(List<PinData> mmPins, PinData mmPin)
	{
		FieldInfo[] fields = typeof(PinData).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!(fieldInfo.FieldType != typeof(RectTransform)))
			{
				object? value = fieldInfo.GetValue(mmPin);
				RectTransform val = (RectTransform)((value is RectTransform) ? value : null);
				if ((Object)(object)val != (Object)null)
				{
					Object.Destroy((Object)(object)((Component)val).gameObject);
				}
				break;
			}
		}
		mmPins.Remove(mmPin);
		object value2 = Traverse.Create((object)mmPin).Field("m_NamePinData").GetValue();
		if (value2 != null)
		{
			object value3 = Traverse.Create(value2).Property("PinNameGameObject", (object[])null).GetValue();
			GameObject val2 = (GameObject)((value3 is GameObject) ? value3 : null);
			if ((Object)(object)val2 != (Object)null)
			{
				Object.Destroy((Object)(object)val2);
			}
		}
	}

	private static PinData GetMapPin(SharedPin needle)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (!(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list))
		{
			return null;
		}
		foreach (PinData item in list)
		{
			if (MapStateRepository.ArePinsEqual(needle, item))
			{
				return item;
			}
		}
		if (needle.OwnerId == "auto")
		{
			foreach (PinData item2 in list)
			{
				if (needle.Type == item2.m_type && Utils.DistanceXZ(needle.Pos, item2.m_pos) < 1f)
				{
					return item2;
				}
			}
		}
		return null;
	}

	private static SharedPin GetClientPin(PinData needle)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (MapStateRepository.ArePinsEqual(clientPin, needle))
			{
				return clientPin;
			}
		}
		foreach (SharedPin clientPin2 in MapStateRepository.ClientPins)
		{
			if (clientPin2.OwnerId == "auto" && clientPin2.Type == needle.m_type && Utils.DistanceXZ(clientPin2.Pos, needle.m_pos) < 1f)
			{
				return clientPin2;
			}
		}
		return null;
	}
}
[BepInPlugin("drummercraig.one_map_to_rule_them_all", "One Map To Rule Them All", "1.2.1")]
public class OneMapToRuleThemAllPlugin : BaseUnityPlugin
{
	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	private class ZNetPatchOnNewConnection
	{
		private static void Postfix(ZNetPeer peer, ZNet __instance)
		{
			if (!__instance.IsServer())
			{
				ModSettings.ModActive = true;
			}
			if (ModSettings.ModActive)
			{
				if (__instance.IsServer())
				{
					peer.m_rpc.Register<int, int>("OM_ClientExplore", (Action<ZRpc, int, int>)ExplorationSynchronizer.OnClientExplore);
					peer.m_rpc.Register<ZPackage>("OM_ClientExploreBatch", (Action<ZRpc, ZPackage>)ExplorationSynchronizer.OnClientExploreBatch);
					peer.m_rpc.Register<ZPackage>("OM_ClientInitialMap", (Action<ZRpc, ZPackage>)ExplorationSynchronizer.OnClientInitialMap);
					peer.m_rpc.Register<ZPackage>("OM_ClientRequestInitialMap", (Action<ZRpc, ZPackage>)ExplorationSynchronizer.OnClientRequestInitialMap);
					peer.m_rpc.Register<ZPackage>("OM_ClientAddPin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnClientAddPin);
					peer.m_rpc.Register<ZPackage>("OM_ClientRemovePin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnClientRemovePin);
					peer.m_rpc.Register<ZPackage>("OM_ClientCheckPin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnClientCheckPin);
					peer.m_rpc.Register<ZPackage>("OM_AdminClearArea", (Action<ZRpc, ZPackage>)AdminCommands.OnAdminClearArea);
					peer.m_rpc.Register<ZPackage>("OM_AdminClearAll", (Action<ZRpc, ZPackage>)AdminCommands.OnAdminClearAll);
					peer.m_rpc.Register<ZPackage>("OM_AdminRenameAbbr", (Action<ZRpc, ZPackage>)AdminCommands.OnAdminRenameAbbr);
					peer.m_rpc.Register<ZPackage>("OM_AdminUpdateDiscoveryConfig", (Action<ZRpc, ZPackage>)AdminCommands.OnAdminUpdateDiscoveryConfig);
				}
				else
				{
					peer.m_rpc.Register<ZPackage>("OM_ServerMapData", (Action<ZRpc, ZPackage>)ExplorationSynchronizer.OnServerMapData);
					peer.m_rpc.Register<ZPackage>("OM_ServerMapInitial", (Action<ZRpc, ZPackage>)ExplorationSynchronizer.OnServerMapInitial);
					peer.m_rpc.Register<ZPackage>("OM_ServerInitialPins", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnServerInitialPins);
					peer.m_rpc.Register<ZPackage>("OM_ServerAddPin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnServerAddPin);
					peer.m_rpc.Register<ZPackage>("OM_ServerRemovePin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnServerRemovePin);
					peer.m_rpc.Register<ZPackage>("OM_ServerCheckPin", (Action<ZRpc, ZPackage>)MapPinSynchronizer.OnServerCheckPin);
					peer.m_rpc.Register<ZPackage>("OM_ServerDiscoveryConfig", (Action<ZRpc, ZPackage>)AutoPinConfig.OnServerDiscoveryConfig);
					peer.m_rpc.Register<ZPackage>("OM_ServerRadarConfig", (Action<ZRpc, ZPackage>)RadarConfig.OnServerRadarConfig);
				}
			}
		}
	}

	private void Awake()
	{
		ModSettings.Log = ((BaseUnityPlugin)this).Logger;
		ConfigMigration.MigrateIfNeeded(((BaseUnityPlugin)this).Config);
		try
		{
			ConfigMigration.MigrateFilesToSubfolder();
		}
		catch (Exception ex)
		{
			((BaseUnityPlugin)this).Logger.LogWarning((object)("[ConfigMigration] File migration failed: " + ex.Message));
		}
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), (string)null);
		WorldObjectConfig.PrepareCreatureData();
		ModSettings.ClientResetToDefaults = ((BaseUnityPlugin)this).Config.Bind<bool>("Client", "ResetToDefaults", false, "Set to true to reset ALL client settings to their default values. Resets itself to false automatically.");
		ModSettings.ClientResetToDefaults.SettingChanged += delegate
		{
			if (!ModSettings.ClientResetToDefaults.Value)
			{
				return;
			}
			foreach (ConfigDefinition key in ((BaseUnityPlugin)this).Config.Keys)
			{
				if (key.Section.Equals("Client", StringComparison.OrdinalIgnoreCase) || key.Section.StartsWith("Client.", StringComparison.OrdinalIgnoreCase))
				{
					((BaseUnityPlugin)this).Config[key].BoxedValue = ((BaseUnityPlugin)this).Config[key].DefaultValue;
				}
			}
		};
		ModSettings.ShowOtherPlayerPins = ((BaseUnityPlugin)this).Config.Bind<bool>("Client", "ShowOtherPlayerPins", true, "Show map pins created by other players on your minimap.");
		ModSettings.ExploreFlushInterval = ((BaseUnityPlugin)this).Config.Bind<float>("Client", "ExploreFlushInterval", 1f, "How often (seconds) batched exploration cells are sent to the server. Range: 0.1–5.0.");
		ModSettings.ExploreFlushInterval.SettingChanged += delegate
		{
			ModSettings.ActiveExploreFlushInterval = Mathf.Clamp(ModSettings.ExploreFlushInterval.Value, 0.1f, 5f);
		};
		ModSettings.ActiveExploreFlushInterval = Mathf.Clamp(ModSettings.ExploreFlushInterval.Value, 0.1f, 5f);
		ModSettings.UpdateThrottleInterval = ((BaseUnityPlugin)this).Config.Bind<float>("Client", "UpdateThrottleInterval", 0.25f, "How often (seconds) proximity and radar watchers check for nearby objects. Range: 0.05–1.0.");
		ModSettings.UpdateThrottleInterval.SettingChanged += delegate
		{
			ModSettings.ActiveUpdateThrottleInterval = Mathf.Clamp(ModSettings.UpdateThrottleInterval.Value, 0.05f, 1f);
		};
		ModSettings.ActiveUpdateThrottleInterval = Mathf.Clamp(ModSettings.UpdateThrottleInterval.Value, 0.05f, 1f);
		ModSettings.DiscoveryIconInterval = ((BaseUnityPlugin)this).Config.Bind<float>("Client", "DiscoveryIconInterval", 0.05f, "How often (seconds) discovery pin icons are reapplied after Valheim's per-frame icon reset. 0 = every frame (maximum fidelity). 0.05 = 20 Hz (default, imperceptible). Increase to 0.5–1.0 on low-end hardware. Range: 0–1.0.");
		ModSettings.DiscoveryIconInterval.SettingChanged += delegate
		{
			ModSettings.ActiveDiscoveryIconInterval = Mathf.Clamp(ModSettings.DiscoveryIconInterval.Value, 0f, 1f);
		};
		ModSettings.ActiveDiscoveryIconInterval = Mathf.Clamp(ModSettings.DiscoveryIconInterval.Value, 0f, 1f);
		ModSettings.QuantityRecountInterval = ((BaseUnityPlugin)this).Config.Bind<float>("Client", "QuantityRecountInterval", 10f, "Minimum time (seconds) before re-running the nearby-object Physics query for an already-discovered pin. Prevents redundant OverlapSphere calls when the player oscillates near a detection boundary. Higher values reduce Physics overhead; lower values keep resource counts fresher. Range: 1–120.");
		ModSettings.QuantityRecountInterval.SettingChanged += delegate
		{
			ModSettings.ActiveQuantityRecountInterval = Mathf.Clamp(ModSettings.QuantityRecountInterval.Value, 1f, 120f);
		};
		ModSettings.ActiveQuantityRecountInterval = Mathf.Clamp(ModSettings.QuantityRecountInterval.Value, 1f, 120f);
		AutoPinConfig.BindClientEntries(((BaseUnityPlugin)this).Config);
		RadarConfig.BindClientEntry(((BaseUnityPlugin)this).Config);
		WorldObjectConfig.BindVisibilityEntries(((BaseUnityPlugin)this).Config);
		ModSettings.ResetToDefaults = ((BaseUnityPlugin)this).Config.Bind<bool>("Server.General", "ResetToDefaults", false, "Set to true to reset ALL server settings to their default values. Resets itself to false automatically.");
		ModSettings.ResetToDefaults.SettingChanged += delegate
		{
			if (!ModSettings.ResetToDefaults.Value)
			{
				return;
			}
			foreach (ConfigDefinition key2 in ((BaseUnityPlugin)this).Config.Keys)
			{
				if (key2.Section.StartsWith("Server.", StringComparison.OrdinalIgnoreCase))
				{
					((BaseUnityPlugin)this).Config[key2].BoxedValue = ((BaseUnityPlugin)this).Config[key2].DefaultValue;
				}
			}
		};
		WorldObjectConfig.BindCategoryEntries(((BaseUnityPlugin)this).Config);
		CustomPrefabLoader.LoadAll(((BaseUnityPlugin)this).Config, ((BaseUnityPlugin)this).Config);
		AutoPinConfig.BindServerEntries(((BaseUnityPlugin)this).Config);
		RadarConfig.BindServerEntries(((BaseUnityPlugin)this).Config);
		WorldObjectConfig.WireEvents(((BaseUnityPlugin)this).Config);
		AutoPinConfig.WireEvents(((BaseUnityPlugin)this).Config);
		RadarConfig.WireEvents(((BaseUnityPlugin)this).Config);
		WorldObjectConfig.DiscoveryVisibilityChanged += MapPinSynchronizer.AppendPins;
		AutoPinConfig.DiscoveryPinObjectIcon.SettingChanged += delegate
		{
			MapPinSynchronizer.AppendPins();
		};
		AutoPinConfig.DiscoveryPinText.SettingChanged += delegate
		{
			MapPinSynchronizer.AppendPins();
		};
		ModSettings.ShowOtherPlayerPins.SettingChanged += delegate
		{
			MapPinSynchronizer.AppendPins();
		};
		AutoPinConfig.Reload();
	}
}
public static class MapFilePersistence
{
	[HarmonyPatch(typeof(ZNet), "LoadWorld")]
	private class ZNetPatchLoadWorld
	{
		private static void Postfix(ZNet __instance)
		{
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Expected O, but got Unknown
			object value = Traverse.Create((object)__instance).Field("m_world").GetValue();
			World val = (World)((value is World) ? value : null);
			if (val == null)
			{
				ManualLogSource log = ModSettings.Log;
				if (log != null)
				{
					log.LogWarning((object)"[MapFilePersistence] LoadWorld — m_world is null, cannot determine save path.");
				}
				return;
			}
			string dBPath = val.GetDBPath();
			string directoryName = Path.GetDirectoryName(dBPath);
			string text = ((string.IsNullOrEmpty(directoryName) || !Directory.Exists(directoryName)) ? Path.ChangeExtension(Utils.GetSaveDataPath((FileSource)1) + dBPath, null) : Path.ChangeExtension(dBPath, null));
			string text2 = (_savePath = text + ".one_map_to_rule_them_all.explored");
			ManualLogSource log2 = ModSettings.Log;
			if (log2 != null)
			{
				log2.LogInfo((object)$"[MapFilePersistence] LoadWorld — savePath={text2} exists={File.Exists(text2)}");
			}
			if (File.Exists(text2))
			{
				try
				{
					MapStateRepository.SetMapData(new ZPackage(File.ReadAllBytes(text2)));
					ManualLogSource log3 = ModSettings.Log;
					if (log3 != null)
					{
						log3.LogInfo((object)$"[MapFilePersistence] Loaded {MapStateRepository.GetPins().Count} pins from save file.");
					}
					return;
				}
				catch (Exception ex)
				{
					ManualLogSource log4 = ModSettings.Log;
					if (log4 != null)
					{
						log4.LogWarning((object)("[MapFilePersistence] Failed to read save file: " + ex.Message));
					}
				}
			}
			ZPackage val2 = TryMigrateFromServerSideMap(text);
			if (val2 != null)
			{
				MapStateRepository.SetMapData(val2);
				File.WriteAllBytes(text2, MapStateRepository.GetMapData().GetArray());
			}
			else
			{
				MapStateRepository.SetMapData(MapStateRepository.Default());
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "SaveWorldThread")]
	private class ZNetPatchSaveWorldThread
	{
		private static void Postfix()
		{
			SaveMapData();
		}
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private class ZNetPatchShutdownSave
	{
		private static void Prefix()
		{
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogInfo((object)$"[MapFilePersistence] ZNet.Shutdown Prefix — ServerPins={MapStateRepository.GetPins().Count} savePath={_savePath}");
			}
			SaveMapData();
		}
	}

	[HarmonyPatch(typeof(ZNet), "OnDestroy")]
	private class ZNetPatchOnDestroySave
	{
		private static void Prefix()
		{
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogInfo((object)$"[MapFilePersistence] ZNet.OnDestroy Prefix — ServerPins={MapStateRepository.GetPins().Count} savePath={_savePath}");
			}
			SaveMapData();
		}
	}

	private static string _savePath;

	public static bool MapDirty;

	private static void SaveMapData()
	{
		if (MapStateRepository.Explored == null)
		{
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogWarning((object)"[MapFilePersistence] SaveMapData skipped — Explored is null.");
			}
		}
		else if (_savePath == null)
		{
			ManualLogSource log2 = ModSettings.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)"[MapFilePersistence] SaveMapData skipped — _savePath is null.");
			}
		}
		else
		{
			if (!MapDirty)
			{
				return;
			}
			MapDirty = false;
			try
			{
				List<SharedPin> pins = MapStateRepository.GetPins();
				byte[] array = MapStateRepository.GetMapData().GetArray();
				File.WriteAllBytes(_savePath, array);
				ManualLogSource log3 = ModSettings.Log;
				if (log3 != null)
				{
					log3.LogInfo((object)$"[MapFilePersistence] Saved {pins.Count} pins ({array.Length} bytes) to {_savePath}");
				}
			}
			catch (Exception ex)
			{
				ManualLogSource log4 = ModSettings.Log;
				if (log4 != null)
				{
					log4.LogError((object)("[MapFilePersistence] Save failed: " + ex.Message));
				}
			}
		}
	}

	private static ZPackage TryMigrateFromServerSideMap(string basePath)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		string path = basePath + ".mod.serversidemap.explored";
		if (!File.Exists(path))
		{
			return null;
		}
		try
		{
			ZPackage val = new ZPackage(File.ReadAllBytes(path));
			val.SetPos(0);
			val.ReadInt();
			int num = val.ReadInt();
			bool[] array = new bool[num * num];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = val.ReadBool();
			}
			int num2 = val.ReadInt();
			string[] array2 = new string[num2];
			Vector3[] array3 = (Vector3[])(object)new Vector3[num2];
			int[] array4 = new int[num2];
			bool[] array5 = new bool[num2];
			for (int j = 0; j < num2; j++)
			{
				array2[j] = val.ReadString();
				array3[j] = val.ReadVector3();
				array4[j] = val.ReadInt();
				array5[j] = val.ReadBool();
			}
			string text = ZNet.GetUID().ToString();
			ZPackage val2 = new ZPackage();
			val2.Write(1);
			val2.Write(num);
			bool[] array6 = array;
			foreach (bool flag in array6)
			{
				val2.Write(flag);
			}
			val2.Write(num2);
			for (int l = 0; l < num2; l++)
			{
				val2.Write(array2[l]);
				val2.Write(array3[l]);
				val2.Write(array4[l]);
				val2.Write(array5[l]);
				val2.Write(text);
			}
			val2.SetPos(0);
			return val2;
		}
		catch
		{
			return null;
		}
	}
}
public static class ModSettings
{
	private static string _modFolder;

	public static ConfigEntry<bool> ResetToDefaults;

	public static ConfigEntry<bool> ClientResetToDefaults;

	public static ConfigEntry<bool> ShowOtherPlayerPins;

	public static ConfigEntry<float> ExploreFlushInterval;

	public static float ActiveExploreFlushInterval = 1f;

	public static ConfigEntry<float> UpdateThrottleInterval;

	public static float ActiveUpdateThrottleInterval = 0.25f;

	public static ConfigEntry<float> DiscoveryIconInterval;

	public static float ActiveDiscoveryIconInterval = 0.05f;

	public static ConfigEntry<float> QuantityRecountInterval;

	public static float ActiveQuantityRecountInterval = 10f;

	public static bool ModActive = true;

	public static ManualLogSource Log;

	public static string ModFolder
	{
		get
		{
			if (_modFolder == null)
			{
				_modFolder = Path.Combine(Paths.ConfigPath, "OneMapToRuleThemAll");
				Directory.CreateDirectory(_modFolder);
			}
			return _modFolder;
		}
	}

	public static bool IsServer()
	{
		return ZNetProxy.IsServer(ZNetProxy.ZNetInstance);
	}
}
public class BiomeCritterCategory
{
	public string CategoryName;

	public ConfigEntry<float> RadarRadiusEntry;

	public List<(string MatchKeyword, string DisplayName, string IconName)> CreatureDefinitions = new List<(string, string, string)>();

	public float ActiveRadarRadius;

	public List<(string InternalId, string DisplayName)> ActiveOrderedCreatures = new List<(string, string)>();

	public string[] Keywords = Array.Empty<string>();
}
public class PickableCategory
{
	public string CategoryName;

	public ConfigEntry<float> DiscoveryRadiusEntry;

	public ConfigEntry<float> RadarRadiusEntry;

	public ConfigEntry<string> TrackedDiscoveryEntry;

	public ConfigEntry<string> TrackedRadarEntry;

	public float ActiveDiscoveryRadius;

	public float ActiveRadarRadius;

	public string[] DiscoveryKeywords = Array.Empty<string>();

	public string[] RadarKeywords = Array.Empty<string>();
}
public static class WorldObjectConfig
{
	public static readonly List<PickableCategory> PickableCategories = new List<PickableCategory>();

	public static ConfigEntry<float> OresDiscoveryRadius;

	public static ConfigEntry<float> OresRadarRadius;

	public static ConfigEntry<string> TrackedOresDiscovery;

	public static ConfigEntry<string> TrackedOresRadar;

	public static ConfigEntry<float> LocationsDiscoveryRadius;

	public static ConfigEntry<float> LocationsRadarRadius;

	public static ConfigEntry<string> TrackedLocationsDiscovery;

	public static ConfigEntry<string> TrackedLocationsRadar;

	public static readonly List<BiomeCritterCategory> BiomeCritterCategories = new List<BiomeCritterCategory>();

	public static float ActiveOresDiscoveryRadius;

	public static float ActiveOresRadarRadius;

	public static float ActiveLocationsDiscoveryRadius;

	public static float ActiveLocationsRadarRadius;

	public static string[] PickablesDiscovery = Array.Empty<string>();

	public static string[] PickablesRadar = Array.Empty<string>();

	public static string[] OresDiscovery = Array.Empty<string>();

	public static string[] OresRadar = Array.Empty<string>();

	public static string[] LocationsDiscovery = Array.Empty<string>();

	public static string[] LocationsRadar = Array.Empty<string>();

	public static readonly HashSet<string> DiscoveryKeywordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static readonly HashSet<string> RadarKeywordSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	internal static readonly HashSet<string> CustomOresKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	internal static readonly HashSet<string> CustomLocationsKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, ConfigEntry<bool>> DiscoveryVisibilityEntries = new Dictionary<string, ConfigEntry<bool>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, ConfigEntry<bool>> RadarVisibilityEntries = new Dictionary<string, ConfigEntry<bool>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, ConfigEntry<bool>> _sectionToggleAllEntries = new Dictionary<string, ConfigEntry<bool>>(StringComparer.OrdinalIgnoreCase);

	private static ConfigFile _config;

	public static bool UsingServerConfig = false;

	private static readonly (string Biome, string Defaults)[] PickableBiomeDefaults = new(string, string)[8]
	{
		("Meadows", "Raspberry,Dandelion,Flint,Mushroom"),
		("BlackForest", "Blueberry,Thistle,Carrot"),
		("Swamp", "SurtlingCore,Turnip,BogIron"),
		("Mountain", "Crystal,Onion,DragonEgg"),
		("Plains", "Cloudberry,Barley,Flax,Tar"),
		("Ocean", "Chitin"),
		("Mistlands", "JotunPuff,Magecap,Fiddlehead,RoyalJelly"),
		("AshLands", "BlackCore,Sulfur,VoltureEgg,Ashstone,MoltenCore,Charredskull")
	};

	private static readonly string[] DefaultOreKeywords = new string[9] { "Copper", "Tin", "Iron", "Silver", "Obsidian", "BlackMarble", "Meteorite", "Flametal", "Stone" };

	private static readonly string[] DefaultLocationKeywords = new string[26]
	{
		"Eikthyr", "Hildir", "Runestone", "GDKing", "TrollCave", "Vendor", "Bonemass", "SunkenCrypt", "Crypt", "MudPile",
		"BogWitch", "DragonQueen", "MountainCave", "DrakeNest", "FrostCave", "GoblinKing", "TarPit", "Henge", "GoblinCamp", "WoodFarm",
		"ShipWreck", "SeekerQueen", "InfestedMine", "DvergrTown", "Fader", "GiantSkull"
	};

	public static event Action DiscoveryVisibilityChanged;

	public static event Action RadarVisibilityChanged;

	public static string DeriveMatchKeyword(string iconName)
	{
		if (!iconName.StartsWith("Trophy", StringComparison.OrdinalIgnoreCase))
		{
			return iconName;
		}
		return iconName.Substring(6);
	}

	public static string NormalizeForMatch(string s)
	{
		return s?.Replace("_", "").Replace(" ", "").ToLower() ?? "";
	}

	public static Dictionary<string, string> GetCreatureIconMap()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			foreach (var creatureDefinition in biomeCritterCategory.CreatureDefinitions)
			{
				string item = creatureDefinition.DisplayName;
				string item2 = creatureDefinition.IconName;
				if (!dictionary.ContainsKey(item))
				{
					dictionary[item] = item2;
				}
			}
		}
		return dictionary;
	}

	public static bool IsKeywordDiscoveryVisible(string keyword)
	{
		if (DiscoveryVisibilityEntries.TryGetValue(keyword, out var value))
		{
			return value.Value;
		}
		return true;
	}

	public static bool IsKeywordRadarVisible(string keyword)
	{
		if (RadarVisibilityEntries.TryGetValue(keyword, out var value))
		{
			return value.Value;
		}
		return true;
	}

	public static (string displayName, BiomeCritterCategory category) MatchCreatureKeyword(string objectName)
	{
		string text = NormalizeForMatch(objectName);
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			foreach (var (s, item) in biomeCritterCategory.ActiveOrderedCreatures)
			{
				if (text.Contains(NormalizeForMatch(s)))
				{
					return (item, biomeCritterCategory);
				}
			}
		}
		return (null, null);
	}

	public static string GetInternalIdForCreature(string displayName)
	{
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			foreach (var activeOrderedCreature in biomeCritterCategory.ActiveOrderedCreatures)
			{
				var (result, _) = activeOrderedCreature;
				if (string.Equals(activeOrderedCreature.DisplayName, displayName, StringComparison.OrdinalIgnoreCase))
				{
					return result;
				}
			}
		}
		return displayName;
	}

	public static float GetPickableDiscoveryRadius(string keyword)
	{
		if (keyword == null)
		{
			return 6f;
		}
		foreach (PickableCategory pickableCategory in PickableCategories)
		{
			string[] discoveryKeywords = pickableCategory.DiscoveryKeywords;
			for (int i = 0; i < discoveryKeywords.Length; i++)
			{
				if (string.Equals(discoveryKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
				{
					return pickableCategory.ActiveDiscoveryRadius;
				}
			}
			discoveryKeywords = pickableCategory.RadarKeywords;
			for (int i = 0; i < discoveryKeywords.Length; i++)
			{
				if (string.Equals(discoveryKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
				{
					return pickableCategory.ActiveDiscoveryRadius;
				}
			}
		}
		return 6f;
	}

	public static void EnsureVisibilityEntries()
	{
		string[] discoveryKeywords;
		foreach (PickableCategory pickableCategory in PickableCategories)
		{
			string section = "Client.MapPin.Pickables." + pickableCategory.CategoryName;
			string section2 = "Client.Radar.Pickables." + pickableCategory.CategoryName;
			EnsureToggleAllEntry(section);
			EnsureToggleAllEntry(section2);
			discoveryKeywords = pickableCategory.DiscoveryKeywords;
			for (int i = 0; i < discoveryKeywords.Length; i++)
			{
				EnsureDiscoveryVisibilityEntry(discoveryKeywords[i], section);
			}
			discoveryKeywords = pickableCategory.RadarKeywords;
			for (int i = 0; i < discoveryKeywords.Length; i++)
			{
				EnsureRadarVisibilityEntry(discoveryKeywords[i], section2);
			}
		}
		EnsureToggleAllEntry("Client.MapPin.Ores");
		EnsureToggleAllEntry("Client.Radar.Ores");
		EnsureToggleAllEntry("Client.MapPin.Locations");
		EnsureToggleAllEntry("Client.Radar.Locations");
		discoveryKeywords = OresDiscovery;
		for (int i = 0; i < discoveryKeywords.Length; i++)
		{
			EnsureDiscoveryVisibilityEntry(discoveryKeywords[i], "Client.MapPin.Ores");
		}
		discoveryKeywords = OresRadar;
		for (int i = 0; i < discoveryKeywords.Length; i++)
		{
			EnsureRadarVisibilityEntry(discoveryKeywords[i], "Client.Radar.Ores");
		}
		discoveryKeywords = LocationsDiscovery;
		for (int i = 0; i < discoveryKeywords.Length; i++)
		{
			EnsureDiscoveryVisibilityEntry(discoveryKeywords[i], "Client.MapPin.Locations");
		}
		discoveryKeywords = LocationsRadar;
		for (int i = 0; i < discoveryKeywords.Length; i++)
		{
			EnsureRadarVisibilityEntry(discoveryKeywords[i], "Client.Radar.Locations");
		}
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			string section3 = "Client.Radar.Critters." + biomeCritterCategory.CategoryName.Replace(" ", "_");
			EnsureToggleAllEntry(section3);
			discoveryKeywords = biomeCritterCategory.Keywords;
			for (int i = 0; i < discoveryKeywords.Length; i++)
			{
				EnsureRadarVisibilityEntry(discoveryKeywords[i], section3);
			}
		}
	}

	private static void BindToggleAll(ConfigFile config, string section)
	{
		if (!_sectionToggleAllEntries.ContainsKey(section))
		{
			_sectionToggleAllEntries[section] = config.Bind<bool>(section, "_ToggleAll", false, "Set all entries in this section on or off at once.");
		}
	}

	private static void EnsureToggleAllEntry(string section)
	{
		if (!_sectionToggleAllEntries.ContainsKey(section))
		{
			BindToggleAll(_config, section);
		}
	}

	private static void EnsureDiscoveryVisibilityEntry(string keyword, string section)
	{
		if (!DiscoveryVisibilityEntries.ContainsKey(keyword))
		{
			DiscoveryVisibilityEntries[keyword] = _config.Bind<bool>(section, keyword, false, "Show '" + keyword + "' discovery pins on your minimap. Pins are still created and shared; this only controls local visibility.");
		}
	}

	private static void EnsureRadarVisibilityEntry(string keyword, string section)
	{
		if (!RadarVisibilityEntries.ContainsKey(keyword))
		{
			RadarVisibilityEntries[keyword] = _config.Bind<bool>(section, keyword, false, "Show '" + keyword + "' radar pins on your minimap. Client-only — never overridden by the server.");
		}
	}

	public static void PrepareCreatureData()
	{
		string path = Path.Combine(ModSettings.ModFolder, "OneMapToRuleThemAll.creatures.txt");
		if (!File.Exists(path))
		{
			WriteDefaultCreaturesFile(path);
		}
		LoadCreaturesFile(path);
	}

	public static void BindVisibilityEntries(ConfigFile clientConfig)
	{
		_config = clientConfig;
		(string, string)[] pickableBiomeDefaults = PickableBiomeDefaults;
		string[] array;
		for (int i = 0; i < pickableBiomeDefaults.Length; i++)
		{
			(string, string) tuple = pickableBiomeDefaults[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string text = "Client.MapPin.Pickables." + item;
			string text2 = "Client.Radar.Pickables." + item;
			BindToggleAll(clientConfig, text);
			BindToggleAll(clientConfig, text2);
			array = AutoPinConfig.ParseKeywordList(item2);
			foreach (string text3 in array)
			{
				DiscoveryVisibilityEntries[text3] = clientConfig.Bind<bool>(text, text3, false, "Show '" + text3 + "' discovery pins on your minimap. Pins are still created and shared; this only controls local visibility.");
				RadarVisibilityEntries[text3] = clientConfig.Bind<bool>(text2, text3, false, "Show '" + text3 + "' radar pins on your minimap. Client-only — never overridden by the server.");
			}
		}
		BindToggleAll(clientConfig, "Client.MapPin.Ores");
		BindToggleAll(clientConfig, "Client.Radar.Ores");
		array = DefaultOreKeywords;
		foreach (string text4 in array)
		{
			DiscoveryVisibilityEntries[text4] = clientConfig.Bind<bool>("Client.MapPin.Ores", text4, false, "Show '" + text4 + "' discovery pins on your minimap. Pins are still created and shared; this only controls local visibility.");
			RadarVisibilityEntries[text4] = clientConfig.Bind<bool>("Client.Radar.Ores", text4, false, "Show '" + text4 + "' radar pins on your minimap. Client-only — never overridden by the server.");
		}
		BindToggleAll(clientConfig, "Client.MapPin.Locations");
		BindToggleAll(clientConfig, "Client.Radar.Locations");
		array = DefaultLocationKeywords;
		foreach (string text5 in array)
		{
			DiscoveryVisibilityEntries[text5] = clientConfig.Bind<bool>("Client.MapPin.Locations", text5, false, "Show '" + text5 + "' discovery pins on your minimap. Pins are still created and shared; this only controls local visibility.");
			RadarVisibilityEntries[text5] = clientConfig.Bind<bool>("Client.Radar.Locations", text5, false, "Show '" + text5 + "' radar pins on your minimap. Client-only — never overridden by the server.");
		}
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			string text6 = "Client.Radar.Critters." + biomeCritterCategory.CategoryName.Replace(" ", "_");
			BindToggleAll(clientConfig, text6);
			foreach (var creatureDefinition in biomeCritterCategory.CreatureDefinitions)
			{
				string item3 = creatureDefinition.DisplayName;
				RadarVisibilityEntries[item3] = clientConfig.Bind<bool>(text6, item3, false, "Show '" + item3 + "' radar pins on your minimap. Client-only — never overridden by the server.");
			}
		}
	}

	public static void BindCategoryEntries(ConfigFile serverConfig)
	{
		PickableCategories.Clear();
		(string, string)[] pickableBiomeDefaults = PickableBiomeDefaults;
		for (int i = 0; i < pickableBiomeDefaults.Length; i++)
		{
			(string, string) tuple = pickableBiomeDefaults[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string text = "Server.Pickables." + item;
			PickableCategory pickableCategory = new PickableCategory
			{
				CategoryName = item
			};
			pickableCategory.DiscoveryRadiusEntry = serverConfig.Bind<float>(text, "DiscoveryRadius", 6f, "How close (meters) the player must approach a " + item + " pickable to trigger a discovery pin. Maximum 150.");
			pickableCategory.RadarRadiusEntry = serverConfig.Bind<float>(text, "RadarRadius", 50f, "How close (meters) the player must be to a " + item + " pickable for its radar pin to appear. Maximum 150.");
			pickableCategory.TrackedDiscoveryEntry = serverConfig.Bind<string>(text, "TrackedDiscovery", item2, "Comma-separated keywords for " + item + " pickables that trigger a permanent discovery pin.");
			pickableCategory.TrackedRadarEntry = serverConfig.Bind<string>(text, "TrackedRadar", item2, "Comma-separated keywords for " + item + " pickables that appear as radar (transient proximity) pins.");
			pickableCategory.ActiveDiscoveryRadius = pickableCategory.DiscoveryRadiusEntry.Value;
			pickableCategory.ActiveRadarRadius = pickableCategory.RadarRadiusEntry.Value;
			pickableCategory.DiscoveryKeywords = AutoPinConfig.ParseKeywordList(pickableCategory.TrackedDiscoveryEntry.Value);
			pickableCategory.RadarKeywords = AutoPinConfig.ParseKeywordList(pickableCategory.TrackedRadarEntry.Value);
			PickableCategories.Add(pickableCategory);
		}
		OresDiscoveryRadius = serverConfig.Bind<float>("Server.Ores", "DiscoveryRadius", 6f, "How close (meters) the player must approach an ore deposit to trigger a discovery pin. Maximum 150.");
		OresRadarRadius = serverConfig.Bind<float>("Server.Ores", "RadarRadius", 50f, "How close (meters) the player must be to an ore deposit for its radar pin to appear. Maximum 150.");
		TrackedOresDiscovery = serverConfig.Bind<string>("Server.Ores", "TrackedDiscovery", "Copper,Tin,Iron,Silver,Obsidian,BlackMarble,Meteorite,Flametal,Stone", "Comma-separated keywords for ore deposits that trigger a permanent discovery pin.");
		TrackedOresRadar = serverConfig.Bind<string>("Server.Ores", "TrackedRadar", "Copper,Tin,Iron,Silver,Obsidian,BlackMarble,Meteorite,Flametal,Stone", "Comma-separated keywords for ore deposits that appear as radar pins.");
		LocationsDiscoveryRadius = serverConfig.Bind<float>("Server.Locations", "DiscoveryRadius", 6f, "How close (meters) the player must approach a location to trigger a discovery pin. Maximum 150.");
		LocationsRadarRadius = serverConfig.Bind<float>("Server.Locations", "RadarRadius", 50f, "How close (meters) the player must be to a location for its radar pin to appear. Maximum 150.");
		TrackedLocationsDiscovery = serverConfig.Bind<string>("Server.Locations", "TrackedDiscovery", "Eikthyr,Hildir,Runestone,GDKing,TrollCave,Vendor,Bonemass,SunkenCrypt,Crypt,MudPile,BogWitch,DragonQueen,MountainCave,DrakeNest,FrostCave,GoblinKing,TarPit,Henge,GoblinCamp,WoodFarm,ShipWreck,SeekerQueen,InfestedMine,DvergrTown,Fader,GiantSkull", "Comma-separated keywords for locations that trigger a permanent discovery pin.");
		TrackedLocationsRadar = serverConfig.Bind<string>("Server.Locations", "TrackedRadar", "Eikthyr,Hildir,Runestone,GDKing,TrollCave,Vendor,Bonemass,SunkenCrypt,Crypt,MudPile,BogWitch,DragonQueen,MountainCave,DrakeNest,FrostCave,GoblinKing,TarPit,Henge,GoblinCamp,WoodFarm,ShipWreck,SeekerQueen,InfestedMine,DvergrTown,Fader,GiantSkull", "Comma-separated keywords for locations that appear as radar pins.");
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Meadows", 50f },
			{ "BlackForest", 50f },
			{ "Swamp", 50f },
			{ "Mountain", 50f },
			{ "Plains", 50f },
			{ "Mistlands", 50f },
			{ "AshLands", 50f },
			{ "Ocean", 50f },
			{ "Bosses", 50f },
			{ "Minibosses", 50f },
			{ "General", 50f }
		};
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			string text2 = "Server.Critters." + biomeCritterCategory.CategoryName.Replace(" ", "_");
			float value;
			float num = (dictionary.TryGetValue(biomeCritterCategory.CategoryName, out value) ? value : 40f);
			biomeCritterCategory.RadarRadiusEntry = serverConfig.Bind<float>(text2, "RadarRadius", num, "Detection radius (meters) for " + biomeCritterCategory.CategoryName + " creatures to show as radar pins. Maximum 150.");
			biomeCritterCategory.ActiveRadarRadius = biomeCritterCategory.RadarRadiusEntry.Value;
			RebuildBiomeCreatureState(biomeCritterCategory);
		}
	}

	public static void WireEvents(ConfigFile config)
	{
		config.SettingChanged += delegate(object _, SettingChangedEventArgs e)
		{
			string section = e.ChangedSetting.Definition.Section;
			if (e.ChangedSetting.Definition.Key == "_ToggleAll")
			{
				bool value = (bool)e.ChangedSetting.BoxedValue;
				foreach (ConfigEntry<bool> value2 in DiscoveryVisibilityEntries.Values)
				{
					if (((ConfigEntryBase)value2).Definition.Section == section)
					{
						value2.Value = value;
					}
				}
				{
					foreach (ConfigEntry<bool> value3 in RadarVisibilityEntries.Values)
					{
						if (((ConfigEntryBase)value3).Definition.Section == section)
						{
							value3.Value = value;
						}
					}
					return;
				}
			}
			if (section.StartsWith("Client.MapPin."))
			{
				WorldObjectConfig.DiscoveryVisibilityChanged?.Invoke();
			}
			if (section.StartsWith("Client.Radar."))
			{
				WorldObjectConfig.RadarVisibilityChanged?.Invoke();
			}
		};
	}

	public static void Reload()
	{
		if (OresDiscoveryRadius.Value > 150f)
		{
			OresDiscoveryRadius.Value = 150f;
		}
		if (OresRadarRadius.Value > 150f)
		{
			OresRadarRadius.Value = 150f;
		}
		if (LocationsDiscoveryRadius.Value > 150f)
		{
			LocationsDiscoveryRadius.Value = 150f;
		}
		if (LocationsRadarRadius.Value > 150f)
		{
			LocationsRadarRadius.Value = 150f;
		}
		foreach (PickableCategory pickableCategory in PickableCategories)
		{
			if (pickableCategory.DiscoveryRadiusEntry != null)
			{
				if (pickableCategory.DiscoveryRadiusEntry.Value > 150f)
				{
					pickableCategory.DiscoveryRadiusEntry.Value = 150f;
				}
				if (!UsingServerConfig)
				{
					pickableCategory.ActiveDiscoveryRadius = pickableCategory.DiscoveryRadiusEntry.Value;
				}
			}
			if (pickableCategory.RadarRadiusEntry != null)
			{
				if (pickableCategory.RadarRadiusEntry.Value > 150f)
				{
					pickableCategory.RadarRadiusEntry.Value = 150f;
				}
				if (!UsingServerConfig)
				{
					pickableCategory.ActiveRadarRadius = pickableCategory.RadarRadiusEntry.Value;
				}
			}
			if (!UsingServerConfig)
			{
				if (pickableCategory.TrackedDiscoveryEntry != null)
				{
					pickableCategory.DiscoveryKeywords = AutoPinConfig.ParseKeywordList(pickableCategory.TrackedDiscoveryEntry.Value);
				}
				if (pickableCategory.TrackedRadarEntry != null)
				{
					pickableCategory.RadarKeywords = AutoPinConfig.ParseKeywordList(pickableCategory.TrackedRadarEntry.Value);
				}
			}
		}
		PickablesDiscovery = PickableCategories.SelectMany((PickableCategory c) => c.DiscoveryKeywords).ToArray();
		PickablesRadar = PickableCategories.SelectMany((PickableCategory c) => c.RadarKeywords).ToArray();
		if (!UsingServerConfig)
		{
			ActiveOresDiscoveryRadius = OresDiscoveryRadius.Value;
			ActiveOresRadarRadius = OresRadarRadius.Value;
			OresDiscovery = AutoPinConfig.ParseKeywordList(TrackedOresDiscovery.Value).Concat(CustomOresKeywords).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				.ToArray();
			OresRadar = AutoPinConfig.ParseKeywordList(TrackedOresRadar.Value).Concat(CustomOresKeywords).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				.ToArray();
			ActiveLocationsDiscoveryRadius = LocationsDiscoveryRadius.Value;
			ActiveLocationsRadarRadius = LocationsRadarRadius.Value;
			LocationsDiscovery = AutoPinConfig.ParseKeywordList(TrackedLocationsDiscovery.Value).Concat(CustomLocationsKeywords).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				.ToArray();
			LocationsRadar = AutoPinConfig.ParseKeywordList(TrackedLocationsRadar.Value).Concat(CustomLocationsKeywords).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				.ToArray();
		}
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			if (biomeCritterCategory.RadarRadiusEntry != null)
			{
				if (biomeCritterCategory.RadarRadiusEntry.Value > 150f)
				{
					biomeCritterCategory.RadarRadiusEntry.Value = 150f;
				}
				if (!UsingServerConfig)
				{
					biomeCritterCategory.ActiveRadarRadius = biomeCritterCategory.RadarRadiusEntry.Value;
				}
			}
			if (!UsingServerConfig)
			{
				RebuildBiomeCreatureState(biomeCritterCategory);
			}
		}
		RebuildKeywordSets();
		EnsureVisibilityEntries();
		WorldObjectPatches.ReScanForKeywordChanges();
	}

	public static void ResetToLocalConfig()
	{
		UsingServerConfig = false;
		Reload();
	}

	private static void RebuildBiomeCreatureState(BiomeCritterCategory cat)
	{
		cat.ActiveOrderedCreatures.Clear();
		List<string> list = new List<string>();
		foreach (var (text, text2, _) in cat.CreatureDefinitions)
		{
			if (!string.IsNullOrEmpty(text))
			{
				cat.ActiveOrderedCreatures.Add((text, text2));
				list.Add(text2);
			}
		}
		cat.Keywords = list.ToArray();
	}

	private static void RebuildKeywordSets()
	{
		DiscoveryKeywordSet.Clear();
		RadarKeywordSet.Clear();
		string[] pickablesDiscovery = PickablesDiscovery;
		foreach (string item in pickablesDiscovery)
		{
			DiscoveryKeywordSet.Add(item);
		}
		pickablesDiscovery = OresDiscovery;
		foreach (string item2 in pickablesDiscovery)
		{
			DiscoveryKeywordSet.Add(item2);
		}
		pickablesDiscovery = LocationsDiscovery;
		foreach (string item3 in pickablesDiscovery)
		{
			DiscoveryKeywordSet.Add(item3);
		}
		pickablesDiscovery = PickablesRadar;
		foreach (string item4 in pickablesDiscovery)
		{
			RadarKeywordSet.Add(item4);
		}
		pickablesDiscovery = OresRadar;
		foreach (string item5 in pickablesDiscovery)
		{
			RadarKeywordSet.Add(item5);
		}
		pickablesDiscovery = LocationsRadar;
		foreach (string item6 in pickablesDiscovery)
		{
			RadarKeywordSet.Add(item6);
		}
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			pickablesDiscovery = biomeCritterCategory.Keywords;
			foreach (string item7 in pickablesDiscovery)
			{
				RadarKeywordSet.Add(item7);
			}
		}
	}

	public static void PackCategoryData(ZPackage z)
	{
		z.Write(PickableCategories.Count);
		foreach (PickableCategory pickableCategory in PickableCategories)
		{
			z.Write(pickableCategory.CategoryName);
			z.Write(pickableCategory.DiscoveryRadiusEntry?.Value ?? pickableCategory.ActiveDiscoveryRadius);
			z.Write(pickableCategory.RadarRadiusEntry?.Value ?? pickableCategory.ActiveRadarRadius);
			z.Write(pickableCategory.TrackedDiscoveryEntry?.Value ?? string.Join(",", pickableCategory.DiscoveryKeywords));
			z.Write(pickableCategory.TrackedRadarEntry?.Value ?? string.Join(",", pickableCategory.RadarKeywords));
		}
		z.Write(OresDiscoveryRadius.Value);
		z.Write(OresRadarRadius.Value);
		z.Write(TrackedOresDiscovery.Value);
		z.Write(TrackedOresRadar.Value);
		z.Write(LocationsDiscoveryRadius.Value);
		z.Write(LocationsRadarRadius.Value);
		z.Write(TrackedLocationsDiscovery.Value);
		z.Write(TrackedLocationsRadar.Value);
		z.Write(BiomeCritterCategories.Count);
		foreach (BiomeCritterCategory biomeCritterCategory in BiomeCritterCategories)
		{
			z.Write(biomeCritterCategory.CategoryName);
			z.Write(biomeCritterCategory.RadarRadiusEntry?.Value ?? biomeCritterCategory.ActiveRadarRadius);
			z.Write(biomeCritterCategory.CreatureDefinitions.Count);
			foreach (var (text, text2, text3) in biomeCritterCategory.CreatureDefinitions)
			{
				z.Write(text2);
				z.Write(text);
				z.Write(text3);
			}
		}
	}

	public static void UnpackCategoryDataIntoEntries(ZPackage z)
	{
		int num = z.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string name2 = z.ReadString();
			float value = z.ReadSingle();
			float value2 = z.ReadSingle();
			string value3 = z.ReadString();
			string value4 = z.ReadString();
			PickableCategory pickableCategory = PickableCategories.FirstOrDefault((PickableCategory c) => string.Equals(c.CategoryName, name2, StringComparison.OrdinalIgnoreCase));
			if (pickableCategory != null)
			{
				if (pickableCategory.DiscoveryRadiusEntry != null)
				{
					pickableCategory.DiscoveryRadiusEntry.Value = value;
				}
				if (pickableCategory.RadarRadiusEntry != null)
				{
					pickableCategory.RadarRadiusEntry.Value = value2;
				}
				if (pickableCategory.TrackedDiscoveryEntry != null)
				{
					pickableCategory.TrackedDiscoveryEntry.Value = value3;
				}
				if (pickableCategory.TrackedRadarEntry != null)
				{
					pickableCategory.TrackedRadarEntry.Value = value4;
				}
			}
		}
		OresDiscoveryRadius.Value = z.ReadSingle();
		OresRadarRadius.Value = z.ReadSingle();
		TrackedOresDiscovery.Value = z.ReadString();
		TrackedOresRadar.Value = z.ReadString();
		LocationsDiscoveryRadius.Value = z.ReadSingle();
		LocationsRadarRadius.Value = z.ReadSingle();
		TrackedLocationsDiscovery.Value = z.ReadString();
		TrackedLocationsRadar.Value = z.ReadString();
		int num2 = z.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			string name = z.ReadString();
			float value5 = z.ReadSingle();
			int num3 = z.ReadInt();
			for (int k = 0; k < num3; k++)
			{
				z.ReadString();
				z.ReadString();
				z.ReadString();
			}
			BiomeCritterCategory biomeCritterCategory = BiomeCritterCategories.FirstOrDefault((BiomeCritterCategory c) => string.Equals(c.CategoryName, name, StringComparison.OrdinalIgnoreCase));
			if (biomeCritterCategory?.RadarRadiusEntry != null)
			{
				biomeCritterCategory.RadarRadiusEntry.Value = value5;
			}
		}
	}

	public static void ApplyCategoryData(ZPackage z)
	{
		if (!ModSettings.IsServer())
		{
			UsingServerConfig = true;
		}
		int num = z.ReadInt();
		PickableCategories.Clear();
		for (int i = 0; i < num; i++)
		{
			string categoryName = z.ReadString();
			float activeDiscoveryRadius = z.ReadSingle();
			float activeRadarRadius = z.ReadSingle();
			string csv = z.ReadString();
			string csv2 = z.ReadString();
			PickableCategory item = new PickableCategory
			{
				CategoryName = categoryName,
				ActiveDiscoveryRadius = activeDiscoveryRadius,
				ActiveRadarRadius = activeRadarRadius,
				DiscoveryKeywords = AutoPinConfig.ParseKeywordList(csv),
				RadarKeywords = AutoPinConfig.ParseKeywordList(csv2)
			};
			PickableCategories.Add(item);
		}
		PickablesDiscovery = PickableCategories.SelectMany((PickableCategory c) => c.DiscoveryKeywords).ToArray();
		PickablesRadar = PickableCategories.SelectMany((PickableCategory c) => c.RadarKeywords).ToArray();
		ActiveOresDiscoveryRadius = z.ReadSingle();
		ActiveOresRadarRadius = z.ReadSingle();
		OresDiscovery = AutoPinConfig.ParseKeywordList(z.ReadString());
		OresRadar = AutoPinConfig.ParseKeywordList(z.ReadString());
		ActiveLocationsDiscoveryRadius = z.ReadSingle();
		ActiveLocationsRadarRadius = z.ReadSingle();
		LocationsDiscovery = AutoPinConfig.ParseKeywordList(z.ReadString());
		LocationsRadar = AutoPinConfig.ParseKeywordList(z.ReadString());
		int num2 = z.ReadInt();
		BiomeCritterCategories.Clear();
		for (int j = 0; j < num2; j++)
		{
			string categoryName2 = z.ReadString();
			float activeRadarRadius2 = z.ReadSingle();
			BiomeCritterCategory biomeCritterCategory = new BiomeCritterCategory
			{
				CategoryName = categoryName2,
				ActiveRadarRadius = activeRadarRadius2
			};
			int num3 = z.ReadInt();
			for (int k = 0; k < num3; k++)
			{
				string item2 = z.ReadString();
				string text = z.ReadString();
				string item3 = z.ReadString();
				biomeCritterCategory.CreatureDefinitions.Add((text, item2, item3));
				if (!string.IsNullOrEmpty(text))
				{
					biomeCritterCategory.ActiveOrderedCreatures.Add((text, item2));
				}
			}
			biomeCritterCategory.Keywords = biomeCritterCategory.ActiveOrderedCreatures.Select(((string InternalId, string DisplayName) x) => x.DisplayName).ToArray();
			BiomeCritterCategories.Add(biomeCritterCategory);
		}
		EnsureVisibilityEntries();
		RebuildKeywordSets();
		WorldObjectPatches.ReScanForKeywordChanges();
	}

	private static void LoadCreaturesFile(string path)
	{
		BiomeCritterCategories.Clear();
		Dictionary<string, BiomeCritterCategory> dictionary = new Dictionary<string, BiomeCritterCategory>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (string.IsNullOrEmpty(text) || text.StartsWith("#"))
			{
				continue;
			}
			string[] array2 = text.Split(new char[1] { ':' });
			if (array2.Length < 3)
			{
				continue;
			}
			string text2 = array2[0].Trim();
			string text3 = array2[1].Trim();
			string text4 = array2[2].Trim();
			string item = ((array2.Length >= 4) ? array2[3].Trim() : text4);
			if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3) && !string.IsNullOrEmpty(text4))
			{
				string item2 = DeriveMatchKeyword(text4);
				if (!dictionary.TryGetValue(text2, out var value))
				{
					value = (dictionary[text2] = new BiomeCritterCategory
					{
						CategoryName = text2
					});
					list.Add(text2);
				}
				value.CreatureDefinitions.Add((item2, text3, item));
			}
		}
		foreach (string item3 in list)
		{
			BiomeCritterCategories.Add(dictionary[item3]);
		}
	}

	private static void WriteDefaultCreaturesFile(string path)
	{
		File.WriteAllText(path, "# OneMapToRuleThemAll — Creature Definitions\n# Format:  category:displayname:internalname[:iconoverride]\n#\n# internalname  = used to derive the match keyword ('Trophy' prefix stripped if present).\n# iconoverride  = optional 4th field; ObjectDB item name for the icon when the creature\n#                 has no trophy. If omitted, internalname is also used for the icon lookup.\n# Matching is normalized (case-insensitive, underscores/spaces ignored), so\n# 'GreydwarfShaman' matches the game object 'Greydwarf_Shaman'.\n#\n# ORDER MATTERS: more-specific entries must appear before less-specific ones within\n# and across categories. Bosses and Minibosses are listed first so their specific\n# prefab names are checked before generic biome keywords.\n#\n# Radar radius per category is configured in One_Map_To_Rule_Them_All_Settings.cfg\n# under [Critters.<CategoryName>] RadarRadius.\n\n# ── Bosses ────────────────────────────────────────────────────────────────────\nBosses:Eikthyr:TrophyEikthyr\nBosses:The Elder:TrophyTheElder\nBosses:Bonemass:TrophyBonemass\nBosses:Moder:TrophyDragonQueen\nBosses:Yagluth:TrophyGoblinKing\nBosses:The Queen:TrophySeekerQueen\nBosses:Fader:TrophyFader\n\n# ── Minibosses ────────────────────────────────────────────────────────────────\n# Listed before biome categories so specific prefab names match before generic ones.\n# e.g. GoblinBruteBros must match before GoblinBrute (Fuling Berserker).\nMinibosses:Brenna:TrophySkeletonHildir\nMinibosses:Geirrhafa:TrophyCultist_Hildir\nMinibosses:Zil & Thungr:GoblinBruteBros\nMinibosses:Lord Reto:Charred_Melee_Dyrnwyn\n\n# ── Meadows ───────────────────────────────────────────────────────────────────\nMeadows:Deer:TrophyDeer\nMeadows:Boar:TrophyBoar\nMeadows:Neck:TrophyNeck\nMeadows:Greyling:Greyling:TrophyGreydwarf\n\n# ── Black Forest ──────────────────────────────────────────────────────────────\n# Category name 'BlackForest' (no space) matches the existing config section [Critters.BlackForest].\n# Greydwarf shaman must appear before Greydwarf (shaman's keyword is a substring check).\n# Rancid remains (SkeletonPoison) must appear before Skeleton.\nBlackForest:Greydwarf shaman:TrophyGreydwarfShaman\nBlackForest:Greydwarf:TrophyGreydwarf\nBlackForest:Rancid remains:TrophySkeletonPoison\nBlackForest:Skeleton:TrophySkeleton\nBlackForest:Troll:Troll:TrophyFrostTroll\nBlackForest:Ghost:TrophyGhost\nBlackForest:Bear:TrophyBjorn\n\n# ── Swamp ─────────────────────────────────────────────────────────────────────\n# Oozer (BlobElite) before Blob. Draugr elite before Draugr.\nSwamp:Abomination:TrophyAbomination\nSwamp:Oozer:BlobElite\nSwamp:Blob:TrophyBlob\nSwamp:Draugr elite:TrophyDraugrElite\nSwamp:Draugr:TrophyDraugr\nSwamp:Leech:TrophyLeech\nSwamp:Leech (cave variant):TrophyLeech\nSwamp:Surtling:TrophySurtling\nSwamp:Wraith:TrophyWraith\nSwamp:Kvastur:BogWitchKvastur:TrophyWraith\n\n# ── Mountain ──────────────────────────────────────────────────────────────────\n# Cultist (Fenring_Cultist) must appear before Fenring.\nMountain:Wolf:TrophyWolf\nMountain:Drake:TrophyHatchling\nMountain:Stone Golem:Golem:TrophySGolem\nMountain:Cultist:TrophyCultist\nMountain:Fenring:TrophyFenring\nMountain:Ulv:TrophyUlv\n\n# ── Plains ────────────────────────────────────────────────────────────────────\n# Fuling Berserker and Fuling shaman must appear before generic Fuling (Goblin).\nPlains:Fuling Berserker:TrophyGoblinBrute\nPlains:Fuling shaman:TrophyGoblinShaman\nPlains:Fuling:TrophyGoblin\nPlains:Deathsquito:TrophyDeathsquito\nPlains:Lox:TrophyLox\nPlains:Growth:TrophyGrowth\nPlains:Vile:TrophyBjornUndead\n\n# ── Mistlands ─────────────────────────────────────────────────────────────────\n# Seeker Soldier and Seeker Brood must appear before generic Seeker.\n# All Dvergr entries derive the same match keyword; first listed wins for all variants.\nMistlands:Seeker Soldier:TrophySeekerBrute\nMistlands:Seeker Brood:SeekerBrood\nMistlands:Seeker:TrophySeeker\nMistlands:Gjall:TrophyGjall\nMistlands:Tick:TrophyTick\nMistlands:Dvergr mage (Fire Variant):TrophyDvergr\nMistlands:Dvergr mage (Ice Variant):TrophyDvergr\nMistlands:Dvergr mage (Support Variant):TrophyDvergr\nMistlands:Dvergr mage:TrophyDvergr\nMistlands:Dvergr rogue:TrophyDvergr\nMistlands:Hare:TrophyHare\n\n# ── Ashlands ──────────────────────────────────────────────────────────────────\n# Category name 'AshLands' matches the existing config section [Critters.AshLands].\n# Specific Charred variants before any generic 'Charred' entry.\nAshLands:Charred Warlock:TrophyCharredMage\nAshLands:Charred Warrior:TrophyCharredMelee\nAshLands:Charred Marksman:TrophyCharredArcher\nAshLands:Charred Twitcher:Charred_Twitcher\nAshLands:Bonemaw:TrophyBonemawSerpent\nAshLands:Lava Blob:BlobLava\nAshLands:Fallen Valkyrie:TrophyFallenValkyrie\nAshLands:Volture:TrophyVolture\nAshLands:Morgen:TrophyMorgen\nAshLands:Asksvin:TrophyAsksvin\nAshLands:Asksvin hatchling:Asksvin_hatchling:TrophyAsksvin\nAshLands:Unbjorn:Unbjorn:TrophyMorgen\n# Skugg: icon is a building piece; if Skugg is not detected, check its in-game prefab name.\nAshLands:Skugg:piece_Charred_Balista\n\n# ── Ocean ─────────────────────────────────────────────────────────────────────\n# Fish10-12 must appear before Fish1 (Fish10 contains 'Fish1' as a substring).\nOcean:Northern salmon:Fish10\nOcean:Magmafish:Fish11\nOcean:Pufferfish:Fish12\nOcean:Perch:Fish1\nOcean:Pike:Fish2\nOcean:Tuna:Fish3\nOcean:Tetra:Fish4_cave\nOcean:Trollfish:Fish5\nOcean:Giant herring:Fish6\nOcean:Grouper:Fish7\nOcean:Coral cod:Fish8\nOcean:Anglerfish:Fish9\nOcean:Serpent:TrophySerpent\nOcean:Leviathan:Leviathan\n\n# ── General ───────────────────────────────────────────────────────────────────\nGeneral:Chicken:Chicken\nGeneral:Hen:Hen\n");
	}
}
public static class AutoPinConfig
{
	public static ConfigEntry<bool> Enabled;

	public static ConfigEntry<float> QuantitySearchRadius;

	public static bool ActiveEnabled;

	public static float ActiveQuantitySearchRadius;

	public static bool UsingServerConfig = false;

	private static readonly Dictionary<string, ConfigEntry<string>> AbbreviationEntries = new Dictionary<string, ConfigEntry<string>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> Abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static ConfigEntry<bool> DiscoveryPinObjectIcon;

	public static ConfigEntry<bool> DiscoveryPinText;

	private static bool _reloading = false;

	public static void BindClientEntries(ConfigFile clientConfig)
	{
		DiscoveryPinObjectIcon = clientConfig.Bind<bool>("Client", "DiscoveryPinObjectIcon", true, "Replace the standard pin icon on discovery pins with the item sprite from ObjectDB. Client-only — does not affect other players.");
		DiscoveryPinText = clientConfig.Bind<bool>("Client", "DiscoveryPinText", true, "Show the abbreviation label on auto-discovery pins. Set to false to show only the pin icon. Client-only — does not affect other players.");
	}

	public static void BindServerEntries(ConfigFile serverConfig)
	{
		Enabled = serverConfig.Bind<bool>("Server.Discovery", "Enabled", true, "Master switch for the automatic discovery pin system.");
		QuantitySearchRadius = serverConfig.Bind<float>("Server.Discovery", "QuantitySearchRadius", 16f, "Radius (centered on the discovered object) searched for nearby objects of the same type. The count is appended to the pin label, e.g. 'RB 4'. Also defines the cluster boundary — objects within this radius share one pin. Keep larger than the per-category DiscoveryRadius.");
		BindAbbreviations(serverConfig);
	}

	public static void WireEvents(ConfigFile config)
	{
		config.SettingChanged += delegate
		{
			if (WorldObjectConfig.UsingServerConfig)
			{
				PushDiscoveryConfigToServer();
			}
			else
			{
				Reload();
			}
		};
	}

	private static void BindAbbreviations(ConfigFile serverConfig)
	{
		(string, string)[] array = new(string, string)[111]
		{
			("Raspberry", "Rb"),
			("Blueberry", "Bb"),
			("Mushroom", "Mu"),
			("Thistle", "Th"),
			("Cloudberry", "Cb"),
			("Barley", "Br"),
			("Flax", "Fl"),
			("Onion", "On"),
			("Carrot", "Ca"),
			("Turnip", "Tu"),
			("Chitin", "Ch"),
			("Crystal", "Cr"),
			("SurtlingCore", "Su"),
			("BlackCore", "Bc"),
			("JotunPuff", "JP"),
			("Magecap", "Mc"),
			("Dandelion", "Dan"),
			("Flint", "Fli"),
			("BogIron", "BI"),
			("DragonEgg", "DEgg"),
			("Tar", "Tar"),
			("Fiddlehead", "Fern"),
			("RoyalJelly", "RJly"),
			("Sulfur", "Sulf"),
			("VoltureEgg", "VEgg"),
			("Ashstone", "Ash"),
			("MoltenCore", "MCor"),
			("Charredskull", "CSku"),
			("Copper", "Cu"),
			("Tin", "Sn"),
			("Silver", "Ag"),
			("Iron", "Fe"),
			("Obsidian", "Ob"),
			("Flametal", "Fm"),
			("Meteorite", "Met"),
			("BlackMarble", "BkM"),
			("Stone", "Stn"),
			("SunkenCrypt", "SCrypt"),
			("Crypt", "Crypt"),
			("MountainCave", "MCave"),
			("TrollCave", "TCave"),
			("MudPile", "Mud"),
			("Vendor", "Shop"),
			("DrakeNest", "DNest"),
			("TarPit", "Tar"),
			("Henge", "Henge"),
			("Eikthyr", "Eik"),
			("GDKing", "Elder"),
			("Bonemass", "Bone"),
			("DragonQueen", "Moder"),
			("GoblinKing", "Yag"),
			("SeekerQueen", "Queen"),
			("Fader", "Fader"),
			("FrostCave", "FCave"),
			("InfestedMine", "IMine"),
			("Hildir", "Hild"),
			("BogWitch", "Witch"),
			("GoblinCamp", "GCamp"),
			("WoodFarm", "WFarm"),
			("DvergrTown", "Dtown"),
			("ShipWreck", "Ship"),
			("Runestone", "Rune"),
			("GiantSkull", "GSku"),
			("Beehive", "BHive"),
			("Greyling", "Grl"),
			("Boar", "Boar"),
			("Deer", "Deer"),
			("Neck", "Neck"),
			("Chicken", "Hen"),
			("Hare", "Hare"),
			("GreydwarfElite", "GdwE"),
			("GreydwarfShaman", "GdwS"),
			("Greydwarf", "Gdw"),
			("Troll", "Trl"),
			("Skeleton", "Skel"),
			("DraugrElite", "DrE"),
			("Draugr", "Dr"),
			("Leech", "Lch"),
			("Blob", "Blob"),
			("Wraith", "Wra"),
			("Surtling", "Srt"),
			("Abomination", "Abom"),
			("Ghost", "Gst"),
			("Wolf", "Wolf"),
			("Drake", "Drk"),
			("Stone Golem", "Glm"),
			("Fenring", "Fen"),
			("Cultist", "Cult"),
			("Bat", "Bat"),
			("FulingBerserker", "FulB"),
			("FulingShaman", "FulS"),
			("FulingArcher", "FulA"),
			("Fuling", "Ful"),
			("Deathsquito", "Dsq"),
			("Lox", "Lox"),
			("SeekerBrood", "SkrB"),
			("Seeker", "Skr"),
			("Gjall", "Gjl"),
			("Tick", "Tick"),
			("DvergrMage", "DvrM"),
			("Dvergr", "Dvr"),
			("Ulv", "Ulv"),
			("Charred", "Chr"),
			("Morgen", "Mrg"),
			("Asksvin", "Ask"),
			("Volture", "Vlt"),
			("Unbjorn", "Ubj"),
			("Kvastur", "Kvas"),
			("Serpent", "Spt"),
			("Leviathan", "Lev"),
			("Fish", "Fish")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			ConfigEntry<string> value = serverConfig.Bind<string>("Server.Discovery.Abbreviations", item, item2, "Pin-label abbreviation used when a '" + item + "' object is discovered.");
			AbbreviationEntries[item] = value;
		}
	}

	public static void EnsureAbbreviationEntry(string keyword, string suggestedAbbr, ConfigFile serverConfig)
	{
		if (!string.IsNullOrEmpty(keyword) && !AbbreviationEntries.ContainsKey(keyword))
		{
			string text = ((!string.IsNullOrWhiteSpace(suggestedAbbr)) ? suggestedAbbr : ((keyword.Length <= 6) ? keyword : keyword.Substring(0, 6)));
			ConfigEntry<string> val = serverConfig.Bind<string>("Server.Discovery.Abbreviations", keyword, text, "Pin-label abbreviation used when a '" + keyword + "' object is discovered.");
			AbbreviationEntries[keyword] = val;
			Abbreviations[keyword] = val.Value;
		}
	}

	public static string GetKeywordForLabel(string pinLabel)
	{
		foreach (KeyValuePair<string, string> abbreviation in Abbreviations)
		{
			string value = abbreviation.Value;
			if (string.Equals(pinLabel, value, StringComparison.OrdinalIgnoreCase) || pinLabel.StartsWith(value + " ", StringComparison.OrdinalIgnoreCase))
			{
				return abbreviation.Key;
			}
		}
		return null;
	}

	public static void ApplyDiscoveryIcon(PinData mmPin, string keyword)
	{
		if (mmPin != null && DiscoveryPinObjectIcon.Value && RadarIconLoader.TryGetIcon(keyword, out var sprite))
		{
			mmPin.m_icon = sprite;
		}
	}

	public static void Reload()
	{
		if (_reloading)
		{
			return;
		}
		_reloading = true;
		try
		{
			ReloadImpl();
		}
		finally
		{
			_reloading = false;
		}
	}

	private static void ReloadImpl()
	{
		if (QuantitySearchRadius.Value > 150f)
		{
			QuantitySearchRadius.Value = 150f;
		}
		WorldObjectConfig.Reload();
		if (!UsingServerConfig)
		{
			ActiveEnabled = Enabled.Value;
			ActiveQuantitySearchRadius = QuantitySearchRadius.Value;
			Abbreviations.Clear();
			foreach (KeyValuePair<string, ConfigEntry<string>> abbreviationEntry in AbbreviationEntries)
			{
				Abbreviations[abbreviationEntry.Key] = abbreviationEntry.Value.Value;
			}
		}
		if (!ModSettings.ModActive || !ModSettings.IsServer() || !((Object)(object)ZNetProxy.ZNetInstance != (Object)null) || !(Traverse.Create((object)ZNetProxy.ZNetInstance).Field("m_peers").GetValue() is List<ZNetPeer> list))
		{
			return;
		}
		foreach (ZNetPeer item in list)
		{
			if (item.IsReady())
			{
				item.m_rpc.Invoke("OM_ServerDiscoveryConfig", new object[1] { PackDiscoveryConfig() });
			}
		}
	}

	public static string GetAbbreviation(string keyword)
	{
		if (Abbreviations.TryGetValue(keyword, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		if (keyword.Length <= 6)
		{
			return keyword;
		}
		return keyword.Substring(0, 6);
	}

	public static string MatchKeyword(string objectName, string[] keywords)
	{
		foreach (string text in keywords)
		{
			if (objectName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return text;
			}
		}
		return null;
	}

	internal static string[] ParseKeywordList(string csv)
	{
		return (from s in csv.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)
			select s.Trim() into s
			where s.Length > 0
			select s).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	public static ZPackage PackDiscoveryConfig()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(Enabled.Value);
		val.Write(QuantitySearchRadius.Value);
		WorldObjectConfig.PackCategoryData(val);
		val.Write(AbbreviationEntries.Count);
		foreach (KeyValuePair<string, ConfigEntry<string>> abbreviationEntry in AbbreviationEntries)
		{
			val.Write(abbreviationEntry.Key);
			val.Write(abbreviationEntry.Value.Value);
		}
		val.SetPos(0);
		return val;
	}

	private static void PushDiscoveryConfigToServer()
	{
		ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
		if (serverRPC != null)
		{
			serverRPC.Invoke("OM_AdminUpdateDiscoveryConfig", new object[1] { PackDiscoveryConfig() });
		}
	}

	public static void ApplyPackedConfigToEntries(ZPackage z)
	{
		Enabled.Value = z.ReadBool();
		QuantitySearchRadius.Value = z.ReadSingle();
		WorldObjectConfig.UnpackCategoryDataIntoEntries(z);
		int num = z.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string key = z.ReadString();
			string value = z.ReadString();
			if (AbbreviationEntries.TryGetValue(key, out var value2))
			{
				value2.Value = value;
			}
		}
	}

	public static void OnServerDiscoveryConfig(ZRpc rpc, ZPackage z)
	{
		z.SetPos(0);
		if (!ModSettings.IsServer())
		{
			UsingServerConfig = true;
		}
		ActiveEnabled = z.ReadBool();
		ActiveQuantitySearchRadius = z.ReadSingle();
		WorldObjectConfig.ApplyCategoryData(z);
		int num = z.ReadInt();
		Abbreviations.Clear();
		for (int i = 0; i < num; i++)
		{
			Abbreviations[z.ReadString()] = z.ReadString();
		}
	}

	public static void ResetToLocalConfig()
	{
		UsingServerConfig = false;
		WorldObjectConfig.ResetToLocalConfig();
		Reload();
	}
}
public class ProximityWatcher : MonoBehaviour
{
	private string _keyword;

	private float _detectionRadius;

	private bool _inRange;

	private float _updateTimer;

	private float _lastCountTime = float.MinValue;

	private int _lastCount;

	public string Keyword => _keyword;

	public void Initialize(string keyword, float detectionRadius)
	{
		_keyword = keyword;
		_detectionRadius = detectionRadius;
		_updateTimer = Random.Range(0f, ModSettings.ActiveUpdateThrottleInterval);
	}

	private void Update()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive || !AutoPinConfig.ActiveEnabled)
		{
			return;
		}
		_updateTimer -= Time.deltaTime;
		if (_updateTimer > 0f)
		{
			return;
		}
		_updateTimer = ModSettings.ActiveUpdateThrottleInterval;
		Player localPlayer = Player.m_localPlayer;
		if ((Object)(object)localPlayer == (Object)null)
		{
			return;
		}
		Vector3 val = ((Component)localPlayer).transform.position - ((Component)this).transform.position;
		if (((Vector3)(ref val)).sqrMagnitude > _detectionRadius * _detectionRadius)
		{
			_inRange = false;
		}
		else
		{
			if (_inRange || !MapStateRepository.InitialPinsReceived)
			{
				return;
			}
			_inRange = true;
			SharedPin sharedPin = AutoPinPlacer.FindNearbyPin(((Component)this).transform.position, _keyword);
			int num;
			if (sharedPin != null && Time.time - _lastCountTime < ModSettings.ActiveQuantityRecountInterval)
			{
				num = _lastCount;
			}
			else
			{
				num = (_lastCount = AutoPinPlacer.CountNearbyObjects(sharedPin?.Pos ?? ((Component)this).transform.position, _keyword, AutoPinConfig.ActiveQuantitySearchRadius));
				_lastCountTime = Time.time;
			}
			string abbreviation = AutoPinConfig.GetAbbreviation(_keyword);
			string text = ((num > 1) ? $"{abbreviation} {num}" : abbreviation);
			if (sharedPin != null)
			{
				if (sharedPin.Name != text)
				{
					AutoPinPlacer.UpdateDiscoveryPin(sharedPin, text, _keyword);
				}
			}
			else
			{
				AutoPinPlacer.PlaceDiscoveryPin(((Component)this).transform.position, text, _keyword);
			}
		}
	}
}
public static class AutoPinPlacer
{
	private static bool _pinIndexDirty = true;

	private static readonly Dictionary<string, List<SharedPin>> _pinsByAbbr = new Dictionary<string, List<SharedPin>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Collider[] _overlapBuffer = (Collider[])(object)new Collider[512];

	private static readonly HashSet<GameObject> _nearbyRootsBuffer = new HashSet<GameObject>();

	public static SharedPin FindNearbyPin(Vector3 position, string keyword)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (_pinIndexDirty)
		{
			RebuildPinIndex();
		}
		string abbreviation = AutoPinConfig.GetAbbreviation(keyword);
		float num = AutoPinConfig.ActiveQuantitySearchRadius * AutoPinConfig.ActiveQuantitySearchRadius;
		if (!_pinsByAbbr.TryGetValue(abbreviation, out var value))
		{
			return null;
		}
		SharedPin result = null;
		float num2 = float.MaxValue;
		foreach (SharedPin item in value)
		{
			Vector3 val = item.Pos - position;
			float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
			if (sqrMagnitude < num && sqrMagnitude < num2)
			{
				result = item;
				num2 = sqrMagnitude;
			}
		}
		return result;
	}

	public static void UpdateDiscoveryPin(SharedPin existing, string newLabel, string keyword)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (ModSettings.ModActive)
		{
			if ((Object)(object)MinimapProxy.Instance != (Object)null)
			{
				RemoveMinimapPin(existing);
			}
			MapPinSynchronizer.RemovePinFromServer(existing);
		}
		else
		{
			MapStateRepository.ClientPins.Remove(existing);
			InvalidatePinIndex();
			if ((Object)(object)MinimapProxy.Instance != (Object)null)
			{
				RemoveMinimapPin(existing);
			}
		}
		PlaceDiscoveryPin(existing.Pos, newLabel, keyword);
	}

	private static void RemoveMinimapPin(SharedPin pin)
	{
		if (!(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list))
		{
			return;
		}
		foreach (PinData item in list)
		{
			if (MapStateRepository.ArePinsEqual(pin, item))
			{
				Traverse.Create((object)MinimapProxy.Instance).Method("RemovePin", new object[1] { item }).GetValue();
				break;
			}
		}
	}

	public static void InvalidatePinIndex()
	{
		_pinIndexDirty = true;
	}

	private static void RebuildPinIndex()
	{
		_pinsByAbbr.Clear();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (!string.IsNullOrEmpty(clientPin.Name))
			{
				int num = clientPin.Name.IndexOf(' ');
				string key = ((num >= 0) ? clientPin.Name.Substring(0, num) : clientPin.Name);
				if (!_pinsByAbbr.TryGetValue(key, out var value))
				{
					value = (_pinsByAbbr[key] = new List<SharedPin>());
				}
				value.Add(clientPin);
			}
		}
		_pinIndexDirty = false;
	}

	public static int CountNearbyObjects(Vector3 origin, string keyword, float radius)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		string internalIdForCreature = WorldObjectConfig.GetInternalIdForCreature(keyword);
		int num = Physics.OverlapSphereNonAlloc(origin, radius, _overlapBuffer);
		_nearbyRootsBuffer.Clear();
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = ((Component)((Component)_overlapBuffer[i]).transform.root).gameObject;
			if (((Object)gameObject).name.IndexOf(internalIdForCreature, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				_nearbyRootsBuffer.Add(gameObject);
			}
		}
		return Mathf.Max(1, _nearbyRootsBuffer.Count);
	}

	public static void PlaceDiscoveryPin(Vector3 position, string label, string keyword)
	{
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)MinimapProxy.Instance == (Object)null)
		{
			return;
		}
		bool flag = WorldObjectConfig.IsKeywordDiscoveryVisible(keyword);
		if (ModSettings.ModActive)
		{
			MapPinSynchronizer.SendPinToServer(new PinData
			{
				m_name = label,
				m_pos = position,
				m_type = (PinType)3,
				m_checked = false,
				m_ownerID = 0L
			}, isDiscovery: true);
			if (flag)
			{
				AutoPinConfig.ApplyDiscoveryIcon(MinimapProxy.AddPin(MinimapProxy.Instance, position, (PinType)3, AutoPinConfig.DiscoveryPinText.Value ? label : "", save: false, isChecked: false, 0L, new PlatformUserID("")), keyword);
			}
			return;
		}
		MapStateRepository.ClientPins.Add(new SharedPin
		{
			Name = label,
			Pos = position,
			Type = (PinType)3,
			Checked = false,
			OwnerId = "auto"
		});
		InvalidatePinIndex();
		if (flag)
		{
			AutoPinConfig.ApplyDiscoveryIcon(MinimapProxy.AddPin(MinimapProxy.Instance, position, (PinType)3, AutoPinConfig.DiscoveryPinText.Value ? label : "", save: true, isChecked: false, 0L, new PlatformUserID("")), keyword);
		}
	}

	public static void SetNearbyPinChecked(Vector3 position, string keyword, bool state)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		SharedPin sharedPin = FindNearbyPin(position, keyword);
		if (sharedPin == null || sharedPin.Checked == state)
		{
			return;
		}
		sharedPin.Checked = state;
		if ((Object)(object)MinimapProxy.Instance != (Object)null && Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list)
		{
			foreach (PinData item in list)
			{
				if (MapStateRepository.ArePinsEqual(sharedPin, item))
				{
					item.m_checked = state;
					break;
				}
			}
		}
		MapPinSynchronizer.CheckPinOnServer(sharedPin, state);
	}

	private static bool LabelMatchesAbbreviation(string pinLabel, string abbr)
	{
		if (!string.Equals(pinLabel, abbr, StringComparison.OrdinalIgnoreCase))
		{
			return pinLabel.StartsWith(abbr + " ", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}
}
public static class WorldObjectPatches
{
	[HarmonyPatch(typeof(ObjectDB), "Awake")]
	private class ObjectDBAwakeHook
	{
		private static void Postfix(ObjectDB __instance)
		{
			RadarIconLoader.LoadAll(__instance);
		}
	}

	[HarmonyPatch(typeof(Pickable), "Awake")]
	private class PickableAwakeHook
	{
		private static void Postfix(Pickable __instance)
		{
			if (__instance.CanBePicked())
			{
				string text = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.PickablesDiscovery);
				string text2 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.PickablesRadar);
				if (text != null || text2 != null)
				{
					AttachTrackers(((Component)__instance).gameObject, text, text2, WorldObjectConfig.GetPickableDiscoveryRadius(text ?? text2));
				}
			}
		}
	}

	[HarmonyPatch(typeof(Pickable), "SetPicked")]
	private class PickableSetPickedHook
	{
		private static void Postfix(Pickable __instance, bool picked)
		{
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			string text = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.PickablesDiscovery);
			string text2 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.PickablesRadar);
			if (text == null && text2 == null)
			{
				return;
			}
			if (picked)
			{
				ProximityWatcher component = ((Component)__instance).GetComponent<ProximityWatcher>();
				if ((Object)(object)component != (Object)null)
				{
					Object.Destroy((Object)(object)component);
				}
				RadarPinComponent component2 = ((Component)__instance).GetComponent<RadarPinComponent>();
				if ((Object)(object)component2 != (Object)null)
				{
					Object.Destroy((Object)(object)component2);
				}
				if (text != null && AutoPinConfig.ActiveEnabled)
				{
					AutoPinPlacer.SetNearbyPinChecked(((Component)__instance).transform.position, text, state: true);
				}
			}
			else
			{
				if (text != null && AutoPinConfig.ActiveEnabled)
				{
					AutoPinPlacer.SetNearbyPinChecked(((Component)__instance).transform.position, text, state: false);
				}
				AttachTrackers(((Component)__instance).gameObject, text, text2, WorldObjectConfig.GetPickableDiscoveryRadius(text ?? text2));
			}
		}
	}

	[HarmonyPatch(typeof(Destructible), "Awake")]
	private class DestructibleAwakeHook
	{
		private static void Postfix(Destructible __instance)
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			string text = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.OresDiscovery);
			string text2 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.OresRadar);
			if (text != null || text2 != null)
			{
				if (text != null && AutoPinConfig.ActiveEnabled)
				{
					AutoPinPlacer.SetNearbyPinChecked(((Component)__instance).transform.position, text, state: false);
				}
				AttachTrackers(((Component)__instance).gameObject, text, text2, WorldObjectConfig.ActiveOresDiscoveryRadius);
				return;
			}
			string text3 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.LocationsDiscovery);
			string text4 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.LocationsRadar);
			if (text3 != null || text4 != null)
			{
				AttachTrackers(((Component)__instance).gameObject, text3, text4, WorldObjectConfig.ActiveLocationsDiscoveryRadius);
			}
		}
	}

	[HarmonyPatch(typeof(Destructible), "Destroy")]
	private class DestructibleDestroyHook
	{
		private static void Prefix(Destructible __instance)
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			if (AutoPinConfig.ActiveEnabled)
			{
				string text = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.OresDiscovery);
				if (text != null)
				{
					AutoPinPlacer.SetNearbyPinChecked(((Component)__instance).transform.position, text, state: true);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Location), "Awake")]
	private class LocationAwakeHook
	{
		private static void Postfix(Location __instance)
		{
			string text = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.LocationsDiscovery);
			string text2 = AutoPinConfig.MatchKeyword(((Object)__instance).name, WorldObjectConfig.LocationsRadar);
			if (text != null || text2 != null)
			{
				AttachTrackers(((Component)__instance).gameObject, text, text2, WorldObjectConfig.ActiveLocationsDiscoveryRadius);
			}
		}
	}

	[HarmonyPatch(typeof(Character), "Awake")]
	private class CharacterAwakeHook
	{
		private static void Postfix(Character __instance)
		{
			string item = WorldObjectConfig.MatchCreatureKeyword(((Object)__instance).name).displayName;
			if (item != null)
			{
				AttachTrackers(((Component)__instance).gameObject, null, item, 0f);
			}
		}
	}

	[HarmonyPatch(typeof(Beehive), "Awake")]
	private class BeehiveAwakeHook
	{
		private static void Postfix(Beehive __instance)
		{
			string item = WorldObjectConfig.MatchCreatureKeyword(((Object)__instance).name).displayName;
			if (item != null)
			{
				AttachTrackers(((Component)__instance).gameObject, null, item, 0f);
			}
		}
	}

	private static void AttachTrackers(GameObject go, string discoveryKw, string radarKw, float discoveryRadius)
	{
		if (discoveryKw != null && (Object)(object)go.GetComponent<ProximityWatcher>() == (Object)null && AutoPinConfig.ActiveEnabled)
		{
			go.AddComponent<ProximityWatcher>().Initialize(discoveryKw, discoveryRadius);
		}
		if (radarKw != null && (Object)(object)go.GetComponent<RadarPinComponent>() == (Object)null && RadarConfig.ActiveEnabled)
		{
			go.AddComponent<RadarPinComponent>().Initialize(radarKw);
		}
	}

	public static void ReScanForKeywordChanges()
	{
		Pickable[] array = Object.FindObjectsByType<Pickable>((FindObjectsSortMode)0);
		foreach (Pickable val in array)
		{
			if (val.CanBePicked())
			{
				string text = AutoPinConfig.MatchKeyword(((Object)val).name, WorldObjectConfig.PickablesDiscovery);
				string text2 = AutoPinConfig.MatchKeyword(((Object)val).name, WorldObjectConfig.PickablesRadar);
				if (text != null || text2 != null)
				{
					AttachTrackers(((Component)val).gameObject, text, text2, WorldObjectConfig.GetPickableDiscoveryRadius(text ?? text2));
				}
			}
		}
		Destructible[] array2 = Object.FindObjectsByType<Destructible>((FindObjectsSortMode)0);
		foreach (Destructible val2 in array2)
		{
			string text3 = AutoPinConfig.MatchKeyword(((Object)val2).name, WorldObjectConfig.OresDiscovery);
			string text4 = AutoPinConfig.MatchKeyword(((Object)val2).name, WorldObjectConfig.OresRadar);
			if (text3 != null || text4 != null)
			{
				AttachTrackers(((Component)val2).gameObject, text3, text4, WorldObjectConfig.ActiveOresDiscoveryRadius);
			}
		}
		Location[] array3 = Object.FindObjectsByType<Location>((FindObjectsSortMode)0);
		foreach (Location val3 in array3)
		{
			string text5 = AutoPinConfig.MatchKeyword(((Object)val3).name, WorldObjectConfig.LocationsDiscovery);
			string text6 = AutoPinConfig.MatchKeyword(((Object)val3).name, WorldObjectConfig.LocationsRadar);
			if (text5 != null || text6 != null)
			{
				AttachTrackers(((Component)val3).gameObject, text5, text6, WorldObjectConfig.ActiveLocationsDiscoveryRadius);
			}
		}
		Character[] array4 = Object.FindObjectsByType<Character>((FindObjectsSortMode)0);
		foreach (Character val4 in array4)
		{
			string item = WorldObjectConfig.MatchCreatureKeyword(((Object)val4).name).displayName;
			if (item != null)
			{
				AttachTrackers(((Component)val4).gameObject, null, item, 0f);
			}
		}
		Beehive[] array5 = Object.FindObjectsByType<Beehive>((FindObjectsSortMode)0);
		foreach (Beehive val5 in array5)
		{
			string item2 = WorldObjectConfig.MatchCreatureKeyword(((Object)val5).name).displayName;
			if (item2 != null)
			{
				AttachTrackers(((Component)val5).gameObject, null, item2, 0f);
			}
		}
		ProximityWatcher[] array6 = Object.FindObjectsByType<ProximityWatcher>((FindObjectsSortMode)0);
		foreach (ProximityWatcher proximityWatcher in array6)
		{
			if (!IsKeywordDiscoveryTracked(proximityWatcher.Keyword) || !AutoPinConfig.ActiveEnabled)
			{
				Object.Destroy((Object)(object)proximityWatcher);
			}
		}
		RadarPinComponent[] array7 = Object.FindObjectsByType<RadarPinComponent>((FindObjectsSortMode)0);
		foreach (RadarPinComponent radarPinComponent in array7)
		{
			if (!IsKeywordRadarTracked(radarPinComponent.Keyword) || !RadarConfig.ActiveEnabled)
			{
				Object.Destroy((Object)(object)radarPinComponent);
			}
		}
	}

	private static bool IsKeywordDiscoveryTracked(string keyword)
	{
		return WorldObjectConfig.DiscoveryKeywordSet.Contains(keyword);
	}

	private static bool IsKeywordRadarTracked(string keyword)
	{
		return WorldObjectConfig.RadarKeywordSet.Contains(keyword);
	}
}
public static class AdminCommands
{
	[HarmonyPatch(typeof(Terminal), "InitTerminal")]
	private static class TerminalInitPatch
	{
		[CompilerGenerated]
		private sealed class <>c__DisplayClass0_0
		{
			public Vector3 pos;

			public float radius;

			internal bool <Postfix>b__4(SharedPin p)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				return Utils.DistanceXZ(p.Pos, pos) <= radius;
			}
		}

		[Serializable]
		[CompilerGenerated]
		private sealed class <>c
		{
			public static readonly <>c <>9 = new <>c();

			public static ConsoleEvent <>9__0_0;

			public static ConsoleEvent <>9__0_1;

			public static ConsoleEvent <>9__0_2;

			public static ConsoleEvent <>9__0_3;

			internal void <Postfix>b__0_0(ConsoleEventArgs args)
			{
				//IL_0062: Unknown result type (might be due to invalid IL or missing references)
				//IL_0067: Unknown result type (might be due to invalid IL or missing references)
				//IL_0083: Unknown result type (might be due to invalid IL or missing references)
				//IL_0089: Expected O, but got Unknown
				//IL_008b: Unknown result type (might be due to invalid IL or missing references)
				<>c__DisplayClass0_0 CS$<>8__locals0 = new <>c__DisplayClass0_0();
				if (args.Length < 2 || !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out CS$<>8__locals0.radius) || CS$<>8__locals0.radius <= 0f)
				{
					args.Context.AddString("Usage: om_clearradiuspins <radius>");
					return;
				}
				Player localPlayer = Player.m_localPlayer;
				if ((Object)(object)localPlayer == (Object)null)
				{
					return;
				}
				CS$<>8__locals0.pos = ((Component)localPlayer).transform.position;
				int num = MapStateRepository.ClientPins.Count((SharedPin p) => Utils.DistanceXZ(p.Pos, CS$<>8__locals0.pos) <= CS$<>8__locals0.radius);
				ZPackage val = new ZPackage();
				val.Write(CS$<>8__locals0.pos);
				val.Write(CS$<>8__locals0.radius);
				val.SetPos(0);
				if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
				{
					OnAdminClearArea(null, val);
				}
				else
				{
					ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
					if (serverRPC != null)
					{
						serverRPC.Invoke("OM_AdminClearArea", new object[1] { val });
					}
				}
				args.Context.AddString((num > 0) ? $"OneMapToRuleThemAll: removed {num} pin(s) within {CS$<>8__locals0.radius}m of your position." : $"OneMapToRuleThemAll: no pins found within {CS$<>8__locals0.radius}m of your position.");
			}

			internal void <Postfix>b__0_1(ConsoleEventArgs args)
			{
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Expected O, but got Unknown
				if (args.Length < 3)
				{
					args.Context.AddString("Usage: om_renamepinabbreviations <old_abbr> <new_abbr>");
					return;
				}
				string text = args[1];
				string text2 = args[2];
				ZPackage val = new ZPackage();
				val.Write(text);
				val.Write(text2);
				val.SetPos(0);
				if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
				{
					OnAdminRenameAbbr(null, val);
				}
				else
				{
					ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
					if (serverRPC != null)
					{
						serverRPC.Invoke("OM_AdminRenameAbbr", new object[1] { val });
					}
				}
				args.Context.AddString("OneMapToRuleThemAll: renaming auto pins from '" + text + "' to '" + text2 + "'.");
			}

			internal void <Postfix>b__0_2(ConsoleEventArgs args)
			{
				//IL_000d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Expected O, but got Unknown
				//IL_0036: Unknown result type (might be due to invalid IL or missing references)
				//IL_003c: Expected O, but got Unknown
				if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
				{
					OnAdminClearAll(null, new ZPackage());
				}
				else
				{
					ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
					if (serverRPC != null)
					{
						serverRPC.Invoke("OM_AdminClearAll", new object[1] { (object)new ZPackage() });
					}
				}
				args.Context.AddString("OneMapToRuleThemAll: removing all pins from the map.");
			}

			internal void <Postfix>b__0_3(ConsoleEventArgs args)
			{
				if ((Object)(object)MinimapProxy.Instance == (Object)null)
				{
					args.Context.AddString("OneMapToRuleThemAll: minimap not ready.");
					return;
				}
				if (!(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list))
				{
					args.Context.AddString("OneMapToRuleThemAll: pin list unavailable.");
					return;
				}
				bool value = AutoPinConfig.DiscoveryPinObjectIcon.Value;
				int num = 0;
				foreach (SharedPin clientPin in MapStateRepository.ClientPins)
				{
					if (clientPin.OwnerId != "auto")
					{
						continue;
					}
					string keywordForLabel = AutoPinConfig.GetKeywordForLabel(clientPin.Name);
					if (keywordForLabel == null)
					{
						continue;
					}
					foreach (PinData item in list)
					{
						if (MapStateRepository.ArePinsEqual(clientPin, item))
						{
							if (value && RadarIconLoader.TryGetIcon(keywordForLabel, out var sprite))
							{
								item.m_icon = sprite;
							}
							num++;
							break;
						}
					}
				}
				Traverse.Create((object)MinimapProxy.Instance).Method("UpdatePins", Array.Empty<object>()).GetValue();
				args.Context.AddString($"OneMapToRuleThemAll: refreshed icons on {num} discovery pin(s).");
			}
		}

		private static void Postfix()
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Expected O, but got Unknown
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Expected O, but got Unknown
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Expected O, but got Unknown
			object obj = <>c.<>9__0_0;
			if (obj == null)
			{
				ConsoleEvent val = delegate(ConsoleEventArgs args)
				{
					//IL_0062: Unknown result type (might be due to invalid IL or missing references)
					//IL_0067: Unknown result type (might be due to invalid IL or missing references)
					//IL_0083: Unknown result type (might be due to invalid IL or missing references)
					//IL_0089: Expected O, but got Unknown
					//IL_008b: Unknown result type (might be due to invalid IL or missing references)
					if (args.Length < 2 || !float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var radius) || radius <= 0f)
					{
						args.Context.AddString("Usage: om_clearradiuspins <radius>");
					}
					else
					{
						Player localPlayer = Player.m_localPlayer;
						if (!((Object)(object)localPlayer == (Object)null))
						{
							Vector3 pos = ((Component)localPlayer).transform.position;
							int num2 = MapStateRepository.ClientPins.Count((SharedPin p) => Utils.DistanceXZ(p.Pos, pos) <= radius);
							ZPackage val6 = new ZPackage();
							val6.Write(pos);
							val6.Write(radius);
							val6.SetPos(0);
							if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
							{
								OnAdminClearArea(null, val6);
							}
							else
							{
								ZRpc serverRPC3 = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
								if (serverRPC3 != null)
								{
									serverRPC3.Invoke("OM_AdminClearArea", new object[1] { val6 });
								}
							}
							args.Context.AddString((num2 > 0) ? $"OneMapToRuleThemAll: removed {num2} pin(s) within {radius}m of your position." : $"OneMapToRuleThemAll: no pins found within {radius}m of your position.");
						}
					}
				};
				<>c.<>9__0_0 = val;
				obj = (object)val;
			}
			new ConsoleCommand("om_clearradiuspins", "<radius> — remove all OneMapToRuleThemAll pins within radius meters of your position", (ConsoleEvent)obj, false, false, false, false, false, (ConsoleOptionsFetcher)null, false, false, false);
			object obj2 = <>c.<>9__0_1;
			if (obj2 == null)
			{
				ConsoleEvent val2 = delegate(ConsoleEventArgs args)
				{
					//IL_002a: Unknown result type (might be due to invalid IL or missing references)
					//IL_0030: Expected O, but got Unknown
					if (args.Length < 3)
					{
						args.Context.AddString("Usage: om_renamepinabbreviations <old_abbr> <new_abbr>");
					}
					else
					{
						string text = args[1];
						string text2 = args[2];
						ZPackage val5 = new ZPackage();
						val5.Write(text);
						val5.Write(text2);
						val5.SetPos(0);
						if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
						{
							OnAdminRenameAbbr(null, val5);
						}
						else
						{
							ZRpc serverRPC2 = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
							if (serverRPC2 != null)
							{
								serverRPC2.Invoke("OM_AdminRenameAbbr", new object[1] { val5 });
							}
						}
						args.Context.AddString("OneMapToRuleThemAll: renaming auto pins from '" + text + "' to '" + text2 + "'.");
					}
				};
				<>c.<>9__0_1 = val2;
				obj2 = (object)val2;
			}
			new ConsoleCommand("om_renamepinabbreviations", "<old_abbr> <new_abbr> — rename all auto-discovery pins that use old_abbr to new_abbr", (ConsoleEvent)obj2, false, false, false, false, false, (ConsoleOptionsFetcher)null, false, false, false);
			object obj3 = <>c.<>9__0_2;
			if (obj3 == null)
			{
				ConsoleEvent val3 = delegate(ConsoleEventArgs args)
				{
					//IL_000d: Unknown result type (might be due to invalid IL or missing references)
					//IL_0017: Expected O, but got Unknown
					//IL_0036: Unknown result type (might be due to invalid IL or missing references)
					//IL_003c: Expected O, but got Unknown
					if (ZNetProxy.IsServer(ZNetProxy.ZNetInstance))
					{
						OnAdminClearAll(null, new ZPackage());
					}
					else
					{
						ZRpc serverRPC = ZNetProxy.GetServerRPC(ZNetProxy.ZNetInstance);
						if (serverRPC != null)
						{
							serverRPC.Invoke("OM_AdminClearAll", new object[1] { (object)new ZPackage() });
						}
					}
					args.Context.AddString("OneMapToRuleThemAll: removing all pins from the map.");
				};
				<>c.<>9__0_2 = val3;
				obj3 = (object)val3;
			}
			new ConsoleCommand("om_clearallpins", "Remove all OneMapToRuleThemAll pins from the map", (ConsoleEvent)obj3, false, false, false, false, false, (ConsoleOptionsFetcher)null, false, false, false);
			object obj4 = <>c.<>9__0_3;
			if (obj4 == null)
			{
				ConsoleEvent val4 = delegate(ConsoleEventArgs args)
				{
					if ((Object)(object)MinimapProxy.Instance == (Object)null)
					{
						args.Context.AddString("OneMapToRuleThemAll: minimap not ready.");
					}
					else if (!(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> list))
					{
						args.Context.AddString("OneMapToRuleThemAll: pin list unavailable.");
					}
					else
					{
						bool value = AutoPinConfig.DiscoveryPinObjectIcon.Value;
						int num = 0;
						foreach (SharedPin clientPin in MapStateRepository.ClientPins)
						{
							if (!(clientPin.OwnerId != "auto"))
							{
								string keywordForLabel = AutoPinConfig.GetKeywordForLabel(clientPin.Name);
								if (keywordForLabel != null)
								{
									foreach (PinData item in list)
									{
										if (MapStateRepository.ArePinsEqual(clientPin, item))
										{
											if (value && RadarIconLoader.TryGetIcon(keywordForLabel, out var sprite))
											{
												item.m_icon = sprite;
											}
											num++;
											break;
										}
									}
								}
							}
						}
						Traverse.Create((object)MinimapProxy.Instance).Method("UpdatePins", Array.Empty<object>()).GetValue();
						args.Context.AddString($"OneMapToRuleThemAll: refreshed icons on {num} discovery pin(s).");
					}
				};
				<>c.<>9__0_3 = val4;
				obj4 = (object)val4;
			}
			new ConsoleCommand("om_refreshicons", "Re-apply object icons to all discovery pins (run if icons get out of sync)", (ConsoleEvent)obj4, false, false, false, false, false, (ConsoleOptionsFetcher)null, false, false, false);
		}
	}

	public static void OnAdminUpdateDiscoveryConfig(ZRpc client, ZPackage data)
	{
		if (IsAdmin(client))
		{
			data.SetPos(0);
			AutoPinConfig.ApplyPackedConfigToEntries(data);
		}
	}

	public static void OnAdminClearArea(ZRpc client, ZPackage data)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (!IsAdmin(client))
		{
			return;
		}
		data.SetPos(0);
		Vector3 pos = data.ReadVector3();
		float radius = data.ReadSingle();
		if (ModSettings.ModActive)
		{
			foreach (SharedPin item in (from p in MapStateRepository.GetPins()
				where p.OwnerId == "auto" && Utils.DistanceXZ(p.Pos, pos) <= radius
				select p).ToList())
			{
				RemoveAndBroadcast(item);
			}
			return;
		}
		RemoveMinimapPinsInArea(pos, radius);
	}

	public static void OnAdminRenameAbbr(ZRpc client, ZPackage data)
	{
		if (!IsAdmin(client))
		{
			return;
		}
		data.SetPos(0);
		string oldAbbr = data.ReadString();
		string text = data.ReadString();
		foreach (SharedPin item in (from p in MapStateRepository.GetPins()
			where p.OwnerId == "auto" && LabelMatchesAbbreviation(p.Name, oldAbbr)
			select p).ToList())
		{
			string newLabel = text + item.Name.Substring(oldAbbr.Length);
			RenameAndBroadcast(item, newLabel);
		}
	}

	public static void OnAdminClearAll(ZRpc client, ZPackage data)
	{
		ManualLogSource log = ModSettings.Log;
		if (log != null)
		{
			log.LogInfo((object)string.Format("[ClearAll] received. ModActive={0} client={1}", ModSettings.ModActive, (client == null) ? "null(local)" : "remote"));
		}
		if (!IsAdmin(client))
		{
			ManualLogSource log2 = ModSettings.Log;
			if (log2 != null)
			{
				log2.LogInfo((object)"[ClearAll] rejected: not admin");
			}
			return;
		}
		if (ModSettings.ModActive)
		{
			foreach (SharedPin item in (from p in MapStateRepository.GetPins()
				where p.OwnerId == "auto"
				select p).ToList())
			{
				RemoveAndBroadcast(item);
			}
			return;
		}
		RemoveAllMinimapPins();
	}

	private static void RenameAndBroadcast(SharedPin oldPin, string newLabel)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		SharedPin pin = new SharedPin
		{
			Name = newLabel,
			Pos = oldPin.Pos,
			Type = oldPin.Type,
			Checked = oldPin.Checked,
			OwnerId = oldPin.OwnerId
		};
		MapStateRepository.RemovePin(oldPin);
		MapStateRepository.AddPin(pin);
		if (Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() is List<ZNetPeer> list)
		{
			foreach (ZNetPeer item in list)
			{
				if (item.IsReady())
				{
					item.m_rpc.Invoke("OM_ServerRemovePin", new object[1] { MapStateRepository.PackPin(oldPin) });
					item.m_rpc.Invoke("OM_ServerAddPin", new object[1] { MapStateRepository.PackPin(pin) });
				}
			}
		}
		MapPinSynchronizer.OnServerRemovePin(null, MapStateRepository.PackPin(oldPin));
		MapPinSynchronizer.OnServerAddPin(null, MapStateRepository.PackPin(pin));
	}

	private static void RemoveAndBroadcast(SharedPin pin)
	{
		MapStateRepository.RemovePin(pin);
		if (Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() is List<ZNetPeer> list)
		{
			foreach (ZNetPeer item in list)
			{
				if (item.IsReady())
				{
					item.m_rpc.Invoke("OM_ServerRemovePin", new object[1] { MapStateRepository.PackPin(pin) });
				}
			}
		}
		MapPinSynchronizer.OnServerRemovePin(null, MapStateRepository.PackPin(pin));
	}

	private static void RemoveMinimapPinsInArea(Vector3 pos, float radius)
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)MinimapProxy.Instance == (Object)null || !(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> source))
		{
			return;
		}
		HashSet<PinData> hashSet = new HashSet<PinData>();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (!(clientPin.OwnerId != "auto") && !(Utils.DistanceXZ(clientPin.Pos, pos) > radius))
			{
				PinData val = ((IEnumerable<PinData>)source).FirstOrDefault((Func<PinData, bool>)((PinData p) => MapStateRepository.ArePinsEqual(clientPin, p)));
				if (val != null)
				{
					hashSet.Add(val);
				}
			}
		}
		foreach (PinData item in hashSet)
		{
			MinimapProxy.RemovePin(MinimapProxy.Instance, item);
		}
	}

	private static void RemoveAllMinimapPins()
	{
		if ((Object)(object)MinimapProxy.Instance == (Object)null || !(Traverse.Create((object)MinimapProxy.Instance).Field("m_pins").GetValue() is List<PinData> source))
		{
			return;
		}
		HashSet<PinData> hashSet = new HashSet<PinData>();
		foreach (SharedPin clientPin in MapStateRepository.ClientPins)
		{
			if (!(clientPin.OwnerId != "auto"))
			{
				PinData val = ((IEnumerable<PinData>)source).FirstOrDefault((Func<PinData, bool>)((PinData p) => MapStateRepository.ArePinsEqual(clientPin, p)));
				if (val != null)
				{
					hashSet.Add(val);
				}
			}
		}
		foreach (PinData item in hashSet)
		{
			MinimapProxy.RemovePin(MinimapProxy.Instance, item);
		}
	}

	private static bool LabelMatchesAbbreviation(string pinLabel, string abbr)
	{
		if (!string.Equals(pinLabel, abbr, StringComparison.OrdinalIgnoreCase))
		{
			return pinLabel.StartsWith(abbr + " ", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	internal static bool IsAdmin(ZRpc client)
	{
		if (client == null)
		{
			return true;
		}
		ZNetPeer val = (Traverse.Create((object)ZNet.instance).Field("m_peers").GetValue() as List<ZNetPeer>)?.FirstOrDefault((Func<ZNetPeer, bool>)((ZNetPeer p) => p.m_rpc == client));
		if (val == null)
		{
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogInfo((object)"[IsAdmin] peer not found in m_peers");
			}
			return false;
		}
		ISocket socket = val.m_socket;
		string hostName = ((socket != null) ? socket.GetHostName() : null);
		ManualLogSource log2 = ModSettings.Log;
		if (log2 != null)
		{
			log2.LogInfo((object)("[IsAdmin] hostName=" + hostName));
		}
		if (string.IsNullOrEmpty(hostName))
		{
			return false;
		}
		object value = Traverse.Create((object)ZNet.instance).Field("m_adminList").GetValue();
		if (value == null)
		{
			ManualLogSource log3 = ModSettings.Log;
			if (log3 != null)
			{
				log3.LogInfo((object)"[IsAdmin] m_adminList is null");
			}
			return false;
		}
		bool flag = false;
		MethodInfo methodInfo = AccessTools.Method(value.GetType(), "Contains", (Type[])null, (Type[])null);
		if (methodInfo != null)
		{
			flag = (bool)(methodInfo.Invoke(value, new object[1] { hostName }) ?? ((object)false));
			ManualLogSource log4 = ModSettings.Log;
			if (log4 != null)
			{
				log4.LogInfo((object)$"[IsAdmin] AccessTools Contains({hostName})={flag}");
			}
			return flag;
		}
		if (Traverse.Create(value).Field("m_list").GetValue() is List<string> list)
		{
			flag = list.Any((string id) => string.Equals(id.Trim(), hostName.Trim(), StringComparison.OrdinalIgnoreCase));
			ManualLogSource log5 = ModSettings.Log;
			if (log5 != null)
			{
				log5.LogInfo((object)string.Format("[IsAdmin] m_list fallback Contains({0})={1}  list=[{2}]", hostName, flag, string.Join(",", list)));
			}
			return flag;
		}
		ManualLogSource log6 = ModSettings.Log;
		if (log6 != null)
		{
			log6.LogInfo((object)("[IsAdmin] could not check admin list for " + hostName));
		}
		return false;
	}
}
public static class RadarConfig
{
	public static ConfigEntry<bool> Enabled;

	public static ConfigEntry<bool> PinText;

	public static ConfigEntry<float> ClusterRadius;

	public static ConfigEntry<float> CritterClusterRadius;

	public static bool ActiveEnabled;

	public static bool ActivePinText;

	public static float ActiveClusterRadius;

	public static float ActiveCritterClusterRadius;

	public static bool UsingServerConfig;

	public static void BindClientEntry(ConfigFile clientConfig)
	{
		PinText = clientConfig.Bind<bool>("Client", "RadarPinText", false, "Show the abbreviation label on radar pins. Set to false to show the item icon only.");
		CritterClusterRadius = clientConfig.Bind<float>("Client", "RadarCritterClusterRadius", 16f, "Radius in meters within which nearby critter radar pins are merged into a single pin with a count label. Set to 0 for one pin per critter (no clustering). Range: 0–150. This is client-side and independent of the server ClusterRadius setting.");
	}

	public static void BindServerEntries(ConfigFile serverConfig)
	{
		Enabled = serverConfig.Bind<bool>("Server.Radar", "Enabled", true, "Master switch for the radar system. Radar pins are transient and client-only — they appear while within detection range and vanish when you walk away.");
		ClusterRadius = serverConfig.Bind<float>("Server.Radar", "ClusterRadius", 16f, "Radius in meters within which nearby radar objects are merged into a single pin with a count label. Set to 0 to disable clustering.");
	}

	public static void WireEvents(ConfigFile config)
	{
		config.SettingChanged += delegate(object _, SettingChangedEventArgs e)
		{
			if (e.ChangedSetting == PinText)
			{
				ActivePinText = PinText.Value;
				RadarClusterManager.RefreshAllLabels();
			}
			else
			{
				Reload();
				if (e.ChangedSetting == ClusterRadius || e.ChangedSetting == CritterClusterRadius)
				{
					RadarClusterManager.ClearAndReset();
				}
			}
		};
		Reload();
	}

	public static void Reload()
	{
		if (ClusterRadius.Value < 0f)
		{
			ClusterRadius.Value = 0f;
		}
		if (CritterClusterRadius.Value < 0f)
		{
			CritterClusterRadius.Value = 0f;
		}
		if (CritterClusterRadius.Value > 150f)
		{
			CritterClusterRadius.Value = 150f;
		}
		if (!UsingServerConfig)
		{
			ActiveEnabled = Enabled.Value;
			ActiveClusterRadius = ClusterRadius.Value;
		}
		ActiveCritterClusterRadius = CritterClusterRadius.Value;
		ActivePinText = PinText.Value;
		if (!ModSettings.ModActive || !ModSettings.IsServer() || !((Object)(object)ZNetProxy.ZNetInstance != (Object)null) || !(Traverse.Create((object)ZNetProxy.ZNetInstance).Field("m_peers").GetValue() is List<ZNetPeer> list))
		{
			return;
		}
		foreach (ZNetPeer item in list)
		{
			if (item.IsReady())
			{
				item.m_rpc.Invoke("OM_ServerRadarConfig", new object[1] { PackRadarConfig() });
			}
		}
	}

	public static void ResetToLocalConfig()
	{
		UsingServerConfig = false;
		Reload();
	}

	public static ZPackage PackRadarConfig()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		ZPackage val = new ZPackage();
		val.Write(Enabled.Value);
		val.Write(ClusterRadius.Value);
		val.SetPos(0);
		return val;
	}

	public static void OnServerRadarConfig(ZRpc rpc, ZPackage z)
	{
		z.SetPos(0);
		if (!ModSettings.IsServer())
		{
			UsingServerConfig = true;
		}
		ActiveEnabled = z.ReadBool();
		ActiveClusterRadius = z.ReadSingle();
		RadarClusterManager.ClearAndReset();
		WorldObjectPatches.ReScanForKeywordChanges();
	}
}
public static class RadarIconLoader
{
	private static readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> PickableItemNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Raspberry", "Raspberry" },
		{ "Blueberry", "Blueberries" },
		{ "Mushroom", "Mushroom" },
		{ "Thistle", "Thistle" },
		{ "Cloudberry", "Cloudberry" },
		{ "Barley", "Barley" },
		{ "Flax", "Flax" },
		{ "Onion", "Onion" },
		{ "Carrot", "Carrot" },
		{ "Turnip", "Turnip" },
		{ "Chitin", "Chitin" },
		{ "Crystal", "Crystal" },
		{ "SurtlingCore", "SurtlingCore" },
		{ "BlackCore", "BlackCore" },
		{ "Dandelion", "Dandelion" },
		{ "Flint", "Flint" },
		{ "JotunPuff", "MushroomJotunpuff" },
		{ "Magecap", "MushroomMagecap" },
		{ "Totem", "GoblinTotem" }
	};

	private static readonly Dictionary<string, string> OreItemNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "Copper", "CopperOre" },
		{ "Tin", "TinOre" },
		{ "Iron", "IronOre" },
		{ "Silver", "SilverOre" },
		{ "Obsidian", "Obsidian" }
	};

	private static readonly Dictionary<string, string> LocationItemNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "SunkenCrypt", "IronScrap" },
		{ "Crypt", "SurtlingCore" },
		{ "MountainCave", "Crystal" },
		{ "Vendor", "HelmetYule" },
		{ "MudPile", "IronScrap" },
		{ "TrollCave", "TrophyFrostTroll" },
		{ "DrakeNest", "DragonEgg" },
		{ "TarPit", "Tar" },
		{ "Henge", "ShieldBlackmetal" }
	};

	private static readonly Dictionary<string, string> _customIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public static void RegisterCustomIcons(Dictionary<string, string> map)
	{
		_customIconMap.Clear();
		foreach (KeyValuePair<string, string> item in map)
		{
			_customIconMap[item.Key] = item.Value;
		}
	}

	public static void LoadAll(ObjectDB db)
	{
		_icons.Clear();
		LoadIcons(db, PickableItemNames);
		LoadIcons(db, OreItemNames);
		LoadIcons(db, LocationItemNames);
		LoadIcons(db, WorldObjectConfig.GetCreatureIconMap());
		LoadIcons(db, _customIconMap);
	}

	private static void LoadIcons(ObjectDB db, Dictionary<string, string> nameMap)
	{
		foreach (KeyValuePair<string, string> item in nameMap)
		{
			string key = item.Key;
			string value = item.Value;
			GameObject itemPrefab = db.GetItemPrefab(StringExtensionMethods.GetStableHashCode(value));
			if ((Object)(object)itemPrefab == (Object)null)
			{
				continue;
			}
			ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
			if (!((Object)(object)component == (Object)null))
			{
				Sprite val = component.m_itemData?.m_shared?.m_icons?.FirstOrDefault();
				if (!((Object)(object)val == (Object)null))
				{
					_icons[key] = val;
				}
			}
		}
	}

	public static bool TryGetIcon(string keyword, out Sprite sprite)
	{
		return _icons.TryGetValue(keyword, out sprite);
	}
}
public class RadarCluster
{
	public string Keyword;

	public Vector3 Anchor;

	public List<RadarPinComponent> Members = new List<RadarPinComponent>();

	public PinData PinData;
}
public static class RadarClusterManager
{
	private static readonly Dictionary<string, List<RadarCluster>> _clusters = new Dictionary<string, List<RadarCluster>>(StringComparer.OrdinalIgnoreCase);

	public static bool Register(RadarPinComponent component, string keyword, Vector3 pos, float clusterRadius)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (!_clusters.TryGetValue(keyword, out var value))
		{
			value = new List<RadarCluster>();
			_clusters[keyword] = value;
		}
		RadarCluster radarCluster = null;
		if (clusterRadius > 0f)
		{
			foreach (RadarCluster item in value)
			{
				if (Vector3.Distance(item.Anchor, pos) <= clusterRadius)
				{
					radarCluster = item;
					break;
				}
			}
		}
		bool flag = radarCluster == null;
		if (flag)
		{
			radarCluster = new RadarCluster
			{
				Keyword = keyword,
				Anchor = pos
			};
		}
		if (radarCluster.PinData == null)
		{
			string abbreviation = AutoPinConfig.GetAbbreviation(keyword);
			string name = (RadarConfig.ActivePinText ? abbreviation : "");
			radarCluster.PinData = MinimapProxy.AddPin(MinimapProxy.Instance, radarCluster.Anchor, (PinType)3, name, save: false, isChecked: false, 0L, new PlatformUserID(""));
			if (radarCluster.PinData != null && RadarIconLoader.TryGetIcon(keyword, out var sprite))
			{
				radarCluster.PinData.m_icon = sprite;
			}
		}
		if (radarCluster.PinData == null)
		{
			if (flag && value.Count == 0)
			{
				_clusters.Remove(keyword);
			}
			return false;
		}
		if (flag)
		{
			value.Add(radarCluster);
		}
		radarCluster.Members.Add(component);
		UpdateClusterLabel(radarCluster);
		return true;
	}

	public static void Unregister(RadarPinComponent component, string keyword)
	{
		if (!_clusters.TryGetValue(keyword, out var value))
		{
			return;
		}
		RadarCluster radarCluster = null;
		foreach (RadarCluster item in value)
		{
			if (item.Members.Contains(component))
			{
				radarCluster = item;
				break;
			}
		}
		if (radarCluster == null)
		{
			return;
		}
		radarCluster.Members.Remove(component);
		if (radarCluster.Members.Count == 0)
		{
			if (radarCluster.PinData != null && (Object)(object)MinimapProxy.Instance != (Object)null)
			{
				try
				{
					MinimapProxy.RemovePin(MinimapProxy.Instance, radarCluster.PinData);
				}
				catch
				{
				}
			}
			value.Remove(radarCluster);
			if (value.Count == 0)
			{
				_clusters.Remove(keyword);
			}
		}
		else
		{
			UpdateClusterLabel(radarCluster);
		}
	}

	public static void UpdatePosition(RadarPinComponent component, string keyword, Vector3 pos)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		if (!_clusters.TryGetValue(keyword, out var value))
		{
			return;
		}
		foreach (RadarCluster item in value)
		{
			if (!item.Members.Contains(component))
			{
				continue;
			}
			if (item.Members.Count == 1)
			{
				item.Anchor = pos;
				if (item.PinData != null)
				{
					item.PinData.m_pos = pos;
				}
				break;
			}
			Vector3 val = Vector3.zero;
			foreach (RadarPinComponent member in item.Members)
			{
				val += ((Component)member).transform.position;
			}
			item.Anchor = val / (float)item.Members.Count;
			if (item.PinData != null)
			{
				item.PinData.m_pos = item.Anchor;
			}
			break;
		}
	}

	private static void UpdateClusterLabel(RadarCluster cluster)
	{
		if (cluster.PinData != null)
		{
			int count = cluster.Members.Count;
			string abbreviation = AutoPinConfig.GetAbbreviation(cluster.Keyword);
			string name = ((!RadarConfig.ActivePinText) ? "" : ((count == 1) ? abbreviation : $"{abbreviation} {count}"));
			cluster.PinData.m_name = name;
		}
	}

	public static void RefreshAllLabels()
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)MinimapProxy.Instance == (Object)null)
		{
			return;
		}
		foreach (List<RadarCluster> value in _clusters.Values)
		{
			foreach (RadarCluster item in value)
			{
				if (item.PinData != null)
				{
					try
					{
						MinimapProxy.RemovePin(MinimapProxy.Instance, item.PinData);
					}
					catch
					{
					}
					string abbreviation = AutoPinConfig.GetAbbreviation(item.Keyword);
					int count = item.Members.Count;
					string name = ((!RadarConfig.ActivePinText) ? "" : ((count == 1) ? abbreviation : $"{abbreviation} {count}"));
					item.PinData = MinimapProxy.AddPin(MinimapProxy.Instance, item.Anchor, (PinType)3, name, save: false, isChecked: false, 0L, new PlatformUserID(""));
					if (item.PinData != null && RadarIconLoader.TryGetIcon(item.Keyword, out var sprite))
					{
						item.PinData.m_icon = sprite;
					}
				}
			}
		}
	}

	public static void Clear()
	{
		foreach (List<RadarCluster> value in _clusters.Values)
		{
			foreach (RadarCluster item in value)
			{
				if (item.PinData != null && (Object)(object)MinimapProxy.Instance != (Object)null)
				{
					try
					{
						MinimapProxy.RemovePin(MinimapProxy.Instance, item.PinData);
					}
					catch
					{
					}
				}
			}
		}
		_clusters.Clear();
	}

	public static void ClearAndReset()
	{
		List<RadarPinComponent> list = new List<RadarPinComponent>();
		foreach (List<RadarCluster> value in _clusters.Values)
		{
			foreach (RadarCluster item in value)
			{
				list.AddRange(item.Members);
			}
		}
		Clear();
		foreach (RadarPinComponent item2 in list)
		{
			item2.ClearRegistration();
		}
	}
}
public class RadarPinComponent : MonoBehaviour
{
	private string _keyword;

	private bool _registered;

	private bool _isCritter;

	private Func<float> _getCategoryRadius;

	private float _updateTimer;

	public string Keyword => _keyword;

	public void ClearRegistration()
	{
		_registered = false;
	}

	public void Initialize(string keyword)
	{
		_keyword = keyword;
		_updateTimer = Random.Range(0f, ModSettings.ActiveUpdateThrottleInterval);
		string[] radarKeywords;
		foreach (PickableCategory pickableCategory in WorldObjectConfig.PickableCategories)
		{
			radarKeywords = pickableCategory.RadarKeywords;
			for (int i = 0; i < radarKeywords.Length; i++)
			{
				if (string.Equals(radarKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
				{
					PickableCategory capturedPickCat = pickableCategory;
					_getCategoryRadius = () => capturedPickCat.ActiveRadarRadius;
					return;
				}
			}
		}
		radarKeywords = WorldObjectConfig.OresRadar;
		for (int i = 0; i < radarKeywords.Length; i++)
		{
			if (string.Equals(radarKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
			{
				_getCategoryRadius = () => WorldObjectConfig.ActiveOresRadarRadius;
				return;
			}
		}
		radarKeywords = WorldObjectConfig.LocationsRadar;
		for (int i = 0; i < radarKeywords.Length; i++)
		{
			if (string.Equals(radarKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
			{
				_getCategoryRadius = () => WorldObjectConfig.ActiveLocationsRadarRadius;
				return;
			}
		}
		foreach (BiomeCritterCategory biomeCritterCategory in WorldObjectConfig.BiomeCritterCategories)
		{
			radarKeywords = biomeCritterCategory.Keywords;
			for (int i = 0; i < radarKeywords.Length; i++)
			{
				if (string.Equals(radarKeywords[i], keyword, StringComparison.OrdinalIgnoreCase))
				{
					_isCritter = true;
					BiomeCritterCategory capturedCat = biomeCritterCategory;
					_getCategoryRadius = () => capturedCat.ActiveRadarRadius;
					return;
				}
			}
		}
		_getCategoryRadius = () => 6f;
	}

	private void Update()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (!ModSettings.ModActive || !RadarConfig.ActiveEnabled)
		{
			if (_registered)
			{
				RadarClusterManager.Unregister(this, _keyword);
				_registered = false;
			}
			return;
		}
		_updateTimer -= Time.deltaTime;
		if (_updateTimer > 0f)
		{
			return;
		}
		_updateTimer = ModSettings.ActiveUpdateThrottleInterval;
		Player localPlayer = Player.m_localPlayer;
		if (!((Object)(object)localPlayer == (Object)null))
		{
			Vector3 val = ((Component)localPlayer).transform.position - ((Component)this).transform.position;
			float sqrMagnitude = ((Vector3)(ref val)).sqrMagnitude;
			bool flag = WorldObjectConfig.IsKeywordRadarVisible(_keyword);
			float num = _getCategoryRadius();
			float num2 = num + 2f;
			if (flag && sqrMagnitude < num * num && !_registered && (Object)(object)MinimapProxy.Instance != (Object)null)
			{
				float clusterRadius = (_isCritter ? RadarConfig.ActiveCritterClusterRadius : RadarConfig.ActiveClusterRadius);
				_registered = RadarClusterManager.Register(this, _keyword, ((Component)this).transform.position, clusterRadius);
			}
			else if (_registered && (!flag || sqrMagnitude > num2 * num2))
			{
				RadarClusterManager.Unregister(this, _keyword);
				_registered = false;
			}
			else if (_registered)
			{
				RadarClusterManager.UpdatePosition(this, _keyword, ((Component)this).transform.position);
			}
		}
	}

	private void OnDestroy()
	{
		if (_registered)
		{
			RadarClusterManager.Unregister(this, _keyword);
			_registered = false;
		}
	}
}
public static class CustomPrefabLoader
{
	private static ConfigFile _serverConfig;

	private static ConfigFile _clientConfig;

	private static readonly Dictionary<string, string> _pendingCustomIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> _customPickableBiomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> _customCritterCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static void Reload()
	{
		if (_serverConfig != null && _clientConfig != null)
		{
			LoadAll(_serverConfig, _clientConfig);
			WorldObjectConfig.Reload();
			if ((Object)(object)ObjectDB.instance != (Object)null)
			{
				RadarIconLoader.LoadAll(ObjectDB.instance);
			}
		}
	}

	public static void LoadAll(ConfigFile serverConfig, ConfigFile clientConfig)
	{
		_serverConfig = serverConfig;
		_clientConfig = clientConfig;
		_pendingCustomIcons.Clear();
		WorldObjectConfig.CustomOresKeywords.Clear();
		WorldObjectConfig.CustomLocationsKeywords.Clear();
		HashSet<string> prevPickable = new HashSet<string>(_customPickableBiomes, StringComparer.OrdinalIgnoreCase);
		HashSet<string> prevCritter = new HashSet<string>(_customCritterCategories, StringComparer.OrdinalIgnoreCase);
		_customPickableBiomes.Clear();
		_customCritterCategories.Clear();
		WorldObjectConfig.PickableCategories.RemoveAll((PickableCategory c) => prevPickable.Contains(c.CategoryName));
		WorldObjectConfig.BiomeCritterCategories.RemoveAll((BiomeCritterCategory c) => prevCritter.Contains(c.CategoryName));
		string modFolder = ModSettings.ModFolder;
		if (!Directory.Exists(modFolder))
		{
			return;
		}
		string[] files = Directory.GetFiles(modFolder, "*.customprefabs.txt");
		for (int i = 0; i < files.Length; i++)
		{
			LoadFile(files[i]);
		}
		foreach (string biome in _customPickableBiomes)
		{
			PickableCategory pickableCategory = WorldObjectConfig.PickableCategories.FirstOrDefault((PickableCategory c) => string.Equals(c.CategoryName, biome, StringComparison.OrdinalIgnoreCase));
			if (pickableCategory != null)
			{
				string text = string.Join(",", pickableCategory.DiscoveryKeywords);
				string text2 = string.Join(",", pickableCategory.RadarKeywords);
				if (pickableCategory.TrackedDiscoveryEntry != null && pickableCategory.TrackedDiscoveryEntry.Value != text)
				{
					pickableCategory.TrackedDiscoveryEntry.Value = text;
				}
				if (pickableCategory.TrackedRadarEntry != null && pickableCategory.TrackedRadarEntry.Value != text2)
				{
					pickableCategory.TrackedRadarEntry.Value = text2;
				}
			}
		}
		RadarIconLoader.RegisterCustomIcons(_pendingCustomIcons);
	}

	private static void LoadFile(string path)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			int num = text.IndexOf('#');
			if (num >= 0)
			{
				text = text.Substring(0, num).Trim();
			}
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			string[] array2 = text.Split(new char[1] { ':' });
			if (array2.Length >= 3)
			{
				switch (array2[0].Trim().ToLowerInvariant())
				{
				case "pickable":
					LoadPickable(array2, fileNameWithoutExtension);
					break;
				case "ore":
					LoadOre(array2);
					break;
				case "location":
					LoadLocation(array2);
					break;
				case "critter":
					LoadCritter(array2);
					break;
				}
			}
		}
	}

	private static void LoadPickable(string[] parts, string modTag)
	{
		if (parts.Length < 4)
		{
			return;
		}
		string biome = parts[1].Trim();
		if (string.Equals(biome, "Modded", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(modTag))
		{
			biome = modTag;
		}
		string text = parts[2].Trim();
		string displayName = parts[3].Trim();
		bool num = parts.Length < 5 || ParseEnabled(parts[4]);
		string value = ((parts.Length >= 6) ? parts[5].Trim() : "");
		if (num && !string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(biome))
		{
			PickableCategory pickableCategory = WorldObjectConfig.PickableCategories.FirstOrDefault((PickableCategory c) => string.Equals(c.CategoryName, biome, StringComparison.OrdinalIgnoreCase));
			if (pickableCategory == null)
			{
				pickableCategory = new PickableCategory
				{
					CategoryName = biome
				};
				pickableCategory.DiscoveryRadiusEntry = _serverConfig.Bind<float>("Server.CustomPickables." + biome, "DiscoveryRadius", 6f, "How close (meters) the player must approach a " + biome + " pickable to trigger a discovery pin. Maximum 150.");
				pickableCategory.RadarRadiusEntry = _serverConfig.Bind<float>("Server.CustomPickables." + biome, "RadarRadius", 50f, "How close (meters) the player must be to a " + biome + " pickable for its radar pin to appear. Maximum 150.");
				pickableCategory.ActiveDiscoveryRadius = pickableCategory.DiscoveryRadiusEntry.Value;
				pickableCategory.ActiveRadarRadius = pickableCategory.RadarRadiusEntry.Value;
				pickableCategory.DiscoveryKeywords = Array.Empty<string>();
				pickableCategory.RadarKeywords = Array.Empty<string>();
				pickableCategory.TrackedDiscoveryEntry = _serverConfig.Bind<string>("Server.CustomPickables." + biome, "TrackedDiscovery", text, "Comma-separated keywords for " + biome + " pickables (custom).");
				pickableCategory.TrackedRadarEntry = _serverConfig.Bind<string>("Server.CustomPickables." + biome, "TrackedRadar", text, "Comma-separated keywords for " + biome + " pickables radar (custom).");
				WorldObjectConfig.PickableCategories.Add(pickableCategory);
				_customPickableBiomes.Add(biome);
			}
			pickableCategory.DiscoveryKeywords = pickableCategory.DiscoveryKeywords.Append(text).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
			pickableCategory.RadarKeywords = pickableCategory.RadarKeywords.Append(text).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
			AutoPinConfig.EnsureAbbreviationEntry(text, DefaultAbbr(text, displayName), _serverConfig);
			if (!string.IsNullOrEmpty(value))
			{
				_pendingCustomIcons[text] = value;
			}
		}
	}

	private static void LoadOre(string[] parts)
	{
		if (parts.Length < 3)
		{
			return;
		}
		string text = parts[1].Trim();
		string displayName = parts[2].Trim();
		bool num = parts.Length < 4 || ParseEnabled(parts[3]);
		string value = ((parts.Length >= 5) ? parts[4].Trim() : "");
		if (num && !string.IsNullOrEmpty(text))
		{
			WorldObjectConfig.CustomOresKeywords.Add(text);
			AutoPinConfig.EnsureAbbreviationEntry(text, DefaultAbbr(text, displayName), _serverConfig);
			if (!string.IsNullOrEmpty(value))
			{
				_pendingCustomIcons[text] = value;
			}
		}
	}

	private static void LoadLocation(string[] parts)
	{
		if (parts.Length < 3)
		{
			return;
		}
		string text = parts[1].Trim();
		string displayName = parts[2].Trim();
		bool num = parts.Length < 4 || ParseEnabled(parts[3]);
		string value = ((parts.Length >= 5) ? parts[4].Trim() : "");
		if (num && !string.IsNullOrEmpty(text))
		{
			WorldObjectConfig.CustomLocationsKeywords.Add(text);
			AutoPinConfig.EnsureAbbreviationEntry(text, DefaultAbbr(text, displayName), _serverConfig);
			if (!string.IsNullOrEmpty(value))
			{
				_pendingCustomIcons[text] = value;
			}
		}
	}

	private static void LoadCritter(string[] parts)
	{
		if (parts.Length < 4)
		{
			return;
		}
		string catName = parts[1].Trim();
		string displayName = parts[2].Trim();
		string text = parts[3].Trim();
		string item = ((parts.Length >= 5) ? parts[4].Trim() : text);
		bool flag = true;
		if (parts.Length >= 6)
		{
			flag = ParseEnabled(parts[5]);
		}
		else if (parts.Length >= 5 && (parts[4].Trim().ToLowerInvariant() == "true" || parts[4].Trim().ToLowerInvariant() == "false"))
		{
			flag = ParseEnabled(parts[4]);
			item = text;
		}
		if (flag && !string.IsNullOrEmpty(catName) && !string.IsNullOrEmpty(text))
		{
			string derivedKw = WorldObjectConfig.DeriveMatchKeyword(text);
			BiomeCritterCategory biomeCritterCategory = WorldObjectConfig.BiomeCritterCategories.FirstOrDefault((BiomeCritterCategory c) => string.Equals(c.CategoryName, catName, StringComparison.OrdinalIgnoreCase));
			if (biomeCritterCategory == null)
			{
				biomeCritterCategory = new BiomeCritterCategory
				{
					CategoryName = catName
				};
				biomeCritterCategory.ActiveRadarRadius = 50f;
				WorldObjectConfig.BiomeCritterCategories.Add(biomeCritterCategory);
				_customCritterCategories.Add(catName);
			}
			if (!biomeCritterCategory.CreatureDefinitions.Any(((string MatchKeyword, string DisplayName, string IconName) d) => string.Equals(d.MatchKeyword, derivedKw, StringComparison.OrdinalIgnoreCase) && string.Equals(d.DisplayName, displayName, StringComparison.OrdinalIgnoreCase)))
			{
				biomeCritterCategory.CreatureDefinitions.Add((derivedKw, displayName, item));
			}
		}
	}

	private static bool ParseEnabled(string s)
	{
		return !string.Equals(s.Trim(), "false", StringComparison.OrdinalIgnoreCase);
	}

	private static string DefaultAbbr(string keyword, string displayName)
	{
		string[] array = displayName.Split(new char[2] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length >= 2)
		{
			string text = string.Concat(array.Select((string w) => char.ToUpper(w[0])));
			if (text.Length > 5)
			{
				return text.Substring(0, 5);
			}
			return text;
		}
		if (keyword.Length > 6)
		{
			return keyword.Substring(0, 6);
		}
		return keyword;
	}
}
public static class CustomPrefabScanner
{
	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	private class ZNetSceneAwakeHook
	{
		private static void Postfix(ZNetScene __instance)
		{
			if (_scanned)
			{
				return;
			}
			_scanned = true;
			try
			{
				ScanAndGenerate(__instance);
			}
			catch (Exception ex)
			{
				ManualLogSource log = ModSettings.Log;
				if (log != null)
				{
					log.LogWarning((object)("[CustomPrefabScanner] Scan failed: " + ex.Message));
				}
			}
		}
	}

	private static bool _scanned = false;

	private static readonly string[] SkipSuffixes = new string[7] { "_frac", "_old", "_broken", "_LOD", "_sfx", "_fx", "_visual" };

	private static readonly string[] SkipPrefixes = new string[10] { "Spawner_", "LocationProxy", "_", "Player", "fx_", "sfx_", "vfx_", "DungeonGenerator", "EventSystem", "gd_" };

	private static readonly string[] SkipContains = new string[7] { "_Random", "CryptRandom", "CaveRandom", "SunkenCryptRandom", "ForestCryptRandom", "MountainCaveRandom", "DolmenTreasure" };

	private static readonly string[] VanillaSubstrings = new string[88]
	{
		"Raspberry", "Dandelion", "Flint", "Mushroom", "Blueberry", "Thistle", "Carrot", "SurtlingCore", "Turnip", "BogIron",
		"Crystal", "Onion", "DragonEgg", "Cloudberry", "Barley", "Flax", "Tar", "Chitin", "JotunPuff", "Magecap",
		"Fiddlehead", "RoyalJelly", "BlackCore", "Sulfur", "VoltureEgg", "Ashstone", "MoltenCore", "Charredskull", "Copper", "Tin",
		"Iron", "Silver", "Obsidian", "BlackMarble", "Meteorite", "Flametal", "Stone", "Eikthyr", "Hildir", "Runestone",
		"GDKing", "TrollCave", "Vendor", "Bonemass", "SunkenCrypt", "Crypt", "MudPile", "BogWitch", "DragonQueen", "MountainCave",
		"DrakeNest", "FrostCave", "GoblinKing", "TarPit", "Henge", "GoblinCamp", "WoodFarm", "ShipWreck", "SeekerQueen", "InfestedMine",
		"DvergrTown", "Fader", "GiantSkull", "Goblin", "Charred", "Dverg", "Bat", "Bjorn", "Unbjorn", "Dragon",
		"Hatchling", "Hive", "Mistile", "FallenValkyrie", "TrainingDummy", "Branch", "Swordpiece", "Hairstrands", "VineAsh", "VineGreen",
		"HardRock", "LuredWisp", "MeatPile", "MountainRemains", "SmokePuff", "Fishingrod", "Totempole", "Shard"
	};

	private static void ScanAndGenerate(ZNetScene scene)
	{
		Dictionary<string, List<(string, string, string, string, string)>> dictionary = new Dictionary<string, List<(string, string, string, string, string)>>(StringComparer.OrdinalIgnoreCase);
		foreach (GameObject prefab in scene.m_prefabs)
		{
			if ((Object)(object)prefab == (Object)null)
			{
				continue;
			}
			string name = ((Object)prefab).name;
			if (ShouldSkip(name) || IsAlreadyTracked(name))
			{
				continue;
			}
			string text = null;
			string text2 = "Modded";
			if ((Object)(object)prefab.GetComponent<Pickable>() != (Object)null)
			{
				text = "Pickable";
			}
			else if ((Object)(object)prefab.GetComponent<Destructible>() != (Object)null && name.IndexOf("MineRock", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				text = "Ore";
			}
			else if ((Object)(object)prefab.GetComponent<Location>() != (Object)null)
			{
				text = "Location";
			}
			else if ((Object)(object)prefab.GetComponent<Character>() != (Object)null && (Object)(object)prefab.GetComponent<Player>() == (Object)null)
			{
				text = "Critter";
			}
			if (text == null)
			{
				continue;
			}
			string text3 = ExtractModTag(name, prefab);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = "Modded";
			}
			string text4 = DeriveKeyword(name, text);
			if (string.IsNullOrEmpty(text4))
			{
				continue;
			}
			string displayName = GetDisplayName(prefab, text4);
			if (text == "Pickable")
			{
				text2 = GuessBiome(name);
				if (text2 == "Modded")
				{
					text2 = text3;
				}
			}
			if (!dictionary.ContainsKey(text3))
			{
				dictionary[text3] = new List<(string, string, string, string, string)>();
			}
			dictionary[text3].Add((text, text2, text4, displayName, name));
		}
		if (dictionary.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (KeyValuePair<string, List<(string, string, string, string, string)>> item in dictionary)
		{
			string key = item.Key;
			List<(string, string, string, string, string)> value = item.Value;
			string text5 = ResolveFileName(key) + ".customprefabs.txt";
			string text6 = Path.Combine(ModSettings.ModFolder, text5);
			if (!File.Exists(text6))
			{
				WriteFile(text6, key, value);
				flag = true;
				ManualLogSource log = ModSettings.Log;
				if (log != null)
				{
					log.LogInfo((object)$"[CustomPrefabScanner] Generated {text5} with {value.Count} detected entries.");
				}
			}
		}
		if (flag)
		{
			CustomPrefabLoader.Reload();
		}
	}

	private static bool ShouldSkip(string name)
	{
		string[] skipSuffixes = SkipSuffixes;
		foreach (string value in skipSuffixes)
		{
			if (name.EndsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		skipSuffixes = SkipPrefixes;
		foreach (string value2 in skipSuffixes)
		{
			if (name.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		skipSuffixes = SkipContains;
		foreach (string value3 in skipSuffixes)
		{
			if (name.IndexOf(value3, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsAlreadyTracked(string name)
	{
		foreach (string item in WorldObjectConfig.DiscoveryKeywordSet)
		{
			if (name.IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		foreach (string item2 in WorldObjectConfig.RadarKeywordSet)
		{
			if (name.IndexOf(item2, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		string[] vanillaSubstrings = VanillaSubstrings;
		foreach (string value in vanillaSubstrings)
		{
			if (name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string ExtractModTag(string name, GameObject prefab)
	{
		int num = name.LastIndexOf('_');
		if (num >= 0 && num < name.Length - 1)
		{
			string text = name.Substring(num + 1);
			if (text.Length >= 2 && text.Length <= 5 && text.All(char.IsLetter))
			{
				foreach (PluginInfo value in Chainloader.PluginInfos.Values)
				{
					string text2 = value.Metadata.GUID.ToLower();
					string text3 = value.Metadata.Name.ToLower().Replace(" ", "");
					if (text2.Contains(text.ToLower()) || text3.Contains(text.ToLower()))
					{
						return SanitizeName(value.Metadata.Name);
					}
				}
				return text;
			}
		}
		return null;
	}

	private static string DeriveKeyword(string name, string category)
	{
		string text = name;
		if (text.StartsWith("Pickable_", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("Pickable_".Length);
		}
		else if (text.StartsWith("MineRock_", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("MineRock_".Length);
		}
		int num = text.LastIndexOf('_');
		if (num >= 0 && num < text.Length - 1)
		{
			string text2 = text.Substring(num + 1);
			if (text2.Length >= 2 && text2.Length <= 5 && text2.All(char.IsLetter))
			{
				text = text.Substring(0, num);
			}
		}
		text = string.Concat(from part in text.Split(new char[1] { '_' })
			select (part.Length <= 0) ? "" : (char.ToUpper(part[0]) + part.Substring(1)));
		if (text.Length <= 0)
		{
			return null;
		}
		return text;
	}

	private static string GetDisplayName(GameObject prefab, string fallback)
	{
		try
		{
			Type type = Type.GetType("Localization, assembly_valheim");
			if (type != null)
			{
				object obj = type.GetProperty("instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
				if (obj != null)
				{
					string text = "$" + ((Object)prefab).name.ToLower();
					string text2 = type.GetMethod("Localize", new Type[1] { typeof(string) })?.Invoke(obj, new object[1] { text }) as string;
					if (!string.IsNullOrWhiteSpace(text2) && text2 != text)
					{
						return text2;
					}
				}
			}
		}
		catch
		{
		}
		return fallback;
	}

	private static string GuessBiome(string name)
	{
		string text = name.ToLower();
		if (text.Contains("ash") || text.Contains("charred") || text.Contains("flametal"))
		{
			return "AshLands";
		}
		if (text.Contains("mist") || text.Contains("dverg") || text.Contains("seeker"))
		{
			return "Mistlands";
		}
		if (text.Contains("plain") || text.Contains("goblin") || text.Contains("fuling"))
		{
			return "Plains";
		}
		if (text.Contains("mountain") || text.Contains("frost") || text.Contains("drake"))
		{
			return "Mountain";
		}
		if (text.Contains("swamp") || text.Contains("bog") || text.Contains("crypt"))
		{
			return "Swamp";
		}
		if (text.Contains("forest") || text.Contains("troll") || text.Contains("grey"))
		{
			return "BlackForest";
		}
		if (text.Contains("ocean") || text.Contains("fish") || text.Contains("sea"))
		{
			return "Ocean";
		}
		return "Modded";
	}

	private static string ResolveFileName(string tag)
	{
		return SanitizeName(tag);
	}

	private static string SanitizeName(string name)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in name)
		{
			if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	private static void WriteFile(string filePath, string tag, List<(string cat, string biome, string kw, string display, string prefab)> entries)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# " + Path.GetFileName(filePath) + " — Generated by OneMapToRuleThemAll");
		stringBuilder.AppendLine("# Detected mod tag: " + tag);
		stringBuilder.AppendLine("# Edit freely. This file is NOT overwritten on subsequent startups.");
		stringBuilder.AppendLine("#");
		stringBuilder.AppendLine("# FORMAT: Category:Biome:Keyword:DisplayName[:enabled[:iconItem]]");
		stringBuilder.AppendLine("#   Category = Pickable | Ore | Location");
		stringBuilder.AppendLine("#   Biome    = Meadows | BlackForest | Swamp | Mountain | Plains | Ocean | Mistlands | AshLands | (custom)");
		stringBuilder.AppendLine("#   Keyword  = case-insensitive substring matched against prefab names");
		stringBuilder.AppendLine("#   Enabled  = true/false  (default: false for auto-detected entries)");
		stringBuilder.AppendLine("#   iconItem = ObjectDB item prefab name whose sprite is used for the pin icon");
		stringBuilder.AppendLine("#             e.g. Pickable:AshLands:MyOre:My Ore:true:CopperOre");
		stringBuilder.AppendLine("#");
		stringBuilder.AppendLine("# Critter format (same as creatures.txt):");
		stringBuilder.AppendLine("#   Critter:category:displayName:matchKeyword[:iconName][:enabled]");
		stringBuilder.AppendLine("#");
		stringBuilder.AppendLine("# All entries below are DISABLED by default. Review and set enabled=true");
		stringBuilder.AppendLine("# for the ones you want to track. Radii for custom Pickable biomes can");
		stringBuilder.AppendLine("# be adjusted in One_Map_To_Rule_Them_All_Settings.cfg under");
		stringBuilder.AppendLine("# [CustomPickables.{Biome}].");
		stringBuilder.AppendLine("#");
		stringBuilder.AppendLine("# Auto-detected prefabs:");
		foreach (var item6 in from e in entries
			orderby e.cat, e.kw
			select e)
		{
			string item = item6.cat;
			string item2 = item6.biome;
			string item3 = item6.kw;
			string item4 = item6.display;
			string item5 = item6.prefab;
			string text = ((!(item == "Pickable")) ? ((!(item == "Critter")) ? (item + ":" + item3 + ":" + item4 + ":false") : ("Critter:Modded:" + item4 + ":" + item3 + "::false")) : ("Pickable:" + item2 + ":" + item3 + ":" + item4 + ":false"));
			stringBuilder.AppendLine(text + "    # " + item5);
		}
		File.WriteAllText(filePath, stringBuilder.ToString(), Encoding.UTF8);
	}
}
public static class ConfigMigration
{
	private const string OldServerFileName = "One_Map_To_Rule_Them_All_Settings.cfg";

	public static void MigrateIfNeeded(ConfigFile config)
	{
		string text = Path.Combine(Paths.ConfigPath, "One_Map_To_Rule_Them_All_Settings.cfg");
		string configFilePath = config.ConfigFilePath;
		bool flag = File.Exists(text);
		string text2 = (File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "");
		bool flag2 = HasUnprefixedClientSections(text2);
		if (!flag && !flag2)
		{
			return;
		}
		try
		{
			string text3 = (flag2 ? PrefixClientSections(text2) : text2);
			if (flag)
			{
				List<(string, string, string)> list = ParseIni(text);
				if (list.Count > 0)
				{
					StringBuilder stringBuilder = new StringBuilder(text3);
					string text4 = null;
					foreach (var item4 in list)
					{
						string item = item4.Item1;
						string item2 = item4.Item2;
						string item3 = item4.Item3;
						string text5 = "Server." + item;
						if (text5 != text4)
						{
							stringBuilder.AppendLine();
							stringBuilder.AppendLine("[" + text5 + "]");
							text4 = text5;
						}
						stringBuilder.AppendLine(item2 + " = " + item3);
					}
					text3 = stringBuilder.ToString();
				}
				RenameOld(text);
			}
			File.WriteAllText(configFilePath, text3, Encoding.UTF8);
			config.Reload();
			List<string> list2 = new List<string>();
			if (flag2)
			{
				list2.Add("renamed client sections to Client.* prefix");
			}
			if (flag)
			{
				list2.Add("merged One_Map_To_Rule_Them_All_Settings.cfg into Server.* sections");
			}
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogInfo((object)("[ConfigMigration] " + string.Join("; ", list2) + "."));
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log2 = ModSettings.Log;
			if (log2 != null)
			{
				log2.LogWarning((object)("[ConfigMigration] Migration failed: " + ex.Message));
			}
		}
	}

	private static bool HasUnprefixedClientSections(string content)
	{
		string[] array = content.Split(new char[1] { '\n' });
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.StartsWith("[MapPin.") || text.StartsWith("[Radar."))
			{
				return true;
			}
		}
		return false;
	}

	private static string PrefixClientSections(string content)
	{
		string[] array = content.Split(new char[1] { '\n' });
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if ((text.StartsWith("[MapPin.") || text.StartsWith("[Radar.")) && !text.StartsWith("[Client."))
			{
				array[i] = "[Client." + text.Substring(1);
			}
		}
		return string.Join("\n", array);
	}

	private static List<(string section, string key, string value)> ParseIni(string path)
	{
		List<(string, string, string)> list = new List<(string, string, string)>();
		string text = "";
		string[] array = File.ReadAllLines(path);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0 || text2.StartsWith("#"))
			{
				continue;
			}
			if (text2.StartsWith("[") && text2.EndsWith("]"))
			{
				text = text2.Substring(1, text2.Length - 2).Trim();
				continue;
			}
			int num = text2.IndexOf('=');
			if (num >= 1 && !string.IsNullOrEmpty(text))
			{
				string text3 = text2.Substring(0, num).Trim();
				string item = text2.Substring(num + 1).Trim();
				if (!string.IsNullOrEmpty(text3))
				{
					list.Add((text, text3, item));
				}
			}
		}
		return list;
	}

	public static void MigrateFilesToSubfolder()
	{
		string configPath = Paths.ConfigPath;
		string modFolder = ModSettings.ModFolder;
		string text = Path.Combine(configPath, "OneMapToRuleThemAll.creatures.txt");
		string text2 = Path.Combine(modFolder, "OneMapToRuleThemAll.creatures.txt");
		if (File.Exists(text) && !File.Exists(text2))
		{
			File.Move(text, text2);
			ManualLogSource log = ModSettings.Log;
			if (log != null)
			{
				log.LogInfo((object)"[ConfigMigration] Moved creatures.txt to subfolder.");
			}
		}
		string[] files = Directory.GetFiles(configPath, "*.customprefabs.txt");
		foreach (string text3 in files)
		{
			string text4 = Path.Combine(modFolder, Path.GetFileName(text3));
			if (!File.Exists(text4))
			{
				File.Move(text3, text4);
				ManualLogSource log2 = ModSettings.Log;
				if (log2 != null)
				{
					log2.LogInfo((object)("[ConfigMigration] Moved " + Path.GetFileName(text3) + " to subfolder."));
				}
			}
		}
	}

	private static void RenameOld(string oldPath)
	{
		string text = oldPath + ".bak";
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		File.Move(oldPath, text);
	}
}
