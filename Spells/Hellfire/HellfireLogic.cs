using BlackMagicAPI.Modules.Spells;
using FishUtilities.Attributes;
using System.Collections;
using UnityEngine;

namespace MoreSpells.Spells.Hellfire;

internal class HellfireLogic : SpellLogic
{
    // Prefab reference for the fireball projectile
    private static FireballController? firePrefab;

    // Configuration parameters for the spell
    private static readonly float duration = 12f;        // How long the spell lasts in seconds
    private static readonly float rangeWidth = 50f;      // Width of the area where fireballs can spawn
    private static readonly float rangeDistance = 35f;   // Distance range for fireball spawning

    public override bool CastSpell(PlayerMovement caster, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        // Only execute on the owner client to avoid duplicate execution
        if (caster.IsOwner)
        {
            // Send command to start the hellfire routine on host client
            CmdHellfireRoutine(caster.gameObject, viewDirectionVector);

            // Start coroutine to dispose of the spell after it completes
            StartCoroutine(CoWaitDisposeSpell());
        }

        return true;
    }

    // Coroutine to wait for spell duration and then dispose of it
    private IEnumerator CoWaitDisposeSpell()
    {
        // Wait for spell duration plus buffer time
        yield return new WaitForSeconds(duration + 5f);

        // Clean up the spell
        DisposeSpell();
        yield break;
    }

    // Command method (executed on host client) to start the hellfire routine
    [FishCmd]
    private static void CmdHellfireRoutine(GameObject caster, Vector3 viewDirectionVector)
    {
        // Start the hellfire coroutine on the caster's PlayerMovement component
        caster.GetComponent<PlayerMovement>().StartCoroutine(CoHellfire(caster, viewDirectionVector * 50f));
    }

    // Main hellfire coroutine that spawns fireballs over time
    private static IEnumerator CoHellfire(GameObject caster, Vector3 viewDirectionVector)
    {
        // Calculate when the spell should end
        float endTime = Time.time + duration;

        // Calculate the center point in front of the player
        Vector3 centerPoint = caster.transform.position + (viewDirectionVector.normalized * (rangeDistance / 2f));
        centerPoint.y = 0f; // Ensure center is at ground level

        // Continue spawning fireballs until time runs out
        while (Time.time < endTime)
        {
            // Generate random angle and distance for circular distribution
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            float randomDistance = UnityEngine.Random.Range(0f, rangeWidth / 2f); // Use half rangeWidth as radius

            // Convert polar coordinates (angle + distance) to Cartesian coordinates
            Vector3 randomOffset = new(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad) * randomDistance,
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad) * randomDistance
            );

            // Apply the circular offset relative to the center point
            Vector3 groundTarget = centerPoint + randomOffset;

            // Calculate spawn position above the target (directly overhead)
            Vector3 spawnPosition = groundTarget + new Vector3(0f, 125f, 0f);

            // Calculate direction from spawn position to ground target (straight down)
            Vector3 angledDirection = (groundTarget - spawnPosition).normalized;

            // Spawn fireball on all clients
            RpcSpawnFireball(caster, spawnPosition, angledDirection);

            // Wait random interval before spawning next fireball
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1f));
        }

        yield break;
    }

    // RPC method (executed on all clients) to spawn individual fireballs
    [FishRpc]
    private static void RpcSpawnFireball(GameObject caster, Vector3 spawnPos, Vector3 direction)
    {
        // Instantiate the fireball prefab
        var fireBall = Instantiate(firePrefab);

        if (fireBall != null)
        {
            // Set fireball properties
            fireBall.transform.position = spawnPos;
            fireBall.level = 0;
            fireBall.playerOwner = caster;

            // Apply initial force to the fireball
            fireBall.rb.AddForce(direction * 50f, ForceMode.VelocityChange);
            fireBall.StartCoroutine(fireBall.Shoott());
        }
    }

    public override void OnPrefabCreatedAutomatically(GameObject prefab)
    {
        // Find and store reference to the fireball prefab
        firePrefab = Resources.FindObjectsOfTypeAll<FireballController>()[1];
    }
}