using BlackMagicAPI.Modules.Spells;
using System.Collections;
using UnityEngine;

namespace MoreSpells.Spells.MagicShield;

public class MagicShieldLogic : SpellLogic
{
    // Prefab reference for the shield orb visual effect
    private static GameObject? OrbPrefab;
    // Instance of the spawned orb
    private GameObject? orb;
    // Duration/health of the shield in seconds
    private float shieldLife = 15f;

    // Main spell casting method
    public override void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        // Instantiate the shield orb
        orb = Instantiate(OrbPrefab);
        if (orb != null)
        {
            // Activate and parent it to the player
            orb.SetActive(true);
            orb.transform.SetParent(playerObj.transform, false);

            // Get player component and start protection routine
            var player = playerObj.GetComponent<PlayerMovement>();
            if (player != null)
            {
                StartCoroutine(CoProtect(player));
            }
        }
    }

    // Coroutine that handles all shield functionality
    private IEnumerator CoProtect(PlayerMovement player)
    {
        // Safety check - exit if player reference is null
        if (player == null) yield break;

        // Initialize shield variables:
        float lastHealth = player.playerHealth; // Track health from previous frame
        float healthRecoveryRate = 3f;         // Health recovery per second (when not taking damage)
        float shieldDamagePenalty = 0.2f;      // Shield penalty when taking damage (20% of damage taken)

        // Main shield loop
        while (shieldLife > 0 && player != null && !player.isDead)
        {
            // Deplete shield over time (duration effect)
            shieldLife -= Time.deltaTime;

            // Special case: If player gets frozen
            if (player.isFrozen)
            {
                // Trigger breakout effect and immediately end shield
                player.breakoutFireball = true;
                yield break;
            }

            // Damage handling system:
            // Case 1: Player took damage since last frame
            if (player.playerHealth < lastHealth)
            {
                // Calculate damage taken and reduce it by 75% (25% damage gets through)
                float damageTaken = lastHealth - player.playerHealth;
                player.playerHealth = lastHealth - damageTaken * 0.75f;

                // Apply penalty to shield based on damage taken (20% of original damage)
                shieldLife -= damageTaken * shieldDamagePenalty;
            }
            // Case 2: Player is not at full health but not taking damage
            else if (player.playerHealth < 100f)
            {
                // Gradually recover health (3 HP per second)
                float healthToAdd = healthRecoveryRate * Time.deltaTime;
                player.playerHealth = Mathf.Min(player.playerHealth + healthToAdd, 100f);

                // Small shield cost for healing (20% of health recovered)
                shieldLife -= healthToAdd * 0.2f;
            }

            // Fire resistance effect: Limit fire timer to 1 second max
            if (player.fireTimer > 1f)
                player.fireTimer = 1f;

            // Stamina drain effect: Gradually deplete stamina while shield is active
            if (player.stamina > 0)
                player.stamina -= 3f * player.stamina * 0.1f * Time.deltaTime;

            // Update lastHealth for next frame comparison
            lastHealth = player.playerHealth;
            yield return null; // Wait for next frame
        }

        // Shield broken sequence:
        // Trigger break animation if orb exists
        orb?.GetComponent<Animator>().SetTrigger("Break");

        // Wait for animation to play (0.5 seconds)
        yield return new WaitForSeconds(0.5f);

        // Clean up orb and spell object
        Destroy(orb);
        DisposeSpell();
    }

    // Called when the spell prefab is created to load required assets
    public override void OnPrefabCreatedAutomatically(GameObject prefab)
    {
        // Load the shield orb prefab from assets
        OrbPrefab = MSPlugin.SpellsAssets?.LoadAsset<GameObject>("Assets/SpellAssets/Spells/MagicShield.prefab");
        // Ensure it persists between scene loads
        DontDestroyOnLoad(OrbPrefab);
    }
}