using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Modules;

internal class MagicShieldData : SpellData
{
    public override string Name => "Magic Shield";

    public override float Cooldown => 25;

    public override Color GlowColor => Color.blue;
}
