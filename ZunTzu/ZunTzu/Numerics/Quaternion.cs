// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Numerics
{
	/// <summary>Rotation in 3D space.</summary>
	public sealed class Quaternion
	{
		/// <summary>Constructor from an axis and an angle.</summary>
		/// <param name="axisX">X component of the rotation axis vector.</param>
		/// <param name="axisY">Y component of the rotation axis vector.</param>
		/// <param name="axisZ">Z component of the rotation axis vector.</param>
		/// <param name="angle">Rotation angle.</param>
		/// <returns>A normalized quaternion.</returns>
		public static Quaternion FromAxisAndAngle(float axisX, float axisY, float axisZ, float angle)
		{
			float axisNorm = (float)Math.Sqrt(axisX * axisX + axisY * axisY + axisZ * axisZ);
			float halfAngle = angle * 0.5f;
			float sinHalfAngleTimesInvAxisNorm = (float)Math.Sin(halfAngle) / axisNorm;

			Quaternion q = new Quaternion();
			q.X = axisX * sinHalfAngleTimesInvAxisNorm;
			q.Y = axisY * sinHalfAngleTimesInvAxisNorm;
			q.Z = axisZ * sinHalfAngleTimesInvAxisNorm;
			q.W = (float)Math.Cos(halfAngle);

			return q;
		}

		/// <summary>Constructor from the composition of three rotations along the Z, X and then Y axis.</summary>
		/// <param name="yaw">Rotation angle along the Y axis.</param>
		/// <param name="pitch">Rotation angle along the X axis.</param>
		/// <param name="roll">Rotation angle along the Z axis.</param>
		/// <returns>A normalized quaternion.</returns>
		public static Quaternion FromYawPitchRoll(float yaw, float pitch, float roll)
		{
			float halfRoll = roll * 0.5f;
			float sr = (float)Math.Sin(halfRoll);
			float cr = (float)Math.Cos(halfRoll);

			float halfPitch = pitch * 0.5f;
			float sp = (float)Math.Sin(halfPitch);
			float cp = (float)Math.Cos(halfPitch);

			float halfYaw = yaw * 0.5f;
			float sy = (float)Math.Sin(halfYaw);
			float cy = (float)Math.Cos(halfYaw);

			Quaternion q = new Quaternion();
			q.X = cy * sp * cr + sy * cp * sr;
			q.Y = sy * cp * cr - cy * sp * sr;
			q.Z = cy * cp * sr - sy * sp * cr;
			q.W = cy * cp * cr + sy * sp * sr;

			return q;
		}

		/// <summary>Computes the composition of two rotations in 3D space.</summary>
		/// <param name="secondRotation">Another rotation.</param>
		/// <returns>The composition rotation, as a normalized quaternion.</returns>
		public Quaternion ComposeWith(Quaternion secondRotation)
		{
			float x2 = X;
			float y2 = Y;
			float z2 = Z;
			float w2 = W;

			float x1 = secondRotation.X;
			float y1 = secondRotation.Y;
			float z1 = secondRotation.Z;
			float w1 = secondRotation.W;

			Quaternion composition = new Quaternion();
			composition.X = x1 * w2 + x2 * w1 + y1 * z2 - z1 * y2;
			composition.Y = y1 * w2 + y2 * w1 + z1 * x2 - x1 * z2;
			composition.Z = z1 * w2 + z2 * w1 + x1 * y2 - y1 * x2;
			composition.W = w1 * w2 - (x1 * x2 + y1 * y2 + z1 * z2);

			composition.Normalize();

			return composition;
		}

		/// <summary>Interpolates between two rotations in 3D space, using spherical linear interpolation.</summary>
		/// <param name="finalRotation">Another rotation.</param>
		/// <param name="coefficient">Parameter that indicates how far to interpolate between the rotations.</param>
		/// <returns>The interpolation rotation, as a normalized quaternion.</returns>
		public Quaternion InterpolateWith(Quaternion finalRotation, float coefficient)
		{
			float x1 = X;
			float y1 = Y;
			float z1 = Z;
			float w1 = W;

			float x2 = finalRotation.X;
			float y2 = finalRotation.Y;
			float z2 = finalRotation.Z;
			float w2 = finalRotation.W;

			float cosOmega = x1 * x2 + y1 * y2 + z1 * z2 + w1 * w2;

			bool flip = (cosOmega < 0.0f);
			if (flip) cosOmega = -cosOmega;

			float s1, s2;

			const float epsilon = 1e-6f;
			bool useLinearOptimization = (cosOmega > (1.0f - epsilon));
			if (useLinearOptimization)
			{
				// too close -> use linear interpolation (lerp).
				s1 = 1.0f - coefficient;
				s2 = coefficient;
			}
			else
			{
				// far enough -> use spherical linear interpolation (slerp).
				float omega = (float)Math.Acos(cosOmega);
				float invSinOmega = 1.0f / (float)Math.Sin(omega);

				s1 = (float)Math.Sin((1.0f - coefficient) * omega) * invSinOmega;
				s2 = (float)Math.Sin(coefficient * omega) * invSinOmega;
			}

			if (flip) s2 = -s2;

			Quaternion slerp = new Quaternion();
			slerp.X = s1 * x1 + s2 * x2;
			slerp.Y = s1 * y1 + s2 * y2;
			slerp.Z = s1 * z1 + s2 * z2;
			slerp.W = s1 * w1 + s2 * w2;

			slerp.Normalize();

			return slerp;
		}

		void Normalize()
		{
			float invNorm = 1.0f / (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

			X *= invNorm;
			Y *= invNorm;
			Z *= invNorm;
			W *= invNorm;
		}

		Quaternion()
		{
			// Identity quaternion
			X = 0.0f;
			Y = 0.0f;
			Z = 0.0f;
			W = 1.0f;
		}

		/// <summary>The X-value of the vector component of the Quaternion.</summary>
		public float X;

		/// <summary>The Y-value of the vector component of the Quaternion.</summary>
		public float Y;

		/// <summary>The Z-value of the vector component of the Quaternion.</summary>
		public float Z;

		/// <summary>The rotation component of the Quaternion.</summary>
		public float W;
	}
}
