using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PlayerController playerController;

    [Header("Clips")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0f, 1f)] private float minimumBlendWeight = 0.01f;
    [SerializeField, Min(0f)] private float minimumInterval = 0.05f;

    private int lastClipIndex = -1;
    private float nextAllowedTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    // Animation Event receiver. Add this event to each foot-contact frame.
    public void PlayFootstep(AnimationEvent animationEvent)
    {
        if (playerController == null
            || !playerController.HasMoveIntent
            || audioSource == null
            || footstepClips == null
            || footstepClips.Length == 0
            || !IsContributingClip(animationEvent)
            || Time.time < nextAllowedTime)
        {
            return;
        }

        AudioClip clip = GetNextClip();
        if (clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
        nextAllowedTime = Time.time + minimumInterval;
    }

    private void ResolveReferences()
    {
        audioSource ??= GetComponentInParent<AudioSource>();
        playerController ??= GetComponentInParent<PlayerController>();
    }

    private bool IsContributingClip(AnimationEvent animationEvent)
    {
        return animationEvent != null
            && animationEvent.animatorClipInfo.clip != null
            && animationEvent.animatorClipInfo.weight > minimumBlendWeight;
    }

    private AudioClip GetNextClip()
    {
        if (footstepClips.Length == 1)
        {
            lastClipIndex = 0;
            return footstepClips[0];
        }

        int clipIndex;
        do
        {
            clipIndex = Random.Range(0, footstepClips.Length);
        }
        while (clipIndex == lastClipIndex);

        lastClipIndex = clipIndex;
        return footstepClips[clipIndex];
    }
}
