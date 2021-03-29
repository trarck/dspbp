using System;

namespace YH
{
	public class YHMath
	{
		public const float PI = (float)Math.PI;
		public const float DoublePI = (float)(Math.PI*2.0f);

		public static float Clamp(float value, float min, float max)
		{
			if (value < min)
			{
				value = min;
			}
			else if (value > max)
			{
				value = max;
			}
			return value;
		}

		public static float Floor(float f)
		{
			return (float)Math.Floor(f);
		}

		public static float Repeat(float t, float length)
		{
			return Clamp(t - Floor(t / length) * length, 0f, length);
		}

		public static float DeltaRadian(float current, float target)
		{
			var delta =Repeat(target - current, DoublePI);
			if (delta > PI)
			{
				delta -= DoublePI;
			}
			return delta;
		}
	}
}
