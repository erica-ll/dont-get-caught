using System;
using UnityEngine;

namespace DontGetCaught.Core
{
    /// <summary>
    /// Inspector-editable tuning values for one game round.
    /// This is a serializable value object stored by GameManager, not a scene component.
    /// </summary>
    [Serializable]
    public sealed class GameConfig
    {
        [Header("Round")]
        [Min(1f)] public float roundDurationSeconds = 180f;
        [Min(1)] public int startingLives = 3;
        [Min(0)] public int pointsPerCompletedNoodle = 300;

        [Header("Noodle Input")]
        [Min(0f)] public float airflowSmoothingGraceSeconds = 0.08f;

        [Header("Teacher and Recovery")]
        [Min(0.1f)] public float teacherFacingDurationSeconds = 2f;
        [Min(0.1f)] public float caughtRecoveryTimeoutSeconds = 3f;

        [Header("Testing")]
        [Min(1f)] public float testRoundDurationSeconds = 30f;
    }
}
