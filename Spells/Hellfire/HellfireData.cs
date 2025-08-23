using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Spells.Hellfire;

internal class HellfireData : SpellData
{
    public override string Name => "Hellfire";

    public override float Cooldown => 40f;

    public override Color GlowColor => Color.yellow;
}
