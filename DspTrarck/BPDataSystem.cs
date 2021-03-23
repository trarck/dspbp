using System;
using System.Collections.Generic;
using UnityEngine;

namespace DspTrarck
{
	public class BPDataSystem
	{
		public BPData data;

		public PlanetData planetData;

		private PlanetCoordinate m_PlanetCoordinate;

		private PlanetFactory m_PlanetFactory;

		public void Init(PlanetData planetData)
		{
			this.planetData = planetData;
			m_PlanetFactory = planetData.factory;

			data = new BPData();
			m_PlanetCoordinate = new PlanetCoordinate();
			if (planetData!=null)
			{
				m_PlanetCoordinate.segment = planetData.aux.mainGrid.segment;
				m_PlanetCoordinate.radius = planetData.realRadius;
			}

		}

		public void SaveEntities(List<EntityData> entities)
		{
			foreach (var entity in entities)
			{
				BPEntityData bpEntity = CreateBPEntity(entity);
				data.entities.Add(bpEntity);
			}

			UpdateBPData();
		}

		public BPEntityData CreateBPEntity(EntityData entity)
		{
			BPEntityData bpEntity = new BPEntityData();

			bpEntity.protoId = entity.protoId;
			bpEntity.pos = entity.pos;
			bpEntity.rot = entity.rot;

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
				bpEntity.refCount = (int)((inserterComponent.stt - 0.499f )/ itemProto.prefabDesc.inserterSTT);
				bpEntity.filterId = inserterComponent.filter;
				bpEntity.pos2 = inserterComponent.pos2;
				bpEntity.rot2 = inserterComponent.rot2;
				bpEntity.pickOffset = inserterComponent.pickOffset;
				bpEntity.insertOffset = inserterComponent.insertOffset;
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

			return bpEntity;
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
			prebuildData.refArr = new int[bpEntity.refCount];
			Array.Copy(bpEntity.refArr, prebuildData.refArr, bpEntity.refCount);


			return prebuildData;
		}


		public void UpdateBPData()
		{
			//更新物体位置
			UpdateEntities();

			//更新bp的范围
			UpdateBPRect();
			UpdateBPCell();
		}

		public void UpdateBPRect()
		{
			int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				minX = Math.Min(minX, entityData.gcsCellIndex.x);
				minY = Math.Min(minY, entityData.gcsCellIndex.y);
				maxX = Math.Max(maxX, entityData.gcsCellIndex.x);
				maxY = Math.Max(maxY, entityData.gcsCellIndex.y);
			}

			data.gridBounds = new BoundsInt(minX, minY, 0, maxX, maxY, 0);

			Vector3 gcs = m_PlanetCoordinate.CellToGcs(data.gridBounds.min);
			data.longitude = gcs.x;
			data.latitude = gcs.y;
		}

		public void UpdateBPCell()
		{

		}


		public void UpdateEntities()
		{
			for (int i = 0; i < data.entities.Count; ++i)
			{
				BPEntityData entityData = data.entities[i];
				UpdateEntityCell(entityData);
			}
		}

		public void UpdateEntityCell(BPEntityData entityData )
		{
			entityData.gcsCellIndex = m_PlanetCoordinate.LocalToCell(entityData.pos);
			entityData.gcsCellIndex2= m_PlanetCoordinate.LocalToCell(entityData.pos2);
		}

		public Vector3 GetPlanetLocalPosition(Vector2Int cellIndex)
		{
			Vector3 localNormal = m_PlanetCoordinate.CellToLocalNormal(cellIndex);
			return m_PlanetCoordinate.LocalNormalToLocal(localNormal);
		}
	}
}
