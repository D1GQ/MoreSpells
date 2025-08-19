using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Spells.MagicShield;

public class MagicShieldData : SpellData
{
    public override string Name => "Magic Shield";

    public override string[] SubNames => ["Magic", "Shield"];

    public override float Cooldown => 35f;

    public override Color GlowColor => Color.blue;
}
