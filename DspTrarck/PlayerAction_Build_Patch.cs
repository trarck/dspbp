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
			BuildTool_BluePrint_Build bpToolBuild = new BuildTool_BluePrint_Build();
			List<BuildTool> tools = new List<BuildTool>(__instance.tools);
			tools.Insert(1,bpToolBuild);

			BuildTool_BluePrint_Create bpToolCreate = new BuildTool_BluePrint_Create();
			tools.Insert(1, bpToolCreate);
			TrarckPlugin.Instance.bluePrintCreateTool = bpToolCreate;

			__instance.tools = tools.ToArray();
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineActive")]
		public static bool PlayerAction_Build_DetermineActive_Prefix(ref PlayerAction_Build __instance, ref bool __result)
		{
			//Debug.LogFormat("PlayerAction_Build pre DetermineActive {0}", __result);
			var runOriginal = true;

			if (TrarckPlugin.Instance.isBluePrintMode)
			{
				runOriginal = false;
				__result = true;
			}
			return runOriginal;
		}

		//[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineActive")]
		//public static void PlayerAction_Build_DetermineActive_Postfix(ref PlayerAction_Build __instance, ref bool __result)
		//{
		//	//Debug.LogFormat("PlayerAction_Build after DetermineActive {0},{1},{2}", TrarckPlugin.Instance.isBluePrintMode, __result,__instance.activeTool);

		//}

		//static int i = 0;
		//[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "Close")]
		//public static bool PlayerAction_Build_Close_Prefix(ref PlayerAction_Build __instance)
		//{
		//	//if (++i > 120)
		//	{
		//		Debug.LogFormat("PlayerAction_Build_Close_Prefix {0}", __instance.active);
		//		i= 0;
		//	}

		//	return true;
		//}

		//static int j = 0;
		//[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "EscLogic")]
		//public static bool PlayerAction_Build_EscLogic_Prefix(ref PlayerAction_Build __instance)
		//{
		//	if (++j > 120)
		//	{
		//		Debug.LogFormat("PlayerAction_Build_EscLogic_Prefix {0}", __instance.active);
		//		j = 0;
		//	}
		//	return true;
		//}

		//[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(CommandState), "SetNoneCommand")]
		//public static bool CommandState_SetNoneCommandc_Prefix(ref CommandState __instance)
		//{
		//	try
		//	{
		//		throw new System.Exception("Test");
		//	}
		//	catch(System.Exception e)
		//	{
		//		Debug.Log(e);
		//	}

		//	return true;
		//}
	}
}
