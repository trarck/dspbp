using HarmonyLib;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class BuildTool_Click_Patch
	{
		private static int[] _overlappedIds = new int[4096];

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "DetermineActive")]
		public static void BuildTool_Click_DetermineActive_Postfix(ref BuildTool_Click __instance, ref bool __result)
		{
			//Debug.LogFormat("BuildTool_Click DetermineActive：{0}", __result);
			if (TrarckPlugin.Instance.BPBuild)
			{
				__result = true;
			}
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "UpdateHandItem")]
		public static void BuildTool_Click_UpdateHandItem_Postfix(ref BuildTool_Click __instance, ref bool __result)
		{
			//Debug.LogFormat("UpdateHandItem：{0}",__result);
			if (TrarckPlugin.Instance.BPBuild && !__result)
			{
				__result = true;
			}
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "UpdateRaycast")]
		public static bool BuildTool_Click_UpdateRaycast_Prefix(ref BuildTool_Click __instance)
		{
			__instance.handPrefabDesc = new PrefabDesc();
			return true;

			//__instance.castTerrain = false;
			//__instance.castPlatform = false;
			//__instance.castGround = false;
			//__instance.castGroundPos = Vector3.zero;
			//__instance.castGroundPosSnapped = Vector3.zero;
			//__instance.castObject = false;
			//__instance.castObjectId = 0;
			//__instance.castObjectPos = Vector3.zero;
			//__instance.cursorValid = false;
			//__instance.cursorTarget = Vector3.zero;
			//__instance.multiLevelCovering = false;
			//if (!VFInput.onGUI && VFInput.inScreen)
			//{
			//	int layerMask = 8720;
			//	RaycastHit raycastHit;
			//	__instance.castGround = Physics.Raycast(__instance.mouseRay, out raycastHit, 400f, layerMask, QueryTriggerInteraction.Collide);
			//	if (!__instance.castGround)
			//	{
			//		__instance.castGround = Physics.Raycast(new Ray(__instance.mouseRay.GetPoint(200f), -__instance.mouseRay.direction), out raycastHit, 200f, layerMask, QueryTriggerInteraction.Collide);
			//	}
			//	if (__instance.castGround)
			//	{
			//		Layer layer = (Layer)raycastHit.collider.gameObject.layer;
			//		__instance.castTerrain = (layer == Layer.Terrain || layer == Layer.Water);
			//		__instance.castPlatform = (layer == Layer.Platform);
			//		__instance.castGroundPos = (__instance.controller.cmd.test = (__instance.controller.cmd.target = raycastHit.point));
			//		if (VFInput._ignoreGrid && __instance.handPrefabDesc.minerType == EMinerType.Vein)
			//		{
			//			__instance.castGroundPosSnapped = __instance.castGroundPos.normalized * (__instance.planet.realRadius + 0.2f);
			//		}
			//		else
			//		{
			//			__instance.castGroundPosSnapped = __instance.actionBuild.planetAux.Snap(__instance.castGroundPos, __instance.castTerrain);
			//		}
			//		if (__instance.controller.cmd.stage == 1)
			//		{
			//			__instance.castGroundPosSnapped = __instance.castGroundPosSnapped.normalized * __instance.startGroundPosSnapped.magnitude;
			//		}
			//		__instance.controller.cmd.test = __instance.castGroundPosSnapped;
			//		Vector3 normalized = __instance.castGroundPosSnapped.normalized;
			//		if (Physics.Raycast(new Ray(__instance.castGroundPosSnapped + normalized * 10f, -normalized), out raycastHit, 20f, 8720, QueryTriggerInteraction.Collide))
			//		{
			//			__instance.controller.cmd.test = raycastHit.point;
			//		}
			//		__instance.cursorTarget = __instance.castGroundPosSnapped;
			//		__instance.cursorValid = true;
			//	}
		
			//	//if (__instance.handPrefabDesc.multiLevel)
			//	//{
			//	//	int castAllCount = __instance.controller.cmd.raycast.castAllCount;
			//	//	RaycastData[] castAll = __instance.controller.cmd.raycast.castAll;
			//	//	int num = 0;
			//	//	for (int i = 0; i < castAllCount; i++)
			//	//	{
			//	//		if (castAll[i].objType == EObjectType.Entity || castAll[i].objType == EObjectType.Prebuild)
			//	//		{
			//	//			num = ((castAll[i].objType == EObjectType.Entity) ? castAll[i].objId : (-castAll[i].objId));
			//	//			break;
			//	//		}
			//	//	}
			//	//	if (num != 0 && __instance.GetObjectProtoId(num) == __instance.handItem.ID)
			//	//	{
			//	//		bool flag;
			//	//		int num2;
			//	//		int num3;
			//	//		__instance.factory.ReadObjectConn(num, 15, out flag, out num2, out num3);
			//	//		if (num2 == 0)
			//	//		{
			//	//			__instance.castObject = true;
			//	//			__instance.castObjectId = num;
			//	//			__instance.castObjectPos = __instance.GetObjectPose(num).position;
			//	//		}
			//	//	}
			//	//}
			//	//if (__instance.castObject)
			//	//{
			//	//	__instance.cursorTarget = __instance.castObjectPos;
			//	//	__instance.controller.cmd.test = __instance.castObjectPos;
			//	//	__instance.cursorValid = true;
			//	//	__instance.multiLevelCovering = true;
			//	//}
			//}
			//__instance.controller.cmd.state = (__instance.cursorValid ? 1 : 0);
			//__instance.controller.cmd.target = (__instance.cursorValid ? __instance.cursorTarget : Vector3.zero);

			//return false;
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "DeterminePreviews")]
		public static bool BuildTool_Click_DeterminePreviews_Prefix(ref BuildTool_Click __instance)
		{
			//Debug.LogFormat("DeterminePreviews");
			var runOriginal = true;

			if (TrarckPlugin.Instance.BPBuild)
			{
				runOriginal = false;
				__instance.waitForConfirm = __instance.cursorValid;

				if (VFInput._rotate.onDown)
				{
					__instance.yaw += 90f;
					__instance.yaw = Mathf.Repeat(__instance.yaw, 360f);
					__instance.yaw = Mathf.Round(__instance.yaw / 90f) * 90f;
				}
				if (VFInput._counterRotate.onDown)
				{
					__instance.yaw -= 90f;
					__instance.yaw = Mathf.Repeat(__instance.yaw, 360f);
					__instance.yaw = Mathf.Round(__instance.yaw / 90f) * 90f;
				}

				TrarckPlugin.Instance.factoryBP.UpdateBuildPosition(__instance.castGroundPosSnapped, __instance.yaw);
				__instance.buildPreviews.Clear();
				__instance.buildPreviews.AddRange(TrarckPlugin.Instance.factoryBP.buildPreviews);
			}

			return runOriginal;
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "CheckBuildConditions")]
		public static void BuildTool_Click_CheckBuildConditions_Postfix(ref BuildTool_Click __instance, ref bool __result)
		{
			if (TrarckPlugin.Instance.BPBuild && !__result)
			{
				YHDebug.LogFormat("CheckBuildConditions:{0}", __result);

				Pose pose = default(Pose);

				foreach (var buildPreview in TrarckPlugin.Instance.factoryBP.buildPreviews)
				{
					if (buildPreview.desc.isInserter)
					{
						if (buildPreview.condition != EBuildCondition.TooClose && buildPreview.condition != EBuildCondition.TooFar)
						{
							continue;
						}

						buildPreview.condition = EBuildCondition.Ok;

						pose.position = Vector3.Lerp(buildPreview.lpos, buildPreview.lpos2, 0.5f);
						Vector3 forward = buildPreview.lpos2 - buildPreview.lpos;
						if (forward.sqrMagnitude < 0.0001f)
						{
							forward = Maths.SphericalRotation(buildPreview.lpos, 0f).Forward();
						}

						pose.rotation = Quaternion.LookRotation(forward, buildPreview.lpos.normalized);

						bool inputIsBelt = buildPreview.input != null ? buildPreview.input.desc.isBelt : false;
						bool outputIsBelt = buildPreview.output != null ? buildPreview.output.desc.isBelt : false;

						Vector3 zero = Vector3.zero;
						if (buildPreview.input != null && buildPreview.output != null)
						{
							zero = (inputIsBelt && !outputIsBelt) ? buildPreview.output.lpos :
								((inputIsBelt || !outputIsBelt) ? ((buildPreview.input.lpos + buildPreview.output.lpos) * 0.5f) : buildPreview.input.lpos);
						}
						else if (buildPreview.input == null & buildPreview.output == null)
						{
							zero = (buildPreview.lpos + buildPreview.lpos2) * 0.5f;
						}
						else if (buildPreview.input == null)
						{
							zero = (buildPreview.lpos + buildPreview.output.lpos) * 0.5f;
						}
						else if (buildPreview.output == null)
						{
							zero = (buildPreview.input.lpos + buildPreview.lpos2) * 0.5f;
						}

						float num2 = __instance.player.planetData.aux.mainGrid.CalcSegmentsAcross(zero, buildPreview.lpos, buildPreview.lpos2);
						float num3 = num2;
						float magnitude = forward.magnitude;
						float num4 = 5.5f;
						float num5 = 0.6f;
						float num6 = 3.499f;
						float num7 = 0.88f;

						if (buildPreview.input != null && buildPreview.output != null)
						{
							if (inputIsBelt && outputIsBelt)
							{
								num5 = 0.4f;
								num4 = 5f;
								num6 = 3.2f;
								num7 = 0.8f;
							}
							else if (!inputIsBelt && !outputIsBelt)
							{
								num5 = 0.98f;
								num4 = 7.5f;
								num6 = 3.799f;
								num7 = 1.501f;
								num3 -= 0.3f;
							}
						}
						//Debug.LogFormat("innsert:{0},{1},{2},{3},{4},{5},{6}", magnitude, num2, num3, num4, num5, num6, num7);
						if (magnitude > num4)
						{
							buildPreview.condition = EBuildCondition.TooFar;
						}
						else if (magnitude < num5)
						{
							buildPreview.condition = EBuildCondition.TooClose;
						}
						else if (num2 > num6)
						{
							buildPreview.condition = EBuildCondition.TooFar;
						}
						else
						{
							if (num2 < num7)
							{
								buildPreview.condition = EBuildCondition.TooClose;
							}
							else
							{
								buildPreview.paramCount = Mathf.RoundToInt(Mathf.Clamp(num3, 1f, 3f));
								buildPreview.parameters = new int[buildPreview.paramCount];
								UIInserterBuildTip inserterBuildTip = UIRoot.instance.uiGame.inserterBuildTip;
								inserterBuildTip.gridLen = buildPreview.paramCount;
							}
						}
						//Debug.LogFormat("condition:{0},{1}", buildPreview.objId, buildPreview.condition);
						if (buildPreview.condition != EBuildCondition.Ok)
						{
							__result = false;
							__instance.actionBuild.model.cursorText = buildPreview.conditionText;
							__instance.actionBuild.model.cursorState = -1;
							return;
						}
					}
				}

				//check others
				foreach (var buildPreview in TrarckPlugin.Instance.factoryBP.buildPreviews)
				{
					if (buildPreview.condition != EBuildCondition.Ok)
					{
						__result = false;
						__instance.actionBuild.model.cursorText = buildPreview.conditionText;
						__instance.actionBuild.model.cursorState = -1;
						return;
					}
				}
				//all pass
				__result = true;
				__instance.actionBuild.model.cursorText = "点击鼠标建造".Translate();
				__instance.actionBuild.model.cursorState = 0;
				YHDebug.LogFormat("CheckBuildConditions:{0}", __result);
			}
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "UpdatePreviewModelConditions")]
		public static void BuildTool_Click_UpdatePreviewModelConditions_Postfix(ref BuildTool_Click __instance, ref BuildModel model)
		{
			if (TrarckPlugin.Instance.BPBuild)
			{
				for (int i = 0; i < __instance.buildPreviews.Count; i++)
				{
					BuildPreview buildPreview = __instance.buildPreviews[i];
					if (buildPreview.isConnNode)
					{
						model.connGraph.AddPoint(buildPreview.lpos, (buildPreview.condition != EBuildCondition.Ok) ? 0U : 4U);
					}
				}
				if (__instance.buildPreviews.Count == 0 && __instance.controller.cmd.stage == 0 && __instance.cursorValid)
				{
					model.connGraph.AddPoint(__instance.controller.cmd.target, 4U);
				}
				foreach (BuildPreview buildPreview2 in __instance.buildPreviews)
				{
					if (buildPreview2.previewIndex >= 0)
					{
						model.SetPreviewModelPose(buildPreview2);
						model.SetPreviewModelMaterial(buildPreview2.previewIndex, (buildPreview2.condition == EBuildCondition.Ok) ? Configs.builtin.previewOkMat : Configs.builtin.previewErrorMat);
					}
				}
			}
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "ConfirmOperation")]
		public static void BuildTool_Click_ConfirmOperation_Postfix(ref BuildTool_Click __instance, ref bool condition, ref bool __result)
		{
			YHDebug.LogFormat("ConfirmOperation：{0},{1},{2}", condition, __result,__instance.waitForConfirm);
			//if (TrarckPlugin.Instance.BPBuild && !__result)
			//{
			//	__result = true;
			//}
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "CreatePrebuilds")]
		public static bool BuildTool_Click_CreatePrebuilds_Prefix(ref BuildTool_Click __instance)
		{
			YHDebug.LogFormat("CreatePrebuilds");
			TrarckPlugin.Instance.NeedResetBuildPreview = false;

			if (__instance.waitForConfirm &&  __instance.buildPreviews.Count > 0)
			{
				if (TrarckPlugin.Instance.BPBuild)
				{
					FactoryBP factoryBP = TrarckPlugin.Instance.factoryBP;
					foreach (BuildPreview buildPreview in __instance.buildPreviews)
					{
						if (buildPreview.desc.isInserter)
						{
							//parse cover
							if (buildPreview.input == null)
							{
								int overlappedCount = factoryBP.GetOverlappedObjectsNonAlloc(buildPreview.lpos, 0.3f, 3f,false, _overlappedIds);
								YHDebug.LogFormat("CreatePrebuilds_Prefix:insert input over {0}", overlappedCount);
								if (overlappedCount > 0)
								{
									int objId = _overlappedIds[0];
									bool isBelt = FactoryHelper.ObjectIsBelt(__instance.player.factory, objId);
									if (isBelt)
									{
										buildPreview.inputObjId = objId;
										buildPreview.inputToSlot = 1;
										buildPreview.inputFromSlot = -1;
										buildPreview.inputOffset = 0;

										TrarckPlugin.Instance.NeedResetBuildPreview = true;
									}
								}
							}

							if (buildPreview.output == null)
							{
								int overlappedCount = factoryBP.GetOverlappedObjectsNonAlloc(buildPreview.lpos2, 0.3f, 3f, false, _overlappedIds);
								YHDebug.LogFormat("CreatePrebuilds_Prefix:insert output over {0}", overlappedCount);
								if (overlappedCount > 0)
								{
									int objId = _overlappedIds[0];
									bool isBelt = FactoryHelper.ObjectIsBelt(__instance.player.factory, objId);
									if (isBelt)
									{
										buildPreview.outputObjId = objId;
										buildPreview.outputFromSlot = 0;
										buildPreview.outputToSlot = -1;
										buildPreview.outputOffset = 0;
										TrarckPlugin.Instance.NeedResetBuildPreview = true;
									}
								}
							}
						}
						else if (buildPreview.desc.isBelt && buildPreview.ignoreCollisionCheck)
						{
							//parse cover
							int overlappedCount = factoryBP.GetOverlappedObjectsNonAlloc(buildPreview.lpos, 0.3f, 3f, false, _overlappedIds);
							YHDebug.LogFormat("CreatePrebuilds_Prefix:belt over {0}", overlappedCount);
							if (overlappedCount > 0)
							{
								int objId = _overlappedIds[0];
								bool isBelt = FactoryHelper.ObjectIsBelt(__instance.player.factory, objId);
								if (isBelt)
								{
									buildPreview.coverObjId = objId;
									buildPreview.willRemoveCover = false;

									TrarckPlugin.Instance.NeedResetBuildPreview = true;
								}
							}
						}
					}
				}
			}

			return true;
		}

		[HarmonyPostfix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(BuildTool_Click), "CreatePrebuilds")]
		public static void BuildTool_Click_CreatePrebuilds_Postfix(ref BuildTool_Click __instance)
		{
			foreach (BuildPreview buildPreview3 in __instance.buildPreviews)
			{
				YHDebug.LogFormat("pb: {0},{1}", buildPreview3.objId,buildPreview3.output);
				if (buildPreview3.condition == EBuildCondition.Ok && buildPreview3.objId != 0)
				{
					if (buildPreview3.outputObjId != 0)
					{
						YHDebug.LogFormat("Connect1: {0},{1},{2},{3},{4}",buildPreview3.objId, buildPreview3.outputFromSlot, true, buildPreview3.outputObjId, buildPreview3.outputToSlot);
					}
					else if (buildPreview3.output != null)
					{
						YHDebug.LogFormat("Connect2: {0},{1},{2},{3},{4}", buildPreview3.objId, buildPreview3.outputFromSlot, true, buildPreview3.output.objId, buildPreview3.outputToSlot);
					}
					if (buildPreview3.inputObjId != 0)
					{
						YHDebug.LogFormat("Connect3: {0},{1},{2},{3},{4}", buildPreview3.objId, buildPreview3.inputToSlot, false, buildPreview3.inputObjId, buildPreview3.inputFromSlot);
					}
					else if (buildPreview3.input != null)
					{
						YHDebug.LogFormat("Connect4: {0},{1},{2},{3},{4}", buildPreview3.objId, buildPreview3.inputToSlot, false, buildPreview3.input.objId, buildPreview3.inputFromSlot);
					}
				}
			}

			if (TrarckPlugin.Instance.NeedResetBuildPreview)
			{
				TrarckPlugin.Instance.factoryBP.ResetBuildPreviewsRealChanges();
			}
		}
	}
}
