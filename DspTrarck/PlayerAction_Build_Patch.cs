using HarmonyLib;
using UnityEngine;

namespace DspTrarck
{
	public class PlayerAction_Build_Patch
	{
		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "DetermineBuildPreviews")]
		public static bool PlayerAction_Build_DetermineBuildPreviews_Prefix(ref PlayerAction_Build __instance)
		{
			var runOriginal = true;

			if (TrarckPlugin.Instance.BPBuild)
			{
				runOriginal = false;
				__instance.waitConfirm = __instance.cursorValid;

				if (__instance.buildPreviews.Count > 0)
				{
					__instance.buildPreviews.Clear();
				}

				TrarckPlugin.Instance.factoryBP.UpdateBuildPosition(__instance.groundSnappedPos);
				foreach (var buildPreview in TrarckPlugin.Instance.factoryBP.buildPreviews)
				{
					__instance.AddBuildPreview(buildPreview);
				}


				__instance.previewPose = Pose.identity;
			}

			return runOriginal;
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "CheckBuildConditions")]
		public static void PlayerAction_Build_CheckBuildConditions_Postfix(ref PlayerAction_Build __instance,ref bool __result)
		{
			if (TrarckPlugin.Instance.BPBuild)
				Debug.LogFormat("CheckBuildConditions:{0}", __result);
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "CreatePrebuilds")]
		public static bool PlayerAction_Build_CreatePrebuilds_Prefix(ref PlayerAction_Build __instance)
		{
			if (TrarckPlugin.Instance.BPBuild)
				Debug.LogFormat("CreatePrebuilds:waitConfirm={0},build down={1},previes={2}", __instance.waitConfirm, VFInput._buildConfirm.onDown, __instance.buildPreviews.Count);
			return true;
		}
	}
}
