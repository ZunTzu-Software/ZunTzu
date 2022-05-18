// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using Microsoft.DirectX;

namespace ZunTzu.Modelization {

	/// <summary>Rotation in 3D space.</summary>
	internal sealed class Rotation : IRotation {

		/// <summary>Constructor from an axis and an angle.</summary>
		/// <param name="axisX">X component of the rotation axis vector.</param>
		/// <param name="axisY">Y component of the rotation axis vector.</param>
		/// <param name="axisZ">Z component of the rotation axis vector.</param>
		/// <param name="angle">Rotation angle.</param>
		internal Rotation(float axisX, float axisY, float axisZ, float angle) {
			q = Quaternion.RotationAxis(new Vector3(axisX, axisY, axisZ), angle);
			q.Normalize();
		}

		/// <summary>Constructor from the composition of three rotations along the Z, X and then Y axis.</summary>
		/// <param name="yaw">Rotation angle along the Y axis.</param>
		/// <param name="pitch">Rotation angle along the X axis.</param>
		/// <param name="roll">Rotation angle along the Z axis.</param>
		internal Rotation(float yaw, float pitch, float roll) {
			q = Quaternion.RotationYawPitchRoll(yaw, pitch, roll);
			q.Normalize();
		}

		/// <summary>Private constructor.</summary>
		private Rotation(Quaternion q) {
			this.q = q;
			q.Normalize();
		}

		/// <summary>Computes the composition of two rotations in 3D space.</summary>
		/// <param name="secondRotation">Another rotation.</param>
		/// <returns>The composition rotation.</returns>
		public IRotation ComposeWith(IRotation secondRotation) {
			return new Rotation(Quaternion.Multiply(q, ((Rotation)secondRotation).q));
		}

		/// <summary>Interpolates between two rotations in 3D space, using spherical linear interpolation.</summary>
		/// <param name="finalRotation">Another rotation.</param>
		/// <param name="coefficient">Parameter that indicates how far to interpolate between the rotations.</param>
		/// <returns>The interpolation rotation.</returns>
		public IRotation InterpolateWith(IRotation finalRotation, float coefficient) {
			return new Rotation(Quaternion.Slerp(q, ((Rotation)finalRotation).q, coefficient));
		}

		/// <summary>Converts to a rotation matrix.</summary>
		/// <returns>A 3x3 matrix.</returns>
		public float[,] ToRotationMatrix() {
			Matrix m = Matrix.RotationQuaternion(q);
			return new float[3,3] {
				{ m.M11, m.M12, m.M13 },
				{ m.M21, m.M22, m.M23 },
				{ m.M31, m.M32, m.M33 }
			};
		}

		/// <summary>A DirectX quaternion.</summary>
		private readonly Quaternion q;
	}
}
