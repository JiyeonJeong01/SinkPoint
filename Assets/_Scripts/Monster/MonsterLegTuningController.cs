using System.Collections.Generic;
using DistantLands;
using UnityEngine;

/// <summary>
/// Collects every Kaiju Leg under one monster and applies shared tuning values so many legs can be adjusted together.
/// </summary>
[DisallowMultipleComponent]
public class MonsterLegTuningController : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] private Transform searchRoot;
    [SerializeField] private bool includeInactiveLegs = true;
    [SerializeField] private bool collectOnAwake = true;

    [Header("Runtime Apply")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool liveApplyInPlayMode = true;

    [Header("Legs")]
    [SerializeField] private List<Leg> legs = new List<Leg>();

    [Header("Movement")]
    [SerializeField] private bool overrideSpeed;
    [SerializeField] private float speed = 5f;
    [SerializeField] private bool overrideOffsetByVelocity;
    [SerializeField] private float offsetByVelocity = 0.1f;

    [Header("Ground Probe")]
    [SerializeField] private bool overrideMaxRayDistance;
    [SerializeField] private float maxRayDistance = 10f;
    [SerializeField] private bool overrideGroundOffset;
    [SerializeField] private float groundOffset = 0.1f;

    [Header("Step Shape")]
    [SerializeField] private bool overrideMaxDistance;
    [SerializeField] private float maxDistance = 1f;
    [SerializeField] private bool overrideGroundSnap;
    [SerializeField] private float groundSnap = 0.75f;
    [SerializeField] private bool overrideFailDistance;
    [SerializeField] private float failDistance = 2f;
    [SerializeField] private bool overrideLegLift;
    [SerializeField] private float legLift = 0.25f;
    [SerializeField] private bool overrideDistanceToLiftLeg;
    [SerializeField] private float distanceToLiftLeg = 0.5f;

    [Header("Coordination")]
    [SerializeField] private bool overrideMinLegsGrounded;
    [SerializeField] private int minLegsGrounded = 0;
    [SerializeField] private bool overridePaused;
    [SerializeField] private bool paused;

    public IReadOnlyList<Leg> Legs => legs;

    private void Reset()
    {
        CollectLegs();
        ReadFromFirstLeg();
    }

    private void Awake()
    {
        if (collectOnAwake)
            CollectLegs();
    }

    private void Start()
    {
        if (applyOnStart)
            ApplyLegSettings();
    }

    private void OnValidate()
    {
        // During Play Mode, changing this controller in the Inspector should immediately affect every collected leg.
        if (Application.isPlaying && liveApplyInPlayMode)
            ApplyLegSettings();
    }

    /// <summary>
    /// Finds every Leg under searchRoot, or this monster object when searchRoot is empty.
    /// </summary>
    [ContextMenu("Collect Legs")]
    public void CollectLegs()
    {
        Transform root = searchRoot != null ? searchRoot : transform;
        legs.Clear();
        legs.AddRange(root.GetComponentsInChildren<Leg>(includeInactiveLegs));
    }

    /// <summary>
    /// Applies the enabled override values to every collected Leg.
    /// </summary>
    [ContextMenu("Apply Leg Settings")]
    public void ApplyLegSettings()
    {
        for (int i = legs.Count - 1; i >= 0; i--)
        {
            Leg leg = legs[i];
            if (leg == null)
            {
                legs.RemoveAt(i);
                continue;
            }

            ApplyToLeg(leg);
        }
    }

    /// <summary>
    /// Rebuilds the leg list first, then applies the current tuning values.
    /// </summary>
    [ContextMenu("Collect And Apply Legs")]
    public void CollectAndApplyLegs()
    {
        CollectLegs();
        ApplyLegSettings();
    }

    /// <summary>
    /// Copies the first collected Leg values into this controller as a convenient starting point.
    /// </summary>
    [ContextMenu("Read From First Leg")]
    public void ReadFromFirstLeg()
    {
        if (legs.Count == 0)
            CollectLegs();

        Leg source = null;
        for (int i = 0; i < legs.Count; i++)
        {
            if (legs[i] != null)
            {
                source = legs[i];
                break;
            }
        }

        if (source == null)
            return;

        speed = source.speed;
        offsetByVelocity = source.offsetByVelocity;
        maxRayDistance = source.maxRayDistance;
        groundOffset = source.groundOffset;
        maxDistance = source.maxDistance;
        groundSnap = source.groundSnap;
        failDistance = source.failDistance;
        legLift = source.legLift;
        distanceToLiftLeg = source.distanceToLiftLeg;
        minLegsGrounded = source.minLegsGrounded;
        paused = source.paused;
    }

    private void ApplyToLeg(Leg leg)
    {
        if (overrideSpeed) leg.speed = speed;
        if (overrideOffsetByVelocity) leg.offsetByVelocity = offsetByVelocity;
        if (overrideMaxRayDistance) leg.maxRayDistance = maxRayDistance;
        if (overrideGroundOffset) leg.groundOffset = groundOffset;
        if (overrideMaxDistance) leg.maxDistance = maxDistance;
        if (overrideGroundSnap) leg.groundSnap = groundSnap;
        if (overrideFailDistance) leg.failDistance = failDistance;
        if (overrideLegLift) leg.legLift = legLift;
        if (overrideDistanceToLiftLeg) leg.distanceToLiftLeg = distanceToLiftLeg;
        if (overrideMinLegsGrounded) leg.minLegsGrounded = minLegsGrounded;
        if (overridePaused) leg.paused = paused;
    }
}
