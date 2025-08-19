using BlackMagicAPI.Modules.Spells;
using UnityEngine;

namespace MoreSpells.Spells.Resurrection;

public class ResurrectionData : SpellData
{
    public override string Name => "Resurrection";

    public override float Cooldown => 60f;

    public override Color GlowColor => Color.cyan;

    public override bool DebugForceSpawn => true;
}
