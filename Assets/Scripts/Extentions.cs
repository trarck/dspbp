using System;
using UnityEngine;

public static class Extention
{
	public const float Deg2Rad = (float)Math.PI / 180f;

	public const float Rad2Deg = 57.29578f;
	public static Vector2 ToDegrees(this Vector2 vector)
	{
		return vector * Rad2Deg;
	}

	public static Vector2 ToRadians(this Vector2 vector)
	{
		return vector * Deg2Rad;
	}

	public static Vector2 ToSpherical(this Vector3 vector)
	{
		float inclination = Mathf.Acos(vector.y / vector.magnitude);
		float azimuth = Mathf.Atan2(vector.z, vector.x);
		return new Vector2(inclination, azimuth);
	}

	public static Vector3 ToCartesian(this Vector2 vector, float realRadius)
	{
		float x = realRadius * Mathf.Sin(vector.x) * Mathf.Cos(vector.y);
		float y = realRadius * Mathf.Cos(vector.x);
		float z = realRadius * Mathf.Sin(vector.x) * Mathf.Sin(vector.y);
		return new Vector3(x, y, z);
	}

	public static Quaternion SphericalRotation(Vector3 pos, float angle)
	{
		pos.Normalize();
		Vector3 normalized = Vector3.Cross(pos, Vector3.up).normalized;
		Vector3 forward;
		if (normalized.sqrMagnitude < 0.0001f)
		{
			float num = Mathf.Sign(pos.y);
			normalized = Vector3.right * num;
			forward = Vector3.forward * num;
		}
		else
		{
			forward = Vector3.Cross(normalized, pos).normalized;
		}
		if (angle == 0f)
		{
			return Quaternion.LookRotation(forward, pos);
		}
		return Quaternion.LookRotation(forward, pos) * Quaternion.AngleAxis(angle, Vector3.up);
	}
}

