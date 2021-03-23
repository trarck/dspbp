using System;
using UnityEngine;
using DspTrarck;

namespace Test
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

			FactoryBP fbp = new FactoryBP();
			fbp.bpDir = Environment.CurrentDirectory;


			BPData bPData = new BPData();
			bPData.name = "test";

			BPEntityData bPEntity = new BPEntityData();
			bPEntity.protoId = 1;
			bPEntity.type = BPEntityType.Belt;
			bPEntity.pos = new Vector3(1, 2, 3);
			bPEntity.rot = Quaternion.identity;
			bPEntity.gcsCellIndex = new Vector2Int(100, 100);

			bPData.entities.Add(bPEntity);

			bPEntity = new BPEntityData();
			bPEntity.protoId = 2;
			bPEntity.type = BPEntityType.Assembler;
			bPEntity.pos = new Vector3(4, 5, 6);
			bPEntity.rot = Quaternion.identity;
			bPEntity.gcsCellIndex = new Vector2Int(50, 50);

			bPData.entities.Add(bPEntity);

			fbp.SaveBPData(bPData);

		}
	}
}
