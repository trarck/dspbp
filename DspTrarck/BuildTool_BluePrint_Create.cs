using System;
using System.Collections.Generic;
using UnityEngine;

namespace DspTrarck
{

	public class BuildTool_BluePrint_Create : BuildTool
	{
		public int cursorType;

		public int cursorSize = 3;

		public bool filterFacility = true;

		public bool filterBelt = true;

		public bool filterInserter = true;

		public bool chainReaction;

		public static bool showDemolishContainerQuery = true;

		public int handItemBeforeExtraMode;

		public bool castTerrain;

		public bool castPlatform;

		public bool castGround;

		public Vector3 castGroundPos = Vector3.zero;

		public Vector3 castGroundPosSnapped = Vector3.zero;

		public bool castObject;

		public int castObjectId;

		public Vector3 castObjectPos;

		public bool cursorValid;

		public Vector3 cursorTarget;

		private HashSet<EntityData> m_SelectEntities;
		//key:entity id
		private Dictionary<EntityData, BoxGizmo> m_SelectEntitiesGizmo;
		private List<EntityData> m_NeedRemoveGizmosKeys;


		protected override void _OnInit()
		{
			m_SelectEntities = new HashSet<EntityData>();
			m_SelectEntitiesGizmo = new Dictionary<EntityData, BoxGizmo>();
			m_NeedRemoveGizmosKeys = new List<EntityData>();
		}

		protected override void _OnFree()
		{
			m_SelectEntities = null;
			m_SelectEntitiesGizmo = null;
			m_NeedRemoveGizmosKeys = null;
		}

		protected override void _OnOpen()
		{
			Debug.Log("bp create open");
			controller.cmd.stage = 1;
		}

		protected override void _OnClose()
		{
			Debug.Log("bp create close");

			m_SelectEntities.Clear();
			m_NeedRemoveGizmosKeys.Clear();

			foreach (var iter in m_SelectEntitiesGizmo)
			{
				iter.Value.Close();
			}
			m_SelectEntitiesGizmo.Clear();
		}

		protected override void _OnTick(long time)
		{
			UpdateRaycast();
			DeterminePreviews();
			UpdateCollidersForCursor();
			UpdatePreviewModels(actionBuild.model);
			UpdateGizmos(actionBuild.model);
			CreateAction();
		}

		public override bool DetermineActive()
		{
			return controller.cmd.type == ECommand.Build &&  TrarckPlugin.Instance.BPCreate;
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
					castGroundPosSnapped = base.actionBuild.planetAux.Snap(castGroundPos, castTerrain);
					base.controller.cmd.test = castGroundPosSnapped;
					Vector3 normalized = castGroundPosSnapped.normalized;
					if (Physics.Raycast(new Ray(castGroundPosSnapped + normalized * 10f, -normalized), out hitInfo, 20f, 8720, QueryTriggerInteraction.Collide))
					{
						base.controller.cmd.test = hitInfo.point;
					}
					cursorTarget = castGroundPosSnapped;
					cursorValid = true;
				}
				int castAllCount = base.controller.cmd.raycast.castAllCount;
				RaycastData[] castAll = base.controller.cmd.raycast.castAll;
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				float num4 = float.MaxValue;
				float num5 = float.MaxValue;
				float num6 = float.MaxValue;
				for (int i = 0; i < castAllCount; i++)
				{
					if (castAll[i].objType == EObjectType.Entity || castAll[i].objType == EObjectType.Prebuild)
					{
						int num7 = ((castAll[i].objType == EObjectType.Entity) ? castAll[i].objId : (-castAll[i].objId));
						if (ObjectIsInserter(num7))
						{
							num = num7;
							num4 = castAll[i].rch.dist;
							break;
						}
					}
				}
				for (int j = 0; j < castAllCount; j++)
				{
					if (castAll[j].objType == EObjectType.Entity || castAll[j].objType == EObjectType.Prebuild)
					{
						int num8 = ((castAll[j].objType == EObjectType.Entity) ? castAll[j].objId : (-castAll[j].objId));
						if (ObjectIsBelt(num8))
						{
							num2 = num8;
							num5 = castAll[j].rch.dist;
							break;
						}
					}
				}
				for (int k = 0; k < castAllCount; k++)
				{
					if (castAll[k].objType == EObjectType.Entity || castAll[k].objType == EObjectType.Prebuild)
					{
						num3 = ((castAll[k].objType == EObjectType.Entity) ? castAll[k].objId : (-castAll[k].objId));
						num6 = castAll[k].rch.dist;
						break;
					}
				}
				if (num > 0)
				{
					num4 -= 2f;
				}
				if (num2 > 0)
				{
					num5 -= 2f;
				}
				if (num != 0 && num4 < num5 && num4 < num6)
				{
					castObject = true;
					castObjectId = num;
					castObjectPos = GetObjectPose(num).position;
				}
				else if (num2 != 0 && num5 < num4 && num5 < num6)
				{
					castObject = true;
					castObjectId = num2;
					castObjectPos = GetObjectPose(num2).position;
				}
				else if (num3 != 0)
				{
					castObject = true;
					castObjectId = num3;
					castObjectPos = GetObjectPose(num3).position;
				}
			}
			if (castObject)
			{
				cursorTarget = castObjectPos;
				base.controller.cmd.test = castObjectPos;
				cursorValid = true;
			}
			base.controller.cmd.state = (cursorValid ? 1 : 0);
			base.controller.cmd.target = (cursorValid ? cursorTarget : Vector3.zero);
		}

		public void DeterminePreviews()
		{
			buildPreviews.Clear();
			if (Input.GetKeyDown(KeyCode.F1))
			{
				cursorType = 0;
			}
			if (Input.GetKeyDown(KeyCode.F2))
			{
				cursorType = 2;
			}

			if (cursorType == 0)
			{
				if (castObjectId > 0)
				{
					EntityData entityData = factory.GetEntityData(castObjectId);
					if (VFInput.alt)
					{
						m_SelectEntities.Remove(entityData);
					}
					else
					{
						m_SelectEntities.Add(entityData);
					}

				}
			}
			else if (cursorType == 1)
			{
				Vector4 zero = Vector4.zero;
				if (VFInput._cursorPlusKey.onDown)
				{
					cursorSize++;
				}
				if (VFInput._cursorMinusKey.onDown)
				{
					cursorSize--;
				}
				if (cursorSize < 1)
				{
					cursorSize = 1;
				}
				else if (cursorSize > 11)
				{
					cursorSize = 11;
				}
				if (castGround)
				{
					GetOverlappedObjectsNonAlloc(castGroundPos, 1.5f * (float)cursorSize, 1.5f * (float)cursorSize, ignoreAltitude: true);
					for (int i = 0; i < _overlappedCount; i++)
					{
						if (_overlappedIds[i] > 0)
						{
							EntityData entityData = factory.GetEntityData(_overlappedIds[i]);
							if (VFInput.alt)
							{
								m_SelectEntities.Remove(entityData);
							}
							else
							{
								m_SelectEntities.Add(entityData);
							}
						}
					}
				}
			}

			//Debug.LogFormat("select {0}", m_SelectEntities.Count);
		}

		public override void UpdateGizmos(BuildModel model)
		{
			base.UpdateGizmos(model);

			//check add
			foreach (var entityData in m_SelectEntities)
			{
				//Debug.LogFormat("gizmos:{0}", entityData.id);
				//belt and prebuild no gizmos
				if (entityData.id == 0 )		//		|| entityData.id <= 0
				{
					continue;
				}

				BoxGizmo gizmo = null;
				if (!m_SelectEntitiesGizmo.TryGetValue(entityData, out gizmo))
				{
					gizmo = CreateEntityGizmo(entityData);
					gizmo.Open();
					m_SelectEntitiesGizmo[entityData] = gizmo;
				}
			}

			//check removed
			m_NeedRemoveGizmosKeys.Clear();
			foreach (var iter in m_SelectEntitiesGizmo)
			{
				if (!m_SelectEntities.Contains(iter.Key))
				{
					iter.Value.Close();
					m_NeedRemoveGizmosKeys.Add(iter.Key);
				}
			}

			foreach (var k in m_NeedRemoveGizmosKeys)
			{
				m_SelectEntitiesGizmo.Remove(k);
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
			bool onDown = VFInput._buildModeKey.onDown;
			bool flag = VFInput.rtsCancel.onDown || VFInput.escKey.onDown || VFInput.escape || onDown;
			bool flag2 = !VFInput.onGUI && VFInput.inScreen;
			if (!(num && flag && flag2))
			{
				return;
			}
			VFInput.UseBuildKey();
			VFInput.UseEscape();
			VFInput.UseRtsCancel();
			if (base.controller.cmd.stage == 0)
			{
				base.controller.cmd.mode = 0;
				if (handItemBeforeExtraMode > 0 || onDown)
				{
					_Close();
				}
				else
				{
					actionBuild.Close();
				}
			}
			else
			{
				controller.cmd.stage = 0;
			}
		}

		public void CreateAction()
		{
			if (VFInput._buildConfirm.onDown  && m_SelectEntities.Count > 0)
			{
				Debug.LogFormat("Create bp {0}", m_SelectEntities.Count);
				if (GameMain.localPlanet != null)
				{
					//过滤
					List<EntityData> filterEntities = FilterEntitis(m_SelectEntities);
					Debug.LogFormat("Create bp after filter {0}", filterEntities.Count);
					//排序 by entity id
					filterEntities.Sort(SortEntityDataCompare);
					List<EntityData> sortedEntities = filterEntities;
					Debug.LogFormat("Create bp after sort {0}", sortedEntities.Count);
					CreateBluePrint(sortedEntities);
				}
				m_SelectEntities.Clear();
			}
		}

		private int SortEntityDataCompare(EntityData a, EntityData b)
		{
			return a.id - b.id;
		}

		private List<EntityData> FilterEntitis(IEnumerable<EntityData> entities)
		{
			bool noPowerNode = TrarckPlugin.Instance.factoryBPUI.isWithoutPowerNode;
			bool noBelt = TrarckPlugin.Instance.factoryBPUI.isWithoutBelt;

			List<EntityData> filterResults = new List<EntityData>(entities);

			//不要电线杆，会有碰撞问题,新版本会修复
			if (noPowerNode)
			{
				for (int i = filterResults.Count - 1; i >= 0; --i)
				{
					if (filterResults[i].powerNodeId != 0)
					{
						filterResults.RemoveAt(i);
					}
				}
			}

			//不要传送带
			if (noBelt)
			{
				//filter etities
				for (int i = filterResults.Count - 1; i >= 0; --i)
				{
					if (filterResults[i].beltId != 0)
					{
						filterResults.RemoveAt(i);
					}
				}
			}

			return filterResults;
		}

		private void CreateBluePrint(List<EntityData> entities)
		{
			string bpName = TrarckPlugin.Instance.factoryBPUI.bpName;
			if (string.IsNullOrEmpty(bpName))
			{
				bpName = GetDefaultName();
				TrarckPlugin.Instance.factoryBPUI.bpName = bpName;
			}
			TrarckPlugin.Instance.factoryBP.CopyEntities(bpName, entities, BPData.PosType.Relative);
		}

		private string GetDefaultName()
		{
			DateTime now = DateTime.Now;
			string name = now.ToString("yyyy-MM-dd-HH-mm-ss");
			return name;
		}

		private BoxGizmo CreateEntityGizmo(EntityData entityData)
		{
			Debug.LogFormat("create gizmo {0}", entityData.id);
			if (entityData.id <= 0)         //|| entityData.beltId != 0
			{
				return null;
			}

			BoxGizmo gizmo = null;

			ItemProto itemProto2 = LDB.items.Select(entityData.protoId);
			Vector3 position = entityData.pos;
			float num = itemProto2.prefabDesc.buildCollider.ext.magnitude * 0.7f;
			if (entityData.inserterId == 0)
			{
				gizmo = BoxGizmo.Create(entityData.pos, entityData.rot, itemProto2.prefabDesc.selectCenter, itemProto2.prefabDesc.selectSize);
			}
			else
			{
				ColliderData colliderData2 = player.planetData.physics.GetColliderData(entityData.colliderId);
				colliderData2.ext.x += 0.1f;
				gizmo = BoxGizmo.Create(colliderData2.pos, colliderData2.q, Vector3.zero, colliderData2.ext * 2f);
			}
			gizmo.multiplier = 1f;
			gizmo.alphaMultiplier = itemProto2.prefabDesc.selectAlpha;
			gizmo.fadeInScale = 1.3f;
			gizmo.fadeInTime = 0.05f;
			gizmo.fadeInFalloff = 0.5f;
			gizmo.fadeOutScale = 1.3f;
			gizmo.fadeOutTime = 0.05f;
			gizmo.fadeOutFalloff = 0.5f;
			gizmo.color = Color.white;
			return gizmo;
		}
	}
}