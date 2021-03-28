using System;
using UnityEngine;


namespace DspTrarck
{
	public class PlanetCoordinate
	{
		public static int[] segmentTable = new int[512]
	{
		1,
		4,
		4,
		4,
		4,
		4,
		4,
		4,
		8,
		8,
		8,
		8,
		8,
		8,
		8,
		8,
		16,
		16,
		16,
		16,
		20,
		20,
		20,
		20,
		20,
		20,
		20,
		20,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		32,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		40,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		60,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		80,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		100,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		120,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		160,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		200,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		240,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		300,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		400,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500,
		500
	};

		public int segment;
		public float radius;
		public float groundOffset = 0.2f;

		public Vector3 kernelPosition;
		public Quaternion rotation;

		public Vector3 GcsToNormal(Vector3 gcs)
		{
			return GcsToNormal(gcs.x, gcs.y);
		}

		public Vector3 GcsToNormal(float longitude, float latitude)
		{
			float sinLat = Mathf.Sin(latitude);
			float cosLat = Mathf.Cos(latitude);
			float sinLong = Mathf.Sin(longitude);
			float cosLong = Mathf.Cos(longitude);
			return new Vector3(cosLat * sinLong, sinLat, -cosLat * cosLong);
		}

		public Vector2 GcsToGrid(Vector3 gcs)
		{
			float latIdx = (float)(gcs.y * segment / (Math.PI * 2f));

			int latitudeIndex = Mathf.FloorToInt(Mathf.Max(0f, Mathf.Abs(latIdx) - 0.1f));

			int longitudeSegment = DetermineLongitudeSegmentCount(latitudeIndex, segment);
			float longIdx = (float)(gcs.x / (Math.PI * 2f) * longitudeSegment);
			return new Vector2(longIdx, latIdx);
		}

		public Vector2Int GcsToCell(Vector3 gcs)
		{
			Vector2 grid = GcsToGrid(gcs);
			return GridToCell(grid);
		}

		public Vector3 LocalToGcs(Vector3 localPos)
		{
			//离地面的距离
			float offsetGround = localPos.magnitude - radius - groundOffset;

			localPos.Normalize();
			float latitude = Mathf.Asin(localPos.y);
			float longitude = Mathf.Atan2(localPos.x, -localPos.z);
			return new Vector3(longitude, latitude, offsetGround);
		}

		public Vector2 LocalToGrid(Vector3 localPos)
		{
			Vector3 gcs = LocalToGcs(localPos);
			return GcsToGrid(gcs);
		}

		public Vector2 LocalToGridDirector(Vector3 localPos)
		{
			Vector3 gcs = LocalToGcs(localPos);
			//latitude to grid coord gridY=latitude/cellSize
			float gridY = (float)(gcs.y * segment / (Math.PI * 2f));
			//longitude to grid coord 
			float gridX = (float)(gcs.x * Mathf.Cos(gcs.y) * segment / (Math.PI * 2f));
			return new Vector2(gridX, gridY);
		}

		public Vector2Int LocalToCell(Vector3 localPos)
		{
			Vector3 gcs = LocalToGcs(localPos);
			//float latIdx = (float)(gcs.y * segment / (Math.PI * 2f));

			//int latitudeIndex = Mathf.FloorToInt(Mathf.Max(0f, Mathf.Abs(latIdx) - 0.1f));

			//float longitudeSegment = DetermineLongitudeSegmentCount(latitudeIndex, segment);
			//float longIdx = (float)(gcs.x / (Math.PI * 2f) * longitudeSegment);
			//if (longIdx < 0)
			//{
			//	longIdx += longitudeSegment;
			//}
			//Cell cell = new Cell((int)longIdx, (int)latIdx);
			return GcsToCell(gcs);
		}

		public Vector3 GridToGcs(Vector2 grid)
		{
			int latitudeIndex = Mathf.FloorToInt(Mathf.Max(0f, Mathf.Abs(grid.y) - 0.1f));
			int longitudeSegment = DetermineLongitudeSegmentCount(latitudeIndex, segment);

			float longitude = (float)(grid.x * Math.PI * 2f / longitudeSegment);
			float latitude = (float)(grid.y * Math.PI * 2f / segment);

			return new Vector3(longitude, latitude, 0);
		}

		public Vector2Int GridToCell(Vector2 grid)
		{
			return new Vector2Int(Mathf.RoundToInt(grid.x * 5), Mathf.RoundToInt(grid.y * 5));
		}

		public Vector2 CellToGrid(int longIdx, int latIdx)
		{
			return new Vector2(longIdx * 0.2f, latIdx * 0.2f);
		}

		public Vector2 CellToGrid(Vector2Int cell)
		{
			return CellToGrid(cell.x, cell.y);
		}

		public Vector3 CellToGcs(Vector2Int cell)
		{
			Vector2 grid = CellToGrid(cell);
			return GridToGcs(grid);
		}

		public Vector3 CellToGcs(Vector3Int cell)
		{
			Vector2 grid = CellToGrid(cell.x, cell.y);
			return GridToGcs(grid);
		}

		public Vector3 CellToNormal(Vector2Int cell)
		{
			Vector3 gcs = CellToGcs(cell);
			return GcsToNormal(gcs);
		}

		public Vector3 CellToNormal(Vector3Int cell)
		{
			Vector3 gcs = CellToGcs(cell);
			return GcsToNormal(gcs);
		}

		public Vector3 NormalToGround(Vector3 vec)
		{
			return vec * (radius + groundOffset);
		}

		public Vector3 LocalToWorldPosition(Vector3 localPos)
		{
			return kernelPosition += localPos;
		}

		public Vector3 WorldToLocalPosition(Vector3 worldPos)
		{
			return worldPos-kernelPosition;
		}

		public Vector2Int GcsOffset(Vector3 gcs, Vector2Int offsetCellIndex)
		{
			float latGrid = (float)(gcs.y * segment / (Math.PI * 2f));
			latGrid += offsetCellIndex.y * 0.2f;

			int latCell = LatGridToIndex(latGrid);
			int longitudeSegment = DetermineLongitudeSegmentCount(latCell, segment);

			float longGrid = (float)(gcs.x / (Math.PI * 2f) * longitudeSegment);
			longGrid += offsetCellIndex.x * 0.2f;

			return GridToCell(new Vector2(longGrid, latGrid));
		}

		public Vector2Int GcsOffset(Vector2 grid, float longitude ,Vector2Int offsetCellIndex)
		{
			grid.y += offsetCellIndex.y * 0.2f;

			int latCell = LatGridToIndex(grid.x);
			int longitudeSegment = DetermineLongitudeSegmentCount(latCell, segment);

			grid.x = (float)(longitude / (Math.PI * 2f) * longitudeSegment);
			Vector2Int cell = GridToCell(grid);
			cell.x += offsetCellIndex.x;

			return cell;
		}

		public Vector2Int GcsOffset(Vector2Int cellIndex, float longitude, Vector2Int offsetCellIndex)
		{
			cellIndex.y += offsetCellIndex.y;
			int longitudeSegment = GetLongitudeSegmentCountOfLatitudeCell(cellIndex.y);

			float longGrid = (float)(longitude / (Math.PI * 2f) * longitudeSegment);
			cellIndex.x = Mathf.RoundToInt(longGrid * 5) + offsetCellIndex.x;
			return cellIndex;
		}

		public int GetLongitudeSegmentCountOfLatitudeCell(int latCell)
		{
			int latitudeIndex = LatCellToIndex(latCell);
			return DetermineLongitudeSegmentCount(latitudeIndex, segment);
		}

		public static int LatGridToIndex(float latGrid)
		{
			int latitudeIndex = Mathf.FloorToInt(Mathf.Max(0f, Mathf.Abs(latGrid) - 0.1f));
			return latitudeIndex;
		}

		public static int LatCellToIndex(int latCell)
		{
			float latGrid = latCell * 0.2f;
			return LatGridToIndex(latGrid);
		}

		public static int DetermineLongitudeSegmentCount(int latitudeIndex, int segment)
		{
			int num = Mathf.CeilToInt(Mathf.Abs(Mathf.Cos((float)latitudeIndex / ((float)segment / 4f) * (float)Math.PI * 0.5f)) * (float)segment);
			if (num < 500)
			{
				return segmentTable[num];
			}
			return (num + 49) / 100 * 100;
		}
	}
}
