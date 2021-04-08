using System.IO;
using UnityEngine;

namespace YH
{
	public class BinaryHelper
	{
		#region Reader
		public static bool TryReadVector3(BinaryReader reader, ref Vector3 v)
		{
			v.x = reader.ReadSingle();
			v.y = reader.ReadSingle();
			v.z = reader.ReadSingle();
			return true;
		}

		public static bool TryReadVector2(BinaryReader reader, ref Vector2 v)
		{
			v.x = reader.ReadSingle();
			v.y = reader.ReadSingle();
			return true;
		}


		public static bool TryReadVector2Int(BinaryReader reader, ref Vector2Int v)
		{
			v.x = reader.ReadInt32();
			v.y = reader.ReadInt32();
			return true;
		}

		public static bool TryReadQuaternion(BinaryReader reader, ref Quaternion v)
		{
			v.x = reader.ReadSingle();
			v.y = reader.ReadSingle();
			v.z = reader.ReadSingle();
			v.w = reader.ReadSingle();
			return true;
		}

		public static BoundsInt ReadBoundsInt(BinaryReader reader)
		{
			int xMin = reader.ReadInt32();
			int yMin = reader.ReadInt32();
			int zMin = reader.ReadInt32();

			int xMax = reader.ReadInt32();
			int yMax = reader.ReadInt32();
			int zMax = reader.ReadInt32();

			return new BoundsInt(xMin, yMin, zMin, xMax, yMax, zMax);
		}

		public static bool TryReadIntArray(BinaryReader reader, int count, ref int[] arr)
		{
			for (int i = 0; i < count; ++i)
			{
				arr[i] = reader.ReadInt32();
			}
			return true;
		}

		public static int[] ReadIntArray(BinaryReader reader, ref int count)
		{
			count = reader.ReadInt32();
			int[] arr = null;
			if (count > 0)
			{
				arr = new int[count];
				TryReadIntArray(reader, count, ref arr);
			}
			return arr;
		}

		#endregion

		#region Write

		public static void WriteVector3(BinaryWriter writer, ref Vector3 v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
		}

		public static void WriteVector2(BinaryWriter writer, ref Vector2 v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
		}

		public static void WriteVector2Int(BinaryWriter writer, ref Vector2Int v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
		}

		public static void WriteQuaternion(BinaryWriter writer, ref Quaternion v)
		{
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
			writer.Write(v.w);
		}

		public static void WriteBoundsInt(BinaryWriter writer, ref BoundsInt bounds)
		{
			writer.Write(bounds.xMin);
			writer.Write(bounds.yMin);
			writer.Write(bounds.zMin);
			writer.Write(bounds.xMax);
			writer.Write(bounds.yMax);
			writer.Write(bounds.zMax);
		}

		public static void WriteIntArr(BinaryWriter writer, int[] arr,int index,int count)
		{
			if (arr == null || arr.Length == 0)
			{
				count = 0;
				writer.Write(count);
			}
			else
			{
				count = count-index > arr.Length ? arr.Length-index : count;
				writer.Write(count);
				for (int i = 0; i < count; ++i)
				{
					writer.Write(arr[i]);
				}
			}
		}

		#endregion

	}
}
