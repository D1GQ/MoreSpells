using BlackMagicAPI.Modules.Spells;
using System.Collections;
using UnityEngine;

namespace MoreSpells.Modules;

internal class MagicShieldLogic : SpellLogic
{
    private static GameObject? OrbPrefab;
    private GameObject? orb;
    private float shieldLife = 15f;

    public override void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        orb = Instantiate(OrbPrefab);
        if (orb != null)
        {
            orb.SetActive(true);
            orb.transform.SetParent(playerObj.transform, false);
            var player = playerObj.GetComponent<PlayerMovement>();
            if (player != null)
            {
                StartCoroutine(CoProtect(player));
            }
        }
    }

    private IEnumerator CoProtect(PlayerMovement player)
    {
        // Exit if player reference is null
        if (player == null) yield break;

        // Initialize variables:
        float lastHealth = player.playerHealth; // Track health from previous frame
        float healthRecoveryRate = 3f;         // Health recovery per second
        float shieldDamagePenalty = 0.2f;      // Shield penalty when taking damage (20% of damage taken)

        // Main shield loop - runs while shield has life, player exists and isn't dead
        while (shieldLife > 0 && player != null && !player.isDead)
        {
            // Deplete shield over time
            shieldLife -= Time.deltaTime;

            // If player gets frozen, trigger breakout and end shield
            if (player.isFrozen)
            {
                player.breakoutFireball = true;
                yield break;
            }

            // Damage handling:
            // If player took damage since last frame
            if (player.playerHealth < lastHealth)
            {
                // Calculate damage taken and reduce it by 75%
                float damageTaken = lastHealth - player.playerHealth;
                player.playerHealth = lastHealth - (damageTaken * 0.75f);

                // Apply penalty to shield based on damage taken
                shieldLife -= damageTaken * shieldDamagePenalty;
            }
            // If player is not at full health
            else if (player.playerHealth < 100f)
            {
                // Gradually recover health
                float healthToAdd = healthRecoveryRate * Time.deltaTime;
                player.playerHealth = Mathf.Min(player.playerHealth + healthToAdd, 100f);

                // Small shield cost for healing (20% of health recovered)
                shieldLife -= healthToAdd * 0.2f;
            }

            // Limit fire timer to 1 second if it exceeds
            if (player.fireTimer > 1f)
                player.fireTimer = 1f;

            // Gradually deplete stamina while shield is active
            if (player.stamina > 0)
                player.stamina -= 3f * player.stamina * 0.1f * Time.deltaTime;

            // Update lastHealth for next frame comparison
            lastHealth = player.playerHealth;
            yield return null; // Wait for next frame
        }

        // Shield broken sequence:
        // Trigger break animation if orb exists
        orb?.GetComponent<Animator>().SetTrigger("Break");

        // Wait for animation to play
        yield return new WaitForSeconds(0.5f);

        // Clean up orb and object
        Destroy(orb);
        Destroy(gameObject);
    }

    public override void OnPrefabCreatedAutomatically(GameObject prefab)
    {
        OrbPrefab = MSPlugin.SpellsAssets?.LoadAsset<GameObject>("Assets/Spells/MagicShield.prefab");
        DontDestroyOnLoad(OrbPrefab);
    }
}
