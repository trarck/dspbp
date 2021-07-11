using System.IO;
using System.Collections.Generic;
using UnityEngine;
using YH;

namespace DspTrarck
{
	public class BPDataReader
	{
		public static BPData ReadBPDataFromFile(string filepath)
		{
			BPData data = new BPData();
			using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
			using (BinaryReader reader = new BinaryReader(fs))
			{
				ReadBPData(reader, ref data);
			}
			return data;
		}

		public static void ReadConnectData(BinaryReader reader, ref ConnectData connectData)
		{
			connectData.fromObjId = reader.ReadInt32();
			connectData.fromSlot = reader.ReadInt32();
			connectData.toObjId = reader.ReadInt32();
			connectData.toSlot = reader.ReadInt32();
			connectData.offset = reader.ReadInt32();
			connectData.isOutput = reader.ReadBoolean();
		}

		public static void ReadBPEntityData(BinaryReader reader, ref BPEntityData bpEntityData)
		{
			bpEntityData.entityId = reader.ReadInt32();
			bpEntityData.protoId = reader.ReadInt32();
			bpEntityData.type = (BPEntityType)reader.ReadByte();
			BinaryHelper.TryReadVector3(reader, ref bpEntityData.pos);
			BinaryHelper.TryReadQuaternion(reader, ref bpEntityData.rot);
			BinaryHelper.TryReadVector2(reader, ref bpEntityData.grid);
			bpEntityData.offsetGround = reader.ReadSingle();
			bpEntityData.pickOffset = reader.ReadInt16();
			bpEntityData.insertOffset = reader.ReadInt16();
			bpEntityData.recipeId = reader.ReadInt32();
			bpEntityData.filterId = reader.ReadInt32();
			BinaryHelper.TryReadVector3(reader, ref bpEntityData.pos2);
			BinaryHelper.TryReadQuaternion(reader, ref bpEntityData.rot2);
			BinaryHelper.TryReadVector2(reader, ref bpEntityData.grid2);
			bpEntityData.offsetGround2 = reader.ReadSingle();

			bpEntityData.parameters = BinaryHelper.ReadIntArray(reader, ref bpEntityData.paramCount);
		}

		public static void ReadEntities(BinaryReader reader, ref List<BPEntityData> entities)
		{
			int count = reader.ReadInt32();
			entities.Capacity = count;
			for (int i = 0; i < count; ++i)
			{
				BPEntityData bpEntityData = new BPEntityData();
				ReadBPEntityData(reader, ref bpEntityData);
				entities.Add(bpEntityData);
			}
		}

		public static void ReadConnects(BinaryReader reader, ref List<ConnectData> connects)
		{
			int count = reader.ReadInt32();
			connects.Capacity = count;
			for (int i = 0; i < count; ++i)
			{
				ConnectData connData = new ConnectData();
				ReadConnectData(reader, ref connData);
				connects.Add(connData);
			}
		}

		public static void ReadBPData(BinaryReader reader, ref BPData bpData)
		{
			bpData.version = reader.ReadInt32();
			bpData.name = reader.ReadString();
			bpData.posType = (BPData.PosType)reader.ReadByte();
			bpData.entities = new List<BPEntityData>();
			ReadEntities(reader, ref bpData.entities);
			bpData.connects = new List<ConnectData>();
			ReadConnects(reader, ref bpData.connects);

			bpData.latitude = reader.ReadSingle();
			bpData.longitude = reader.ReadSingle();
			BinaryHelper.TryReadVector2(reader, ref bpData.gcsMin);
			BinaryHelper.TryReadVector2(reader, ref bpData.gcsMax);
			BinaryHelper.TryReadVector3(reader, ref bpData.originalPos);
			bpData.planetRadius = reader.ReadSingle();
		}

	}
}
