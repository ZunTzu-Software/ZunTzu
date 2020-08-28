// Copyright (c) 2020 ZunTzu Software and contributors

using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using ZunTzu.FileSystem;

namespace ZunTzu.Graphics {

	/// <summary>3D object.</summary>
	internal sealed class DXDieMesh : IDieMesh {

		public DXDieMesh(DXGraphics graphics, float[,] vertice, Int16[,] triangles, float inradius, IFile textureFile, bool custom) {
			this.graphics = graphics;
			this.vertice = vertice;
			this.triangles = triangles;
			this.inradius = inradius;
			this.textureFile = textureFile;
			this.custom = custom;
			Initialize();
		}

		/// <summary>Render this die.</summary>
		public void Render(System.Drawing.PointF position, float sizeFactor, float[,] rotationMatrix, uint dieColor, uint pipsColor) {
			if(custom)
				graphics.RenderCustomDieMesh(this, position, sizeFactor, rotationMatrix);
			else
				graphics.RenderDieMesh(this, position, sizeFactor, rotationMatrix, dieColor, pipsColor);
		}

		/// <summary>Render the shadow of this die.</summary>
		public void RenderShadow(System.Drawing.PointF position, float sizeFactor, float[,] projectionMatrix, uint shadowColor) {
			graphics.RenderDieMeshShadow(this, position, sizeFactor, projectionMatrix, shadowColor);
		}

		public void Initialize() {
			loadTexture();
			loadGeometry();
		}

		private unsafe void loadTexture() {
			using(Stream stream = textureFile.Open()) {
				using(Bitmap bitmap = new Bitmap(stream)) {
					BitmapData bitmapData = bitmap.LockBits(
						new Rectangle(0, 0, bitmap.Width, bitmap.Height),
						ImageLockMode.ReadOnly,
						PixelFormat.Format24bppRgb);

					texture = new Texture(graphics.Device, bitmap.Width, bitmap.Height, 1, 0, Format.A8R8G8B8, Pool.Managed);
					int texturePitch;
					byte * textureBits = (byte*) texture.LockRectangle(0, LockFlags.None, out texturePitch).InternalData.ToPointer();

					byte* source = (byte*) bitmapData.Scan0;
					int sourceWidth = bitmapData.Width;
					int sourceHeight = bitmapData.Height;
					int stride = bitmapData.Stride;
					if(custom) {
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

					texture.UnlockRectangle(0);
					bitmap.UnlockBits(bitmapData);
				}
			}
		}

		private void loadGeometry() {
		
			int vertexCount = vertice.GetLength(0);
			CustomVertex.PositionNormalTextured[] verts = new CustomVertex.PositionNormalTextured[vertexCount];
			for(int i = 0; i < vertexCount; ++i) {
				verts[i].X = vertice[i, 0];
				verts[i].Y = vertice[i, 1];
				verts[i].Z = vertice[i, 2];
				verts[i].Nx = vertice[i, 3];
				verts[i].Ny = vertice[i, 4];
				verts[i].Nz = vertice[i, 5];
				verts[i].Tu = vertice[i, 6];
				verts[i].Tv = vertice[i, 7];
			}
			vb = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), vertexCount, graphics.Device, Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Managed);
			vb.SetData(verts, 0, 0);
		
			int triangleCount = triangles.GetLength(0);
			short[] indice = new short[triangleCount * 3];
			for(int i = 0; i < triangleCount; ++i) {
				indice[i * 3 + 0] = triangles[i, 0];
				indice[i * 3 + 1] = triangles[i, 1];
				indice[i * 3 + 2] = triangles[i, 2];
			}
			ib = new IndexBuffer(typeof(short), triangleCount * 3, graphics.Device, Usage.WriteOnly, Pool.Managed);
			ib.SetData(indice, 0, 0);
		}

		public void Dispose() {
			if(texture != null) texture.Dispose();
			if(vb != null) vb.Dispose();
			if(ib != null) ib.Dispose();
		}

		public Texture Texture { get { return texture; } }
		public VertexBuffer VertexBuffer { get { return vb; } }
		public IndexBuffer IndexBuffer { get { return ib; } }
		public int VertexCount { get { return vertice.GetLength(0); } }
		public int TriangleCount { get { return triangles.GetLength(0); } }
		public float Inradius { get { return inradius; } }

		private readonly DXGraphics graphics;
		private readonly float[,] vertice;
		private readonly Int16[,] triangles;
		private readonly float inradius;
		private readonly bool custom;
		private readonly IFile textureFile;
		private Texture texture = null;
		private VertexBuffer vb = null;
		private IndexBuffer ib = null;
	}
}
