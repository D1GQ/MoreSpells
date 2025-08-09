using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Spells.EchoLocation;

internal class EchoLocationData : SpellData
{
    public override string Name => "Echo Location";

    public override float Cooldown => 30f;

    public override Color GlowColor => Color.gray;
}
