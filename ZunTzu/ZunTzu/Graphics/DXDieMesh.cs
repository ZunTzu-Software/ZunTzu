// Copyright (c) 2022 ZunTzu Software and contributors

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZunTzu.FileSystem;

namespace ZunTzu.Graphics
{

    /// <summary>3D object.</summary>
    internal sealed class DXDieMesh : IDieMesh {

		public DXDieMesh(float[,] vertice, Int16[,] triangles, float inradius, IFile textureFile, bool custom) {
			_vertice = vertice;
			_triangles = triangles;
			_inradius = inradius;
			_textureFile = textureFile;
			_custom = custom;
			Initialize();
		}

		/// <summary>Render this die.</summary>
		public void Render(System.Drawing.PointF position, float sizeFactor, float[,] rotationMatrix, uint dieColor, uint pipsColor) {
			if (_custom)
				D3D.RenderCustomDieMesh(
					_vb, _ib, _texture,
					VertexCount, TriangleCount,
					position.X, position.Y,
					sizeFactor, rotationMatrix);
			else
				D3D.RenderDieMesh(
					_vb, _ib, _texture,
					VertexCount, TriangleCount,
					position.X, position.Y,
					sizeFactor, rotationMatrix,
					dieColor, pipsColor);
		}

		/// <summary>Render the shadow of this die.</summary>
		public void RenderShadow(System.Drawing.PointF position, float sizeFactor, float[,] rotationMatrix, uint shadowColor) {
			D3D.RenderDieMeshShadow(
				_vb, _ib, _texture,
				VertexCount, TriangleCount, _inradius,
				position.X, position.Y,
				sizeFactor, rotationMatrix,
				shadowColor);
		}

		public void Initialize() {
			loadTexture();
			loadGeometry();
		}

		private unsafe void loadTexture() {
			using(Stream stream = _textureFile.Open()) {
				using(Bitmap bitmap = new Bitmap(stream)) {
					BitmapData bitmapData = bitmap.LockBits(
						new Rectangle(0, 0, bitmap.Width, bitmap.Height),
						ImageLockMode.ReadOnly,
						PixelFormat.Format24bppRgb);

					_texture = D3DTexture.Create(bitmap.Width, bitmap.Height, D3DTextureFormat.A8R8G8B8);
					_texture.Lock(out int texturePitch, out byte* textureBits);

					byte* source = (byte*) bitmapData.Scan0;
					int sourceWidth = bitmapData.Width;
					int sourceHeight = bitmapData.Height;
					int stride = bitmapData.Stride;
					if(_custom) {
						for(int y = 0; y < sourceHeight; ++y) {
							for(int x = 0; x < sourceWidth; ++x) {
								*(textureBits + 0) = *(source + 0);
								*(textureBits + 1) = *(source + 1);
								*(textureBits + 2) = *(source + 2);
								*(textureBits + 3) = 0xff;
								textureBits += 4;
								source += 3;
							}
							textureBits += texturePitch - sourceWidth * 4;
							source += stride - sourceWidth * 3;
						}
					} else {
						for(int y = 0; y < sourceHeight; ++y) {
							for(int x = 0; x < sourceWidth; ++x) {
								*(textureBits + 0) = 0xff;
								*(textureBits + 1) = 0xff;
								*(textureBits + 2) = 0xff;
								*(textureBits + 3) = (byte) (0xff - *(source + 0));
								textureBits += 4;
								source += 3;
							}
							textureBits += texturePitch - sourceWidth * 4;
							source += stride - sourceWidth * 3;
						}
					}

					_texture.Unlock();
					bitmap.UnlockBits(bitmapData);
				}
			}
		}

		private void loadGeometry() {
		
			int vertexCount = _vertice.GetLength(0);
			var verts = new D3DVertexBuffer.PosNormTexVertex[vertexCount];
			for(int i = 0; i < vertexCount; ++i) {
				verts[i].X = _vertice[i, 0];
				verts[i].Y = _vertice[i, 1];
				verts[i].Z = _vertice[i, 2];
				verts[i].Nx = _vertice[i, 3];
				verts[i].Ny = _vertice[i, 4];
				verts[i].Nz = _vertice[i, 5];
				verts[i].Tu = _vertice[i, 6];
				verts[i].Tv = _vertice[i, 7];
			}
			_vb = D3DVertexBuffer.Create(verts);
		
			int triangleCount = _triangles.GetLength(0);
			short[] indices = new short[triangleCount * 3];
			for(int i = 0; i < triangleCount; ++i) {
				indices[i * 3 + 0] = _triangles[i, 0];
				indices[i * 3 + 1] = _triangles[i, 1];
				indices[i * 3 + 2] = _triangles[i, 2];
			}
			_ib = D3DIndexBuffer.Create(indices);
		}

		public void Dispose() {
			if(_texture != null) _texture.Dispose();
			if(_vb != null) _vb.Dispose();
			if(_ib != null) _ib.Dispose();
		}

		public D3DTexture Texture => _texture;
		public D3DVertexBuffer VertexBuffer => _vb;
		public D3DIndexBuffer IndexBuffer => _ib;
		public int VertexCount => _vertice.GetLength(0);
		public int TriangleCount => _triangles.GetLength(0);
		public float Inradius => _inradius;

		readonly float[,] _vertice;
		readonly Int16[,] _triangles;
		readonly float _inradius;
		readonly bool _custom;
		readonly IFile _textureFile;
		D3DTexture _texture = null;
		D3DVertexBuffer _vb = null;
		D3DIndexBuffer _ib = null;
	}
}
