using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Spells.TheEyeOfHell;

internal class TheEyeOfHellData : SpellData
{
    public override string Name => "The Eye Of Hell";

    public override float Cooldown => 60f;

    public override Color GlowColor => Color.red;
}
