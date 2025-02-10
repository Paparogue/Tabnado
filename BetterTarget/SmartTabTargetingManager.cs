using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using BetterTarget.UI;

namespace BetterTarget
{
    public class SmartTabTargetingManager
    {
        private IClientState clientState;
        private IObjectTable objectTable;
        private ITargetManager targetManager;
        private IChatGui chatGui;
        private PluginConfiguration config;

        // For target cycling.
        private DateTime lastCycleTime = DateTime.MinValue;
        private int lastCycleIndex = 0;

        /// <summary>
        /// Stores the list of candidates from the last targeting cycle.
        /// Useful for debugging.
        /// </summary>
        public List<Candidate> LastCandidateList { get; private set; } = new List<Candidate>();

        public SmartTabTargetingManager(IClientState clientState, IObjectTable objectTable, ITargetManager targetManager, IChatGui chatGui, PluginConfiguration config)
        {
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.targetManager = targetManager;
            this.chatGui = chatGui;
            this.config = config;
        }

        /// <summary>
        /// Returns the best candidate target based on various factors.
        /// </summary>
        public Candidate? GetSmartTabCandidate()
        {
            var localPlayer = clientState.LocalPlayer;
            if (localPlayer == null)
            {
                chatGui.Print("Local player not found.");
                return null;
            }

            // Get camera data (using the player's rotation as a proxy).
            CameraData camData = GetCameraData(localPlayer);
            float maxDistance = config.MaxTargetDistance;
            List<Candidate> candidates = new List<Candidate>();

            // Iterate over potential targets from the ObjectTable.
            foreach (var obj in objectTable)
            {
                // Filter: only consider objects whose ObjectKind indicates an enemy (BattleNpc).
                if (obj.ObjectKind != ObjectKind.BattleNpc)
                    continue;

                // Check if the object is targetable.
                if (!obj.IsTargetable)
                    continue;

                // (Optionally, you might wish to check for "dead" NPCs here if you have access to HP data.)

                float distance = Vector3.Distance(localPlayer.Position, obj.Position);
                if (distance > maxDistance)
                    continue;

                // --- Line-of-Sight Check (placeholder) ---
                if (!HasLineOfSight(obj))
                    continue;

                // --- Distance Factor ---
                float distanceScore = 1f / (distance + 0.1f);

                // --- Camera Alignment Factor ---
                Vector3 directionToObj = Vector3.Normalize(obj.Position - camData.Position);
                float dot = Vector3.Dot(camData.Forward, directionToObj);
                float clampedDot = Clamp(dot, -1f, 1f);
                float angleDegrees = MathF.Acos(clampedDot) * (180f / MathF.PI);
                float alignmentScore = dot;
                if (angleDegrees > camData.FieldOfView / 2f)
                    alignmentScore *= 0.5f;

                // --- Base Composite Score ---
                float baseScore = (config.DistanceWeight * distanceScore) + (config.AlignmentWeight * alignmentScore);

                // --- Additional Multipliers ---
                float aggroMultiplier = IsAggro(obj) ? (1f + config.AggroWeight) : 1f;
                float typeMultiplier = GetTypeMultiplier(obj);
                float compositeScore = baseScore * aggroMultiplier * typeMultiplier;

                Candidate candidate = new Candidate
                {
                    Target = obj,
                    Score = compositeScore,
                    DistanceScore = distanceScore,
                    AlignmentScore = alignmentScore,
                    Distance = distance,
                    AngleDegrees = angleDegrees
                };

                candidates.Add(candidate);
            }

            // Exclude the current target if more than one candidate exists.
            if (targetManager.Target != null && candidates.Count > 1)
                candidates = candidates.Where(c => c.Target != targetManager.Target).ToList();

            if (candidates.Count == 0)
                return null;

            // Sort candidates by composite score (highest first).
            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            LastCandidateList = candidates; // Store for debug display.

            // --- Target Cycling ---
            Candidate selectedCandidate = candidates[0];
            if (config.EnableTargetCycling)
            {
                if ((DateTime.Now - lastCycleTime).TotalSeconds > config.CycleTimeout)
                    lastCycleIndex = 0;
                else
                    lastCycleIndex++;

                lastCycleTime = DateTime.Now;
                selectedCandidate = candidates[lastCycleIndex % candidates.Count];
            }

            return selectedCandidate;
        }

        /// <summary>
        /// Retrieves camera data using the player's position and rotation.
        /// </summary>
        private CameraData GetCameraData(IGameObject localPlayer)
        {
            // Since localPlayer.Rotation is now a float (yaw in radians), we compute the forward vector manually.
            return new CameraData
            {
                Position = localPlayer.Position,
                Forward = new Vector3(MathF.Sin(localPlayer.Rotation), 0, MathF.Cos(localPlayer.Rotation)),
                FieldOfView = config.OverrideFieldOfView
            };
        }

        /// <summary>
        /// Placeholder for a proper line-of-sight check.
        /// Replace with raycasting/occlusion logic as needed.
        /// </summary>
        private bool HasLineOfSight(IGameObject obj)
        {
            // TODO: Implement proper line-of-sight logic.
            return true;
        }

        /// <summary>
        /// Placeholder for aggro detection.
        /// Returns true if the object is actively targeting the player.
        /// </summary>
        private bool IsAggro(IGameObject obj)
        {
            // TODO: Implement actual aggro detection.
            return false;
        }

        /// <summary>
        /// Placeholder for adjusting scores based on target type.
        /// For example, bosses or elite mobs might have a different multiplier.
        /// </summary>
        private float GetTypeMultiplier(IGameObject obj)
        {
            // TODO: Adjust multiplier based on NPC type.
            return 1f;
        }

        /// <summary>
        /// Helper: Clamp value between min and max.
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        /// <summary>
        /// Structure to hold camera data.
        /// </summary>
        private struct CameraData
        {
            public Vector3 Position;
            public Vector3 Forward;
            public float FieldOfView;
        }
    }

    /// <summary>
    /// Container for candidate target data.
    /// </summary>
    public class Candidate
    {
        public IGameObject Target { get; set; } = null!;
        public float Score { get; set; }
        public float DistanceScore { get; set; }
        public float AlignmentScore { get; set; }
        public float Distance { get; set; }
        public float AngleDegrees { get; set; }
    }
}
