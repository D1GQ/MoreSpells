using BlackMagicAPI.Modules.Spells;
using System.Collections;
using UnityEngine;

namespace MoreSpells.Spells.EchoLocation;

public class EchoLocationLogic : SpellLogic
{
    // Cache for the shader that will be used to highlight players
    private static Shader? shaderPefab;
    // Duration of the spell effect
    private float duration = 10f;

    // Main spell casting method
    public override void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        // Get the PlayerMovement component of the caster
        var owner = playerObj.GetComponent<PlayerMovement>();
        // Only execute if this is the local player
        if (!owner.IsOwner) return;

        // Find all player objects and get their PlayerMovement components
        var players = GameObject.FindGameObjectsWithTag("Player").Select(obj => obj.GetComponent<PlayerMovement>()).ToArray();
        // Start the coroutine that handles the spell effect, filtering out the caster
        StartCoroutine(CoCastSpell(owner, players.Where(p => p != owner).ToArray(), castingLevel));
    }

    // Coroutine that manages the spell's duration and periodic reveals
    private IEnumerator CoCastSpell(PlayerMovement owner, PlayerMovement[] players, int castingLevel)
    {
        float reveal = 0f; // Timer for periodic reveals

        // Main spell duration loop
        while (duration > 0 && owner != null && !owner.isDead)
        {
            // Time to reveal nearby players again
            if (reveal <= 0)
            {
                foreach (var player in players)
                {
                    if (player == null) continue;
                    // Check if player is within range (range increases with casting level)
                    if (Vector3.Distance(owner.transform.position, player.transform.position) < (15f + (2 * (castingLevel - 1))))
                    {
                        // Start revealing this player's location
                        StartCoroutine(CoRevealLocation(player, owner.playerTeam, castingLevel));
                    }
                }
                reveal = 2f; // Reset reveal timer
            }

            // Update timers
            duration -= Time.deltaTime;
            reveal -= Time.deltaTime;
            yield return null;
        }

        // Wait for any active reveals to finish before disposing
        while (busyWithReveal > 0)
        {
            yield return null;
        }

        // Clean up the spell
        DisposeSpell();
    }

    // Counter for active reveal effects
    private int busyWithReveal;

    // Coroutine that handles revealing a single player's location
    private IEnumerator CoRevealLocation(PlayerMovement player, int team, int castingLevel)
    {
        busyWithReveal++; // Increment active reveal counter

        // Create a duplicate of the player's wizard model
        var dupe = Instantiate(player.transform.Find("wizardtrio").gameObject);
        dupe.transform.position = player.transform.position;
        dupe.transform.rotation = player.transform.rotation;

        // Get all materials from the duplicate model
        Material[] playerMaterials = dupe.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(smr => smr.materials).ToArray() ?? Array.Empty<Material>();

        // Apply the X-ray shader to all materials
        foreach (var mat in playerMaterials)
        {
            if (mat == null) continue;
            mat.shader = shaderPefab;
        };

        // Calculate reveal duration (increases with casting level)
        var time = 2f + (0.5f * (castingLevel - 1));

        // Reveal effect loop
        while (time > 0)
        {
            time -= Time.deltaTime;

            // Update shader properties for each material
            foreach (var mat in playerMaterials)
            {
                if (mat == null) continue;
                // Set color based on team (cyan for allies, red for enemies)
                var color = player.playerTeam == team ? Color.cyan : Color.red;
                mat.SetColor("_XRayColor", color);
                // Fade out the effect over time
                mat.SetFloat("_XRayIntensity", time * 0.5f);
            };

            yield return null;
        }

        // Clean up the duplicate
        Destroy(dupe);
        busyWithReveal--; // Decrement active reveal counter
    }

    // Called when the spell prefab is created to load required assets
    public override void OnPrefabCreatedAutomatically(GameObject prefab)
    {
        // Load the X-ray shader from the spell assets
        shaderPefab = MSPlugin.SpellsAssets?.LoadAsset<Shader>("Assets/SpellAssets/Spells/XRayShader.shader");
    }
}