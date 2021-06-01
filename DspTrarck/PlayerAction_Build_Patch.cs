using HarmonyLib;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class PlayerAction_Build_Patch
	{
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

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineActive")]
		public static void BuildTool_Click_DetermineActive_Postfix(ref PlayerAction_Build __instance, ref bool __result)
		{
			//Debug.LogFormat("PlayerAction_Build post DetermineActive：{0}", __result);
		}

	}
}
