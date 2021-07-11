using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YH.Log;

namespace DspTrarck
{
	public class FactoryBP
	{
		public string bpDir = Environment.CurrentDirectory + "\\BepInEx\\config\\DspTrarck";

		public BPData currentData;

		public PlanetData planetData;
		public Player player;

		private PlanetCoordinate m_PlanetCoordinate;

		private PlanetFactory m_PlanetFactory;

		//建造位置
		private Vector3 m_BuildPos;

		private List<BuildPreview> m_BuildPreviews;
		//当前使用的Entity。和buildpreview一一对应。
		private List<BPEntityData> m_CurrentEntities;
		//原entity id到新建立的BuildPreview的映射。
		private Dictionary<int, BuildPreview> m_EntitiesIdToBuildPreviewMap;

		private List<PrebuildData> m_PrebuildDatas;


		public Vector3 buildPos
		{
			get
			{
				return m_BuildPos;
			}
			set
			{
				m_BuildPos = value;
			}
		}
		
		public PlanetCoordinate planetCoordinate
		{
			get
			{
				return m_PlanetCoordinate;
			}
		}

		public List<BuildPreview> buildPreviews
		{
			get
			{
				return m_BuildPreviews;
			}
			set
			{
				m_BuildPreviews = value;
			}
		}

		public List<PrebuildData> prebuildDatas
		{
			get
			{
				return m_PrebuildDatas;
			}
			set
			{
				m_PrebuildDatas = value;
			}
		}

		public PlayerController playerController
		{
			get
			{
				if (player != null)
				{
					return player.controller;
				}
				return null;
			}
		}

		public PlayerAction_Build playerActionBuild
		{
			get
			{
				if (playerController != null)
				{
					return playerController.actionBuild;
				}
				return null;
			}
		}
		public void Init()
		{
			m_PlanetCoordinate = new PlanetCoordinate();

			//创建bp保存目录
			if (!string.IsNullOrEmpty(bpDir) && !System.IO.Directory.Exists(bpDir))
			{
				System.IO.Directory.CreateDirectory(bpDir);
			}
		}

		public void Clean()
		{
			currentData = null;
			planetData = null;
			player = null;
			m_PlanetFactory = null;
			m_PlanetCoordinate = null;
			m_BuildPreviews = null;
			m_CurrentEntities = null;
			m_EntitiesIdToBuildPreviewMap = null;
			m_PrebuildDatas = null;
		}

		public void Clear()
		{
			currentData = null;
			planetData = null;
			player = null;
			m_PlanetFactory = null;

			m_BuildPos = Vector3.zero;
			if (m_BuildPreviews != null)
			{
				m_BuildPreviews.Clear();
			}

			if (m_CurrentEntities != null)
			{
				m_CurrentEntities.Clear();
			}

			if (m_EntitiesIdToBuildPreviewMap != null)
			{
				m_EntitiesIdToBuildPreviewMap.Clear();
			}

			if (m_PrebuildDatas != null)
			{
				m_PrebuildDatas.Clear();
			}
		}

		public void SetPlanetData(PlanetData planetData)
		{
			if (this.planetData != planetData)
			{
				this.planetData = planetData;

				m_PlanetFactory = planetData.factory;

				if (planetData != null)
				{
					m_PlanetCoordinate.segment = planetData.aux.mainGrid.segment;
					m_PlanetCoordinate.radius = planetData.realRadius;
				}
			}
		}

		#region Create

		public void CopyEntities(string name, List<EntityData> entities, BPData.PosType posType)
		{
			//crate data
			currentData = CreateBPData(name, entities, posType);
		}

		public void LoadCurrentData(string name)
		{
			//get bp data from file by name
			currentData = LoadBPData(name);
		}

		public void CreateBuildPreviews()
		{

			if (m_BuildPreviews == null)
			{
				m_BuildPreviews = new List<BuildPreview>();
			}
			else
			{
				m_BuildPreviews.Clear();
			}

			if (m_CurrentEntities == null)
			{
				m_CurrentEntities = new List<BPEntityData>();
			}
			else
			{
				m_CurrentEntities.Clear();
			}

			if (m_EntitiesIdToBuildPreviewMap == null)
			{
				m_EntitiesIdToBuildPreviewMap = new Dictionary<int, BuildPreview>();
			}
			else
			{
				m_EntitiesIdToBuildPreviewMap.Clear();
			}

			TryCreateBuildPrevies(currentData, ref m_BuildPreviews, ref m_CurrentEntities,ref m_EntitiesIdToBuildPreviewMap);
		}

		public bool TryCreateBuildPrevies(BPData data, ref List<BuildPreview> buildPreviews, ref List<BPEntityData> entities, ref Dictionary<int,BuildPreview> entitiesIdToBuildPreviewMap)
		{
			if (data != null && data.entities.Count > 0)
			{
				BuildPreview buildPreview = null;
				//创建build previews
				for (int i = 0; i < data.entities.Count; ++i)
				{
					BPEntityData entityData = data.entities[i];
					buildPreview = CreateBuildPreview(entityData);
					buildPreviews.Add(buildPreview);
					entities.Add(entityData);

					entitiesIdToBuildPreviewMap[entityData.entityId] = buildPreview;
				}

				//Debug.LogFormat("map count:{0}", entitiesIdToBuildPreviewMap.Count);

				//处理连接。连接保存的是entity id之间的联系
				ConnectBuildPreviews(data.connects, entitiesIdToBuildPreviewMap);

				//带子首尾设置忽略碰撞
				ParseBeltsCollider(buildPreviews);

				return true;
			}
			return false;	
		}

		private void ConnectBuildPreviews(List<ConnectData> connects, Dictionary<int, BuildPreview> entitiesIdToBuildPreviewMap)
		{
			if (connects != null && connects.Count > 0)
			{
				BuildPreview buildPreview = null;
				BuildPreview other = null;
				
				foreach (var connect in connects)
				{
					YHDebug.LogFormat("[{0}]connect:{1},{2},{3},{4},{5}", connect.isOutput ? "output" : "input", connect.fromObjId, connect.fromSlot, connect.toObjId, connect.toSlot, connect.offset);
					if (connect.isOutput)
					{
						if (entitiesIdToBuildPreviewMap.TryGetValue(connect.fromObjId, out buildPreview))
						{
							if (entitiesIdToBuildPreviewMap.TryGetValue(connect.toObjId, out other))
							{
								buildPreview.output = other;
								buildPreview.outputObjId = 0;
								buildPreview.outputFromSlot = connect.fromSlot;
								buildPreview.outputToSlot = connect.toSlot;
								//buildPreview.outputOffset = connect.offset;
							}
							else
							{
								YHDebug.LogFormat("Can't find to entity {0}", connect.toObjId);
							}
						}
						else
						{
							YHDebug.LogFormat("Can't find from entity {0}", connect.fromObjId);
						}
					}
					else
					{
						//抓子会同时有input和output。传送带和其他建筑的intout关系。
						if (entitiesIdToBuildPreviewMap.TryGetValue(connect.fromObjId, out buildPreview))
						{
							if (entitiesIdToBuildPreviewMap.TryGetValue(connect.toObjId, out other))
							{
								buildPreview.input = other;
								buildPreview.inputObjId = 0;
								buildPreview.inputFromSlot = connect.toSlot;
								buildPreview.inputToSlot = connect.fromSlot;
								//buildPreview.inputOffset = connect.offset;
							}
							else
							{
								YHDebug.LogFormat("Can't find to entity {0}", connect.toObjId);
							}
						}
						else
						{
							YHDebug.LogFormat("Can't find from entity {0}", connect.fromObjId);
						}
					}
				}
			}
		}

		private void ParseBeltsCollider(List<BuildPreview> buildPreviews)
		{
			HashSet<BuildPreview> inputBelts = new HashSet<BuildPreview>();
			HashSet<BuildPreview> outputBelts = new HashSet<BuildPreview>();

			for (int i = 0; i < buildPreviews.Count; ++i)
			{
				BuildPreview buildPreview = buildPreviews[i];
				if (buildPreview.desc.isBelt)
				{
					if (buildPreview.output != null)
					{
						if (buildPreview.output.desc.isBelt)
						{
							//检查输出是不是作为输入存在过
							if (inputBelts.Contains(buildPreview.output))
							{
								//作为输入存在过，则从输入中移除
								inputBelts.Remove(buildPreview.output);
							}
							else
							{
								//没有，则加入输出。
								outputBelts.Add(buildPreview.output);
							}
						}

						//是否是别的输出。
						if (outputBelts.Contains(buildPreview))
						{
							//已经是别的输出，则从输出中移除
							outputBelts.Remove(buildPreview);
						}
						else
						{
							//不是，则加入输入
							inputBelts.Add(buildPreview);
						}
					}
					else
					{
						//on connect or no connect
						buildPreview.genNearColliderArea2 = 0;
					}
				}
			}

			YHDebug.LogFormat("inputs Count:{0},outs Count:{1}", inputBelts.Count, outputBelts.Count);
			foreach (var bp in inputBelts)
			{
				if (bp.desc.isBelt)
				{
					bp.genNearColliderArea2 = 0;
				}
			}

			foreach (var bp in outputBelts)
			{
				if (bp.desc.isBelt)
				{
					bp.genNearColliderArea2 = 0;
				}
			}
		}

		public void CreateCurrentPrebuildDatas()
		{
			if (m_PrebuildDatas == null)
			{
				m_PrebuildDatas = new List<PrebuildData>();
			}
			else
			{
				m_PrebuildDatas.Clear();
			}

			TryCreatePrebuildDatas(currentData,ref m_PrebuildDatas);
		}

		public List<PrebuildData> CreatePrebuildDatas(BPData data)
		{
			List<PrebuildData> prebuildDatas = new List<PrebuildData>();

			TryCreatePrebuildDatas(data, ref prebuildDatas);
			return prebuildDatas;
		}

		public bool TryCreatePrebuildDatas(BPData data,ref List<PrebuildData> prebuildDatas)
		{

			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				PrebuildData prebuildData = CreatePrebuildData(entityData);
				prebuildDatas.Add(prebuildData);
			}

			return true;
		}

		public void UpdateBuildPosition(Vector3 pos, float yaw=0)
		{
			m_BuildPos = pos;
			UpdateBuildPreviewsPosition(pos, yaw);
		}

		public void UpdateBuildPreviewsPosition(Vector3 pos, float yaw)
		{
			//更新build preview的坐标。
			if (m_BuildPreviews != null && m_BuildPreviews.Count > 0)
			{

				Vector3 gcs = m_PlanetCoordinate.LocalToGcs(pos);
				

				Vector2 buildGrid = Vector2.zero;
				if (currentData.posType == BPData.PosType.Relative)
				{
					buildGrid = m_PlanetCoordinate.GcsToGrid(gcs);
				}
				//Debug.LogFormat("UpdatePos:pos:({0},{1},{2}),gcs:({3},{4}),grid:({5},{6})", pos.x, pos.y, pos.z, gcs.x, gcs.y,buildGrid.x,buildGrid.y);
				for (int i = 0; i < m_BuildPreviews.Count; ++i)
				{
					BPEntityData entityData = m_CurrentEntities[i];
					BuildPreview buildPreview = m_BuildPreviews[i];
					SetBuildPreviewPosition(buildPreview, entityData, buildGrid, gcs.x, yaw);
					buildPreview.condition = EBuildCondition.Ok;
				}
			}
		}

		/// <summary>
		/// 更新buildPreview的位置
		/// </summary>
		/// <param name="buildPreview"></param>
		/// <param name="bpEntity"></param>
		/// <param name="buildGrid"></param>
		/// <param name="longitude"></param>
		/// <param name="yaw"></param>
		public void SetBuildPreviewPosition(BuildPreview buildPreview, BPEntityData bpEntity, Vector2 buildGrid,float longitude, float yaw=0)
		{
			Vector2 gridOffset = GetGridOffset(bpEntity.grid, yaw);
			Vector2 entityGrid = m_PlanetCoordinate.GscApplyGridOffset(buildGrid, longitude, gridOffset);
			Vector3 posNormal = m_PlanetCoordinate.GridToNormal(entityGrid);
			buildPreview.lpos = m_PlanetCoordinate.NormalToGround(posNormal) + posNormal * bpEntity.offsetGround;

			//rotation
			Quaternion rot = Maths.SphericalRotation(buildPreview.lpos, yaw);
			buildPreview.lrot = rot * bpEntity.rot;

			YHDebug.LogFormat("SetBuildPreviewPosition:grid={0},offset={1},pos={2},proto={3},type={4},entityId={5},ci={6},yaw={7},buildGrid={8},long={9}",
				bpEntity.grid, entityGrid, buildPreview.lpos,
				bpEntity.protoId, bpEntity.type, bpEntity.entityId,
				gridOffset, yaw	,buildGrid,longitude
				);

			if (bpEntity.type == BPEntityType.Inserter)
			{
				gridOffset = GetGridOffset(bpEntity.grid2, yaw);
				entityGrid = m_PlanetCoordinate.GscApplyGridOffset(buildGrid, longitude, gridOffset);
				posNormal = m_PlanetCoordinate.GridToNormal(entityGrid);
				buildPreview.lpos2 = m_PlanetCoordinate.NormalToGround(posNormal) + posNormal * bpEntity.offsetGround2;

				//rotation
				rot = Maths.SphericalRotation(buildPreview.lpos2, yaw);
				buildPreview.lrot2 = rot * bpEntity.rot2;

				//YHDebug.LogFormat("SetBuildPreviewPosition2:grid={0},offset={1},pos={2},proto={3},type={4},entityId={5},ci={6},yaw={7}", 
				//	bpEntity.grid2, entityGrid, buildPreview.lpos2, 
				//	bpEntity.protoId, bpEntity.type, bpEntity.entityId			  ,
				//	gridOffset,yaw
				//	);
			}
		}

		public void ResetBuildPreviewsRealChanges()
		{
			foreach (var buildPreview in m_BuildPreviews)
			{
				if (buildPreview.desc.isInserter)
				{
					ResetBuildPreviewRealConnect(buildPreview);
				}
				else if (buildPreview.desc.isBelt && buildPreview.genNearColliderArea2 <0.001f)
				{
					ResetBuildPreviewCover(buildPreview);
				}
			}			
		}

		public static void ResetBuildPreviewRealConnect(BuildPreview buildPreview)
		{
			if (buildPreview.inputObjId !=0)
			{
				if (buildPreview.input != null)
				{
					buildPreview.inputObjId = 0;
					YHDebug.LogError("ResetBuildPreviewRealConnect input not null");
				}
				else
				{
					buildPreview.inputObjId = 0;
					buildPreview.inputToSlot = 0;
					buildPreview.inputFromSlot = 0;
					buildPreview.inputOffset = 0;
				}
			}

			if (buildPreview.outputObjId != 0)
			{
				if (buildPreview.output != null)
				{
					buildPreview.outputObjId = 0;
					YHDebug.LogError("ResetBuildPreviewRealConnect output not null");
				}
				else
				{
					buildPreview.outputObjId = 0;
					buildPreview.outputFromSlot = 0;
					buildPreview.outputToSlot = 0;
					buildPreview.outputOffset = 0;
				}
			}
		}

		public static void ResetBuildPreviewCover(BuildPreview buildPreview)
		{
			if (buildPreview.coverObjId != 0)
			{
				buildPreview.coverObjId = 0;
			}
		}

		private Vector2 GetGridOffset(Vector2 gridOffset,float yaw)
		{
			int yawIndex = Mathf.RoundToInt(yaw / 90);
			switch (yawIndex)
			{
				case 0:
					return gridOffset;
				case 1:
					return new Vector2(-gridOffset.y, gridOffset.x);
				case 2:
					return new Vector2(-gridOffset.x, -gridOffset.y);
				case 3:
					return new Vector2(gridOffset.y, -gridOffset.x);
			}

			return gridOffset;
		}
		#endregion

		#region Update

		//public void SaveEntities(string name, List<EntityData> entities, BPData.PosType posType)
		//{
		//	//crate data
		//	BPData bpData = CreateBPData(name, entities, posType);
		//	SaveBPData(bpData);
		//}

		//public void UpdatePosType(BPData.PosType posType)
		//{
		//	if (currentData!=null)
		//	{
		//		currentData.posType = posType;

		//		UpdateBPDataGrid(currentData);

		//		SaveBPData(currentData);
		//	}
		//}


		#endregion

		#region BPEntity
		public BPEntityData CreateBPEntity(EntityData entity)
		{
			BPEntityData bpEntity = new BPEntityData();

			bpEntity.protoId = entity.protoId;
			bpEntity.entityId = entity.id;
			bpEntity.pos = entity.pos;
			bpEntity.rot = entity.rot;

			SetEntityExtData(bpEntity, entity);

			SetEntityGcsGrid(bpEntity, entity);

			return bpEntity;
		}

		public void SetEntityGcsGrid(BPEntityData bpEntity, EntityData entity)
		{
			//直角坐标转换成格子坐标。保留原坐标，比对作用。
			bpEntity.grid = m_PlanetCoordinate.LocalToGrid(bpEntity.pos);
			//Debug.LogFormat("height:{0},{1},{2}", bpEntity.pos.magnitude, planetData.radius,bpEntity.pos.magnitude-planetData.radius);
			bpEntity.offsetGround = Mathf.Max(0, bpEntity.pos.magnitude - planetData.radius-0.2f);

			if (bpEntity.type == BPEntityType.Inserter)
			{
				bpEntity.grid2 = m_PlanetCoordinate.LocalToGrid(bpEntity.pos2);
				bpEntity.offsetGround2 = Mathf.Max(0, bpEntity.pos2.magnitude - planetData.radius - 0.2f);
			}
		}

		public void SetEntityExtData(BPEntityData bpEntity, EntityData entity)
		{
			if (entity.beltId > 0)
			{
				bpEntity.type = BPEntityType.Belt;
			}
			else if (entity.splitterId > 0)
			{
				SplitterComponent[] splitterPool = m_PlanetFactory.cargoTraffic.splitterPool;
				bpEntity.type = BPEntityType.Splitter;
				int splitterId = entity.splitterId;
				bpEntity.filterId = splitterPool[splitterId].outFilter;

				bpEntity.paramCount = 4;
				bpEntity.parameters = new int[bpEntity.paramCount];

				if (splitterPool[splitterId].inPriority)
				{
					int input = splitterPool[splitterId].input0;
					if (input == splitterPool[splitterId].beltA)
					{
						bpEntity.parameters[0] = 1;
					}
					else if (input == splitterPool[splitterId].beltB)
					{
						bpEntity.parameters[1] = 1;
					}
					else if (input == splitterPool[splitterId].beltC)
					{
						bpEntity.parameters[2] = 1;
					}
					else if (input == splitterPool[splitterId].beltD)
					{
						bpEntity.parameters[3] = 1;
					}
				}
				if (splitterPool[splitterId].outPriority)
				{
					int output = splitterPool[splitterId].output0;
					if (output == splitterPool[splitterId].beltA)
					{
						bpEntity.parameters[0] = 1;
					}
					else if (output == splitterPool[splitterId].beltB)
					{
						bpEntity.parameters[1] = 1;
					}
					else if (output == splitterPool[splitterId].beltC)
					{
						bpEntity.parameters[2] = 1;
					}
					else if (output == splitterPool[splitterId].beltD)
					{
						bpEntity.parameters[3] = 1;
					}
				}
			}
			else if (entity.storageId > 0)
			{
				bpEntity.type = BPEntityType.Storage;
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[1];
				bpEntity.parameters[0] = m_PlanetFactory.factoryStorage.storagePool[entity.storageId].bans;
			}
			else if (entity.tankId > 0)
			{
				bpEntity.type = BPEntityType.Tank;
				bpEntity.paramCount = 2;
				bpEntity.parameters = new int[2];
				bpEntity.parameters[0] = m_PlanetFactory.factoryStorage.tankPool[entity.tankId].outputSwitch ? 1 : -1;
				bpEntity.parameters[1] = m_PlanetFactory.factoryStorage.tankPool[entity.tankId].inputSwitch ? 1 : -1;
			}
			else if (entity.minerId > 0)
			{
				bpEntity.type = BPEntityType.Miner;
				MinerComponent minerComponent = m_PlanetFactory.factorySystem.minerPool[entity.minerId];
				bpEntity.paramCount = minerComponent.veinCount;
				if (bpEntity.paramCount > 0)
				{
					bpEntity.parameters = new int[bpEntity.paramCount];
					Array.Copy(minerComponent.veins, bpEntity.parameters, bpEntity.paramCount);
				}
			}
			else if (entity.inserterId > 0)
			{
				bpEntity.type = BPEntityType.Inserter;
				InserterComponent inserterComponent = m_PlanetFactory.factorySystem.inserterPool[entity.inserterId];
				ItemProto itemProto = LDB.items.Select(entity.protoId);
				bpEntity.filterId = inserterComponent.filter;
				bpEntity.pos2 = inserterComponent.pos2;
				bpEntity.rot2 = inserterComponent.rot2;
				bpEntity.pickOffset = inserterComponent.pickOffset;
				bpEntity.insertOffset = inserterComponent.insertOffset;
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[bpEntity.paramCount];
				int num2 = 200000;
				if (itemProto != null)
				{
					num2 = itemProto.prefabDesc.inserterSTT;
				}
				int num3 = (inserterComponent.stt + num2 / 4) / num2;
				bpEntity.parameters[0] = num3;
			}
			else if (entity.assemblerId > 0)
			{
				bpEntity.type = BPEntityType.Assembler;
				AssemblerComponent assemblerComponent = m_PlanetFactory.factorySystem.assemblerPool[entity.assemblerId];
				bpEntity.recipeId = assemblerComponent.recipeId;
			}
			else if (entity.fractionateId > 0)
			{
				bpEntity.type = BPEntityType.Fractionate;
			}
			else if (entity.labId > 0)
			{
				bpEntity.type = BPEntityType.Lab;
				LabComponent labComponent = m_PlanetFactory.factorySystem.labPool[entity.labId];
				bpEntity.recipeId = labComponent.recipeId;
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[1];
				if (labComponent.researchMode)
				{
					bpEntity.parameters[0] = 2;
				}
				else if (labComponent.recipeId > 0)
				{
					bpEntity.parameters[0] = 1;
				}
			}
			else if (entity.stationId > 0)
			{
				bpEntity.type = BPEntityType.Station;
				StationComponent stationComponent = m_PlanetFactory.transport.stationPool[entity.stationId];
				if (stationComponent != null)
				{
					bpEntity.paramCount = 2048;
					bpEntity.parameters = new int[2048];
					int num4 = 0;
					for (int i = 0; i < stationComponent.storage.Length; i++)
					{
						if (!stationComponent.isCollector)
						{
							bpEntity.parameters[num4 + i * 6] = stationComponent.storage[i].itemId;
							bpEntity.parameters[num4 + i * 6 + 1] = (int)stationComponent.storage[i].localLogic;
							bpEntity.parameters[num4 + i * 6 + 2] = (int)stationComponent.storage[i].remoteLogic;
							bpEntity.parameters[num4 + i * 6 + 3] = stationComponent.storage[i].max;
						}
					}
					num4 += 192;
					for (int j = 0; j < stationComponent.slots.Length; j++)
					{
						if (!stationComponent.isCollector)
						{
							bpEntity.parameters[num4 + j * 4] = (int)stationComponent.slots[j].dir;
							bpEntity.parameters[num4 + j * 4 + 1] = stationComponent.slots[j].storageIdx;
						}
					}
					num4 += 128;
					if (!stationComponent.isCollector)
					{
						bpEntity.parameters[num4] = (int)m_PlanetFactory.powerSystem.consumerPool[stationComponent.pcId].workEnergyPerTick;
						bpEntity.parameters[num4 + 1] = _round2int(stationComponent.tripRangeDrones * 100000000.0);
						bpEntity.parameters[num4 + 2] = _round2int(stationComponent.tripRangeShips / 100.0);
						bpEntity.parameters[num4 + 3] = (stationComponent.includeOrbitCollector ? 1 : (-1));
						bpEntity.parameters[num4 + 4] = _round2int(stationComponent.warpEnableDist);
						bpEntity.parameters[num4 + 5] = (stationComponent.warperNecessary ? 1 : (-1));
						bpEntity.parameters[num4 + 6] = stationComponent.deliveryDrones;
						bpEntity.parameters[num4 + 7] = stationComponent.deliveryShips;
					}
					num4 += 64;
				}
			}
			else if (entity.ejectorId > 0)
			{
				bpEntity.type = BPEntityType.Ejector;
				EjectorComponent ejectorComponent = m_PlanetFactory.factorySystem.ejectorPool[entity.ejectorId];
				bpEntity.recipeId = ejectorComponent.orbitId;
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[1];
				bpEntity.parameters[0] = ejectorComponent.orbitId;
			}
			else if (entity.siloId > 0)
			{
				bpEntity.type = BPEntityType.Silo;
			}
			else if (entity.powerGenId > 0)
			{
				bpEntity.type = BPEntityType.PowerGen;
				PowerGeneratorComponent powerGeneratorComponent = m_PlanetFactory.powerSystem.genPool[entity.powerGenId];
				bpEntity.recipeId = powerGeneratorComponent.productId;
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[1];
				bpEntity.parameters[0] = powerGeneratorComponent.productId;
			}
			else if (entity.powerConId > 0)
			{
				bpEntity.type = BPEntityType.PowerConsumer;
			}
			else if (entity.powerExcId > 0)
			{
				bpEntity.type = BPEntityType.PowerExchanger;
				PowerExchangerComponent powerExchangerComponent = m_PlanetFactory.powerSystem.excPool[entity.powerExcId];
				bpEntity.recipeId = Mathf.RoundToInt(powerExchangerComponent.targetState);
				bpEntity.paramCount = 1;
				bpEntity.parameters = new int[1];
				bpEntity.parameters[0] = Mathf.RoundToInt(powerExchangerComponent.targetState);
			}
			else if (entity.powerNodeId > 0)
			{
				bpEntity.type = BPEntityType.PowerNode;
			}
			else if (entity.powerAccId > 0)
			{
				bpEntity.type = BPEntityType.PowerAccumulator;
			}
		}

		public BuildPreview CreateBuildPreview(BPEntityData bpEntity)
		{
			ItemProto item = LDB.items.Select(bpEntity.protoId);
			if (item == null)
			{
				return null;
			}

			PrefabDesc prefabDesc = GetPrefabDesc(item);
			if (prefabDesc == null)
			{
				return null;
			}
			
			BuildPreview buildPreview = new  BuildPreview ();
			buildPreview.ResetAll();
			buildPreview.item = item;
			buildPreview.desc = prefabDesc;
			buildPreview.needModel = bpEntity.type !=BPEntityType.Belt;
			buildPreview.lpos = bpEntity.pos;
			buildPreview.lrot = bpEntity.rot;
			buildPreview.lpos2 = bpEntity.pos2;
			buildPreview.lrot2 = bpEntity.rot2;
			buildPreview.recipeId = bpEntity.recipeId;
			buildPreview.filterId = bpEntity.filterId;

			buildPreview.inputOffset = bpEntity.pickOffset;
			buildPreview.outputOffset = bpEntity.insertOffset;

			buildPreview.paramCount = bpEntity.paramCount;
			if (bpEntity.paramCount > 0)
			{
				buildPreview.parameters = new int[bpEntity.paramCount];
				Array.Copy(bpEntity.parameters, buildPreview.parameters, bpEntity.paramCount);
			}

			buildPreview.previewIndex = -1;
			if (bpEntity.type == BPEntityType.Belt)
			{
				buildPreview.isConnNode = true;
				buildPreview.genNearColliderArea2 =20;
			}


			return buildPreview;
		}

		public PrefabDesc GetPrefabDesc(ItemProto item)
		{
			PrefabDesc prefabDesc = null;

			if (item != null && item.IsEntity)
			{
				int modelIndex = item.ModelIndex;
				int num = item.ModelCount;
				if (num < 1)
				{
					num = 1;
				}

				int modelOffset = 0;

				modelIndex += modelOffset % num;
				ModelProto modelProto = LDB.models.Select(modelIndex);
				if (modelProto != null)
				{
					prefabDesc = modelProto.prefabDesc;
				}
				else
				{
					prefabDesc = item.prefabDesc;
				}
				if (!prefabDesc.hasObject)
				{
					prefabDesc = null;
				}
			}
			return prefabDesc;
		}

		public PrebuildData CreatePrebuildData(BPEntityData bpEntity)
		{
			PrebuildData prebuildData = new PrebuildData();

			prebuildData.protoId = bpEntity.protoId;
			prebuildData.pos = bpEntity.pos;
			prebuildData.rot = bpEntity.rot;
			prebuildData.pos2 = bpEntity.pos2;
			prebuildData.rot2 = bpEntity.rot2;
			prebuildData.pickOffset = bpEntity.pickOffset;
			prebuildData.insertOffset = bpEntity.insertOffset;
			prebuildData.recipeId = bpEntity.recipeId;
			prebuildData.filterId = bpEntity.filterId;
			prebuildData.paramCount = bpEntity.paramCount;
			if (bpEntity.paramCount > 0)
			{
				prebuildData.parameters = new int[bpEntity.paramCount];
				Array.Copy(bpEntity.parameters, prebuildData.parameters, bpEntity.paramCount);
			}

			return prebuildData;
		}

		#endregion

		#region BPData
		public BPData CreateBPData(string name, List<EntityData> entities, BPData.PosType posType)
		{
			//crate data
			BPData data = new BPData();
			data.name = name;
			data.posType = posType;
			data.planetRadius = planetData.realRadius;

			foreach (var entity in entities)
			{
				BPEntityData bpEntity = CreateBPEntity(entity);
				data.entities.Add(bpEntity);
				YHDebug.Log(JsonUtility.ToJson(bpEntity));
			}

			//更新连接
			UpdateEntitiesConnects(data);

			YHDebug.LogFormat("entities count:{0}", data.entities.Count);
			UpdateBPDataGrid(data);
			//Debug.LogFormat("coonect count:{0}", data.connects!=null?data.connects.Count:0);
			//Debug.Log(JsonUtility.ToJson(data));
			return data;
		}

		public void UpdateEntitiesConnects(BPData bpData)
		{
			if (bpData.entities != null && bpData.entities.Count > 0)
			{
				if (bpData.connects == null)
				{
					bpData.connects = new List<ConnectData>();
				}
				else
				{
					bpData.connects.Clear();
				}

				//建筑的connect情况。通过抓子和传送带连接。
				//一个预置体只能保存一个输入一个输出。
				//抓子:一个输入，一个输出。
				//传送带：三个输入，一个输出
				//堆叠建筑:一个输入，一个输出。
				//其他建筑(物流塔，储液灌):输入、输出不固定。连接关系放入传送带。

				//优先处理抓子的input和output。
				foreach (var bpEntity in bpData.entities)
				{
					if (bpEntity.type != BPEntityType.Inserter)
					{
						continue;
					}
					CreateEntityBothConnect(bpEntity, ref bpData.connects);
				}

				//再处理传送带的输出关系
				foreach (var bpEntity in bpData.entities)
				{
					if (bpEntity.type != BPEntityType.Belt)
					{
						continue;
					}
					CreateEntityOutputConnect(bpEntity, ref bpData.connects);
				}

				//再处理其他建筑和传送带的输入关系
				//这是只剩下传送带和其他建筑的关系。传送带之间的关系，已经通过传送带之间的输出关系处理过了。
				foreach (var bpEntity in bpData.entities)
				{
					if (bpEntity.type != BPEntityType.Belt)
					{
						continue;
					}
					CreateEntityInputConnect(bpEntity, ref bpData.connects);
				}
				//再处理堆叠建筑的输入关系
				foreach (var bpEntity in bpData.entities)
				{
					ItemProto itemProto = LDB.items.Select(bpEntity.protoId);
					if (!itemProto.prefabDesc.multiLevel)
					{
						continue;
					}
					//var dict = MultiSelector.SerializeObject(bpEntity);
					//string s = "";
					//foreach (var it in dict)
					//{
					//	s += it.Key + "=" + it.Value + ",";
					//}
					//Debug.LogFormat("堆叠物品:{0}",s);
					CreateEntityInputConnect(bpEntity, ref bpData.connects);
				}
			}
		}

		public void CreateEntityBothConnect(BPEntityData bpEntity,  ref List<ConnectData> connects)
		{
			bool isOutput;
			int otherObjId;
			int otherSlot;

			for (int i = 0; i < 16; ++i)
			{
				m_PlanetFactory.ReadObjectConn(bpEntity.entityId, i, out isOutput, out otherObjId, out otherSlot);
				YHDebug.LogFormat("both connect:{0},{1},{2},{3},{4},{5},{6}",bpEntity.entityId,i, isOutput, otherObjId, otherSlot, bpEntity.pickOffset,bpEntity.insertOffset);

				if (otherObjId != 0)
				{
					ConnectData connect = new ConnectData();
					connect.fromObjId = bpEntity.entityId;
					connect.toObjId = otherObjId;
					connect.fromSlot = i;
					connect.toSlot = otherSlot;
					connect.isOutput = isOutput;

					connect.offset = isOutput ? bpEntity.insertOffset : bpEntity.pickOffset;
					if (!BPData.IsConnectExists(connect, connects))
					{
						connects.Add(connect);
					}
				}
			}
		}

		public void CreateEntityOutputConnect(BPEntityData bpEntity, ref List<ConnectData> connects)
		{
			bool isOutput;
			int otherObjId;
			int otherSlot;

			for (int i = 0; i < 16; ++i)
			{
				m_PlanetFactory.ReadObjectConn(bpEntity.entityId, i, out isOutput, out otherObjId, out otherSlot);
				YHDebug.LogFormat("output connect:{0},{1},{2},{3},{4},{5},{6}",bpEntity.entityId, i, isOutput, otherObjId, otherSlot, bpEntity.pickOffset, bpEntity.insertOffset);

				//连接是相互的,只记录一种连接。
				//如果有截断，则忽略连接。复制的时候没办法补齐另一方。
				//这里只记录output.
				if (otherObjId != 0 && isOutput)
				{
					ConnectData connect = new ConnectData();
					connect.fromObjId = bpEntity.entityId;
					connect.toObjId = otherObjId;
					connect.fromSlot = i;
					connect.toSlot = otherSlot;
					connect.isOutput = isOutput;

					connect.offset = bpEntity.insertOffset;

					if (!BPData.IsConnectExists(connect, connects))
					{
						connects.Add(connect);
					}
				}
			}
		}

		public void CreateEntityInputConnect(BPEntityData bpEntity, ref List<ConnectData> connects)
		{
			bool isOutput;
			int otherObjId;
			int otherSlot;

			for (int i = 0; i < 16; ++i)
			{
				m_PlanetFactory.ReadObjectConn(bpEntity.entityId, i, out isOutput, out otherObjId, out otherSlot);
				YHDebug.LogFormat("output connect:{0},{1},{2},{3},{4},{5},{6}", bpEntity.entityId, i, isOutput, otherObjId, otherSlot, bpEntity.pickOffset, bpEntity.insertOffset);

				//连接是相互的,只记录一种连接。
				//如果有截断，则忽略连接。复制的时候没办法补齐另一方。
				//这里只记录input.
				if (otherObjId != 0 && !isOutput)
				{
					ConnectData connect = new ConnectData();
					connect.fromObjId = bpEntity.entityId;
					connect.toObjId = otherObjId;
					connect.fromSlot = i;
					connect.toSlot = otherSlot;
					connect.isOutput = isOutput;

					connect.offset = bpEntity.insertOffset;

					if (!BPData.IsConnectExists(connect, connects))
					{
						connects.Add(connect);
					}
				}
			}
		}

		public void UpdateBPDataGrid(BPData data)
		{
			if (data.posType == BPData.PosType.Relative)
			{
				//统一化坐标。只有使用相对坐标才能统一化坐标
				NormalizeBPData(data);
			}
		}

		/// <summary>
		/// 设置原点。其他entity都是相对原点位置。
		/// 原点目前使用最小经纬度。后面可以考虑指定。
		/// </summary>
		/// <param name="data"></param>
		public void NormalizeBPData(BPData data)
		{
			Vector3 gcs;

			float latMin = float.MaxValue, latMax = float.MinValue;
			float negativeLongMin = float.MaxValue, negativeLongMax = float.MinValue;
			float positiveLongMin = float.MaxValue, positiveLongMax = float.MinValue;

			float latGridMin = float.MaxValue, latGridMax = float.MinValue;
			//计算经纬度包围盒
			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
				latMin = Math.Min(latMin, gcs.y);
				latMax = Math.Max(latMax, gcs.y);

				latGridMin = Math.Min(latGridMin, entityData.grid.y);
				latGridMax = Math.Max(latGridMax, entityData.grid.y);

				if (gcs.x < 0)
				{
					negativeLongMin = Math.Min(negativeLongMin, gcs.x);
					negativeLongMax = Math.Max(negativeLongMax, gcs.x);
				}
				else
				{
					positiveLongMin = Math.Min(positiveLongMin, gcs.x);
					positiveLongMax = Math.Max(positiveLongMax, gcs.x);
				}
			}

			YHDebug.LogFormat("lat:{0},{1}", latGridMin, latGridMax);

			float longMin = 0, longMax = 0;
			float negativeAdd = 0;
			bool needFixNegativeLong = false;

			if (negativeLongMin == float.MaxValue && negativeLongMax == float.MinValue)
			{
				//只在正边
				longMin = positiveLongMin;
				longMax = positiveLongMax;
			}
			else if (positiveLongMin == float.MaxValue && positiveLongMax == float.MinValue)
			{
				//只在负边
				longMin = negativeLongMin;
				longMax = negativeLongMax;
			}
			else
			{
				//pMin   pMax   nMIn   nMax
				//nMIn nMax  pMin  pMax

				//跨越
				//pMax -> nMin
				float maxDistance = 2 * Mathf.PI + negativeLongMin - positiveLongMax;
				//nMax->pMin
				float minDistance = positiveLongMin - negativeLongMax;

				if (minDistance <= maxDistance)
				{
					longMin = negativeLongMin;
					longMax = positiveLongMax;
				}
				else
				{
					longMin = positiveLongMin;
					longMax = negativeLongMax + 2 * Mathf.PI;

					needFixNegativeLong = true;
					negativeAdd = 2 * Mathf.PI;
				}
			}

			YHDebug.LogFormat("rect:{0},{1},{2},{3}", positiveLongMin, positiveLongMax, negativeLongMin, negativeLongMax);
			YHDebug.LogFormat("long:{0},{1}", longMin, longMax);

			NormalizeEntities(data, new Vector3(longMin, latMin), latGridMin);
			//Debug.LogFormat("Bounds:{0}", data.gridBounds);
			//记录经维度
			data.longitude = longMin;
			data.latitude = latMin;

			//记录原位置
			Vector3 normalPos = m_PlanetCoordinate.GcsToNormal(data.longitude, data.latitude);
			data.originalPos = m_PlanetCoordinate.NormalToGround(normalPos);
		}

		//未完成
		public void NormalizeBPData(BPData data, Vector3 originalPos)
		{
			data.originalPos = originalPos;
			Vector3 originalGcs = m_PlanetCoordinate.LocalToGcs(originalPos);
			data.longitude = originalGcs.x;
			data.latitude = originalGcs.y;

			Vector2 originalGrid = m_PlanetCoordinate.GcsToGrid(originalGcs);

			NormalizeEntities(data, originalGcs, originalGrid.y);
		}

		private void NormalizeEntities(BPData data,Vector3 originalGcs,float originalLatGrid)
		{
			Vector3 gcs;

			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
				//偏移经度
				gcs.x = YH.YHMath.DeltaRadian(originalGcs.x, gcs.x);
				//这里要保证维度是原来的维度。
				//这里的经度已经是偏移的。转换成grid后，就是偏移的grid。
				Vector2 gridOffset = m_PlanetCoordinate.GcsToGrid(gcs);
				YHDebug.LogFormat("e:{0},{1},{2}", gcs.x,gcs.y, gridOffset);
				//偏移维度
				gridOffset.y -= originalLatGrid;
				entityData.grid = gridOffset;

				//rotation
				Quaternion rot = Maths.SphericalRotation(entityData.pos, 0);
				YHDebug.LogFormat("rot:{0},{1},{2},{3}", rot.x, rot.y, rot.z,rot.w);
				YHDebug.LogFormat("erot:{0},{1},{2},{3}", entityData.rot.x, entityData.rot.y, entityData.rot.z, entityData.rot.w);

				Quaternion rotInverse = Quaternion.Inverse(rot);
				entityData.rot = rotInverse * entityData.rot;

				//只有爪子有第二个位置
				if (entityData.type == BPEntityType.Inserter)
				{
					gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos2);
					//偏移经度
					gcs.x = YH.YHMath.DeltaRadian(originalGcs.x, gcs.x);
					//这里要保证维度是原来的维度。
					//这里的经度已经是偏移的。转换成grid后，就是偏移的grid。
					gridOffset = m_PlanetCoordinate.GcsToGrid(gcs);
					YHDebug.LogFormat("e2:{0},{1},{2}", gcs.x, gcs.y, gridOffset);
					//偏移维度
					gridOffset.y -= originalLatGrid;
					entityData.grid2 = gridOffset;

					//rotation
					rot = Maths.SphericalRotation(entityData.pos2, 0);
					YHDebug.LogFormat("rot2:{0},{1},{2},{3}", rot.x, rot.y, rot.z, rot.w);
					YHDebug.LogFormat("erot2:{0},{1},{2},{3}", entityData.rot2.x, entityData.rot2.y, entityData.rot2.z, entityData.rot2.w);
					rotInverse = Quaternion.Inverse(rot);
					entityData.rot2 = rotInverse * entityData.rot2 ;
				}
			}
		}

		public BPData LoadBPData(string fileName)
		{
			//return LoadBPDataJson(name);
			return LoadBPDataBinary(fileName);
		}

		public BPData LoadBPDataJson(string fileName)
		{
			BPData bpData = null;
			if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
			{
				fileName += ".json";
			}

			if (!Path.IsPathRooted(fileName))
			{
				fileName = Path.Combine(bpDir, fileName);
			}

			if (File.Exists(fileName))
			{
				string jsonStr = File.ReadAllText(fileName);
				bpData = JsonUtility.FromJson<BPData>(jsonStr);
			}
			return bpData;
		}

		public BPData LoadBPDataBinary(string fileName)
		{
			BPData bpData = null;

			if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
			{
				fileName += ".bin";
			}
			
			if (!Path.IsPathRooted(fileName))
			{
				fileName = Path.Combine(bpDir, fileName);
			}

			if (File.Exists(fileName))
			{
				bpData = BPDataReader.ReadBPDataFromFile(fileName);
			}
			return bpData;
		}

		public string SaveBPData(BPData bpData)
		{
			if (bpData != null)
			{
				//SaveBPDataJson(bpData);
				return SaveBPDataBinary(bpData);
			}
			return null;
		}

		public string SaveBPDataJson(BPData bpData)
		{
			string filePath = Path.Combine(bpDir, bpData.name+".json");
			string jsonStr = JsonUtility.ToJson(bpData); 
			File.WriteAllText(filePath, jsonStr);
			return filePath;
		}

		public string SaveBPDataBinary(BPData bpData)
		{
			string filePath = Path.Combine(bpDir, bpData.name+".bin");
			YHDebug.LogFormat("dir:{0},file:{1},entities:{2}", bpDir, filePath,bpData.entities.Count);
			string fileDir = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(fileDir))
			{
				Directory.CreateDirectory(fileDir);
			}
			BPDataWriter.WriteBPDataToFile(filePath, bpData);
			return filePath;
		}

		#endregion

		public bool TryScreenPositionToGroundPosition(Vector3 screenPos,ref Vector3 groundSnappedPos)
		{
			if (playerController == null)
			{
				return false;
			}

			int layerMask = 8720;//			((showingAltitude != 0) ? 24576 : 8720);
			Ray currMouseRay = playerController.mainCamera.ScreenPointToRay(screenPos);
			bool castGround = Physics.Raycast(currMouseRay, out var hitInfo, 400f, layerMask, QueryTriggerInteraction.Collide);
			if (!castGround)
			{
				castGround = Physics.Raycast(new Ray(currMouseRay.GetPoint(200f), -currMouseRay.direction), out hitInfo, 200f, layerMask, QueryTriggerInteraction.Collide);
			}

			if (castGround)
			{
				Layer layer = (Layer)hitInfo.collider.gameObject.layer;
				bool castTerrain = layer == Layer.Terrain || layer == Layer.Water;
				bool castGrid = layer == Layer.BuildGrid;
				bool castPlatform = layer == Layer.Platform;

				Vector3 groundTestPos = hitInfo.point;

				PlanetAuxData planetAux = planetData.aux;
				groundSnappedPos = planetAux.Snap(groundTestPos, castTerrain);

				return true;
			}

			return false;
		}
		public Vector3 GetPlanetLocalPosition(Vector2Int cellIndex)
		{
			Vector3 localNormal = m_PlanetCoordinate.CellToNormal(cellIndex);
			return m_PlanetCoordinate.NormalToGround(localNormal);
		}

		public static int[] _nearObjectIds = new int[4096];
		public int GetOverlappedObjectsNonAlloc(Vector3 pos, float objSize, float areaSize, bool ignoreAltitude , int[] overlappedIds)
		{
			int overlappedCount = 0;
			int nearObjectCount = player.planetData.physics.nearColliderLogic.GetBuildingsInAreaNonAlloc(pos, areaSize, _nearObjectIds, ignoreAltitude);
			for (int i = 0; i < nearObjectCount; i++)
			{
				int entityId = _nearObjectIds[i];
				int colliderId = 0;
				ColliderData colliderData = default(ColliderData);
				if (entityId > 0)
				{
					EntityData entityData = player.factory.entityPool[entityId];
					if (entityData.id != entityId)
					{
						continue;
					}
					colliderId = entityData.colliderId;
				}
				else if (entityId < 0)
				{
					PrebuildData prebuildData = player.factory.prebuildPool[-entityId];
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
					colliderData = player.planetData.physics.GetColliderData(colliderId);
					colliderData.ext += new Vector3(objSize, objSize, objSize);
					if (colliderData.ContainsInBox(pos))
					{
						overlappedIds[overlappedCount++] = entityId;
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

		private int _round2int(double d)
		{
			if (!(d > 0.0))
			{
				return (int)(d - 0.5);
			}
			return (int)(d + 0.5);
		}
	}
}
