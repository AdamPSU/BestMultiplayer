using System;
using System.Collections.Generic;
using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Drawing;

/// <summary>
/// Hollow dual-ring silhouette: gap, accent ring, white ring.
/// CPU mask from live DrawDataCache (no RenderTarget — avoids wiping the world RT).
/// Pose-stable frames reuse a distance field; only accent colorization + GPU upload runs each frame.
/// </summary>
internal static class PlayerStatusOutline
{
	private const int GapPx = 5;
	private const int AccentWidthPx = 2;
	private const int WhiteWidthPx = 2;
	private const int MaxDistPx = GapPx + AccentWidthPx + WhiteWidthPx;
	private const int Pad = MaxDistPx + 2;
	private const int MaxSide = 512;
	private const byte OpaqueThreshold = 32;
	private const ushort DistNone = ushort.MaxValue;

	private static readonly Dictionary<Texture2D, Color[]> TexCache = new();

	private static Texture2D _ringTex;
	private static byte[] _maskBuf;
	private static ushort[] _distSqBuf;
	private static Color[] _ringBuf;
	private static int _capW;
	private static int _capH;

	private static int _cachedPoseKey = int.MinValue;
	private static int _cachedW;
	private static int _cachedH;

	/// <param name="displaySeconds">Fuse seconds (−1 unknown). ≤ CountdownVisibleSeconds triggers urgency blink.</param>
	/// <param name="transferFlash">Brief blink right after mark/potato transfer.</param>
	internal static void Apply(ref PlayerDrawSet drawInfo, Color accent, int displaySeconds, bool transferFlash)
	{
		if (drawInfo.shadow != 0f || Main.dedServ)
			return;

		if (!ResolveAnim(displaySeconds, transferFlash, out float flare))
			return;

		List<DrawData> cache = drawInfo.DrawDataCache;
		int n = cache.Count;
		if (n == 0)
			return;

		if (!TryMeasure(cache, out int minX, out int minY, out int maxX, out int maxY))
			return;

		int contentW = Math.Max(1, maxX - minX);
		int contentH = Math.Max(1, maxY - minY);
		int w = contentW + Pad * 2;
		int h = contentH + Pad * 2;
		if (w > MaxSide || h > MaxSide)
			return;

		Player player = drawInfo.drawPlayer;
		int poseKey = PoseKey(player, n, contentW, contentH);
		bool rebuild = poseKey != _cachedPoseKey || _distSqBuf is null || w != _cachedW || h != _cachedH;
		if (rebuild)
		{
			EnsureBuffers(w, h);
			Array.Clear(_maskBuf, 0, w * h);

			Vector2 rtOrigin = new(minX - Pad, minY - Pad);
			for (int i = 0; i < n; i++)
				StampMask(cache[i], rtOrigin, w, h);

			BuildDistanceField(_maskBuf, _distSqBuf, w, h);

			_cachedPoseKey = poseKey;
			_cachedW = w;
			_cachedH = h;
		}

		ColorizeRing(_distSqBuf, _ringBuf, w, h, Color.Lerp(accent, Color.White, flare));
		_ringTex.SetData(0, new Rectangle(0, 0, w, h), _ringBuf, 0, w * h);

		var ring = new DrawData(_ringTex, new Vector2(minX - Pad, minY - Pad), new Rectangle(0, 0, w, h), Color.White)
		{
			shader = 0,
			ignorePlayerRotation = true,
		};
		cache.Insert(0, ring);
	}

	/// <returns>false when blink is off (skip draw).</returns>
	private static bool ResolveAnim(int displaySeconds, bool transferFlash, out float flare)
	{
		uint tick = Main.GameUpdateCount;
		int urgentMax = MarkedSystem.CountdownVisibleSeconds;

		if (transferFlash || (displaySeconds >= 0 && displaySeconds <= urgentMax))
		{
			// Faster near 0s; transfer ~15Hz (2 frames/half).
			int halfFrames = transferFlash
				? 2
				: (int)MathHelper.Lerp(3f, 5f, Utils.Clamp(displaySeconds, 0, urgentMax) / (float)urgentMax);
			bool on = (tick / (uint)halfFrames) % 2u == 0u;
			flare = on ? 0.35f : 0f;
			return on;
		}

		// ~1.1Hz brightness breathe.
		float wave = 0.5f + 0.5f * MathF.Sin(tick / 60f * MathHelper.TwoPi * 1.1f);
		flare = 0.08f + 0.42f * wave;
		return true;
	}

	private static int PoseKey(Player player, int drawCount, int contentW, int contentH)
	{
		// Coarse appearance/pose fingerprint — rebuild mask when equipment or animation changes.
		unchecked
		{
			int h = player.whoAmI;
			h = (h * 397) ^ player.bodyFrame.Y;
			h = (h * 397) ^ player.legFrame.Y;
			h = (h * 397) ^ player.direction;
			h = (h * 397) ^ (int)player.gravDir;
			h = (h * 397) ^ player.head;
			h = (h * 397) ^ player.body;
			h = (h * 397) ^ player.legs;
			h = (h * 397) ^ player.handon;
			h = (h * 397) ^ player.handoff;
			h = (h * 397) ^ player.back;
			h = (h * 397) ^ player.front;
			h = (h * 397) ^ player.shoe;
			h = (h * 397) ^ player.waist;
			h = (h * 397) ^ player.shield;
			h = (h * 397) ^ player.neck;
			h = (h * 397) ^ player.face;
			h = (h * 397) ^ player.balloon;
			h = (h * 397) ^ player.wingFrame;
			h = (h * 397) ^ drawCount;
			h = (h * 397) ^ contentW;
			h = (h * 397) ^ contentH;
			return h;
		}
	}

	internal static void Unload()
	{
		_ringTex?.Dispose();
		_ringTex = null;
		_maskBuf = null;
		_distSqBuf = null;
		_ringBuf = null;
		_capW = _capH = 0;
		_cachedPoseKey = int.MinValue;
		TexCache.Clear();
	}

	private static void EnsureBuffers(int w, int h)
	{
		if (_ringTex != null && _capW >= w && _capH >= h)
			return;

		int nw = Math.Max(_capW, w);
		int nh = Math.Max(_capH, h);
		_ringTex?.Dispose();
		_capW = nw;
		_capH = nh;
		_ringTex = new Texture2D(Main.instance.GraphicsDevice, nw, nh);
		_maskBuf = new byte[nw * nh];
		_distSqBuf = new ushort[nw * nh];
		_ringBuf = new Color[nw * nh];
		_cachedPoseKey = int.MinValue;
	}

	private static Color[] GetTexData(Texture2D tex)
	{
		if (TexCache.TryGetValue(tex, out Color[] data))
			return data;

		data = new Color[tex.Width * tex.Height];
		tex.GetData(data);
		TexCache[tex] = data;
		return data;
	}

	private static bool TryMeasure(List<DrawData> cache, out int minX, out int minY, out int maxX, out int maxY)
	{
		minX = int.MaxValue;
		minY = int.MaxValue;
		maxX = int.MinValue;
		maxY = int.MinValue;
		bool any = false;

		for (int i = 0; i < cache.Count; i++)
		{
			DrawData d = cache[i];
			if (d.texture == null || d.color.A < OpaqueThreshold)
				continue;

			GetDrawBounds(d, out int x0, out int y0, out int x1, out int y1);
			if (x1 <= x0 || y1 <= y0)
				continue;

			any = true;
			if (x0 < minX) minX = x0;
			if (y0 < minY) minY = y0;
			if (x1 > maxX) maxX = x1;
			if (y1 > maxY) maxY = y1;
		}

		return any;
	}

	private static void GetDrawBounds(DrawData d, out int x0, out int y0, out int x1, out int y1)
	{
		if (d.useDestinationRectangle)
		{
			Rectangle r = d.destinationRectangle;
			x0 = r.X;
			y0 = r.Y;
			x1 = r.Right;
			y1 = r.Bottom;
			return;
		}

		Rectangle src = d.sourceRect ?? new Rectangle(0, 0, d.texture.Width, d.texture.Height);
		Vector2 scale = d.scale;
		float w = src.Width * scale.X;
		float h = src.Height * scale.Y;
		Vector2 origin = d.origin;
		Vector2 pos = d.position;

		if (d.rotation != 0f)
		{
			float cos = Math.Abs(MathF.Cos(d.rotation));
			float sin = Math.Abs(MathF.Sin(d.rotation));
			float bw = cos * w + sin * h;
			float bh = sin * w + cos * h;
			float left = pos.X - bw * 0.5f;
			float top = pos.Y - bh * 0.5f;
			x0 = (int)MathF.Floor(left);
			y0 = (int)MathF.Floor(top);
			x1 = (int)MathF.Ceiling(left + bw);
			y1 = (int)MathF.Ceiling(top + bh);
			return;
		}

		float l = pos.X - origin.X * scale.X;
		float t = pos.Y - origin.Y * scale.Y;
		x0 = (int)MathF.Floor(l);
		y0 = (int)MathF.Floor(t);
		x1 = (int)MathF.Ceiling(l + w);
		y1 = (int)MathF.Ceiling(t + h);
	}

	private static void StampMask(DrawData d, Vector2 rtOrigin, int mw, int mh)
	{
		if (d.texture == null || d.color.A < OpaqueThreshold)
			return;

		Texture2D tex = d.texture;
		Color[] texData = GetTexData(tex);
		int tw = tex.Width;

		if (d.useDestinationRectangle)
		{
			Rectangle dest = d.destinationRectangle;
			Rectangle src = d.sourceRect ?? new Rectangle(0, 0, tex.Width, tex.Height);
			if (src.Width <= 0 || src.Height <= 0 || dest.Width <= 0 || dest.Height <= 0)
				return;

			for (int dy = 0; dy < dest.Height; dy++)
			{
				int sy = src.Y + dy * src.Height / dest.Height;
				for (int dx = 0; dx < dest.Width; dx++)
				{
					int sx = src.X + dx * src.Width / dest.Width;
					if (texData[sy * tw + sx].A < OpaqueThreshold)
						continue;
					int mx = dest.X + dx - (int)rtOrigin.X;
					int my = dest.Y + dy - (int)rtOrigin.Y;
					if ((uint)mx >= (uint)mw || (uint)my >= (uint)mh)
						continue;
					_maskBuf[my * mw + mx] = 1;
				}
			}

			return;
		}

		Rectangle source = d.sourceRect ?? new Rectangle(0, 0, tex.Width, tex.Height);
		Vector2 scale = d.scale;
		float scaleX = scale.X;
		float scaleY = scale.Y;
		if (scaleX == 0f || scaleY == 0f)
			return;

		bool flipH = (d.effect & SpriteEffects.FlipHorizontally) != 0;
		bool flipV = (d.effect & SpriteEffects.FlipVertically) != 0;
		Vector2 origin = d.origin;
		Vector2 pos = d.position - rtOrigin;
		float rot = d.rotation;
		bool rotated = rot != 0f;
		float cos = rotated ? MathF.Cos(rot) : 1f;
		float sin = rotated ? MathF.Sin(rot) : 0f;

		int sw = source.Width;
		int sh = source.Height;

		for (int sy = 0; sy < sh; sy++)
		{
			for (int sx = 0; sx < sw; sx++)
			{
				int tx = source.X + sx;
				int ty = source.Y + sy;
				if ((uint)tx >= (uint)tex.Width || (uint)ty >= (uint)tex.Height)
					continue;
				if (texData[ty * tw + tx].A < OpaqueThreshold)
					continue;

				float localX = sx + 0.5f;
				float localY = sy + 0.5f;
				if (flipH)
					localX = sw - localX;
				if (flipV)
					localY = sh - localY;

				float ox = (localX - origin.X) * scaleX;
				float oy = (localY - origin.Y) * scaleY;

				float fx = ox;
				float fy = oy;
				if (rotated)
				{
					fx = ox * cos - oy * sin;
					fy = ox * sin + oy * cos;
				}

				int mx = (int)MathF.Floor(pos.X + fx);
				int my = (int)MathF.Floor(pos.Y + fy);
				if ((uint)mx >= (uint)mw || (uint)my >= (uint)mh)
					continue;
				_maskBuf[my * mw + mx] = 1;
			}
		}
	}

	/// <summary>Exact min distance² to solid for empty pixels within MaxDistPx (pose-stable cache).</summary>
	private static void BuildDistanceField(byte[] mask, ushort[] distSq, int w, int h)
	{
		int total = w * h;
		for (int i = 0; i < total; i++)
			distSq[i] = DistNone;

		int rMax = MaxDistPx;
		int rMaxSq = rMax * rMax;

		for (int y = 0; y < h; y++)
		{
			int row = y * w;
			for (int x = 0; x < w; x++)
			{
				if (mask[row + x] != 0)
					continue;

				int bestSq = rMaxSq + 1;
				int x0 = Math.Max(0, x - rMax);
				int x1 = Math.Min(w - 1, x + rMax);
				int y0 = Math.Max(0, y - rMax);
				int y1 = Math.Min(h - 1, y + rMax);

				for (int yy = y0; yy <= y1; yy++)
				{
					int dy = yy - y;
					int dySq = dy * dy;
					if (dySq >= bestSq)
						continue;
					int mrow = yy * w;
					for (int xx = x0; xx <= x1; xx++)
					{
						if (mask[mrow + xx] == 0)
							continue;
						int dx = xx - x;
						int dSq = dx * dx + dySq;
						if (dSq < bestSq)
							bestSq = dSq;
					}
				}

				if (bestSq <= rMaxSq)
					distSq[row + x] = (ushort)bestSq;
			}
		}
	}

	private static void ColorizeRing(ushort[] distSq, Color[] ring, int w, int h, Color accent)
	{
		Array.Clear(ring, 0, w * h);

		Color accentPx = new(accent.R, accent.G, accent.B, 255);
		Color whitePx = Color.White;
		int gapSq = GapPx * GapPx;
		int accentEndSq = (GapPx + AccentWidthPx) * (GapPx + AccentWidthPx);
		int whiteEndSq = MaxDistPx * MaxDistPx;

		int total = w * h;
		for (int i = 0; i < total; i++)
		{
			ushort d = distSq[i];
			if (d == DistNone || d < gapSq)
				continue;
			if (d < accentEndSq)
				ring[i] = accentPx;
			else if (d < whiteEndSq)
				ring[i] = whitePx;
		}
	}
}
