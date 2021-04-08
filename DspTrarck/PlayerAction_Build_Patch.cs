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

				if (__instance.buildPreviews!=null && __instance.buildPreviews.Count > 0)
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
			if (TrarckPlugin.Instance.BPBuild && !__result)
			{
				//Debug.LogFormat("CheckBuildConditions:{0}", __result);

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
								buildPreview.refCount = Mathf.RoundToInt(Mathf.Clamp(num3, 1f, 3f));
								buildPreview.refArr = new int[buildPreview.refCount];
								UIInserterBuildTip inserterBuildTip = UIRoot.instance.uiGame.inserterBuildTip;
								inserterBuildTip.gridLen = buildPreview.refCount;
							}
						}
						//Debug.LogFormat("condition:{0},{1}", buildPreview.objId, buildPreview.condition);
						if (buildPreview.condition != EBuildCondition.Ok)
						{
							__result = false;
							__instance.cursorText = buildPreview.conditionText;
							__instance.cursorWarning = true;
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
						__instance.cursorText = buildPreview.conditionText;
						__instance.cursorWarning = true;
						return;
					}
				}
				//all pass
				__result = true;
				__instance.cursorText = "点击鼠标建造".Translate();
				__instance.cursorWarning = false;
				//Debug.LogFormat("CheckBuildConditions:{0}", __result);
			}
		}

		[HarmonyPrefix, HarmonyPriority(Priority.Last), HarmonyPatch(typeof(PlayerAction_Build), "CreatePrebuilds")]
		public static bool PlayerAction_Build_CreatePrebuilds_Prefix(ref PlayerAction_Build __instance)
		{
			if (__instance.waitConfirm && VFInput._buildConfirm.onDown && __instance.buildPreviews.Count > 0)
			{
				foreach (BuildPreview buildPreview in __instance.buildPreviews)
				{
					if (buildPreview.desc.isInserter)
					{
						//parse cover
						if (buildPreview.input == null)
						{
							int overlappedCount = GetOverlappedObjectsNonAlloc(__instance,buildPreview.lpos, 0.3f, 3f);
							if(overlappedCount>0)
							{
								buildPreview.inputObjId = _overlappedIds[0];
							}
						}
					}
					else if (buildPreview.desc.isBelt)
					{
						  //parse cover
					}
				}
			}

			return true;
		}

		private static Pose GetBuildPreviewPose(BuildPreview buildPreview)
		{
			return new Pose(buildPreview.lpos, buildPreview.lrot);
		}

		public static int[] _nearObjectIds = new int[4096];
		private static int[] _overlappedIds = new int[4096];

		public static int GetOverlappedObjectsNonAlloc(PlayerAction_Build __instance,Vector3 pos, float objSize = 0f, float areaSize = 10f)
		{
			int overlappedCount = 0;
			int nearObjectCount = __instance.player.planetData.physics.nearColliderLogic.GetBuildingsInAreaNonAlloc(pos, areaSize, _nearObjectIds);
			for (int i = 0; i < nearObjectCount; i++)
			{
				int entityId = _nearObjectIds[i];
				int colliderId = 0;
				ColliderData colliderData = default(ColliderData);
				if (entityId > 0)
				{
					EntityData entityData = __instance.player.factory.entityPool[entityId];
					if (entityData.id != entityId)
					{
						continue;
					}
					colliderId = entityData.colliderId;
				}
				else	if(entityId<0)
				{
					PrebuildData prebuildData = __instance.player.factory.prebuildPool[-entityId];
					if (prebuildData.id != -entityId)
					{
						continue;
					}
					colliderId = prebuildData.colliderId;
				}
				else
				{
					continue;
				}

				int num3 = 0;
				while (colliderId != 0)
				{
					colliderData =__instance.player.planetData.physics.GetColliderData(colliderId);
					colliderData.ext += new Vector3(objSize, objSize, objSize);
					if (colliderData.ContainsInBox(pos))
					{
						_overlappedIds[overlappedCount++] = entityId;
						break;
					}
					colliderId = colliderData.link;
					if (++num3 > 32)
					{
						Assert.CannotBeReached();
						break;
					}
				}
			}

			return overlappedCount;
		}
	}
}
