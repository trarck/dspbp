using HarmonyLib;
using NGPT;
using PowerNetworkStructures;
using System;
using System.Collections.Generic;
using UnityEngine;
using YH.Log;


namespace DspTrarck
{
	public class BuildTool_BluePrint_Build : BuildTool
	{
		public ItemProto handItem;

		public PrefabDesc handPrefabDesc;

		public int modelOffset;

		public bool castTerrain;

		public bool castPlatform;

		public bool castGround;

		public Vector3 castGroundPos = Vector3.zero;

		public Vector3 castGroundPosSnapped = Vector3.zero;

		public bool castObject;

		public int castObjectId;

		public Vector3 castObjectPos;

		private bool isDragging;

		public Vector3 startGroundPosSnapped = Vector3.zero;

		public Vector3[] dotsSnapped;

		public bool cursorValid;

		public Vector3 cursorTarget;

		public bool waitForConfirm;

		public bool multiLevelCovering;

		public float yaw;

		public float gap;

		public bool tabgapDir = true;

		private Pose[] belt_slots = new Pose[4]
		{
		new Pose(new Vector3(0f, 0f, 0f), Quaternion.identity),
		new Pose(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f)),
		new Pose(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f)),
		new Pose(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f))
		};

		private int[] tmp_conn = new int[16];
		private List<int> tmp_links = new List<int>();

		protected override void _OnInit()
		{
			YHDebug.Log("Init in bp tools");
			dotsSnapped = new Vector3[15];
		}

		protected override void _OnFree()
		{
			YHDebug.Log("free in bp tools");
			dotsSnapped = null;
		}

		protected override void _OnOpen()
		{
			YHDebug.Log("open bp tools");
			yaw = BuildingParameters.template.yaw;
		}

		protected override void _OnClose()
		{
			YHDebug.Log("close bp tools");
			isDragging = false;
			yaw = 0f;
			gap = 0f;
			tabgapDir = true;
			modelOffset = 0;
		}

		protected override void _OnTick(long time)
		{
			if (!UpdateHandItem())
			{
				_Close();
				return;
			}
			UpdateRaycast();
			DeterminePreviews();
			UpdateCollidersForCursor();
			UpdatePreviewModels(base.actionBuild.model);
			bool condition = CheckBuildConditions();
			UpdatePreviewModelConditions(base.actionBuild.model);
			UpdateGizmos(base.actionBuild.model);
			if (ConfirmOperation(condition))
			{
				CreatePrebuilds();
			}
		}

		public override bool DetermineActive()
		{
			return controller.cmd.type == ECommand.Build && TrarckPlugin.Instance.BPBuild;
		}

		public bool UpdateHandItem()
		{
			handItem = LDB.items.Select(base.controller.cmd.refId);
			if (handItem != null && handItem.IsEntity && handItem.CanBuild)
			{
				int modelIndex = handItem.ModelIndex;
				int num = handItem.ModelCount;
				if (num < 1)
				{
					num = 1;
				}
				modelIndex += modelOffset % num;
				ModelProto modelProto = LDB.models.Select(modelIndex);
				if (modelProto != null)
				{
					handPrefabDesc = modelProto.prefabDesc;
				}
				else
				{
					handPrefabDesc = handItem.prefabDesc;
				}
				if (!handPrefabDesc.hasObject)
				{
					handPrefabDesc = null;
				}
				if (base.actionBuild.templatePreviews.Count > 0)
				{
					BuildPreview buildPreview = base.actionBuild.templatePreviews[0];
					if (handItem == buildPreview.item)
					{
						handPrefabDesc = buildPreview.desc;
						modelOffset = buildPreview.desc.modelIndex - handItem.ModelIndex;
					}
				}
			}
			else
			{
				handPrefabDesc = null;
			}
			return true;
		}

		public void UpdateRaycast()
		{
			castTerrain = false;
			castPlatform = false;
			castGround = false;
			castGroundPos = Vector3.zero;
			castGroundPosSnapped = Vector3.zero;
			castObject = false;
			castObjectId = 0;
			castObjectPos = Vector3.zero;
			cursorValid = false;
			cursorTarget = Vector3.zero;
			multiLevelCovering = false;
			if (!VFInput.onGUI && VFInput.inScreen)
			{
				int layerMask = 8720;
				castGround = Physics.Raycast(mouseRay, out var hitInfo, 400f, layerMask, QueryTriggerInteraction.Collide);
				if (!castGround)
				{
					castGround = Physics.Raycast(new Ray(mouseRay.GetPoint(200f), -mouseRay.direction), out hitInfo, 200f, layerMask, QueryTriggerInteraction.Collide);
				}
				if (castGround)
				{
					Layer layer = (Layer)hitInfo.collider.gameObject.layer;
					castTerrain = layer == Layer.Terrain || layer == Layer.Water;
					castPlatform = layer == Layer.Platform;
					castGroundPos = (base.controller.cmd.test = (base.controller.cmd.target = hitInfo.point));
					if (VFInput._ignoreGrid && (handPrefabDesc!=null && handPrefabDesc.minerType == EMinerType.Vein))
					{
						castGroundPosSnapped = castGroundPos.normalized * (planet.realRadius + 0.2f);
					}
					else
					{
						castGroundPosSnapped = base.actionBuild.planetAux.Snap(castGroundPos, castTerrain);
					}
					if (base.controller.cmd.stage == 1)
					{
						castGroundPosSnapped = castGroundPosSnapped.normalized * startGroundPosSnapped.magnitude;
					}
					base.controller.cmd.test = castGroundPosSnapped;
					Vector3 normalized = castGroundPosSnapped.normalized;
					if (Physics.Raycast(new Ray(castGroundPosSnapped + normalized * 10f, -normalized), out hitInfo, 20f, 8720, QueryTriggerInteraction.Collide))
					{
						base.controller.cmd.test = hitInfo.point;
					}
					cursorTarget = castGroundPosSnapped;
					cursorValid = true;
				}
				if (!isDragging && (handPrefabDesc!=null && handPrefabDesc.multiLevel))
				{
					int castAllCount = base.controller.cmd.raycast.castAllCount;
					RaycastData[] castAll = base.controller.cmd.raycast.castAll;
					int num = 0;
					for (int i = 0; i < castAllCount; i++)
					{
						if (castAll[i].objType == EObjectType.Entity || castAll[i].objType == EObjectType.Prebuild)
						{
							num = ((castAll[i].objType == EObjectType.Entity) ? castAll[i].objId : (-castAll[i].objId));
							break;
						}
					}
					if (num != 0 && GetObjectProtoId(num) == handItem.ID)
					{
						int num2 = 0;
						int otherObjId;
						do
						{
							factory.ReadObjectConn(num, 15, out var _, out otherObjId, out var _);
							if (otherObjId != 0)
							{
								num = otherObjId;
							}
						}
						while (otherObjId != 0 && num2++ < 200);
						if (otherObjId == 0)
						{
							castObject = true;
							castObjectId = num;
							castObjectPos = GetObjectPose(num).position;
						}
					}
				}
				if (castObject)
				{
					cursorTarget = castObjectPos;
					base.controller.cmd.test = castObjectPos;
					cursorValid = true;
					multiLevelCovering = true;
				}
			}
			base.controller.cmd.state = (cursorValid ? 1 : 0);
			base.controller.cmd.target = (cursorValid ? cursorTarget : Vector3.zero);
		}

		public void DeterminePreviews()
		{
			waitForConfirm = false;
			if (cursorValid)
			{
				waitForConfirm = cursorValid;

				if (VFInput._rotate.onDown)
				{
					yaw += 90f;
					yaw = Mathf.Repeat(yaw, 360f);
					yaw = Mathf.Round(yaw / 90f) * 90f;
				}
				if (VFInput._counterRotate.onDown)
				{
					yaw -= 90f;
					yaw = Mathf.Repeat(yaw, 360f);
					yaw = Mathf.Round(yaw / 90f) * 90f;
				}

				TrarckPlugin.Instance.factoryBP.UpdateBuildPosition(castGroundPosSnapped, yaw);
				buildPreviews.Clear();
				buildPreviews.AddRange(TrarckPlugin.Instance.factoryBP.buildPreviews);
			}
			else
			{
				buildPreviews.Clear();
			}
		}

		private void MatchInserter(BuildPreview bp)
		{
			CargoTraffic cargoTraffic = factory.cargoTraffic;
			EntityData[] entityPool = factory.entityPool;
			_ = cargoTraffic.beltPool;
			bool flag = bp.output == null && bp.outputObjId == 0;
			bool flag2 = bp.input == null && bp.inputObjId == 0;
			if (flag || flag2)
			{
				do
				{
					Vector3 vector = (flag ? bp.lpos2 : bp.lpos);
					Vector3 vector2 = (flag ? bp.lpos : bp.lpos2);
					Vector3 lhs = (flag ? (bp.lpos2 - bp.lpos).normalized : (bp.lpos - bp.lpos2).normalized);
					Quaternion obj = (flag ? bp.lrot2 : bp.lrot);
					Vector3 vector3 = (flag ? bp.lrot : bp.lrot2).Forward();
					Vector3 vector4 = vector;
					Quaternion quaternion = obj;
					int num = 0;
					BuildPreview buildPreview = null;
					int num2 = 0;
					int num3 = 0;
					bool flag3 = false;
					float num4 = 99f;
					int num5 = 0;
					BuildPreview buildPreview2 = null;
					int num6 = 0;
					bool flag4 = false;
					int layerMask = 425984;
					int num7 = Physics.OverlapSphereNonAlloc(vector, 0.8f, BuildTool._tmp_cols, layerMask, QueryTriggerInteraction.Collide);
					if (num7 > 0)
					{
						for (int i = 0; i < num7; i++)
						{
							float num8 = 100f;
							int num9 = 0;
							int num10 = 0;
							bool flag5 = false;
							BuildPreview buildPreview3 = null;
							if (planet.physics.GetColliderData(BuildTool._tmp_cols[i], out var cd))
							{
								if (cd.objType == EObjectType.Entity || cd.objType == EObjectType.Prebuild)
								{
									num10 = 0;
									num9 = ((cd.objType == EObjectType.Entity) ? cd.objId : (-cd.objId));
									flag5 = ObjectIsBelt(num9);
									if (flag5)
									{
										Pose objectPose = GetObjectPose(num9);
										Pose[] array = belt_slots;
										for (int j = 0; j < array.Length; j++)
										{
											Vector3 vector5 = objectPose.position + objectPose.rotation * array[j].position;
											Vector3 rhs = objectPose.rotation * array[j].rotation * new Vector3(0f, 0f, -1f);
											float num11 = Vector3.Dot(lhs, rhs);
											float num12 = Vector3.Dot((vector5 - vector2).normalized, rhs);
											if (num11 > 0.9f && num12 > 0.8f)
											{
												num8 = (objectPose.position - vector).sqrMagnitude;
												num10 = j;
												break;
											}
										}
									}
									else
									{
										Pose objectPose2 = GetObjectPose(num9);
										Pose[] localSlots = GetLocalSlots(num9);
										for (int k = 0; k < localSlots.Length; k++)
										{
											Vector3 vector6 = objectPose2.position + objectPose2.rotation * localSlots[k].position;
											Vector3 rhs2 = objectPose2.rotation * localSlots[k].rotation * new Vector3(0f, 0f, -1f);
											float num13 = Vector3.Dot(lhs, rhs2);
											float num14 = Vector3.Dot((vector6 - vector2).normalized, rhs2);
											if (num13 > 0.9702957f && num14 > 0.9702957f)
											{
												float sqrMagnitude = (vector6 - vector).sqrMagnitude;
												if (sqrMagnitude < num8)
												{
													num8 = sqrMagnitude;
													num10 = k;
												}
											}
										}
									}
								}
							}
							else if (BuildTool._tmp_cols[i].gameObject.layer == 18)
							{
								BuildPreviewModel component = BuildTool._tmp_cols[i].gameObject.GetComponent<BuildPreviewModel>();
								if (component != null)
								{
									Pose[] slotPoses = component.buildPreview.desc.slotPoses;
									if (slotPoses != null && slotPoses.Length != 0)
									{
										Pose pose = new Pose(component.trans.localPosition, component.trans.localRotation);
										for (int l = 0; l < slotPoses.Length; l++)
										{
											Vector3 vector7 = pose.position + pose.rotation * slotPoses[l].position;
											Vector3 rhs3 = pose.rotation * slotPoses[l].rotation * new Vector3(0f, 0f, -1f);
											float num15 = Vector3.Dot(lhs, rhs3);
											float num16 = Vector3.Dot((vector7 - vector2).normalized, rhs3);
											if (num15 > 0.9702957f && num16 > 0.9702957f)
											{
												float sqrMagnitude2 = (vector7 - vector).sqrMagnitude;
												if (sqrMagnitude2 < num8)
												{
													num8 = sqrMagnitude2;
													num10 = l;
													buildPreview3 = component.buildPreview;
												}
											}
										}
									}
								}
							}
							if (num8 < num4)
							{
								num4 = num8;
								num5 = num9;
								buildPreview2 = buildPreview3;
								num6 = num10;
								flag4 = flag5;
							}
						}
					}
					if (num4 < 6f && (num5 != 0 || buildPreview2 != null))
					{
						if (flag4)
						{
							if (num5 > 0)
							{
								Pose objectPose3 = GetObjectPose(num5);
								Pose[] array2 = belt_slots;
								vector4 = objectPose3.position + objectPose3.rotation * array2[num6].position;
								quaternion = objectPose3.rotation * array2[num6].rotation;
								num = num5;
								num2 = -1;
								num3 = 0;
								flag3 = true;
								Vector3 vector8 = vector2 - vector4;
								Vector3 lhs2 = -vector8;
								int beltId = entityPool[num5].beltId;
								Assert.Positive(beltId);
								BeltComponent beltComponent = cargoTraffic.beltPool[beltId];
								Assert.Positive(beltComponent.segPathId);
								int segPathId = beltComponent.segPathId;
								CargoPath cargoPath = cargoTraffic.GetCargoPath(segPathId);
								Assert.NotNull(cargoPath);
								int num17 = beltComponent.segIndex;
								int num18 = beltComponent.segIndex + beltComponent.segLength;
								int num19 = beltComponent.segIndex + beltComponent.segPivotOffset;
								if (num17 < 4)
								{
									num17 = 4;
								}
								if (num17 > cargoPath.pathLength - 5 - 1)
								{
									num17 = cargoPath.pathLength - 5 - 1;
								}
								if (num18 < 4)
								{
									num18 = 4;
								}
								if (num18 > cargoPath.pathLength - 5 - 1)
								{
									num18 = cargoPath.pathLength - 5 - 1;
								}
								if (num19 < 4)
								{
									num19 = 4;
								}
								if (num19 > cargoPath.pathLength - 5 - 1)
								{
									num19 = cargoPath.pathLength - 5 - 1;
								}
								for (int m = num17; m < num18; m++)
								{
									float num20 = Vector3.Dot(lhs2, vector3);
									Vector3 vector9 = cargoPath.pointPos[m];
									Vector3 vector10 = cargoPath.pointPos[m + 1];
									Vector3 point = vector2 + vector3 * num20;
									float num21 = Kit.ClosestPoint2Straight(vector9, vector10, point);
									if (num21 >= 0f && num21 <= 1f)
									{
										vector4 = vector9 + (vector10 - vector9) * num21;
										vector4 -= vector4.normalized * 0.15f;
										quaternion = Quaternion.Slerp(cargoPath.pointRot[m], cargoPath.pointRot[m + 1], num21);
										Quaternion identity = Quaternion.identity;
										Vector3 zero = Vector3.zero;
										identity = quaternion * Quaternion.Euler(0f, 90f, 0f);
										zero = identity.Forward();
										if (Vector3.Angle(vector8, zero) < 40f)
										{
											quaternion = identity;
										}
										identity = quaternion * Quaternion.Euler(0f, 180f, 0f);
										zero = identity.Forward();
										if (Vector3.Angle(vector8, zero) < 40f)
										{
											quaternion = identity;
										}
										identity = quaternion * Quaternion.Euler(0f, -90f, 0f);
										zero = identity.Forward();
										if (Vector3.Angle(vector8, zero) < 40f)
										{
											quaternion = identity;
										}
										num3 = m - num19;
									}
								}
							}
							else if (num5 >= 0)
							{
							}
						}
						else
						{
							Pose pose2 = default(Pose);
							Pose[] array3 = null;
							if (num5 != 0)
							{
								pose2 = GetObjectPose(num5);
								array3 = GetLocalSlots(num5);
							}
							else if (buildPreview2 != null)
							{
								pose2 = new Pose(buildPreview2.lpos, buildPreview2.lrot);
								array3 = buildPreview2.desc.slotPoses;
							}
							if (array3 != null && array3.Length != 0)
							{
								vector4 = pose2.position + pose2.rotation * array3[num6].position;
								quaternion = pose2.rotation * array3[num6].rotation;
								num = num5;
								buildPreview = buildPreview2;
								num2 = num6;
								num3 = 0;
								flag3 = true;
							}
						}
					}
					if (!flag3)
					{
						break;
					}
					if (flag)
					{
						bp.lpos2 = vector4;
						bp.lrot2 = quaternion * Quaternion.Euler(0f, 180f, 0f);
						bp.output = buildPreview;
						if (bp.output == null)
						{
							bp.outputObjId = num;
						}
						bp.outputToSlot = num2;
						bp.outputFromSlot = 0;
						bp.outputOffset = num3;
						flag = false;
						continue;
					}
					bp.lpos = vector4;
					bp.lrot = quaternion;
					bp.input = buildPreview;
					if (bp.input == null)
					{
						bp.inputObjId = num;
					}
					bp.inputFromSlot = num2;
					bp.inputToSlot = 1;
					bp.inputOffset = num3;
					flag2 = false;
					break;
				}
				while (flag2);
			}
			if (flag || flag2)
			{
				bp.condition = EBuildCondition.NeedConn;
			}
			else
			{
				bp.condition = EBuildCondition.Ok;
			}
		}

		public bool IsOtherBeltIntput(BuildPreview buildPreview)
		{
			if (buildPreviews != null && buildPreviews.Count > 0)
			{
				for (int i = 0; i < base.buildPreviews.Count; i++)
				{
					BuildPreview other = base.buildPreviews[i];
					if (other.desc.isBelt &&  other.output== buildPreview)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool CheckBuildConditions()
		{
			if (base.buildPreviews.Count == 0)
			{
				return false;
			}
			GameHistoryData history = base.actionBuild.history;
			bool flag = false;
			int num = 1;
			List<BuildPreview> templatePreviews = base.actionBuild.templatePreviews;
			if (templatePreviews.Count > 0)
			{
				num = templatePreviews.Count;
			}
			for (int i = 0; i < base.buildPreviews.Count; i++)
			{
				BuildPreview buildPreview = base.buildPreviews[i];
				BuildPreview buildPreview2 = base.buildPreviews[i / num * num];
				if (buildPreview.condition != 0)
				{
					continue;
				}
				Vector3 vector = buildPreview.lpos;
				Quaternion quaternion = buildPreview.lrot;
				Vector3 lpos = buildPreview.lpos2;
				_ = buildPreview.lrot2;
				Pose pose = new Pose(buildPreview.lpos, buildPreview.lrot);
				Pose pose2 = new Pose(buildPreview.lpos2, buildPreview.lrot2);
				Vector3 forward = pose.forward;
				_ = pose2.forward;
				Vector3 up = pose.up;
				Vector3 vector2 = Vector3.Lerp(vector, lpos, 0.5f);
				Vector3 forward2 = lpos - vector;
				bool isBelt = buildPreview.desc.isBelt;
				if (forward2.sqrMagnitude < 0.0001f)
				{
					forward2 = Maths.SphericalRotation(vector, 0f).Forward();
				}
				Quaternion quaternion2 = Quaternion.LookRotation(forward2, vector2.normalized);
				bool flag2 = planet != null && planet.type == EPlanetType.Gas;
				if (vector.sqrMagnitude < 1f)
				{
					buildPreview.condition = EBuildCondition.Failure;
					continue;
				}
				bool flag3 = buildPreview.desc.minerType == EMinerType.None && !buildPreview.desc.isBelt && !buildPreview.desc.isSplitter && (!buildPreview.desc.isPowerNode || buildPreview.desc.isPowerGen || buildPreview.desc.isAccumulator || buildPreview.desc.isPowerExchanger) && !buildPreview.desc.isStation && !buildPreview.desc.isSilo && !buildPreview.desc.multiLevel;
				if (buildPreview.desc.veinMiner)
				{
					Array.Clear(BuildTool._tmp_ids, 0, BuildTool._tmp_ids.Length);
					Vector3 vector3 = vector + forward * -1.2f;
					Vector3 rhs = -forward;
					Vector3 vector4 = up;
					int veinsInAreaNonAlloc = base.actionBuild.nearcdLogic.GetVeinsInAreaNonAlloc(vector3, 12f, BuildTool._tmp_ids);
					PrebuildData prebuildData = default(PrebuildData);
					prebuildData.InitParametersArray(veinsInAreaNonAlloc);
					VeinData[] veinPool = factory.veinPool;
					int paramCount = 0;
					for (int j = 0; j < veinsInAreaNonAlloc; j++)
					{
						if (BuildTool._tmp_ids[j] != 0 && veinPool[BuildTool._tmp_ids[j]].id == BuildTool._tmp_ids[j])
						{
							if (veinPool[BuildTool._tmp_ids[j]].type != EVeinType.Oil)
							{
								Vector3 rhs2 = veinPool[BuildTool._tmp_ids[j]].pos - vector3;
								float num2 = Vector3.Dot(vector4, rhs2);
								rhs2 -= vector4 * num2;
								float sqrMagnitude = rhs2.sqrMagnitude;
								float num3 = Vector3.Dot(rhs2.normalized, rhs);
								if (!(sqrMagnitude > 60.0625f) && !(num3 < 0.73f) && !(Mathf.Abs(num2) > 2f))
								{
									prebuildData.parameters[paramCount++] = BuildTool._tmp_ids[j];
								}
							}
						}
						else
						{
							Assert.CannotBeReached();
						}
					}
					prebuildData.paramCount = paramCount;
					prebuildData.ArrageParametersArray();
					buildPreview.parameters = prebuildData.parameters;
					buildPreview.paramCount = prebuildData.paramCount;
					Array.Clear(BuildTool._tmp_ids, 0, BuildTool._tmp_ids.Length);
					if (prebuildData.paramCount == 0)
					{
						buildPreview.condition = EBuildCondition.NeedResource;
						continue;
					}
				}
				else if (buildPreview.desc.oilMiner)
				{
					Array.Clear(BuildTool._tmp_ids, 0, BuildTool._tmp_ids.Length);
					Vector3 vector5 = vector;
					Vector3 vector6 = -up;
					int veinsInAreaNonAlloc2 = base.actionBuild.nearcdLogic.GetVeinsInAreaNonAlloc(vector5, 10f, BuildTool._tmp_ids);
					PrebuildData prebuildData2 = default(PrebuildData);
					prebuildData2.InitParametersArray(veinsInAreaNonAlloc2);
					VeinData[] veinPool2 = factory.veinPool;
					int num4 = 0;
					float num5 = 100f;
					Vector3 pos = vector5;
					for (int k = 0; k < veinsInAreaNonAlloc2; k++)
					{
						if (BuildTool._tmp_ids[k] != 0 && veinPool2[BuildTool._tmp_ids[k]].id == BuildTool._tmp_ids[k] && veinPool2[BuildTool._tmp_ids[k]].type == EVeinType.Oil)
						{
							Vector3 pos2 = veinPool2[BuildTool._tmp_ids[k]].pos;
							Vector3 vector7 = pos2 - vector5;
							float num6 = Vector3.Dot(vector6, vector7);
							float sqrMagnitude2 = (vector7 - vector6 * num6).sqrMagnitude;
							if (sqrMagnitude2 < num5)
							{
								num5 = sqrMagnitude2;
								num4 = BuildTool._tmp_ids[k];
								pos = pos2;
							}
						}
					}
					if (num4 == 0)
					{
						buildPreview.condition = EBuildCondition.NeedResource;
						continue;
					}
					prebuildData2.parameters[0] = num4;
					prebuildData2.paramCount = 1;
					prebuildData2.ArrageParametersArray();
					buildPreview.parameters = prebuildData2.parameters;
					buildPreview.paramCount = prebuildData2.paramCount;
					Vector3 vector8 = factory.planet.aux.Snap(pos, onTerrain: true);
					vector = (pose.position = (buildPreview.lpos2 = (buildPreview.lpos = vector8)));
					quaternion = (pose.rotation = (buildPreview.lrot2 = (buildPreview.lrot = Maths.SphericalRotation(vector8, yaw))));
					forward = pose.forward;
					up = pose.up;
					Array.Clear(BuildTool._tmp_ids, 0, BuildTool._tmp_ids.Length);
				}
				if (buildPreview.desc.isTank || buildPreview.desc.isStorage || buildPreview.desc.isLab || buildPreview.desc.isSplitter)
				{
					int num7 = (buildPreview.desc.isLab ? history.labLevel : history.storageLevel);
					int num8 = (buildPreview.desc.isLab ? 15 : 8);
					int num9 = 0;
					factory.ReadObjectConn(buildPreview.inputObjId, 14, out var isOutput, out var otherObjId, out var otherSlot);
					while (otherObjId != 0)
					{
						num9++;
						factory.ReadObjectConn(otherObjId, 14, out isOutput, out otherObjId, out otherSlot);
					}
					if (num9 >= num7 - 1)
					{
						flag = num7 >= num8;
						buildPreview.condition = EBuildCondition.OutOfVerticalConstructionHeight;
						continue;
					}
				}
				Vector3 vector9 = base.player.position;
				float num10 = base.player.mecha.buildArea * base.player.mecha.buildArea;
				if (flag2)
				{
					vector9 = vector9.normalized;
					vector9 *= planet.realRadius;
					num10 *= 6f;
				}
				if ((vector - vector9).sqrMagnitude > num10)
				{
					buildPreview.condition = EBuildCondition.OutOfReach;
					continue;
				}
				if (planet != null)
				{
					float num11 = history.buildMaxHeight + 0.5f + planet.realRadius * (flag2 ? 1.025f : 1f);
					if (vector.sqrMagnitude > num11 * num11)
					{
						buildPreview.condition = EBuildCondition.OutOfReach;
						continue;
					}
				}

				//belt
				if (isBelt)
				{
					bool flag10 = buildPreview.input != null && buildPreview.input.desc.isBelt;
					bool flag11 = buildPreview.output != null && buildPreview.output.desc.isBelt;
					Vector3 vector5 = buildPreview.lpos.normalized * 0.22f;
					Vector3 lpos2 = buildPreview.lpos;
					Vector3 vector4 = (flag10 ? buildPreview.input.lpos : lpos2);
					Vector3 vector3 = (flag11 ? buildPreview.output.lpos : lpos2);
					vector4 = (vector4 - lpos2) * 0.2f + lpos2;
					vector3 = (vector3 - lpos2) * 0.2f + lpos2;
					lpos2 += vector5;
					vector4 += vector5;
					vector3 += vector5;

					int num7 = 0;
					num7 = ((!(flag11 || flag10)) ? Physics.OverlapSphereNonAlloc(lpos2, 0.28f, BuildTool._tmp_cols, 425984, QueryTriggerInteraction.Collide) : Physics.OverlapCapsuleNonAlloc(vector4, vector3, 0.28f, BuildTool._tmp_cols, 425984, QueryTriggerInteraction.Collide));
					if (num7 > 0)
					{
						bool flag12 = false;
						for (int j = 0; j < num7; j++)
						{
							if (planet.physics.GetColliderData(BuildTool._tmp_cols[j], out var cd))
							{
								int num8 = 0;
								if (cd.objType == EObjectType.Entity)
								{
									num8 = cd.objId;
								}
								else if (cd.objType == EObjectType.Prebuild)
								{
									num8 = -cd.objId;
								}
								if (num8 != 0)
								{
									if (ObjectIsBelt(num8) || GetLocalPorts(num8).Length != 0)
									{
										if (buildPreview.output == null || !IsOtherBeltIntput(buildPreview))
										{
											continue;
										}
									}
								}
							}
							flag12 = true;
							break;
						}
						if (flag12)
						{
							buildPreview.condition = EBuildCondition.Collide;
							continue;
						}
					}

					float num110 = planet.data.QueryModifiedHeight(lpos2) - (planet.realRadius + 0.2f);
					if (num110 < 0f)
					{
						num110 = 0f;
					}
					Vector3 position2 = lpos2 + lpos2.normalized * (num110 + 0.4f);
					num7 = Physics.OverlapSphereNonAlloc(position2, 0.5f, BuildTool._tmp_cols, 2048, QueryTriggerInteraction.Collide);
					if (num7 > 0)
					{
						buildPreview.condition = EBuildCondition.Collide;
						continue;
					}

					bool flag14 = false;
					bool flag15 = false;
					Vector3 vector114 = Vector3.zero;
					Vector3 vector115 = Vector3.zero;
					BuildPreview input = buildPreview.input;
					BuildPreview output = buildPreview.output;
					if (input != null)
					{
						flag14 = true;
						vector114 = buildPreview.input.lpos;
					}

					if (output != null)
					{
						flag15 = true;
						vector115 = buildPreview.output.lpos;
					}

					float num11 = (float)Math.PI;
					if (flag14 && flag15)
					{
						num11 = Maths.SphericalAngleAOBInRAD(buildPreview.lpos, vector114, vector115);
						if (num11 < 0.87266463f)
						{
							buildPreview.condition = EBuildCondition.TooBend;
							continue;
						}
					}
					float num12 = 0f;
					if (flag15)
					{
						num12 = Mathf.Abs(Maths.SphericalSlopeRatio(buildPreview.lpos, vector115));
						if (num12 > 0.75f)
						{
							buildPreview.condition = EBuildCondition.TooSteep;
							continue;
						}

						Vector3 vector6 = vector115 - buildPreview.lpos;
						Vector3 normalized = buildPreview.lpos.normalized;
						_ = vector6 - Vector3.Dot(vector6, normalized) * normalized;
						if ((buildPreview.lpos - vector115).magnitude < 0.4f)
						{
							buildPreview.condition = EBuildCondition.TooClose;
							continue;
						}
					}
					if (flag14)
					{
						num12 = Mathf.Max(Mathf.Abs(Maths.SphericalSlopeRatio(vector114, buildPreview.lpos)), num12);
						if (num12 > 0.75f)
						{
							buildPreview.condition = EBuildCondition.TooSteep;
							continue;
						}
					}

					if (num11 < 2.5f && num12 > 0.1f)
					{
						buildPreview.condition = EBuildCondition.TooBendToLift;
						continue;
					}

					continue;
				}

				if (buildPreview.desc.hasBuildCollider)
				{
					ColliderData[] buildColliders = buildPreview.desc.buildColliders;
					for (int l = 0; l < buildColliders.Length; l++)
					{
						ColliderData colliderData = buildPreview.desc.buildColliders[l];
						if (buildPreview.desc.isInserter)
						{
							colliderData.ext = new Vector3(colliderData.ext.x, colliderData.ext.y, Vector3.Distance(lpos, vector) * 0.5f + colliderData.ext.z - 0.5f);
							if (ObjectIsBelt(buildPreview.inputObjId) || (buildPreview.input != null && buildPreview.input.desc.isBelt))
							{
								colliderData.pos.z -= 0.35f;
								colliderData.ext.z += 0.35f;
							}
							else if (buildPreview.inputObjId == 0 && buildPreview.input == null)
							{
								colliderData.pos.z -= 0.35f;
								colliderData.ext.z += 0.35f;
							}
							if (ObjectIsBelt(buildPreview.outputObjId) || (buildPreview.output != null && buildPreview.output.desc.isBelt))
							{
								colliderData.pos.z += 0.35f;
								colliderData.ext.z += 0.35f;
							}
							else if (buildPreview.outputObjId == 0 && buildPreview.output == null)
							{
								colliderData.pos.z += 0.35f;
								colliderData.ext.z += 0.35f;
							}
							if (colliderData.ext.z < 0.1f)
							{
								colliderData.ext.z = 0.1f;
							}
							colliderData.pos = vector2 + quaternion2 * colliderData.pos;
							colliderData.q = quaternion2 * colliderData.q;
							colliderData.DebugDraw();
						}
						else
						{
							colliderData.pos = vector + quaternion * colliderData.pos;
							colliderData.q = quaternion * colliderData.q;
						}
						int mask = 428032;
						if (buildPreview.desc.veinMiner || buildPreview.desc.oilMiner)
						{
							mask = 425984;
						}
						Array.Clear(BuildTool._tmp_cols, 0, BuildTool._tmp_cols.Length);
						int num12 = Physics.OverlapBoxNonAlloc(colliderData.pos, colliderData.ext, BuildTool._tmp_cols, colliderData.q, mask, QueryTriggerInteraction.Collide);
						if (num12 > 0)
						{
							bool flag4 = false;
							PlanetPhysics physics = base.player.planetData.physics;
							for (int m = 0; m < num12 && buildPreview.coverObjId == 0; m++)
							{
								ColliderData cd;
								bool colliderData2 = physics.GetColliderData(BuildTool._tmp_cols[m], out cd);
								int num13 = 0;
								if (colliderData2 && cd.usage == EColliderUsage.Build)
								{
									if (cd.objType == EObjectType.Entity)
									{
										num13 = cd.objId;
									}
									else if (cd.objType == EObjectType.Prebuild)
									{
										num13 = -cd.objId;
									}
								}
								Collider collider = BuildTool._tmp_cols[m];
								if (collider.gameObject.layer == 18)
								{
									BuildPreviewModel component = collider.GetComponent<BuildPreviewModel>();
									if ((component != null && component.index == buildPreview.previewIndex) || (buildPreview.desc.isInserter && !component.buildPreview.desc.isInserter) || (!buildPreview.desc.isInserter && component.buildPreview.desc.isInserter))
									{
										continue;
									}
								}
								else if (buildPreview.desc.isInserter && num13 != 0 && (num13 == buildPreview.inputObjId || num13 == buildPreview.outputObjId || num13 == buildPreview2.coverObjId))
								{
									continue;
								}
								flag4 = true;
								if (!flag3 || num13 == 0)
								{
									continue;
								}
								ItemProto itemProto = GetItemProto(num13);
								if (!buildPreview.item.IsSimilar(itemProto))
								{
									continue;
								}
								Pose objectPose = GetObjectPose(num13);
								Pose objectPose2 = GetObjectPose2(num13);
								if ((double)(objectPose.position - buildPreview.lpos).sqrMagnitude < 0.01 && (double)(objectPose2.position - buildPreview.lpos2).sqrMagnitude < 0.01 && ((double)(objectPose.forward - forward).sqrMagnitude < 1E-06 || buildPreview.desc.isInserter))
								{
									if (buildPreview.item.ID == itemProto.ID)
									{
										buildPreview.coverObjId = num13;
										buildPreview.willRemoveCover = false;
										flag4 = false;
									}
									else
									{
										buildPreview.coverObjId = num13;
										buildPreview.willRemoveCover = true;
										flag4 = false;
									}
									break;
								}
							}
							if (flag4)
							{
								buildPreview.condition = EBuildCondition.Collide;
								break;
							}
						}
						if (buildPreview.desc.veinMiner && Physics.CheckBox(colliderData.pos, colliderData.ext, colliderData.q, 2048, QueryTriggerInteraction.Collide))
						{
							buildPreview.condition = EBuildCondition.Collide;
							break;
						}
					}
					if (buildPreview.condition != 0)
					{
						continue;
					}
				}

				if (buildPreview2.coverObjId != 0 && buildPreview.desc.isInserter)
				{
					if (buildPreview.output == buildPreview2)
					{
						buildPreview.outputObjId = buildPreview2.coverObjId;
						buildPreview.output = null;
					}
					if (buildPreview.input == buildPreview2)
					{
						buildPreview.inputObjId = buildPreview2.coverObjId;
						buildPreview.input = null;
					}
				}
				if (buildPreview.coverObjId == 0 || buildPreview.willRemoveCover)
				{
					int itemId = buildPreview.item.ID;
					int count = 1;
					if (tmpInhandId == itemId && tmpInhandCount > 0)
					{
						count = 1;
						tmpInhandCount--;
					}
					else
					{
						tmpPackage.TakeTailItems(ref itemId, ref count);
					}
					if (count == 0)
					{
						buildPreview.condition = EBuildCondition.NotEnoughItem;
						continue;
					}
				}
				if (buildPreview.coverObjId != 0)
				{
					continue;
				}
				if (buildPreview.desc.isPowerNode && !buildPreview.desc.isAccumulator)
				{
					if (buildPreview.nearestPowerObjId == null || buildPreview.nearestPowerObjId.Length != buildPreview.nearestPowerObjId.Length)
					{
						buildPreview.nearestPowerObjId = new int[factory.powerSystem.netCursor];
					}
					Array.Clear(buildPreview.nearestPowerObjId, 0, buildPreview.nearestPowerObjId.Length);
					float num14 = buildPreview.desc.powerConnectDistance * buildPreview.desc.powerConnectDistance;
					float x = vector.x;
					float y = vector.y;
					float z = vector.z;
					int netCursor = factory.powerSystem.netCursor;
					PowerNetwork[] netPool = factory.powerSystem.netPool;
					PowerNodeComponent[] nodePool = factory.powerSystem.nodePool;
					PowerGeneratorComponent[] genPool = factory.powerSystem.genPool;
					float num15 = 0f;
					float num16 = 0f;
					float num17 = 0f;
					float num18 = 4900f;
					bool windForcedPower = buildPreview.desc.windForcedPower;
					for (int n = 1; n < netCursor; n++)
					{
						if (netPool[n] == null || netPool[n].id == 0)
						{
							continue;
						}
						List<Node> nodes = netPool[n].nodes;
						int count2 = nodes.Count;
						num18 = 4900f;
						for (int num19 = 0; num19 < count2; num19++)
						{
							float num20 = x - nodes[num19].x;
							num15 = y - nodes[num19].y;
							num16 = z - nodes[num19].z;
							num17 = num20 * num20 + num15 * num15 + num16 * num16;
							if (num17 < num18 && (num17 < nodes[num19].connDistance2 || num17 < num14))
							{
								buildPreview.nearestPowerObjId[n] = nodePool[nodes[num19].id].entityId;
								num18 = num17;
							}
							if (windForcedPower && nodes[num19].genId > 0 && genPool[nodes[num19].genId].id == nodes[num19].genId && genPool[nodes[num19].genId].wind && num17 < 110.25f)
							{
								buildPreview.condition = EBuildCondition.WindTooClose;
							}
							else if (!buildPreview.desc.isPowerGen && nodes[num19].genId == 0 && num17 < 12.25f)
							{
								buildPreview.condition = EBuildCondition.PowerTooClose;
							}
							else if (num17 < 12.25f)
							{
								buildPreview.condition = EBuildCondition.PowerTooClose;
							}
						}
					}
					PrebuildData[] prebuildPool = factory.prebuildPool;
					int prebuildCursor = factory.prebuildCursor;
					num18 = 4900f;
					for (int num21 = 1; num21 < prebuildCursor; num21++)
					{
						if (prebuildPool[num21].id != num21 || prebuildPool[num21].protoId < 2199 || prebuildPool[num21].protoId > 2299)
						{
							continue;
						}
						float num22 = x - prebuildPool[num21].pos.x;
						num15 = y - prebuildPool[num21].pos.y;
						num16 = z - prebuildPool[num21].pos.z;
						num17 = num22 * num22 + num15 * num15 + num16 * num16;
						if (!(num17 < num18))
						{
							continue;
						}
						ItemProto itemProto2 = LDB.items.Select(prebuildPool[num21].protoId);
						if (itemProto2 != null && itemProto2.prefabDesc.isPowerNode)
						{
							if (num17 < itemProto2.prefabDesc.powerConnectDistance * itemProto2.prefabDesc.powerConnectDistance || num17 < num14)
							{
								buildPreview.nearestPowerObjId[0] = -num21;
								num18 = num17;
							}
							if (windForcedPower && itemProto2.prefabDesc.windForcedPower && num17 < 110.25f)
							{
								buildPreview.condition = EBuildCondition.WindTooClose;
							}
							else if (!buildPreview.desc.isPowerGen && !itemProto2.prefabDesc.isPowerGen && num17 < 12.25f)
							{
								buildPreview.condition = EBuildCondition.PowerTooClose;
							}
							else if (num17 < 12.25f)
							{
								buildPreview.condition = EBuildCondition.PowerTooClose;
							}
						}
					}
				}
				if (buildPreview.desc.isCollectStation)
				{
					if (planet == null || planet.gasItems == null || planet.gasItems.Length == 0)
					{
						buildPreview.condition = EBuildCondition.OutOfReach;
						continue;
					}
					for (int num23 = 0; num23 < planet.gasItems.Length; num23++)
					{
						double num24 = 0.0;
						if ((double)buildPreview.desc.stationCollectSpeed * planet.gasTotalHeat != 0.0)
						{
							num24 = 1.0 - (double)buildPreview.desc.workEnergyPerTick / ((double)buildPreview.desc.stationCollectSpeed * planet.gasTotalHeat * 0.016666666666666666);
						}
						if (num24 <= 0.0)
						{
							buildPreview.condition = EBuildCondition.NotEnoughEnergyToWorkCollection;
						}
					}
					float y2 = cursorTarget.y;
					if (y2 > 0.1f || y2 < -0.1f)
					{
						buildPreview.condition = EBuildCondition.BuildInEquator;
						continue;
					}
				}
				if (buildPreview.desc.isStation)
				{
					StationComponent[] stationPool = factory.transport.stationPool;
					int stationCursor = factory.transport.stationCursor;
					PrebuildData[] prebuildPool2 = factory.prebuildPool;
					int prebuildCursor2 = factory.prebuildCursor;
					EntityData[] entityPool = factory.entityPool;
					float num25 = 225f;
					float num26 = 841f;
					float num27 = 14297f;
					num26 = (buildPreview.desc.isCollectStation ? num27 : num26);
					for (int num28 = 1; num28 < stationCursor; num28++)
					{
						if (stationPool[num28] != null && stationPool[num28].id == num28)
						{
							float num29 = ((stationPool[num28].isStellar || buildPreview.desc.isStellarStation) ? num26 : num25);
							if ((entityPool[stationPool[num28].entityId].pos - vector).sqrMagnitude < num29)
							{
								buildPreview.condition = EBuildCondition.TowerTooClose;
							}
						}
					}
					for (int num30 = 1; num30 < prebuildCursor2; num30++)
					{
						if (prebuildPool2[num30].id != num30)
						{
							continue;
						}
						ItemProto itemProto3 = LDB.items.Select(prebuildPool2[num30].protoId);
						if (itemProto3 != null && itemProto3.prefabDesc.isStation)
						{
							float num31 = ((itemProto3.prefabDesc.isStellarStation || buildPreview.desc.isStellarStation) ? num26 : num25);
							float num32 = vector.x - prebuildPool2[num30].pos.x;
							float num33 = vector.y - prebuildPool2[num30].pos.y;
							float num34 = vector.z - prebuildPool2[num30].pos.z;
							if (num32 * num32 + num33 * num33 + num34 * num34 < num31)
							{
								buildPreview.condition = EBuildCondition.TowerTooClose;
							}
						}
					}
				}
				if (!buildPreview.desc.isInserter && vector.magnitude - planet.realRadius + buildPreview.desc.cullingHeight > 4.9f && !buildPreview.desc.isEjector)
				{
					EjectorComponent[] ejectorPool = factory.factorySystem.ejectorPool;
					int ejectorCursor = factory.factorySystem.ejectorCursor;
					PrebuildData[] prebuildPool3 = factory.prebuildPool;
					int prebuildCursor3 = factory.prebuildCursor;
					EntityData[] entityPool2 = factory.entityPool;
					Vector3 ext = buildPreview.desc.buildCollider.ext;
					float num35 = Mathf.Sqrt(ext.x * ext.x + ext.z * ext.z);
					float num36 = 7.2f + num35;
					for (int num37 = 1; num37 < ejectorCursor; num37++)
					{
						if (ejectorPool[num37].id == num37 && (entityPool2[ejectorPool[num37].entityId].pos - vector).sqrMagnitude < num36 * num36)
						{
							buildPreview.condition = EBuildCondition.EjectorTooClose;
						}
					}
					for (int num38 = 1; num38 < prebuildCursor3; num38++)
					{
						if (prebuildPool3[num38].id != num38)
						{
							continue;
						}
						ItemProto itemProto4 = LDB.items.Select(prebuildPool3[num38].protoId);
						if (itemProto4 != null && itemProto4.prefabDesc.isEjector)
						{
							float num39 = vector.x - prebuildPool3[num38].pos.x;
							float num40 = vector.y - prebuildPool3[num38].pos.y;
							float num41 = vector.z - prebuildPool3[num38].pos.z;
							if (num39 * num39 + num40 * num40 + num41 * num41 < num36 * num36)
							{
								buildPreview.condition = EBuildCondition.EjectorTooClose;
							}
						}
					}
				}
				if (buildPreview.desc.isEjector)
				{
					GetOverlappedObjectsNonAlloc(vector, 12f, 14.5f);
					for (int num42 = 0; num42 < BuildTool._overlappedCount; num42++)
					{
						PrefabDesc prefabDesc = GetPrefabDesc(BuildTool._overlappedIds[num42]);
						Vector3 position = GetObjectPose(BuildTool._overlappedIds[num42]).position;
						if (position.magnitude - planet.realRadius + prefabDesc.cullingHeight > 4.9f)
						{
							float num43 = vector.x - position.x;
							float num44 = vector.y - position.y;
							float num45 = vector.z - position.z;
							float num46 = num43 * num43 + num44 * num44 + num45 * num45;
							Vector3 ext2 = prefabDesc.buildCollider.ext;
							float num47 = Mathf.Sqrt(ext2.x * ext2.x + ext2.z * ext2.z);
							float num48 = 7.2f + num47;
							if (prefabDesc.isEjector)
							{
								num48 = 10.6f;
							}
							if (num46 < num48 * num48)
							{
								buildPreview.condition = EBuildCondition.BlockTooClose;
							}
						}
					}
				}
				if ((!buildPreview.desc.multiLevel || buildPreview.inputObjId == 0) && !buildPreview.desc.isInserter)
				{
					RaycastHit hitInfo;
					for (int num49 = 0; num49 < buildPreview.desc.landPoints.Length; num49++)
					{
						Vector3 vector10 = buildPreview.desc.landPoints[num49];
						vector10.y = 0f;
						Vector3 origin = vector + quaternion * vector10;
						Vector3 normalized = origin.normalized;
						origin += normalized * 3f;
						Vector3 direction = -normalized;
						float num50 = 0f;
						float num51 = 0f;
						if (flag2)
						{
							Vector3 vector11 = cursorTarget.normalized * planet.realRadius * 0.025f;
							origin -= vector11;
						}
						if (Physics.Raycast(new Ray(origin, direction), out hitInfo, 5f, 8704, QueryTriggerInteraction.Collide))
						{
							num50 = hitInfo.distance;
							if (hitInfo.point.magnitude - factory.planet.realRadius < -0.3f)
							{
								buildPreview.condition = EBuildCondition.NeedGround;
								continue;
							}
							num51 = ((!Physics.Raycast(new Ray(origin, direction), out hitInfo, 5f, 16, QueryTriggerInteraction.Collide)) ? 1000f : hitInfo.distance);
							if (num50 - num51 > 0.27f)
							{
								buildPreview.condition = EBuildCondition.NeedGround;
							}
						}
						else
						{
							buildPreview.condition = EBuildCondition.NeedGround;
						}
					}
					for (int num52 = 0; num52 < buildPreview.desc.waterPoints.Length; num52++)
					{
						if (factory.planet.waterItemId <= 0)
						{
							buildPreview.condition = EBuildCondition.NeedWater;
							continue;
						}
						Vector3 vector12 = buildPreview.desc.waterPoints[num52];
						vector12.y = 0f;
						Vector3 origin2 = vector + quaternion * vector12;
						Vector3 normalized2 = origin2.normalized;
						origin2 += normalized2 * 3f;
						Vector3 direction2 = -normalized2;
						float num53 = 0f;
						float num54 = 0f;
						num53 = ((!Physics.Raycast(new Ray(origin2, direction2), out hitInfo, 5f, 8704, QueryTriggerInteraction.Collide)) ? 1000f : hitInfo.distance);
						if (Physics.Raycast(new Ray(origin2, direction2), out hitInfo, 5f, 16, QueryTriggerInteraction.Collide))
						{
							num54 = hitInfo.distance;
							if (num53 - num54 <= 0.27f)
							{
								buildPreview.condition = EBuildCondition.NeedWater;
							}
						}
						else
						{
							buildPreview.condition = EBuildCondition.NeedWater;
						}
					}
				}
				if (buildPreview.desc.isInserter && buildPreview.condition == EBuildCondition.Ok)
				{
					bool flag5 = ObjectIsBelt(buildPreview.inputObjId) || (buildPreview.input != null && buildPreview.input.desc.isBelt);
					bool flag6 = ObjectIsBelt(buildPreview.outputObjId) || (buildPreview.output != null && buildPreview.output.desc.isBelt);
					Vector3 zero = Vector3.zero;
					Vector3 vector13 = ((buildPreview.output == null) ? GetObjectPose(buildPreview.outputObjId).position : buildPreview.output.lpos);
					Vector3 vector14 = ((buildPreview.input == null) ? GetObjectPose(buildPreview.inputObjId).position : buildPreview.input.lpos);
					zero = ((flag5 && !flag6) ? vector13 : ((!(!flag5 && flag6)) ? ((vector13 + vector14) * 0.5f) : vector14));
					float num55 = base.actionBuild.planetAux.mainGrid.CalcSegmentsAcross(zero, buildPreview.lpos, buildPreview.lpos2);
					float num56 = num55;
					float magnitude = forward2.magnitude;
					float num57 = 5.5f;
					float num58 = 0.6f;
					float num59 = 3.499f;
					float num60 = 0.88f;
					if (flag5 && flag6)
					{
						num58 = 0.4f;
						num57 = 5f;
						num59 = 3.2f;
						num60 = 0.8f;
					}
					else if (!flag5 && !flag6)
					{
						num58 = 0.9f;
						num57 = 7.5f;
						num59 = 3.799f;
						num60 = 1.451f;
						num56 -= 0.3f;
					}
					if (magnitude > num57)
					{
						buildPreview.condition = EBuildCondition.TooFar;
						continue;
					}
					if (magnitude < num58)
					{
						buildPreview.condition = EBuildCondition.TooClose;
						continue;
					}
					if (num55 > num59)
					{
						buildPreview.condition = EBuildCondition.TooFar;
						continue;
					}
					if (num55 < num60)
					{
						buildPreview.condition = EBuildCondition.TooClose;
						continue;
					}
					int oneParameter = Mathf.RoundToInt(Mathf.Clamp(num56, 1f, 3f));
					buildPreview.SetOneParameter(oneParameter);
				}

			}
			bool flag7 = true;
			for (int num61 = 0; num61 < base.buildPreviews.Count; num61++)
			{
				BuildPreview buildPreview3 = base.buildPreviews[num61];
				if (buildPreview3.condition != 0 && buildPreview3.condition != EBuildCondition.NeedConn)
				{
					flag7 = false;
					base.actionBuild.model.cursorState = -1;
					base.actionBuild.model.cursorText = buildPreview3.conditionText;
					if (buildPreview3.condition == EBuildCondition.OutOfVerticalConstructionHeight && !flag)
					{
						base.actionBuild.model.cursorText += "垂直建造可升级".Translate();
					}
					break;
				}
			}
			if (flag7)
			{
				base.actionBuild.model.cursorState = 0;
				base.actionBuild.model.cursorText = "点击鼠标建造".Translate();
			}
			if (!flag7 && !VFInput.onGUI)
			{
				UICursor.SetCursor(ECursor.Ban);
			}
			return flag7;
		}

		public bool ConfirmOperation(bool condition)
		{
			if (VFInput._buildConfirm.onUp)
			{
				base.controller.cmd.stage = 0;
				isDragging = false;
				if (waitForConfirm)
				{
					return condition;
				}
			}
			if (!VFInput._buildConfirm.pressing)
			{
				base.controller.cmd.stage = 0;
				isDragging = false;
			}
			return false;
		}

		public void CreatePrebuilds()
		{
			bool flag = false;
			int num = 0;
			tmp_links.Clear();
			TrarckPlugin.Instance.NeedResetBuildPreview = false;
			FactoryBP factoryBP = TrarckPlugin.Instance.factoryBP;

			foreach (BuildPreview buildPreview in base.buildPreviews)
			{
				if (buildPreview.desc.isBelt)
				{
					if (buildPreview.isConnNode)
					{
						buildPreview.lrot = Maths.SphericalRotation(buildPreview.lpos, 0f);
					}
					PrebuildData prebuild = default(PrebuildData);
					prebuild.protoId = (short)buildPreview.item.ID;
					prebuild.modelIndex = (short)buildPreview.desc.modelIndex;
					prebuild.pos = buildPreview.lpos;
					prebuild.pos2 = buildPreview.lpos;
					prebuild.rot = Maths.SphericalRotation(buildPreview.lpos, 0f);
					prebuild.rot2 = prebuild.rot;
					prebuild.pickOffset = 0;
					prebuild.insertOffset = 0;
					prebuild.recipeId = buildPreview.recipeId;
					prebuild.filterId = buildPreview.filterId;
					prebuild.InitParametersArray(buildPreview.paramCount);
					for (int i = 0; i < buildPreview.paramCount; i++)
					{
						prebuild.parameters[i] = buildPreview.parameters[i];
					}

					if (buildPreview.genNearColliderArea2 < 0.001f)
					{
						//parse cover
						int overlappedCount = factoryBP.GetOverlappedObjectsNonAlloc(buildPreview.lpos, 0.3f, 3f, false, _overlappedIds);
						YHDebug.LogFormat("CreatePrebuilds_Prefix:belt over {0}", overlappedCount);
						if (overlappedCount > 0)
						{
							int objId = _overlappedIds[0];
							bool isBelt = FactoryHelper.ObjectIsBelt(player.factory, objId);
							if (isBelt)
							{
								buildPreview.coverObjId = objId;
								buildPreview.willRemoveCover = false;

								TrarckPlugin.Instance.NeedResetBuildPreview = true;
							}
						}
					}
					flag = true;
					if (buildPreview.coverObjId == 0 || buildPreview.willRemoveCover)
					{
						int itemId = buildPreview.item.ID;
						int count;
						if (base.player.inhandItemId == itemId && base.player.inhandItemCount > 0)
						{
							count = 1;
							base.player.UseHandItems(count);
						}
						else
						{
							count = 1;
							base.player.package.TakeTailItems(ref itemId, ref count);
						}
						flag = count == 1;
					}
					if (flag)
					{
						if (buildPreview.coverObjId == 0)
						{
							buildPreview.objId = -factory.AddPrebuildDataWithComponents(prebuild);
						}
						else if (buildPreview.willRemoveCover)
						{
							int coverObjId = buildPreview.coverObjId;
							if (ObjectIsBelt(coverObjId))
							{
								for (int j = 0; j < 4; j++)
								{
									factory.ReadObjectConn(coverObjId, j, out var isOutput, out var otherObjId, out var otherSlot);
									num = otherObjId;
									if (num == 0 || !ObjectIsBelt(num))
									{
										continue;
									}
									bool flag2 = false;
									for (int k = 0; k < 2; k++)
									{
										factory.ReadObjectConn(num, k, out isOutput, out otherObjId, out otherSlot);
										if (otherObjId != 0)
										{
											bool num2 = ObjectIsBelt(otherObjId);
											bool flag3 = ObjectIsInserter(otherObjId);
											if (!num2 && !flag3)
											{
												flag2 = true;
												break;
											}
										}
									}
									if (flag2)
									{
										tmp_links.Add(num);
									}
								}
							}
							if (buildPreview.coverObjId > 0)
							{
								Array.Copy(factory.entityConnPool, buildPreview.coverObjId * 16, tmp_conn, 0, 16);
								for (int l = 0; l < 16; l++)
								{
									factory.ReadObjectConn(buildPreview.coverObjId, l, out var _, out var otherObjId2, out var otherSlot2);
									if (otherObjId2 > 0)
									{
										factory.ApplyEntityDisconnection(otherObjId2, buildPreview.coverObjId, otherSlot2, l);
									}
								}
								Array.Clear(factory.entityConnPool, buildPreview.coverObjId * 16, 16);
							}
							else
							{
								Array.Copy(factory.prebuildConnPool, -buildPreview.coverObjId * 16, tmp_conn, 0, 16);
								Array.Clear(factory.prebuildConnPool, -buildPreview.coverObjId * 16, 16);
							}
							buildPreview.objId = -factory.AddPrebuildDataWithComponents(prebuild);
							if (buildPreview.objId > 0)
							{
								Array.Copy(tmp_conn, 0, factory.entityConnPool, buildPreview.objId * 16, 16);
							}
							else
							{
								Array.Copy(tmp_conn, 0, factory.prebuildConnPool, -buildPreview.objId * 16, 16);
							}
							factory.EnsureObjectConn(buildPreview.objId);
						}
						else
						{
							buildPreview.objId = buildPreview.coverObjId;
						}
					}
					else
					{
						Assert.CannotBeReached();
						UIRealtimeTip.Popup("物品不足".Translate(), sound: true, 1);
					}
				}
				else
				{
					if (buildPreview.desc.isInserter)
					{
						//parse cover
						if (buildPreview.input == null)
						{
							int overlappedCount = factoryBP.GetOverlappedObjectsNonAlloc(buildPreview.lpos, 0.3f, 3f, false, _overlappedIds);
							YHDebug.LogFormat("CreatePrebuilds_Prefix:insert input over {0}", overlappedCount);
							if (overlappedCount > 0)
							{
								int objId = _overlappedIds[0];
								bool isBelt = FactoryHelper.ObjectIsBelt(player.factory, objId);
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
								bool isBelt = FactoryHelper.ObjectIsBelt(player.factory, objId);
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


					if (buildPreview.condition == EBuildCondition.Ok && buildPreview.coverObjId == 0)
					{
						if (buildPreview.isConnNode)
						{
							buildPreview.lrot = Maths.SphericalRotation(buildPreview.lpos, 0f);
						}
						PrebuildData prebuild = default(PrebuildData);
						prebuild.protoId = (short)buildPreview.item.ID;
						prebuild.modelIndex = (short)buildPreview.desc.modelIndex;
						prebuild.pos = buildPreview.lpos;
						prebuild.pos2 = buildPreview.lpos2;
						prebuild.rot = buildPreview.lrot;
						prebuild.rot2 = buildPreview.lrot2;
						prebuild.pickOffset = (short)buildPreview.inputOffset;
						prebuild.insertOffset = (short)buildPreview.outputOffset;
						prebuild.recipeId = buildPreview.recipeId;
						prebuild.filterId = buildPreview.filterId;
						prebuild.InitParametersArray(buildPreview.paramCount);
						for (int i = 0; i < buildPreview.paramCount; i++)
						{
							prebuild.parameters[i] = buildPreview.parameters[i];
						}
						int itemId = buildPreview.item.ID;
						int count;
						if (base.player.inhandItemId == itemId && base.player.inhandItemCount > 0)
						{
							count = 1;
							base.player.UseHandItems(count);
						}
						else
						{
							count = 1;
							base.player.package.TakeTailItems(ref itemId, ref count);
						}
						if (count == 1)
						{
							buildPreview.objId = -factory.AddPrebuildDataWithComponents(prebuild);
						}
						else
						{
							Assert.CannotBeReached();
							UIRealtimeTip.Popup("物品不足".Translate(), sound: true, 1);
						}
					}
				}
			}
			flag = false;
			num = 0;
			foreach (BuildPreview buildPreview2 in base.buildPreviews)
			{
				if (buildPreview2.coverObjId == 0)
				{
					continue;
				}
				if (buildPreview2.willRemoveCover)
				{
					int error = 0;
					if (base.actionBuild.DoUpgradeObject(buildPreview2.coverObjId, buildPreview2.item.Grade, 0, out error))
					{
						num++;
					}
					if (error == 1)
					{
						flag = true;
					}
				}
				BuildingParameters buildingParameters = default(BuildingParameters);
				buildingParameters.CopyFromBuildPreview(buildPreview2);
				if (buildingParameters.PasteToFactoryObject(buildPreview2.coverObjId, factory))
				{
					num++;
				}
				buildPreview2.objId = buildPreview2.coverObjId;
				//if (buildPreview2.desc.isBelt)
				//{
				//	if (buildPreview2.objId != 0)
				//	{
				//		if (buildPreview2.outputObjId != 0)
				//		{
				//			factory.WriteObjectConn(buildPreview2.objId, buildPreview2.outputFromSlot, isOutput: true, buildPreview2.outputObjId, buildPreview2.outputToSlot);
				//		}
				//		else if (buildPreview2.output != null)
				//		{
				//			factory.WriteObjectConn(buildPreview2.objId, buildPreview2.outputFromSlot, isOutput: true, buildPreview2.output.objId, buildPreview2.outputToSlot);
				//		}
				//		if (buildPreview2.inputObjId != 0)
				//		{
				//			factory.WriteObjectConn(buildPreview2.objId, buildPreview2.inputToSlot, isOutput: false, buildPreview2.inputObjId, buildPreview2.inputFromSlot);
				//		}
				//		else if (buildPreview2.input != null)
				//		{
				//			factory.WriteObjectConn(buildPreview2.objId, buildPreview2.inputToSlot, isOutput: false, buildPreview2.input.objId, buildPreview2.inputFromSlot);
				//		}
				//	}
				//}
				//else
				//{
				//	if (buildPreview2.coverObjId == 0)
				//	{
				//		continue;
				//	}
				//	if (buildPreview2.willRemoveCover)
				//	{
				//		int error = 0;
				//		if (base.actionBuild.DoUpgradeObject(buildPreview2.coverObjId, buildPreview2.item.Grade, 0, out error))
				//		{
				//			num++;
				//		}
				//		if (error == 1)
				//		{
				//			flag = true;
				//		}
				//	}
				//	BuildingParameters buildingParameters = default(BuildingParameters);
				//	buildingParameters.CopyFromBuildPreview(buildPreview2);
				//	if (buildingParameters.PasteToFactoryObject(buildPreview2.coverObjId, factory))
				//	{
				//		num++;
				//	}
				//	buildPreview2.objId = buildPreview2.coverObjId;
				//}
			}
			if (num > 0)
			{
				VFAudio.Create("ui-click-2", null, Vector3.zero, play: true, 7);
			}
			if (flag)
			{
				VFAudio.Create("ui-error", null, Vector3.zero, play: true, 5);
				UIRealtimeTip.Popup("升级物品不足".Translate(), sound: false);
			}
			foreach (BuildPreview buildPreview3 in base.buildPreviews)
			{
				if (buildPreview3.condition == EBuildCondition.Ok && buildPreview3.objId != 0)
				{
					if (buildPreview3.outputObjId != 0)
					{
						factory.WriteObjectConn(buildPreview3.objId, buildPreview3.outputFromSlot, isOutput: true, buildPreview3.outputObjId, buildPreview3.outputToSlot);
					}
					else if (buildPreview3.output != null)
					{
						factory.WriteObjectConn(buildPreview3.objId, buildPreview3.outputFromSlot, isOutput: true, buildPreview3.output.objId, buildPreview3.outputToSlot);
					}
					if (buildPreview3.inputObjId != 0)
					{
						factory.WriteObjectConn(buildPreview3.objId, buildPreview3.inputToSlot, isOutput: false, buildPreview3.inputObjId, buildPreview3.inputFromSlot);
					}
					else if (buildPreview3.input != null)
					{
						factory.WriteObjectConn(buildPreview3.objId, buildPreview3.inputToSlot, isOutput: false, buildPreview3.input.objId, buildPreview3.inputFromSlot);
					}
				}
			}

			//belt
			foreach (BuildPreview buildPreview3 in base.buildPreviews)
			{
				if (buildPreview3.coverObjId == 0 || !buildPreview3.willRemoveCover || buildPreview3.objId == 0 || !ObjectIsBelt(buildPreview3.objId))
				{
					continue;
				}
				factory.ReadObjectConn(buildPreview3.objId, 0, out var isOutput3, out var otherObjId3, out var otherSlot3);
				if (otherObjId3 != 0 && isOutput3 && ObjectIsBelt(buildPreview3.objId))
				{
					factory.ReadObjectConn(otherObjId3, 0, out isOutput3, out var otherObjId4, out otherSlot3);
					if (otherObjId4 == buildPreview3.objId)
					{
						factory.ClearObjectConn(otherObjId3, 0);
					}
				}
			}
			foreach (BuildPreview buildPreview4 in base.buildPreviews)
			{
				if (buildPreview4.coverObjId != 0 && buildPreview4.willRemoveCover)
				{
					base.actionBuild.DoDismantleObject(buildPreview4.coverObjId);
				}
			}
			foreach (int tmp_link in tmp_links)
			{
				actionBuild.DoDismantleObject(tmp_link);
			}
			foreach (BuildPreview buildPreview5 in base.buildPreviews)
			{
				if (buildPreview5.objId < 0 && !buildPreview5.needModel)
				{
					factory.PostRefreshPrebuildDisplay(-buildPreview5.objId);
				}
			}
			if (!multiLevelCovering && PlayerController.buildTargetAutoMove)
			{
				actionBuild.buildTargetPositionWanted = castGroundPosSnapped;
			}

			if (TrarckPlugin.Instance.NeedResetBuildPreview)
			{
				TrarckPlugin.Instance.factoryBP.ResetBuildPreviewsRealChanges();
			}
			GC.Collect();
		}

		public override void UpdatePreviewModels(BuildModel model)
		{
			model.previewModelCursor = 0;
			for (int i = 0; i < base.buildPreviews.Count; i++)
			{
				BuildPreview buildPreview = base.buildPreviews[i];
				if (buildPreview.needModel)
				{
					model.AddPreviewModel(buildPreview);
				}
			}
			for (int j = model.previewModelCursor; j < model.previewModelPoolLength; j++)
			{
				model.DisablePreviewModel(j);
			}
		}

		private static void ExpandConnGraph(ConnGizmoGraph connGizmoGraph)
		{
			if (connGizmoGraph.pointCount >= connGizmoGraph.points.Length)
			{
				int newLen = connGizmoGraph.points.Length * 2;
				Vector3[] pointsCurrent = new Vector3[newLen];
				connGizmoGraph.pointsCurrent.CopyTo(pointsCurrent, 0);
				connGizmoGraph.pointsCurrent = pointsCurrent;

				Vector3[] points = new Vector3[newLen];
				connGizmoGraph.points.CopyTo(points, 0);
				connGizmoGraph.points = points;

				var colors = new uint[newLen];
				connGizmoGraph.colors.CopyTo(colors, 0);
				connGizmoGraph.colors = colors;
			}
		}
		public override void UpdatePreviewModelConditions(BuildModel model)
		{
			foreach (BuildPreview buildPreview in base.buildPreviews)
			{
				if (buildPreview.isConnNode)
				{
					ExpandConnGraph(model.connGraph);
					model.connGraph.AddPoint(buildPreview.lpos, (buildPreview.condition == EBuildCondition.Ok || buildPreview.condition == EBuildCondition.NeedExport) ? 4u : 0u);
				}
			}

			if (base.buildPreviews.Count == 0 && base.controller.cmd.stage == 0 && cursorValid)
			{
				model.connGraph.AddPoint(base.controller.cmd.target, 4u);
			}
			model.connGraph.SetPointCount(model.connGraph.pointCount, setCurrent: true);

			foreach (BuildPreview buildPreview in base.buildPreviews)
			{
				if (buildPreview.previewIndex < 0)
				{
					continue;
				}
				Pose pose = model.SetPreviewModelPose(buildPreview);
				if (buildPreview.item.prefabDesc.isInserter)
				{
					bool t = buildPreview.input != null || (buildPreview.inputObjId != 0 && !ObjectIsBelt(buildPreview.inputObjId));
					bool t2 = buildPreview.output != null || (buildPreview.outputObjId != 0 && !ObjectIsBelt(buildPreview.outputObjId));
					Quaternion rotation = pose.rotation;
					rotation.w = 0f - rotation.w;
					Material originMaterial = Configs.builtin.previewErrorMat_Inserter;
					if (buildPreview.condition == EBuildCondition.Ok)
					{
						originMaterial = Configs.builtin.previewOkMat_Inserter;
					}
					else if (buildPreview.condition == EBuildCondition.NeedConn)
					{
						originMaterial = Configs.builtin.previewIgnoreMat_Inserter;
					}
					Material material = model.SetPreviewModelMaterial(buildPreview.previewIndex, originMaterial);
					material.SetVector("_Position1", Vector3BoolToVector4(rotation * (buildPreview.lpos - pose.position), t));
					material.SetVector("_Position2", Vector3BoolToVector4(rotation * (buildPreview.lpos2 - pose.position), t2));
					material.SetVector("_Rotation1", QuaternionToVector4(rotation * buildPreview.lrot));
					material.SetVector("_Rotation2", QuaternionToVector4(rotation * buildPreview.lrot2));
				}
				else
				{
					model.SetPreviewModelMaterial(buildPreview.previewIndex, (buildPreview.condition == EBuildCondition.Ok) ? Configs.builtin.previewOkMat : Configs.builtin.previewErrorMat);
				}
			}
		}

		public override void UpdateGizmos(BuildModel model)
		{
			base.UpdateGizmos(model);
			int num = 1;
			List<BuildPreview> templatePreviews = base.actionBuild.templatePreviews;
			if (templatePreviews.Count > 0)
			{
				num = templatePreviews.Count;
			}
			int num2 = base.buildPreviews.Count - num;
			if (num2 < 0)
			{
				num2 = 0;
			}
			BuildPreview buildPreview = ((base.buildPreviews.Count > 0) ? base.buildPreviews[num2] : null);
			if (buildPreview == null)
			{
				return;
			}
			if (buildPreview.desc.minerType == EMinerType.Vein)
			{
				model.previewGizmoOn = true;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				while (buildPreview.parameters != null && num5 < buildPreview.paramCount)
				{
					VeinData veinData = factory.veinPool[buildPreview.parameters[num5]];
					if (num3 == 0)
					{
						num3 = veinData.productId;
					}
					num4 += veinData.amount;
					num5++;
				}
				if (num3 > 0 && num4 > 0)
				{
					UIResourceTip.Show(buildPreview.lpos + buildPreview.lrot.Up() * 3f, num3, num4, 0f);
				}
			}
			if ((buildPreview.desc.portPoses != null && buildPreview.desc.portPoses.Length != 0) || (buildPreview.desc.slotPoses != null && buildPreview.desc.slotPoses.Length != 0))
			{
				model.previewGizmoOn = true;
			}
		}

		public override void NotifyBuilt(int preObjId, int postObjId)
		{
			base.NotifyBuilt(preObjId, postObjId);
			if (castObjectId == preObjId)
			{
				castObjectId = postObjId;
			}
		}

		public override void NotifyDismantled(int objId)
		{
			base.NotifyDismantled(objId);
			if (castObjectId == objId)
			{
				castObjectId = 0;
			}
		}

		public override void EscLogic()
		{
			bool num = !VFInput._godModeMechaMove;
			bool flag = VFInput.rtsCancel.onDown || VFInput.escKey.onDown || VFInput.escape || VFInput._buildModeKey.onDown;
			bool flag2 = !VFInput.onGUI && VFInput.inScreen;
			YHDebug.LogFormat("esc logic:{0},{1},{2}", num, flag, flag2);
			if (num && flag && flag2)
			{
				VFInput.UseBuildKey();
				VFInput.UseEscape();
				VFInput.UseRtsCancel();
				if (base.controller.cmd.stage == 0)
				{
					base.player.SetHandItems(0, 0);
					_Close();
				}
				else
				{
					base.controller.cmd.stage = 0;
				}
			}
		}
	}
}
