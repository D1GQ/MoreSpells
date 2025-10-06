using BlackMagicAPI.Helpers;
using BlackMagicAPI.Modules.Spells;
using BlackMagicAPI.Network;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MoreSpells.Spells.TheEyeOfHell;

internal class TheEyeOfHellLogic : SpellLogic
{
    private static bool active; // Tracks if this spell is currently active (static so only one instance can be active at a time)
    private static AudioClip? clip; // Audio clip for the spell effect (static to load once and share across instances)

    public override bool CastSpell(PlayerMovement caster, PageController page, Vector3 spawnPos, Vector3 viewDirectionVector, int castingLevel)
    {
        // Find the NightAudioSwap component which manages day/night cycle audio
        var nightAudioSwap = FindFirstObjectByType<NightAudioSwap>();

        if (nightAudioSwap != null)
        {
            // Get the player's inventory to check ownership and manage items
            var inv = caster.GetComponent<PlayerInventory>();

            // If this client owns the player, allow item swapping
            if (inv.IsOwner)
            {
                inv.canSwapItem = true;
            }

            // Prevent casting if spell is already active or if it's already night time
            if (active)
            {
                DisposeSpell(); // Clean up if conditions aren't met
                return false;
            }

            // If player owns this character, destroy the hand item (spell page)
            if (inv.IsOwner)
            {
                inv.destroyHandItem();
            }

            // Get weather reference and start the visual/audio effects
            var weather = nightAudioSwap.nightday;
            PlayClip(); // Play the spell audio
            StartCoroutine(StopWeatherCycle(weather));
            StartCoroutine(CoFadeOut(weather.Sun, weather.Moon)); // Start the sun fading effect coroutine
        }

        return true;
    }

    private void PlayClip()
    {
        // Create and configure audio source for the spell sound
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f; // 2D sound (not spatialized)
        source.volume = 0.3f; // Moderate volume
        source.playOnAwake = false; // Don't play automatically
        source.loop = false; // Play once
        source.minDistance = 0.01f; // Very close minimum distance
        source.maxDistance = 1000f; // Very far maximum distance
        source.rolloffMode = AudioRolloffMode.Linear; // Linear volume dropoff
        source.pitch = 0.84f; // Slightly lowered pitch for eerie effect
        source.Play(); // Start playback
    }

    private IEnumerator StopWeatherCycle(WeatherCycle weather)
    {
        var lastRot = weather.rot;
        while (true)
        {
            weather.rot = lastRot;
            yield return null;
        }
    }

    private IEnumerator CoFadeOut(Light sun, Light moon)
    {
        active = true; // Mark spell as active
        var sunColor = sun.color; // Store original sun color
        var moonColor = moon.color; // Store original sun color
        var sunIntensity = sun.intensity; // Store original sun intensity

        float time = 3f; // Duration of fade effect (3 seconds)
        var targetColor = Color.red; // Target color (blood red)
        var targetIntensity = 3000f; // Target intensity (extremely bright)

        // Animate the sun transformation over time
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / time); // Normalized time value (0-1)

            // Interpolate sun properties toward target values
            sun.color = Color.Lerp(sunColor, targetColor, t);
            moon.color = Color.Lerp(moonColor, targetColor, t);
            sun.intensity = Mathf.Lerp(sunIntensity, targetIntensity, t);

            yield return null; // Wait until next frame
        }

        // Ensure exact target values are reached
        sun.color = targetColor;
        moon.color = targetColor;
        sun.intensity = targetIntensity;

        // Main spell duration (60 seconds of hellish effects)
        float wait = 60f;
        // Find all players in the scene to apply effects to them
        var players = GameObject.FindGameObjectsWithTag("Player").Select(obj => obj.GetComponent<PlayerMovement>()).ToArray();

        // During the 60-second duration, continuously apply effects to players
        while (wait > 0f)
        {
            wait -= Time.deltaTime;

            // Apply fire effect to all living players with sufficient health
            foreach (var player in players)
            {
                if (player == null) continue;

                // Only affect players who are alive, not already on fire, and have enough health
                if (!player.isDead && player.fireTimer < 0.25f && player.playerHealth > 15f)
                {
                    player.fireTimer = 0.25f; // Set player on fire briefly
                }
            }

            yield return null; // Wait until next frame
        }

        // After main duration, fade back to normal
        yield return CoFadeIn(sun, moon, (sunColor, moonColor, sunIntensity));
    }

    private IEnumerator CoFadeIn(Light sun, Light moon, (Color sunColor, Color moonColor, float sunIntensity) backto)
    {
        float time = 3f; // Duration of fade back effect (3 seconds)
        Color startSunColor = sun.color; // Current (red) color
        Color startMoonColor = moon.color; // Current (red) color
        float startSunIntensity = sun.intensity; // Current (high) intensity

        // Animate the sun returning to normal
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / time); // Normalized time value (0-1)

            // Interpolate back to original values
            sun.color = Color.Lerp(startSunColor, backto.sunColor, t);
            moon.color = Color.Lerp(startMoonColor, backto.moonColor, t);
            sun.intensity = Mathf.Lerp(startSunIntensity, backto.sunIntensity, t);

            yield return null; // Wait until next frame
        }

        // Ensure exact original values are restored
        sun.color = backto.sunColor;
        moon.color = backto.moonColor;
        sun.intensity = backto.sunIntensity;

        // Brief additional wait before cleaning up (3.5 seconds)
        yield return new WaitForSeconds(3.5f);
        yield return CoCooldown();
    }

    // go on cooldown period (5 minutes) before spell can be cast again!
    private IEnumerator CoCooldown()
    {
        float time = 60f * 5f;
        while (time > 0f)
        {
            time -= Time.deltaTime;
            yield return null;
        }

        active = false; // Mark spell as no longer active
        DisposeSpell(); // Clean up spell resources
    }

    public override void WriteData(DataWriter dataWriter, PageController page, PlayerMovement caster, Vector3 spawnPos, Vector3 viewDirectionVector, int level)
    {
        // Prevent item swapping during spell casting on network
        var inv = caster.GetComponent<PlayerInventory>();
        if (inv.IsOwner)
        {
            inv.canSwapItem = false;
        }
    }

    public override void OnPrefabCreatedAutomatically(GameObject prefab)
    {
        // Load the audio clip from embedded resources
        clip = Assembly.GetExecutingAssembly().LoadWavFromResources("MoreSpells.Resources.Sounds.TheEyeOfHell.wav");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopAllCoroutines();
        active = false; // Ensure active flag is reset if object is destroyed prematurely
    }
}