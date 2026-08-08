using DefinitiveMultiplayer.Common.Configs;
using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>Speed bonus and orange glow for the hot-potato holder.</summary>
public sealed class HotPotatoPlayer : ModPlayer
{
	private static readonly Color Orange = new(255, 170, 50);

	public override void PostUpdateRunSpeeds()
	{
		if (!HotPotatoSystem.IsHolder(Player))
			return;

		int bonus = Utils.Clamp(ServerConfig.Instance.HotPotatoSpeedBonusPercent, 0, 100);
		if (bonus <= 0)
			return;

		float mult = 1f + bonus / 100f;
		Player.moveSpeed *= mult;
		Player.maxRunSpeed *= mult;
		Player.accRunSpeed *= mult;
	}

	public override void FrameEffects()
	{
		if (!HotPotatoSystem.IsHolder(Player))
			return;

		Player.armorEffectDrawOutlines = true;
		Player.armorEffectDrawShadow = true;
	}

	public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
	{
		if (drawInfo.shadow != 0f || !HotPotatoSystem.IsHolder(Player))
			return;

		r = MathHelper.Clamp(r * 0.4f + 1f, 0f, 1.5f);
		g = MathHelper.Clamp(g * 0.45f + 0.55f, 0f, 1.2f);
		b = MathHelper.Clamp(b * 0.35f, 0f, 1f);
		fullBright = true;

		float pulse = 0.55f + 0.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 7f);
		Lighting.AddLight(Player.Center, 0.95f * pulse, 0.55f * pulse, 0.12f * pulse);

		if (Main.rand.NextBool(2))
		{
			Dust d = Dust.NewDustDirect(
				Player.position - new Vector2(4f),
				Player.width + 8,
				Player.height + 8,
				DustID.Torch,
				0f,
				0f,
				100,
				Orange,
				1.15f);
			d.noGravity = true;
			d.velocity *= 0.35f;
		}
	}

	public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
	{
		if (drawInfo.shadow != 0f || !HotPotatoSystem.IsHolder(Player))
			return;

		drawInfo.colorArmorHead = Color.Lerp(drawInfo.colorArmorHead, Orange, 0.35f);
		drawInfo.colorArmorBody = Color.Lerp(drawInfo.colorArmorBody, Orange, 0.35f);
		drawInfo.colorArmorLegs = Color.Lerp(drawInfo.colorArmorLegs, Orange, 0.35f);
		drawInfo.colorHair = Color.Lerp(drawInfo.colorHair, Orange, 0.25f);
	}
}
