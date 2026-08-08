using DefinitiveMultiplayer.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DefinitiveMultiplayer.Common.Players;

/// <summary>Red glow, light, outline, and dust for the marked living player.</summary>
public sealed class MarkedDrawPlayer : ModPlayer
{
	private static readonly Color DustTint = new(255, 40, 40);
	private static readonly Color ArmorTint = new(255, 48, 48);

	public override void FrameEffects()
	{
		if (!MarkedSystem.IsMarked(Player))
			return;

		Player.armorEffectDrawOutlines = true;
		Player.armorEffectDrawShadow = true;
	}

	public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
	{
		if (drawInfo.shadow != 0f || !MarkedSystem.IsMarked(Player))
			return;

		r = MathHelper.Clamp(r * 0.35f + 1f, 0f, 1.5f);
		g = MathHelper.Clamp(g * 0.35f, 0f, 1f);
		b = MathHelper.Clamp(b * 0.35f, 0f, 1f);
		fullBright = true;

		float pulse = 0.55f + 0.25f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
		Lighting.AddLight(Player.Center, 0.9f * pulse, 0.12f * pulse, 0.12f * pulse);

		if (Main.rand.NextBool(2))
		{
			Dust d = Dust.NewDustDirect(
				Player.position - new Vector2(4f),
				Player.width + 8,
				Player.height + 8,
				DustID.LifeDrain,
				0f,
				0f,
				120,
				DustTint,
				1.1f);
			d.noGravity = true;
			d.velocity *= 0.3f;
		}
	}

	public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
	{
		if (drawInfo.shadow != 0f || !MarkedSystem.IsMarked(Player))
			return;

		drawInfo.colorArmorHead = Color.Lerp(drawInfo.colorArmorHead, ArmorTint, 0.35f);
		drawInfo.colorArmorBody = Color.Lerp(drawInfo.colorArmorBody, ArmorTint, 0.35f);
		drawInfo.colorArmorLegs = Color.Lerp(drawInfo.colorArmorLegs, ArmorTint, 0.35f);
		drawInfo.colorHair = Color.Lerp(drawInfo.colorHair, ArmorTint, 0.25f);
		drawInfo.colorEyeWhites = Color.Lerp(drawInfo.colorEyeWhites, Color.White, 0.2f);
	}
}
