using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Network;
using System.Collections;
using UnityEngine;

namespace MoreSpells.Spells.Resurrection;

public class ResurrectionLogic : SpellLogic
{
    /// <summary>
    /// Provides read-only access to the list of currently resurrecting players.
    /// This allows other mods to check which players are currently being resurrected
    /// without being able to modify the internal list directly.
    /// </summary>
    public static PlayerMovement[] ResurrectingPlayers => resurrectingPlayers.ToArray();

    // List to keep track of players currently being resurrected to prevent multiple resurrections
    private static readonly List<PlayerMovement> resurrectingPlayers = [];

    // The target player to be resurrected
    private PlayerMovement? Target;

    public override void CastSpell(GameObject playerObj, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        var caster = playerObj.GetComponent<PlayerMovement>();

        // Prevent self-resurrection and ensure there's a valid target
        if (Target == null || Target == caster)
        {
            DisposeSpell();
            return;
        }

        // Despawn the spell page if this is the host
        if (page.NetworkManager.IsHostStarted)
        {
            page.Despawn();
        }

        // Start the resurrection coroutine
        StartCoroutine(CoResurrection(Target));
    }

    private IEnumerator CoResurrection(PlayerMovement player)
    {
        // Add player to resurrection list
        resurrectingPlayers.Add(player);

        // Clean up death UI if this is the local player
        if (player.IsOwner && !player.prm.gameover)
        {
            player.prm.StopAllCoroutines();
            player.prm.covershite.gameObject.SetActive(false);
            player.prm.youdied.enabled = false;
            foreach (var num in player.prm.digit1)
            {
                num.SetActive(false);
            }
        }

        // Play resurrection sound
        player.portalSource.PlayOneShot(player.recall);

        // Rising animation phase
        float riseTime = 0f;
        float riseDuration = 2.5f;

        while (riseTime < riseDuration)
        {
            // Abort if player is no longer dead or game is over
            if (!player.isDead || player.prm.gameover)
            {
                EndSpell();
                yield break;
            }

            // Animate the resurrection effect
            float t = riseTime / riseDuration;
            player.recallmesh.material.SetVector("_tiling", new(1f, Mathf.Lerp(-1.5f, -1.05f, t)));
            riseTime += Time.deltaTime;
            yield return null;
        }

        player.recallmesh.material.SetVector("_tiling", new(1f, -1.05f));

        // Resurrect the player after rise animation completes
        if (player.IsOwner)
        {
            player.RespawnPlayer();
            player.EnableCols();
            player.vbt.IsMuted = false;
            player.canRecall = false;
            player.canMove = false;
            player.canJump = false;
        }
        yield return new WaitForSeconds(0.5f);

        // Falling animation phase
        float fallTime = 0f;
        float fallDuration = 1f;

        while (fallTime < fallDuration)
        {
            // Abort if player dies again during resurrection
            if (player.isDead || player.prm.gameover)
            {
                EndSpell();
                yield break;
            }

            // Animate the falling effect
            float t = fallTime / fallDuration;
            player.recallmesh.material.SetVector("_tiling", new(1f, Mathf.Lerp(-1.05f, -1.5f, t)));
            fallTime += Time.deltaTime;
            yield return null;
        }

        // Restore player controls after animation completes
        if (player.IsOwner)
        {
            player.canRecall = true;
            player.canMove = true;
            player.canJump = true;
        }

        EndSpell();

        // Helper method to clean up the resurrection effects
        void EndSpell()
        {
            // Remove player from resurrection list
            resurrectingPlayers.Remove(player);

            player.recallmesh.material.SetVector("_tiling", new(1f, -1.5f));
            player.portalSource.Stop();
            DisposeSpell();
        }
    }

    private PlayerMovement? TryGetTarget(PlayerMovement caster, Vector3 spawnPos, Vector3 viewDirectionVector)
    {
        // Get all player objects in the scene
        PlayerMovement[] allPlayers = GameObject.FindGameObjectsWithTag("Player").Select(player => player.GetComponent<PlayerMovement>()).ToArray();
        PlayerMovement? closestPlayer = null;
        float closestDistance = float.MaxValue;

        // Configuration for target selection
        float maxAngle = 40f;  // Maximum angle from view direction
        float maxDistance = 5f; // Maximum distance for valid target

        Vector3 viewDirection = viewDirectionVector.normalized;

        // Check each potential target
        foreach (PlayerMovement player in allPlayers)
        {
            // Skip invalid targets
            if (player == null) continue;
            if (player == caster || player.playerTeam != caster.playerTeam || !player.isDead || resurrectingPlayers.Contains(player)) continue;

            Vector3 directionToPlayer = (player.transform.position - spawnPos).normalized;
            float distanceToPlayer = Vector3.Distance(spawnPos, player.transform.position);

            // Skip if too far away
            if (distanceToPlayer > maxDistance) continue;

            float angleToPlayer = Vector3.Angle(viewDirection, directionToPlayer);

            // Check if player is within view cone
            if (angleToPlayer <= maxAngle)
            {
                // Check for line of sight
                if (Physics.Raycast(spawnPos, directionToPlayer, out RaycastHit hit, distanceToPlayer))
                {
                    if (hit.collider.gameObject != player.gameObject)
                    {
                        continue;
                    }
                }

                // Track closest valid target
                if (distanceToPlayer < closestDistance)
                {
                    closestDistance = distanceToPlayer;
                    closestPlayer = player;
                }
            }
        }

        return closestPlayer;
    }

    public override void WriteData(DataWriter dataWriter, PageController page, GameObject playerObj, Vector3 spawnPos, Vector3 viewDirectionVector, int level)
    {
        var target = TryGetTarget(playerObj.GetComponent<PlayerMovement>(), spawnPos, viewDirectionVector);
        if (target != null)
        {
            dataWriter.Write(0);  // Success code
            dataWriter.Write(target.gameObject);

            // Hide spell page if success, host will despawn item later in CastSpell
            var inv = playerObj.GetComponent<PlayerInventory>();
            if (inv.GetEquippedItemID() == page.ItemID)
            {
                // hide page on client while waiting for host to despawn
                inv.Drop();
                page.gameObject.SetActive(false);
            }
        }
        else
        {
            dataWriter.Write(1);  // Failure code
        }
    }

    public override void SyncData(object[] values)
    {
        var result = (int)values[0];
        if (result == 0)  // Success case
        {
            Target = ((GameObject)values[1]).GetComponent<PlayerMovement>();
        }
        else  // Failure case
        {
            Target = null;
        }
    }
}