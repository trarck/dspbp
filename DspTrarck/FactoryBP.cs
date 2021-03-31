using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

		public void Clean()
		{
			currentData = null;
			planetData = null;
			m_PlanetFactory = null;
			m_PlanetCoordinate = null;
			m_PrebuildDatas = null;
		}

		public void Reset()
		{
			m_BuildPos = Vector3.zero;
			m_PrebuildDatas.Clear();
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

			TryCreateBuildPrevies(currentData, ref m_BuildPreviews, ref m_CurrentEntities);
		}

		public bool TryCreateBuildPrevies(BPData data, ref List<BuildPreview> buildPreviews, ref List<BPEntityData> entities)
		{
			if (data != null && data.entities.Count > 0)
			{
				//复制的时候entity id到新建立的BuildPreview的映射。
				Dictionary<int, BuildPreview> entitiesIdToBuildPreviewMap = new Dictionary<int, BuildPreview>();

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
				if (data.connects != null && data.connects.Count > 0)
				{
					BuildPreview other = null;
					foreach (var connect in data.connects)
					{
						//Debug.LogFormat("[{0}]connect:{1},{2},{3},{4},{5}", connect.isOutput ? "output" : "input", connect.fromObjId, connect.fromSlot, connect.toObjId, connect.toSlot, connect.offset);
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
									buildPreview.outputOffset = connect.offset;
								}
								else
								{
									Debug.LogFormat("Can't find to entity {0}", connect.toObjId);
								}
							}
							else
							{
								Debug.LogFormat("Can't find from entity {0}", connect.fromObjId);
							}
						}
						else
						{
							//抓子会同时有input和output。
							if (entitiesIdToBuildPreviewMap.TryGetValue(connect.fromObjId, out buildPreview))
							{
								if (entitiesIdToBuildPreviewMap.TryGetValue(connect.toObjId, out other))
								{
									buildPreview.input = other;
									buildPreview.inputObjId = 0;
									buildPreview.inputFromSlot = connect.toSlot;
									buildPreview.inputToSlot = connect.fromSlot;
									buildPreview.inputOffset = connect.offset;
								}
								else
								{
									Debug.LogFormat("Can't find to entity {0}", connect.toObjId);
								}
							}
							else
							{
								Debug.LogFormat("Can't find from entity {0}", connect.fromObjId);
							}
						}
					}
				}

				return true;
			}
			return false;	
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

		public void UpdateBuildPosition(Vector3 pos)
		{
			m_BuildPos = pos;
			UpdateBuildPreviewsPosition(pos);
		}

		public void UpdateBuildPreviewsPosition(Vector3 pos)
		{
			//更新build preview的坐标。
			if (m_BuildPreviews != null && m_BuildPreviews.Count > 0)
			{

				Vector3 gcs = m_PlanetCoordinate.LocalToGcs(pos);

				Vector2Int buildCell = Vector2Int.zero;
				if (currentData.posType == BPData.PosType.Relative)
				{
					buildCell = m_PlanetCoordinate.GcsToCell(gcs);
				}

				for (int i = 0; i < m_BuildPreviews.Count; ++i)
				{
					BPEntityData entityData = m_CurrentEntities[i];
					BuildPreview buildPreview = m_BuildPreviews[i];
					SetBuildPreviewPosition(buildPreview, entityData, buildCell, gcs.x);
					buildPreview.condition = EBuildCondition.Ok;
				}
			}
		}
		
		/// <summary>
		/// 更新buildPreview的位置
		/// </summary>
		/// <param name="buildPreview"></param>
		/// <param name="bpEntity"></param>
		/// <param name="cellOffset"></param>
		public void SetBuildPreviewPosition(BuildPreview buildPreview, BPEntityData bpEntity, Vector2Int buildCell,float longitude)
		{
			Vector2Int entityCell = m_PlanetCoordinate.GcsOffset(buildCell, longitude, bpEntity.gcsCellIndex);
			Vector3 posNormal = m_PlanetCoordinate.CellToNormal(entityCell);
			buildPreview.lpos = m_PlanetCoordinate.NormalToGround(posNormal) + posNormal * bpEntity.offsetGround;

			//Debug.LogFormat("SetBuildPreviewPosition:cell={0},offset={1},pos={2},proto={3},type={4},entityId={5}", bpEntity.gcsCellIndex , entityCell, buildPreview.lpos,bpEntity.protoId,bpEntity.type,bpEntity.entityId);

			if (bpEntity.type == BPEntityType.Inserter)
			{
				entityCell = m_PlanetCoordinate.GcsOffset(buildCell, longitude, bpEntity.gcsCellIndex2);
				posNormal = m_PlanetCoordinate.CellToNormal(entityCell);
				buildPreview.lpos2 = m_PlanetCoordinate.NormalToGround(posNormal) + posNormal * bpEntity.offsetGround2;
				//Debug.LogFormat("SetBuildPreviewPosition2:cell={0},offset={1},pos={2}", bpEntity.gcsCellIndex2, cellOffset, buildPreview.lpos2);
			}
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

			SetEntityGcsCell(bpEntity, entity);

			return bpEntity;
		}

		public void SetEntityGcsCell(BPEntityData bpEntity, EntityData entity)
		{
			//直角坐标转换成格子坐标。保留原坐标，比对作用。
			bpEntity.gcsCellIndex = m_PlanetCoordinate.LocalToCell(bpEntity.pos);
			//Debug.LogFormat("height:{0},{1},{2}", bpEntity.pos.magnitude, planetData.radius,bpEntity.pos.magnitude-planetData.radius);
			bpEntity.offsetGround = Mathf.Max(0, bpEntity.pos.magnitude - planetData.radius-0.2f);

			if (bpEntity.type == BPEntityType.Inserter)
			{
				bpEntity.gcsCellIndex2 = m_PlanetCoordinate.LocalToCell(bpEntity.pos2);
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
				bpEntity.type = BPEntityType.Splitter;
			}
			else if (entity.storageId > 0)
			{
				bpEntity.type = BPEntityType.Storage;
			}
			else if (entity.tankId > 0)
			{
				bpEntity.type = BPEntityType.Tank;
			}
			else if (entity.minerId > 0)
			{
				bpEntity.type = BPEntityType.Miner;
				MinerComponent minerComponent = m_PlanetFactory.factorySystem.minerPool[entity.minerId];
				bpEntity.refCount = minerComponent.veinCount;
				if (bpEntity.refCount > 0)
				{
					bpEntity.refArr = new int[bpEntity.refCount];
					Array.Copy(minerComponent.veins, bpEntity.refArr, bpEntity.refCount);
				}
			}
			else if (entity.inserterId > 0)
			{
				bpEntity.type = BPEntityType.Inserter;
				InserterComponent inserterComponent = m_PlanetFactory.factorySystem.inserterPool[entity.inserterId];
				ItemProto itemProto = LDB.items.Select(entity.protoId);
				bpEntity.refCount = (int)((inserterComponent.stt - 0.499f) / itemProto.prefabDesc.inserterSTT);
				bpEntity.filterId = inserterComponent.filter;
				bpEntity.pos2 = inserterComponent.pos2;
				bpEntity.rot2 = inserterComponent.rot2;
				bpEntity.pickOffset = inserterComponent.pickOffset;
				bpEntity.insertOffset = inserterComponent.insertOffset;
				if (bpEntity.refCount > 0)
				{
					bpEntity.refArr = new int[bpEntity.refCount];
				}
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
			}
			else if (entity.stationId > 0)
			{
				bpEntity.type = BPEntityType.Station;
			}
			else if (entity.ejectorId > 0)
			{
				bpEntity.type = BPEntityType.Ejector;
				EjectorComponent ejectorComponent = m_PlanetFactory.factorySystem.ejectorPool[entity.ejectorId];
				bpEntity.recipeId = ejectorComponent.orbitId;
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
			}
			else if (entity.powerConId > 0)
			{
				bpEntity.type = BPEntityType.PowerConsumer;
			}
			else if (entity.powerExcId > 0)
			{
				bpEntity.type = BPEntityType.PowerExchanger;
				PowerExchangerComponent powerExchangerComponent = m_PlanetFactory.powerSystem.excPool[entity.powerExcId];
				bpEntity.recipeId = (int)powerExchangerComponent.targetState;
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
			buildPreview.ResetInfos();
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

			buildPreview.refCount = bpEntity.refCount;
			if (bpEntity.refCount > 0)
			{
				buildPreview.refArr = new int[bpEntity.refCount];
				Array.Copy(bpEntity.refArr, buildPreview.refArr, bpEntity.refCount);
			}

			buildPreview.previewIndex = -1;
			buildPreview.isConnNode = bpEntity.type == BPEntityType.Belt;

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
			prebuildData.refCount = bpEntity.refCount;
			if (bpEntity.refCount > 0)
			{
				prebuildData.refArr = new int[bpEntity.refCount];
				Array.Copy(bpEntity.refArr, prebuildData.refArr, bpEntity.refCount);
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

			foreach (var entity in entities)
			{
				BPEntityData bpEntity = CreateBPEntity(entity);
				data.entities.Add(bpEntity);
				//Debug.Log(JsonUtility.ToJson(bpEntity));
			}

			//更新连接
			UpdateEntitiesConnects(data);

			//Debug.LogFormat("entities count:{0}", data.entities.Count);
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

				//抓子的input和output
				foreach (var bpEntity in bpData.entities)
				{
					if (bpEntity.type != BPEntityType.Inserter)
					{
						continue;
					}
					CreateEntityBothConnect(bpEntity, ref bpData.connects);
				}

				foreach (var bpEntity in bpData.entities)
				{
					//抓子已经处理过
					if (bpEntity.type == BPEntityType.Inserter)
					{
						continue;
					}
					CreateEntityOutputConnect(bpEntity, ref bpData.connects);
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
				//Debug.LogFormat("both connect:{0},{1},{2},{3},{4}",bpEntity.entityId,i, isOutput, otherObjId, otherSlot);

				if (otherObjId != 0)
				{
					ConnectData connect = new ConnectData();
					connect.fromObjId = bpEntity.entityId;
					connect.toObjId = otherObjId;
					connect.fromSlot = i;
					connect.toSlot = otherSlot;
					connect.isOutput = isOutput;
					connect.offset = 0;

					connects.Add(connect);
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
				//Debug.LogFormat("output connect:{0},{1},{2},{3},{4}",bpEntity.entityId, i, isOutput, otherObjId, otherSlot);

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
					connect.offset = 0;

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

			int latCellMin = int.MaxValue, latCellMax = int.MinValue;
			//计算经纬度包围盒
			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
				latMin = Math.Min(latMin, gcs.y);
				latMax = Math.Max(latMax, gcs.y);

				latCellMin = Math.Min(latCellMin, entityData.gcsCellIndex.y);
				latCellMax = Math.Max(latCellMax, entityData.gcsCellIndex.y);

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

			//Debug.LogFormat("lat:{0},{1}", latCellMin, latCellMax);

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

			//Debug.LogFormat("rect:{0},{1},{2},{3}", positiveLongMin, positiveLongMax, negativeLongMin, negativeLongMax);

			//fix cell index
			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
				if (needFixNegativeLong && gcs.x < 0)
				{
					gcs.x += negativeAdd;
				}
				gcs.x -= longMin;
				//这里要保证维度是原来的维度。这里的经度已经是偏移的。转换成cell后，就是偏移的cell。
				Vector2Int cellOffset = m_PlanetCoordinate.GcsToCell(gcs);
				//Debug.LogFormat("e:{0},{1},{2}", gcs, cellOffset, latCellMin);
				//偏移维度
				cellOffset.y -= latCellMin;
				entityData.gcsCellIndex = cellOffset;

				//只有爪子有第二个位置
				if (entityData.type == BPEntityType.Inserter)
				{
					gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos2);
					if (needFixNegativeLong && gcs.x < 0)
					{
						gcs.x += negativeAdd;
					}
					gcs.x -= longMin;
					cellOffset = m_PlanetCoordinate.GcsToCell(gcs);
					cellOffset.y -= latCellMin;
					entityData.gcsCellIndex2 = cellOffset;
				}
			}

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

			Vector2Int originalCell = m_PlanetCoordinate.GcsToCell(originalGcs);

			Vector3 gcs;
			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos);
				//偏移经度
				gcs.x = YH.YHMath.DeltaRadian(gcs.x, originalGcs.x);
				//这里要保证维度是原来的维度。
				//这里的经度已经是偏移的。转换成cell后，就是偏移的cell。
				Vector2Int cellOffset = m_PlanetCoordinate.GcsToCell(gcs);
				//偏移维度
				cellOffset.y -= originalCell.y;
				entityData.gcsCellIndex = cellOffset;

				//只有爪子有第二个位置
				if (entityData.type == BPEntityType.Inserter)
				{
					gcs = m_PlanetCoordinate.LocalToGcs(entityData.pos2);
					//偏移经度
					gcs.x = YH.YHMath.DeltaRadian(gcs.x, originalGcs.x);
					//这里要保证维度是原来的维度。
					//这里的经度已经是偏移的。转换成cell后，就是偏移的cell。
					cellOffset = m_PlanetCoordinate.GcsToCell(gcs);
					//偏移维度
					cellOffset.y -= originalCell.y;
					entityData.gcsCellIndex2 = cellOffset;
				}
			}
		}

		public BPData LoadBPData(string name)
		{
			//return LoadBPDataJson(name);
			return LoadBPDataBinary(name);
		}

		public BPData LoadBPDataJson(string name)
		{
			BPData bpData = null;
			string fileName = Path.Combine(bpDir, name)+".json";
			if (File.Exists(fileName))
			{
				string jsonStr = File.ReadAllText(fileName);
				bpData = JsonUtility.FromJson<BPData>(jsonStr);
			}
			return bpData;
		}

		public BPData LoadBPDataBinary(string name)
		{
			BPData bpData = null;
			string fileName = Path.Combine(bpDir, name) + ".bin";
			if (File.Exists(fileName))
			{
				bpData = BPDataReader.ReadBPDataFromFile(fileName);
			}
			return bpData;
		}

		public void SaveBPData(BPData bpData)
		{
			if (bpData != null)
			{
				//SaveBPDataJson(bpData);
				SaveBPDataBinary(bpData);
			}
		}

		public void SaveBPDataJson(BPData bpData)
		{
			string fileName = Path.Combine(bpDir, bpData.name+".json");
			string jsonStr = JsonUtility.ToJson(bpData); 
			File.WriteAllText(fileName, jsonStr);
		}

		public void SaveBPDataBinary(BPData bpData)
		{
			string fileName = Path.Combine(bpDir, bpData.name+".bin");
			BPDataWriter.WriteBPDataToFile(fileName, bpData);
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
				groundSnappedPos = planetAux.Snap(groundTestPos, castTerrain, castTerrain && planetData.levelized);

				return true;
			}

			return false;
		}
		public Vector3 GetPlanetLocalPosition(Vector2Int cellIndex)
		{
			Vector3 localNormal = m_PlanetCoordinate.CellToNormal(cellIndex);
			return m_PlanetCoordinate.NormalToGround(localNormal);
		}
	}
}
