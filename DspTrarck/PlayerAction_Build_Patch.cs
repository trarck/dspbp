using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class PlayerAction_Build_Patch
	{
		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "Init")]
		public static void PlayerAction_Build_Init_Postfix(ref PlayerAction_Build __instance, ref Player _player)
		{
			BuildTool_BluePrint bpTool = new BuildTool_BluePrint();
			List<BuildTool> tools = new List<BuildTool>(__instance.tools);
			tools.Insert(1,bpTool);
			__instance.tools = tools.ToArray();
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineActive")]
		public static bool PlayerAction_Build_DetermineActive_Prefix(ref PlayerAction_Build __instance, ref bool __result)
		{
			//Debug.LogFormat("PlayerAction_Build pre DetermineActive {0}", TrarckPlugin.Instance.BPBuild);
			var runOriginal = true;

			if (TrarckPlugin.Instance.BPBuild)
			{
				runOriginal = false;
				__result = true;
			}

			return runOriginal;
		}

		//[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineActive")]
		//public static void PlayerAction_Build_DetermineActive_Postfix(ref PlayerAction_Build __instance, ref bool __result)
		//{
		//	Debug.LogFormat("PlayerAction_Build after DetermineActive {0},{1}", TrarckPlugin.Instance.BPBuild,__result);
		//}
	}
}
