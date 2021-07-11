using System.IO;
using System.Collections.Generic;
using UnityEngine;
using YH;

namespace DspTrarck
{
	public class BPDataWriter
	{

		public static void WriteBPDataToFile(string filepath, BPData data)
		{
			using (FileStream fs = new FileStream(filepath,File.Exists(filepath)?FileMode.Truncate: FileMode.OpenOrCreate, FileAccess.Write))
			using (BinaryWriter writer = new BinaryWriter(fs))
			{
				WriteBPData(writer, data);
			}
		}

		public static void WriteConnectData(BinaryWriter writer, ConnectData connectData)
		{
			writer.Write(connectData.fromObjId);
			writer.Write(connectData.fromSlot);
			writer.Write(connectData.toObjId);
			writer.Write(connectData.toSlot);
			writer.Write(connectData.offset);
			writer.Write(connectData.isOutput);
		}

		public static void WriteBPEntityData(BinaryWriter writer, BPEntityData bpEntityData)
		{
			writer.Write(bpEntityData.entityId);
			writer.Write(bpEntityData.protoId);
			writer.Write((byte)bpEntityData.type);
			BinaryHelper.WriteVector3(writer, ref bpEntityData.pos);
			BinaryHelper.WriteQuaternion(writer, ref bpEntityData.rot);
			BinaryHelper.WriteVector2(writer, ref bpEntityData.grid);

			writer.Write(bpEntityData.offsetGround);
			writer.Write(bpEntityData.pickOffset );
			writer.Write(bpEntityData.insertOffset );
			writer.Write(bpEntityData.recipeId);
			writer.Write(bpEntityData.filterId);

			BinaryHelper.WriteVector3(writer, ref bpEntityData.pos2);
			BinaryHelper.WriteQuaternion(writer, ref bpEntityData.rot2);
			BinaryHelper.WriteVector2(writer, ref bpEntityData.grid2);
			writer.Write(bpEntityData.offsetGround2);
			BinaryHelper.WriteIntArr(writer, bpEntityData.parameters,0,bpEntityData.paramCount);
		}

		public static void WriteEntities(BinaryWriter writer, List<BPEntityData> entities)
		{
			int count = 0;
			if (entities == null || entities.Count == 0)
			{
				writer.Write(count);
			}
			else
			{
				count = entities.Count;
				writer.Write(count);
				for (int i = 0; i < count; ++i)
				{
					BPEntityData bpEntityData = entities[i];
					WriteBPEntityData(writer, bpEntityData);
				}
			}
		}

		public static void WriteConnects(BinaryWriter writer, List<ConnectData> connects)
		{
			int count = 0;
			if (connects == null || connects.Count == 0)
			{
				writer.Write(count);
			}
			else
			{
				count = connects.Count;
				writer.Write(count);
				for (int i = 0; i < count; ++i)
				{
					ConnectData connData = connects[i];
					WriteConnectData(writer, connData);
				}
			}
		}

		public static void WriteBPData(BinaryWriter writer, BPData bpData)
		{
			writer.Write(bpData.version);
			writer.Write(bpData.name);
			writer.Write((byte)bpData.posType);
			WriteEntities(writer, bpData.entities);
			WriteConnects(writer, bpData.connects);

			writer.Write(bpData.latitude);
			writer.Write(bpData.longitude);
			BinaryHelper.WriteVector2(writer, ref bpData.gcsMin);
			BinaryHelper.WriteVector2(writer, ref bpData.gcsMax);
			BinaryHelper.WriteVector3(writer, ref bpData.originalPos);
			writer.Write(bpData.planetRadius);
		}

	}
}
