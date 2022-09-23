/* -----------------------------------------------------------------------------
	  Copyright (c) 2006-2022 ZunTzu Software and contributors
----------------------------------------------------------------------------- */

#include "stdafx.h"
#include <vector>
#include "ZunTzuLib.h"

using namespace DirectX;

HWND main_window = nullptr;
LPDIRECT3D9 direct_3D = nullptr;
D3DPRESENT_PARAMETERS present_params;
LPDIRECT3DDEVICE9 device = nullptr;
LPDIRECT3DVERTEXBUFFER9 quad_vb = nullptr;
LPDIRECT3DTEXTURE9 white_tile = nullptr;
LPDIRECT3DTEXTURE9 black_tile = nullptr;

LPDIRECT3DVERTEXBUFFER9 latest_used_mesh_vb = nullptr;
LPDIRECT3DTEXTURE9 latest_used_mesh_texture = nullptr;

enum RENDERING_MODE {
	RM_DEFAULT,
	RM_SILHOUETTE,
	RM_IGNORE_MASK,
	RM_MESH
};
RENDERING_MODE rendering_mode = RM_DEFAULT;

struct PosColorTexVertex {
	// position
	float x;
	float y;
	float z;

	// color
	unsigned int color;

	// texture
	float u;
	float v;
};

struct PosNormalTexVertex {
	// position
	float x;
	float y;
	float z;

	// normal
	float nx;
	float ny;
	float nz;

	// texture
	float u;
	float v;
};

std::vector<D3DDISPLAYMODE> eligible_fullscreen_modes;

extern "C" int __cdecl GetEligibleFullscreenModeCount() {
	if (!direct_3D) direct_3D = Direct3DCreate9(D3D_SDK_VERSION);

	eligible_fullscreen_modes.clear();

	UINT display_mode_count = direct_3D->GetAdapterModeCount(D3DADAPTER_DEFAULT, D3DFMT_X8R8G8B8);

	D3DDISPLAYMODE display_mode;
	for (UINT i = 0; i < display_mode_count; ++i) {
		direct_3D->EnumAdapterModes(D3DADAPTER_DEFAULT, D3DFMT_X8R8G8B8, i, &display_mode);
		if (display_mode.Height >= 600 &&
			SUCCEEDED(direct_3D->CheckDeviceType(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, display_mode.Format, display_mode.Format, FALSE)))
		{
			eligible_fullscreen_modes.push_back(display_mode);
		}
	}

	return (int)eligible_fullscreen_modes.size();
}

extern "C" void __cdecl GetEligibleFullscreenMode(
	int index,
	int* width,
	int* height,
	int* refresh_rate,
	int* format
) {
	const D3DDISPLAYMODE& mode = eligible_fullscreen_modes[index];

	*width = (int)mode.Width;
	*height = (int)mode.Height;
	*refresh_rate = (int)mode.RefreshRate;
	*format = (int)mode.Format;
}

extern "C" void __cdecl GetCurrentDisplayMode(
	int* width,
	int* height,
	int* refresh_rate,
	int* format
) {
	if (!direct_3D) direct_3D = Direct3DCreate9(D3D_SDK_VERSION);

	D3DDISPLAYMODE mode;
	direct_3D->GetAdapterDisplayMode(D3DADAPTER_DEFAULT, &mode);

	*width = (int)mode.Width;
	*height = (int)mode.Height;
	*refresh_rate = (int)mode.RefreshRate;
	*format = (int)mode.Format;
}

extern "C" bool __cdecl CreateDevice(
	void* hMainWnd, 
	bool full_screen,
	int width, 
	int height, 
	int refresh_rate,
	int display_format,
	bool wait_for_vertical_blank
) {
	main_window = static_cast<HWND>(hMainWnd);

	// check that device is compliant
	
	D3DCAPS9 caps;
	direct_3D->GetDeviceCaps(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &caps);
	bool isCompliant =
		(caps.PrimitiveMiscCaps & D3DPMISCCAPS_CULLCW) != 0 &&
		(caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFLINEAR) != 0 &&
		(caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFLINEAR) != 0 &&
		(caps.TextureAddressCaps & D3DPTADDRESSCAPS_CLAMP) != 0 &&
		(caps.TextureOpCaps & D3DTEXOPCAPS_LERP) != 0 &&
		(caps.SrcBlendCaps & D3DPBLENDCAPS_SRCALPHA) != 0 &&
		(caps.DestBlendCaps & D3DPBLENDCAPS_INVSRCALPHA) != 0 &&
		(caps.TextureCaps & D3DPTEXTURECAPS_ALPHA) != 0 &&
		(caps.ShadeCaps & D3DPSHADECAPS_COLORGOURAUDRGB) != 0 &&
		(caps.ShadeCaps & D3DPSHADECAPS_SPECULARGOURAUDRGB) != 0 &&
		caps.MaxStreams >= 1 &&
		caps.MaxTextureBlendStages >= 1 &&
		caps.MaxSimultaneousTextures >= 1;
	if (!isCompliant) return false;

	// set presentation parameters

	memset(&present_params, 0, sizeof(D3DPRESENT_PARAMETERS));
	present_params.hDeviceWindow = main_window;
	present_params.SwapEffect = D3DSWAPEFFECT_DISCARD;
	present_params.BackBufferCount = 1;

	bool is_supported = UpdatePresentParameters(full_screen, width, height, refresh_rate, display_format, wait_for_vertical_blank);
	if (!is_supported) return false;

	// create device

	if (!SUCCEEDED(direct_3D->CreateDevice(
		D3DADAPTER_DEFAULT,
		D3DDEVTYPE_HAL,
		main_window,
		D3DCREATE_SOFTWARE_VERTEXPROCESSING, // Hardware vertex processing produces lower frame rates (even with a pure device), so we stick to software processing
		&present_params,
		&device))) return false;

	// allocate some resources

	// device->SetDialogBoxesEnabled(true);	// required to render controls in fullscreen
	device->CreateVertexBuffer(4 * sizeof(PosColorTexVertex), D3DUSAGE_WRITEONLY, D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX1, D3DPOOL_MANAGED, &quad_vb, nullptr);

	device->CreateTexture(1, 1, 1, 0, D3DFMT_X8R8G8B8, D3DPOOL::D3DPOOL_MANAGED, &black_tile, nullptr);
	D3DLOCKED_RECT locked_bits;
	black_tile->LockRect(0, &locked_bits, nullptr, 0);
	*static_cast<unsigned int*>(locked_bits.pBits) = 0x00000000;
	black_tile->UnlockRect(0);

	device->CreateTexture(1, 1, 1, 0, D3DFMT_X8R8G8B8, D3DPOOL::D3DPOOL_MANAGED, &white_tile, nullptr);
	white_tile->LockRect(0, &locked_bits, nullptr, 0);
	*static_cast<unsigned int*>(locked_bits.pBits) = 0x00FFFFFF;
	white_tile->UnlockRect(0);

	return true;
}

extern "C" void __cdecl FreeDevice()
{
	white_tile->Release();
	black_tile->Release();
	quad_vb->Release();
	device->Release();
	direct_3D->Release();
}

extern "C" int __cdecl CheckCooperativeLevel()
{
	HRESULT result = device->TestCooperativeLevel();
	if (SUCCEEDED(result)) return 0;
	else if (result == D3DERR_DEVICENOTRESET) return 1;
	else return 2;
}

extern "C" bool __cdecl ResetDevice()
{
	return SUCCEEDED(device->Reset(&present_params));
}

extern "C" bool __cdecl UpdatePresentParameters(
	bool full_screen,
	int width,
	int height,
	int refresh_rate,
	int display_format,
	bool wait_for_vertical_blank
) {
	// set presentation parameters

	present_params.PresentationInterval = (wait_for_vertical_blank ? D3DPRESENT_INTERVAL_DEFAULT : D3DPRESENT_INTERVAL_IMMEDIATE);
	present_params.BackBufferWidth = (UINT)width;
	present_params.BackBufferHeight = (UINT)height;

	if (full_screen)
	{
		present_params.Windowed = FALSE;
		present_params.BackBufferFormat = (D3DFORMAT)display_format;
		present_params.FullScreen_RefreshRateInHz = (UINT)refresh_rate;
		present_params.Flags = D3DPRESENTFLAG_LOCKABLE_BACKBUFFER; // required for controls rendering
		if (FAILED(direct_3D->CheckDeviceType(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, (D3DFORMAT)display_format, FALSE)))
		{
			return false;
		}
	}
	else
	{
		present_params.Windowed = TRUE;
		present_params.BackBufferFormat = D3DFMT_UNKNOWN;
		present_params.FullScreen_RefreshRateInHz = 0;
		present_params.Flags = 0;
		if (FAILED(direct_3D->CheckDeviceType(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, D3DFMT_UNKNOWN, TRUE)))
		{
			return false;
		}
	}

	// check device support for texture formats

	bool is_display_format_supported =
		SUCCEEDED(direct_3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, 0, D3DRTYPE_TEXTURE, D3DFMT_X8R8G8B8)) &&
		SUCCEEDED(direct_3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, 0, D3DRTYPE_TEXTURE, D3DFMT_A8R8G8B8)) &&
		SUCCEEDED(direct_3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, 0, D3DRTYPE_TEXTURE, D3DFMT_DXT1)) &&
		SUCCEEDED(direct_3D->CheckDeviceFormat(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, (D3DFORMAT)display_format, 0, D3DRTYPE_TEXTURE, D3DFMT_DXT5));
	return is_display_format_supported;
}

extern "C" void __cdecl BeginFrame()
{
	//Begin the scene
	device->BeginScene();

	// 2D settings
	device->SetRenderState(D3DRS_CULLMODE, D3DCULL_CW);
	device->SetRenderState(D3DRS_LIGHTING, FALSE);
	device->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_LINEAR);
	device->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_LINEAR);
	device->SetSamplerState(0, D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP);
	device->SetSamplerState(0, D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP);

	device->SetFVF(D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX1);
	device->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
	device->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE);
	device->SetTextureStageState(0, D3DTSS_COLORARG2, D3DTA_DIFFUSE);

	device->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
	device->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
	device->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);
	device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_MODULATE);
	device->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_DIFFUSE);
	device->SetTextureStageState(0, D3DTSS_ALPHAARG2, D3DTA_TEXTURE);

	device->SetTextureStageState(1, D3DTSS_COLOROP, D3DTOP_DISABLE);
	device->SetTextureStageState(1, D3DTSS_ALPHAOP, D3DTOP_DISABLE);

	XMMATRIX identity_matrix = XMMatrixIdentity();
	device->SetTransform(D3DTS_VIEW, reinterpret_cast<D3DMATRIX*>(&identity_matrix));
	XMMATRIX projection_matrix = XMMatrixMultiply(
		XMMatrixScaling(2.0f / present_params.BackBufferWidth, -2.0f / present_params.BackBufferHeight, 1.0f),
		XMMatrixTranslation(-1.0f, 1.0f, 0.0f));
	device->SetTransform(D3DTS_PROJECTION, reinterpret_cast<D3DMATRIX*>(&projection_matrix));
	device->SetTransform(D3DTS_WORLD, reinterpret_cast<D3DMATRIX*>(&identity_matrix));

	// 3D settings that can coexist with the 2D settings (so we can declare them ahead of time)
	D3DLIGHT9 light;
	memset(&light, 0, sizeof(D3DLIGHT9));
	light.Type = D3DLIGHT_DIRECTIONAL;
	light.Diffuse.r = 0.5f;
	light.Diffuse.g = 0.5f;
	light.Diffuse.b = 0.5f;
	light.Diffuse.a = 1.0f;
	light.Specular.r = 1.0f;
	light.Specular.g = 1.0f;
	light.Specular.b = 1.0f;
	light.Specular.a = 1.0f;
	light.Ambient.r = 0.5f;
	light.Ambient.g = 0.5f;
	light.Ambient.b = 0.5f;
	light.Ambient.a = 1.0f;
	light.Direction.x = 1.0f / 1.5f;
	light.Direction.y = -1.0f / 1.5f;
	light.Direction.z = 0.5f / 1.5f;
	device->SetLight(0, &light);
	device->LightEnable(0, TRUE);

	device->SetRenderState(D3DRS_SPECULARENABLE, TRUE);
	device->SetRenderState(D3DRS_NORMALIZENORMALS, TRUE);
	device->SetRenderState(D3DRS_AMBIENTMATERIALSOURCE, D3DMCS_MATERIAL);
	device->SetRenderState(D3DRS_DIFFUSEMATERIALSOURCE, D3DMCS_MATERIAL);
	device->SetRenderState(D3DRS_SPECULARMATERIALSOURCE, D3DMCS_MATERIAL);
	device->SetRenderState(D3DRS_SHADEMODE, D3DSHADE_GOURAUD);
	device->SetRenderState(D3DRS_COLORVERTEX, FALSE);

	rendering_mode = RM_DEFAULT;

	device->SetStreamSource(0, quad_vb, 0, sizeof(PosColorTexVertex));
}

extern "C" void __cdecl EndFrame()
{
	// End the scene, and show the result
	device->EndScene();
	device->Present(nullptr, nullptr, nullptr, nullptr);
}

void switch_to_2D_rendering()
{
	if (rendering_mode == RM_MESH) {
		// reset state
		device->SetRenderState(D3DRS_LIGHTING, FALSE);
		device->SetFVF(D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX1);

		// reset transform matrices
		XMMATRIX identity_matrix = XMMatrixIdentity();
		device->SetTransform(D3DTS_VIEW, reinterpret_cast<D3DMATRIX*>(&identity_matrix));
		XMMATRIX projection_matrix = XMMatrixMultiply(
			XMMatrixScaling(2.0f / present_params.BackBufferWidth, -2.0f / present_params.BackBufferHeight, 1.0f),
			XMMatrixTranslation(-1.0f, 1.0f, 0.0f));
		device->SetTransform(D3DTS_PROJECTION, reinterpret_cast<D3DMATRIX*>(&projection_matrix));
		device->SetTransform(D3DTS_WORLD, reinterpret_cast<D3DMATRIX*>(&identity_matrix));

		device->SetStreamSource(0, quad_vb, 0, sizeof(PosColorTexVertex));
	}
}

void switch_to_default_rendering()
{
	if (rendering_mode != RM_DEFAULT) {
		switch_to_2D_rendering();
		device->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
		device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_MODULATE);
		rendering_mode = RM_DEFAULT;
	}
}

void switch_to_silhouette_rendering()
{
	if (rendering_mode != RM_SILHOUETTE) {
		switch_to_2D_rendering();
		device->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG2);
		device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_MODULATE);
		rendering_mode = RM_SILHOUETTE;
	}
}

void switch_to_ignore_mask_rendering()
{
	if (rendering_mode != RM_IGNORE_MASK) {
		switch_to_2D_rendering();
		device->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
		device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
		rendering_mode = RM_IGNORE_MASK;
	}
}

void switch_to_mesh_rendering(void* mesh_vb, void* mesh_ib, void* mesh_texture)
{
	if (rendering_mode != RM_MESH) {
		if (rendering_mode != RM_DEFAULT) {
			device->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
			device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_MODULATE);
		}
		device->SetRenderState(D3DRS_LIGHTING, TRUE);
		device->SetFVF(D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_TEX1);
		latest_used_mesh_vb = nullptr;
		latest_used_mesh_texture = nullptr;

		XMMATRIX view_matrix = XMMatrixTranslation(0.0f, 0.0f, 100.0f);
		device->SetTransform(D3DTS_VIEW, reinterpret_cast<D3DMATRIX*>(&view_matrix));
		XMMATRIX projection_matrix = XMMatrixPerspectiveLH(
			1.0f,
			(float)present_params.BackBufferHeight / (float)present_params.BackBufferWidth,
			1.0f,
			200.0f);
		device->SetTransform(D3DTS_PROJECTION, reinterpret_cast<D3DMATRIX*>(&projection_matrix));

		rendering_mode = RM_MESH;
	}

	// do we need to set up a new stream source?
	LPDIRECT3DVERTEXBUFFER9 vb = static_cast<LPDIRECT3DVERTEXBUFFER9>(mesh_vb);
	if (latest_used_mesh_vb != vb) {
		// yes
		device->SetStreamSource(0, vb, 0, sizeof(PosNormalTexVertex));
		device->SetIndices(static_cast<LPDIRECT3DINDEXBUFFER9>(mesh_ib));
		latest_used_mesh_vb = vb;
	}

	LPDIRECT3DTEXTURE9 tex = static_cast<LPDIRECT3DTEXTURE9>(mesh_texture);
	if (latest_used_mesh_texture != tex) {
		device->SetTexture(0, tex);
		latest_used_mesh_texture = tex;
	}
}

void render_quad(
	LPDIRECT3DTEXTURE9 texture,
	unsigned int modulation_color,
	float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
	float tex_top, float tex_right, float tex_bottom, float tex_left)
{
	// set texture
	device->SetTexture(0, texture);

	// draw quad
	void* data;
	quad_vb->Lock(0, 4 * sizeof(PosColorTexVertex), &data, 0);
	PosColorTexVertex* verts = static_cast<PosColorTexVertex*>(data);

	verts[0].x = x0;
	verts[0].y = y0;
	verts[0].z = 0.0f;
	verts[0].color = modulation_color;
	verts[0].u = tex_left;
	verts[0].v = tex_top;

	verts[1].x = x1;
	verts[1].y = y1;
	verts[1].z = 0.0f;
	verts[1].color = modulation_color;
	verts[1].u = tex_left;
	verts[1].v = tex_bottom;

	verts[2].x = x2;
	verts[2].y = y2;
	verts[2].z = 0.0f;
	verts[2].color = modulation_color;
	verts[2].u = tex_right;
	verts[2].v = tex_top;

	verts[3].x = x3;
	verts[3].y = y3;
	verts[3].z = 0.0f;
	verts[3].color = modulation_color;
	verts[3].u = tex_right;
	verts[3].v = tex_bottom;

	quad_vb->Unlock();

	device->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2);
}

extern "C" void __cdecl RenderMonochromaticQuad(
	unsigned int color,
	float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
{
	switch_to_default_rendering();

	render_quad(
		white_tile, color,
		x0, y0, x1, y1, x2, y2, x3, y3,
		0.0f, 1.0f, 1.0f, 0.0f);
}

extern "C" void __cdecl RenderTexturedQuad(
	void* texture,
	unsigned int modulation_color,
	float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
	float tex_top, float tex_right, float tex_bottom, float tex_left)
{
	switch_to_default_rendering();

	LPDIRECT3DTEXTURE9 tex = (texture != nullptr ? static_cast<LPDIRECT3DTEXTURE9>(texture) : black_tile);
	render_quad(
		tex, modulation_color,
		x0, y0, x1, y1, x2, y2, x3, y3,
		tex_top, tex_right, tex_bottom, tex_left);
}

extern "C" void __cdecl RenderTexturedQuadSilhouette(
	void* texture,
	unsigned int modulation_color,
	float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
	float tex_top, float tex_right, float tex_bottom, float tex_left)
{
	switch_to_silhouette_rendering();

	LPDIRECT3DTEXTURE9 tex = (texture != nullptr ? static_cast<LPDIRECT3DTEXTURE9>(texture) : black_tile);
	render_quad(
		tex, modulation_color,
		x0, y0, x1, y1, x2, y2, x3, y3,
		tex_top, tex_right, tex_bottom, tex_left);
}

extern "C" void __cdecl RenderTexturedQuadIgnoreMask(
	void* texture,
	unsigned int modulation_color,
	float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3,
	float tex_top, float tex_right, float tex_bottom, float tex_left)
{
	switch_to_ignore_mask_rendering();

	LPDIRECT3DTEXTURE9 tex = (texture != nullptr ? static_cast<LPDIRECT3DTEXTURE9>(texture) : black_tile);
	render_quad(
		tex, modulation_color,
		x0, y0, x1, y1, x2, y2, x3, y3,
		tex_top, tex_right, tex_bottom, tex_left);
}

extern "C" void __cdecl RenderDieMesh(
	void* mesh_vb, void* mesh_ib, void* mesh_texture,
	int mesh_vertex_count, int mesh_triangle_count,
	float x, float y,
	float size_factor,
	float rot_x, float rot_y, float rot_z, float rot_w,
	unsigned int die_color, unsigned int pips_color)
{
	switch_to_mesh_rendering(mesh_vb, mesh_ib, mesh_texture);

	float scaling_factor = 100.0f / (float)present_params.BackBufferWidth;
	float scaling = size_factor * scaling_factor;

	XMMATRIX rotation = XMMatrixRotationQuaternion(DirectX::XMVectorSet(rot_x, rot_y, rot_z, rot_w));

	XMMATRIX world_matrix = XMMatrixMultiply(
		XMMatrixMultiply(rotation, XMMatrixScaling(scaling, scaling, scaling)),
		XMMatrixTranslation(
			(x - (float)present_params.BackBufferWidth * 0.5f) * scaling_factor,
			((float)present_params.BackBufferHeight * 0.5f - y) * scaling_factor,
			0.0f)
	);

	device->SetTransform(D3DTS_WORLD, reinterpret_cast<D3DMATRIX*>(&world_matrix));

	D3DMATERIAL9 material;
	memset(&material, 0, sizeof(D3DMATERIAL9));
	material.Specular.a = 1.0f;
	material.Specular.r = 1.0f;
	material.Specular.g = 1.0f;
	material.Specular.b = 1.0f;
	material.Power = 20.0f;

	material.Ambient.a = (float)((die_color & 0xFF000000) >> 24) / 255.0f;
	material.Ambient.r = (float)((die_color & 0x00FF0000) >> 16) / 255.0f;
	material.Ambient.g = (float)((die_color & 0x0000FF00) >> 8) / 255.0f;
	material.Ambient.b = (float)((die_color & 0x000000FF) >> 0) / 255.0f;
	material.Diffuse.a = material.Ambient.a;
	material.Diffuse.r = material.Ambient.r;
	material.Diffuse.g = material.Ambient.g;
	material.Diffuse.b = material.Ambient.b;
	device->SetMaterial(&material);

	device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	device->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, mesh_vertex_count, 0, mesh_triangle_count);

	material.Ambient.a = (float)((pips_color & 0xFF000000) >> 24) / 255.0f;
	material.Ambient.r = (float)((pips_color & 0x00FF0000) >> 16) / 255.0f;
	material.Ambient.g = (float)((pips_color & 0x0000FF00) >> 8) / 255.0f;
	material.Ambient.b = (float)((pips_color & 0x000000FF) >> 0) / 255.0f;
	material.Diffuse.a = material.Ambient.a;
	material.Diffuse.r = material.Ambient.r;
	material.Diffuse.g = material.Ambient.g;
	material.Diffuse.b = material.Ambient.b;
	device->SetMaterial(&material);

	device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG2);
	device->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, mesh_vertex_count, 0, mesh_triangle_count);
}

extern "C" void __cdecl RenderCustomDieMesh(
	void* mesh_vb, void* mesh_ib, void* mesh_texture,
	int mesh_vertex_count, int mesh_triangle_count,
	float x, float y,
	float size_factor,
	float rot_x, float rot_y, float rot_z, float rot_w)
{
	switch_to_mesh_rendering(mesh_vb, mesh_ib, mesh_texture);

	float scaling_factor = 100.0f / (float)present_params.BackBufferWidth;
	float scaling = size_factor * scaling_factor;

	XMMATRIX rotation = XMMatrixRotationQuaternion(DirectX::XMVectorSet(rot_x, rot_y, rot_z, rot_w));

	XMMATRIX world_matrix = XMMatrixMultiply(
		XMMatrixMultiply(rotation, XMMatrixScaling(scaling, scaling, scaling)),
		XMMatrixTranslation(
			(x - (float)present_params.BackBufferWidth * 0.5f) * scaling_factor,
			((float)present_params.BackBufferHeight * 0.5f - y) * scaling_factor,
			0.0f)
	);

	D3DMATERIAL9 material;
	memset(&material, 0, sizeof(D3DMATERIAL9));
	material.Ambient.a = 1.0f;
	material.Ambient.r = 1.0f;
	material.Ambient.g = 1.0f;
	material.Ambient.b = 1.0f;
	material.Diffuse.a = 1.0f;
	material.Diffuse.r = 1.0f;
	material.Diffuse.g = 1.0f;
	material.Diffuse.b = 1.0f;
	material.Specular.a = 1.0f;
	material.Specular.r = 1.0f;
	material.Specular.g = 1.0f;
	material.Specular.b = 1.0f;
	material.Power = 20.0f;
	device->SetMaterial(&material);

	device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG2);
	device->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, mesh_vertex_count, 0, mesh_triangle_count);
}

extern "C" void __cdecl RenderDieMeshShadow(
	void* mesh_vb, void* mesh_ib, void* mesh_texture,
	int mesh_vertex_count, int mesh_triangle_count,
	float mesh_inradius,
	float x, float y,
	float size_factor,
	float rot_x, float rot_y, float rot_z, float rot_w,
	unsigned int shadow_color)
{
	switch_to_mesh_rendering(mesh_vb, mesh_ib, mesh_texture);

	float scaling_factor = 100.0f / (float)present_params.BackBufferWidth;
	float scaling = size_factor * scaling_factor;

	XMMATRIX rotation = XMMatrixRotationQuaternion(DirectX::XMVectorSet(rot_x, rot_y, rot_z, rot_w));

	XMMATRIX projection_on_table = XMMATRIX(
		1.0f, 0.0f, 0.0f, 0.0f,
		0.0f, 1.0f, 0.0f, 0.0f,
		-1.0f / 1.5f, 1.0f / 1.5f, 0.0f, 0.0f,
		mesh_inradius / 1.5f, -(mesh_inradius / 1.5f), mesh_inradius, 1.0f
	);

	XMMATRIX world_matrix = XMMatrixMultiply(
		XMMatrixMultiply(
			XMMatrixMultiply(rotation, projection_on_table),
			XMMatrixScaling(scaling, scaling, scaling)
		),
		XMMatrixTranslation(
			(x - (float)present_params.BackBufferWidth * 0.5f) * scaling_factor,
			((float)present_params.BackBufferHeight * 0.5f - y) * scaling_factor,
			0.0f)
	);

	D3DMATERIAL9 material;
	memset(&material, 0, sizeof(D3DMATERIAL9));
	material.Ambient.a = (float)((shadow_color & 0xFF000000) >> 24) / 255.0f;
	material.Ambient.r = (float)((shadow_color & 0x00FF0000) >> 16) / 255.0f;
	material.Ambient.g = (float)((shadow_color & 0x0000FF00) >> 8) / 255.0f;
	material.Ambient.b = (float)((shadow_color & 0x000000FF) >> 0) / 255.0f;
	material.Diffuse.a = material.Ambient.a;
	material.Diffuse.r = material.Ambient.r;
	material.Diffuse.g = material.Ambient.g;
	material.Diffuse.b = material.Ambient.b;
	material.Specular.a = 1.0f;
	material.Specular.r = 0.0f;
	material.Specular.g = 0.0f;
	material.Specular.b = 0.0f;
	material.Power = 0.0f;
	device->SetMaterial(&material);

	device->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	device->DrawIndexedPrimitive(D3DPT_TRIANGLELIST, 0, 0, mesh_vertex_count, 0, mesh_triangle_count);
}

extern "C" void* __cdecl CreateTexture(int width, int height, int format)
{
	LPDIRECT3DTEXTURE9 texture = nullptr;
	device->CreateTexture((unsigned int)width, (unsigned int)height, 1, 0, (D3DFORMAT)format, D3DPOOL_MANAGED, &texture, nullptr);
	return texture;
}

extern "C" void __cdecl LockTexture(void* texture, int* pitch, char** bits)
{
	LPDIRECT3DTEXTURE9 tex = static_cast<LPDIRECT3DTEXTURE9>(texture);
	D3DLOCKED_RECT locked_rect;
	tex->LockRect(0, &locked_rect, nullptr, 0);
	*pitch = locked_rect.Pitch;
	*bits = static_cast<char*>(locked_rect.pBits);
}

extern "C" void __cdecl LockTextureReadOnly(void* texture, int* pitch, char** bits)
{
	LPDIRECT3DTEXTURE9 tex = static_cast<LPDIRECT3DTEXTURE9>(texture);
	D3DLOCKED_RECT locked_rect;
	tex->LockRect(0, &locked_rect, nullptr, D3DLOCK_READONLY);
	*pitch = locked_rect.Pitch;
	*bits = static_cast<char*>(locked_rect.pBits);
}

extern "C" void __cdecl UnlockTexture(void* texture)
{
	static_cast<LPDIRECT3DTEXTURE9>(texture)->UnlockRect(0);
}

extern "C" void __cdecl FreeTexture(void* texture)
{
	static_cast<LPDIRECT3DTEXTURE9>(texture)->Release();
}

extern "C" void* __cdecl CreateVertexBuffer(int vertex_count, void* data)
{
	unsigned int size = (unsigned int)vertex_count * sizeof(PosNormalTexVertex);

	LPDIRECT3DVERTEXBUFFER9 vb = nullptr;
	device->CreateVertexBuffer(
		size,
		D3DUSAGE_WRITEONLY,
		D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_TEX1,
		D3DPOOL_MANAGED,
		&vb,
		nullptr);

	void* vb_bits = nullptr;
	vb->Lock(0, size, &vb_bits, 0);
	memcpy(vb_bits, data, size);
	vb->Unlock();

	return vb;
}

extern "C" void __cdecl FreeVertexBuffer(void* vb)
{
	static_cast<LPDIRECT3DVERTEXBUFFER9>(vb)->Release();
}

extern "C" void* __cdecl CreateIndexBuffer(int triangle_count, short* data)
{
	unsigned int size = (unsigned int)triangle_count * sizeof(short) * 3;

	LPDIRECT3DINDEXBUFFER9 ib = nullptr;
	device->CreateIndexBuffer(
		size,
		D3DUSAGE_WRITEONLY,
		D3DFMT_INDEX16,
		D3DPOOL_MANAGED,
		&ib,
		nullptr);

	void* ib_bits = nullptr;
	ib->Lock(0, size, &ib_bits, 0);
	memcpy(ib_bits, data, size);
	ib->Unlock();

	return ib;
}

extern "C" void __cdecl FreeIndexBuffer(void* ib)
{
	static_cast<LPDIRECT3DINDEXBUFFER9>(ib)->Release();
}
