using UnityEngine;

/// <summary>
/// 몬스터 공통 사운드 재생을 담당합니다.
/// 이동 루프는 NavTarget의 위치 변화로 자동 판단하고, 공격/사망 사운드는 각 공격 스크립트에서 호출합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterAudioFeedback : MonoBehaviour, IMonsterDeathHandler, IMonsterResettable
{
    public enum MonsterAudioProfile
    {
        Custom,
        Centipede,
        Spider,
        SandWorm,
        Aeropod
    }

    [Header("Profile")]
    [SerializeField, Tooltip("Reset/OnValidate 때 기본 사운드 클립을 자동으로 채우는 프리셋입니다.")]
    private MonsterAudioProfile profile = MonsterAudioProfile.Custom;

    [Header("References")]
    [SerializeField, Tooltip("이동 여부를 판단할 Transform입니다. 비워두면 NavTarget/Nav Target을 찾습니다.")]
    private Transform movementTarget;
    [SerializeField, Tooltip("이동 루프 전용 AudioSource입니다. 비워두면 자동으로 추가합니다.")]
    private AudioSource movementLoopSource;
    [SerializeField, Tooltip("공격/랜덤 사운드 전용 AudioSource입니다. 비워두면 자동으로 추가합니다.")]
    private AudioSource oneShotSource;

    [Header("Clips")]
    [SerializeField, Tooltip("움직이는 동안만 반복 재생할 사운드입니다.")]
    private AudioClip movementLoopClip;
    [SerializeField, Tooltip("몸빵/근접 공격 순간에 한 번 재생할 사운드입니다.")]
    private AudioClip bodySlamClip;
    [SerializeField, Tooltip("투사체나 독침 발사 순간에 한 번 재생할 사운드입니다.")]
    private AudioClip rangedAttackClip;
    [SerializeField, Tooltip("땅 밑으로 사라질 때 한 번 재생할 사운드입니다.")]
    private AudioClip burrowClip;
    [SerializeField, Tooltip("땅 밖으로 등장하며 몸빵할 때 한 번 재생할 사운드입니다.")]
    private AudioClip emergeAttackClip;
    [SerializeField, Tooltip("추적/잠복 공격을 시작할 때 한 번 재생할 사운드입니다.")]
    private AudioClip chaseStartClip;
    [SerializeField, Tooltip("죽는 순간 한 번 재생할 사운드입니다.")]
    private AudioClip deathClip;
    [SerializeField, Tooltip("살아있는 동안 일정 주기마다 한 번 재생할 사운드입니다.")]
    private AudioClip randomAliveClip;

    [Header("Movement Loop")]
    [SerializeField, Min(0f), Tooltip("이 속도보다 빠르게 움직이면 이동 루프를 켭니다.")]
    private float movingSpeedThreshold = 0.05f;
    [SerializeField, Min(0f), Tooltip("속도가 잠깐 0에 가까워져도 이 시간 안에는 이동 루프를 끄지 않습니다.")]
    private float movementStopGraceSeconds = 0.25f;
    [SerializeField, Range(0f, 1f)]
    private float movementVolume = 0.45f;

    [Header("One Shots")]
    [SerializeField, Range(0f, 1f)]
    private float oneShotVolume = 0.8f;
    [SerializeField, Min(0f), Tooltip("몸빵/근접 공격 사운드 볼륨 배율입니다.")]
    private float bodySlamVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("투사체/독침 공격 사운드 볼륨 배율입니다.")]
    private float rangedAttackVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("땅 밑으로 사라지는 사운드 볼륨 배율입니다.")]
    private float burrowVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("등장 공격 사운드 볼륨 배율입니다.")]
    private float emergeAttackVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("추적/공격 시작 사운드 볼륨 배율입니다.")]
    private float chaseStartVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("사망 사운드 볼륨 배율입니다.")]
    private float deathVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("랜덤 울음 사운드 볼륨 배율입니다.")]
    private float randomAliveVolumeMultiplier = 1f;
    [SerializeField, Min(0f), Tooltip("랜덤 사운드 최소 대기 시간입니다.")]
    private float randomIntervalMin = 5f;
    [SerializeField, Min(0f), Tooltip("랜덤 사운드 최대 대기 시간입니다.")]
    private float randomIntervalMax = 9f;
    [SerializeField, Range(0f, 1f), Tooltip("3D 사운드 비율입니다. 1이면 거리감이 생깁니다.")]
    private float spatialBlend = 1f;
    [SerializeField, Min(0f), Tooltip("3D 사운드가 들리는 최대 거리입니다.")]
    private float maxDistance = 22f;

    [Header("Debug Readout")]
    [SerializeField, Tooltip("이동 루프가 현재 재생 중인지 표시합니다.")]
    private bool isMovementLoopPlaying;
    [SerializeField, Tooltip("movementTarget 기준 현재 이동 속도입니다.")]
    private float lastMoveSpeed;
    [SerializeField, Tooltip("다음 랜덤 사운드까지 남은 시간입니다.")]
    private float randomSoundRemaining;

    private MonsterHealth monsterHealth;
    private MonsterStateMachine stateMachine;
    private Vector3 lastMovementPosition;
    private float nextRandomSoundTime;
    private float lastMovingTime = float.NegativeInfinity;
    private bool hasLastMovementPosition;

    private void Awake()
    {
        ResolveReferences();
        ConfigureSources();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureSources();
        ResetMovementTracking();
        ScheduleNextRandomSound();
    }

    private void Reset()
    {
        ResolveReferences();
        InferProfileFromName();
        ApplyProfileDefaults();
        ConfigureSources();
    }

    private void Update()
    {
        bool dead = IsDead();
        UpdateMovementLoop(dead);
        UpdateRandomAliveSound(dead);
    }

    private void OnDisable()
    {
        StopMovementLoop();
    }

    public void ResetMonsterRuntime()
    {
        ResolveReferences();
        ConfigureSources();
        StopMovementLoop();
        ResetMovementTracking();
        ScheduleNextRandomSound();
    }

    public void OnMonsterDied()
    {
        StopMovementLoop();
        PlayAtPosition(deathClip, deathVolumeMultiplier);
    }

    public void PlayBodySlam()
    {
        PlayOneShot(bodySlamClip, bodySlamVolumeMultiplier);
    }

    public void PlayRangedAttack()
    {
        PlayOneShot(rangedAttackClip, rangedAttackVolumeMultiplier);
    }

    public void PlayBurrow()
    {
        PlayOneShot(burrowClip, burrowVolumeMultiplier);
    }

    public void PlayEmergeAttack()
    {
        PlayOneShot(emergeAttackClip, emergeAttackVolumeMultiplier);
    }

    public void PlayChaseStart()
    {
        PlayOneShot(chaseStartClip, chaseStartVolumeMultiplier);
    }

    public void PlayRandomAlive()
    {
        PlayOneShot(randomAliveClip, randomAliveVolumeMultiplier);
    }

    private void UpdateMovementLoop(bool dead)
    {
        if (dead || movementTarget == null || movementLoopClip == null || Time.deltaTime <= 0f)
        {
            lastMoveSpeed = 0f;
            StopMovementLoop();
            return;
        }

        Vector3 currentPosition = movementTarget.position;
        if (!hasLastMovementPosition)
        {
            lastMovementPosition = currentPosition;
            hasLastMovementPosition = true;
            return;
        }

        lastMoveSpeed = (currentPosition - lastMovementPosition).magnitude / Time.deltaTime;
        lastMovementPosition = currentPosition;

        if (lastMoveSpeed >= movingSpeedThreshold)
        {
            lastMovingTime = Time.time;
            PlayMovementLoop();
        }
        else if (Time.time - lastMovingTime > movementStopGraceSeconds)
        {
            StopMovementLoop();
        }
    }

    private void UpdateRandomAliveSound(bool dead)
    {
        if (dead || randomAliveClip == null)
        {
            randomSoundRemaining = 0f;
            return;
        }

        randomSoundRemaining = Mathf.Max(0f, nextRandomSoundTime - Time.time);
        if (Time.time < nextRandomSoundTime)
        {
            return;
        }

        PlayRandomAlive();
        ScheduleNextRandomSound();
    }

    private void PlayMovementLoop()
    {
        if (movementLoopSource == null || movementLoopSource.isPlaying)
        {
            isMovementLoopPlaying = movementLoopSource != null && movementLoopSource.isPlaying;
            return;
        }

        movementLoopSource.clip = movementLoopClip;
        movementLoopSource.volume = movementVolume;
        movementLoopSource.loop = true;
        movementLoopSource.Play();
        isMovementLoopPlaying = true;
    }

    private void StopMovementLoop()
    {
        if (movementLoopSource != null && movementLoopSource.isPlaying)
        {
            movementLoopSource.Stop();
        }

        isMovementLoopPlaying = false;
    }

    private void PlayOneShot(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null)
        {
            return;
        }

        ResolveReferences();
        ConfigureSources();
        if (oneShotSource != null)
        {
            oneShotSource.PlayOneShot(clip, oneShotVolume * volumeMultiplier);
        }
    }

    private void PlayAtPosition(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position, oneShotVolume * volumeMultiplier);
    }

    private void ScheduleNextRandomSound()
    {
        if (randomAliveClip == null)
        {
            nextRandomSoundTime = float.PositiveInfinity;
            randomSoundRemaining = 0f;
            return;
        }

        float min = Mathf.Min(randomIntervalMin, randomIntervalMax);
        float max = Mathf.Max(randomIntervalMin, randomIntervalMax);
        if (max <= 0f)
        {
            nextRandomSoundTime = float.PositiveInfinity;
            randomSoundRemaining = 0f;
            return;
        }

        nextRandomSoundTime = Time.time + Random.Range(min, max);
        randomSoundRemaining = nextRandomSoundTime - Time.time;
    }

    private void ResetMovementTracking()
    {
        hasLastMovementPosition = movementTarget != null;
        lastMovementPosition = movementTarget != null ? movementTarget.position : transform.position;
        lastMovingTime = float.NegativeInfinity;
        lastMoveSpeed = 0f;
    }

    private bool IsDead()
    {
        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return true;
        }

        return stateMachine != null && stateMachine.State == MonsterState.Dead;
    }

    private void ResolveReferences()
    {
        movementTarget ??= FindChildRecursive(transform, "NavTarget");
        movementTarget ??= FindChildRecursive(transform, "Nav Target");
        movementTarget ??= transform;

        monsterHealth ??= GetComponent<MonsterHealth>();
        monsterHealth ??= GetComponentInParent<MonsterHealth>();
        monsterHealth ??= GetComponentInChildren<MonsterHealth>(true);

        stateMachine ??= GetComponent<MonsterStateMachine>();
        stateMachine ??= GetComponentInParent<MonsterStateMachine>();
        stateMachine ??= GetComponentInChildren<MonsterStateMachine>(true);

        if (movementLoopSource == null)
        {
            movementLoopSource = GetComponent<AudioSource>();
            if (movementLoopSource == null)
            {
                movementLoopSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (oneShotSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            oneShotSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureSources()
    {
        ConfigureSource(movementLoopSource, true);
        ConfigureSource(oneShotSource, false);
    }

    private void ConfigureSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = maxDistance;
    }

    private void InferProfileFromName()
    {
        string lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("centipede"))
        {
            profile = MonsterAudioProfile.Centipede;
        }
        else if (lowerName.Contains("spider"))
        {
            profile = MonsterAudioProfile.Spider;
        }
        else if (lowerName.Contains("sandworm") || lowerName.Contains("sand worm"))
        {
            profile = MonsterAudioProfile.SandWorm;
        }
        else if (lowerName.Contains("aeropod"))
        {
            profile = MonsterAudioProfile.Aeropod;
        }
    }

    private void ApplyProfileDefaults()
    {
#if UNITY_EDITOR
        switch (profile)
        {
            case MonsterAudioProfile.Centipede:
                movementLoopClip = LoadClip("Assets/Audios/Enemies/Zombie/Steps/Zombie_Steps_04.wav");
                bodySlamClip = LoadClip("Assets/Audios/Blood&Gore/Gore/Gore_Punch_4.wav");
                deathClip = LoadClip("Assets/Sounds/MonsterDead.mp3");
                randomAliveClip = LoadClip("Assets/Sounds/CentipedeRandom.mp3");
                break;
            case MonsterAudioProfile.Spider:
                movementLoopClip = LoadClip("Assets/Sounds/SpiderWalk.mp3");
                bodySlamClip = LoadClip("Assets/Sounds/SpiderAttack.mp3");
                rangedAttackClip = LoadClip("Assets/Sounds/SpiderAttack.mp3");
                randomAliveClip = LoadClip("Assets/Backrooms Entity SFX/juanjo_sound - Backrooms Entity 2.wav");
                deathClip = LoadClip("Assets/Sounds/MonsterDead.mp3");
                break;
            case MonsterAudioProfile.SandWorm:
                chaseStartClip = LoadClip("Assets/Audios/Enemies/Monster/Voice/Monster_Attack_1.wav");
                emergeAttackClip = LoadClip("Assets/Audios/Enemies/Monster/Steps/Monster_Steps_1.wav");
                burrowClip = LoadClip("Assets/Audios/Enemies/Monster/Steps/Monster_Steps_3.wav");
                deathClip = LoadClip("Assets/Sounds/MonsterDead.mp3");
                break;
            case MonsterAudioProfile.Aeropod:
                randomAliveClip = LoadClip("Assets/Sounds/AeropodRandom.mp3");
                rangedAttackClip = LoadClip("Assets/Audios/Blood&Gore/Gore/Gore_Punch_4.wav");
                deathClip = LoadClip("Assets/Audios/Enemies/Monster/Voice/Monster_Efforts_4.wav");
                break;
        }
#endif
    }

#if UNITY_EDITOR
    private static AudioClip LoadClip(string assetPath)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
    }
#endif

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        movingSpeedThreshold = Mathf.Max(0f, movingSpeedThreshold);
        movementStopGraceSeconds = Mathf.Max(0f, movementStopGraceSeconds);
        bodySlamVolumeMultiplier = Mathf.Max(0f, bodySlamVolumeMultiplier);
        rangedAttackVolumeMultiplier = Mathf.Max(0f, rangedAttackVolumeMultiplier);
        burrowVolumeMultiplier = Mathf.Max(0f, burrowVolumeMultiplier);
        emergeAttackVolumeMultiplier = Mathf.Max(0f, emergeAttackVolumeMultiplier);
        chaseStartVolumeMultiplier = Mathf.Max(0f, chaseStartVolumeMultiplier);
        deathVolumeMultiplier = Mathf.Max(0f, deathVolumeMultiplier);
        randomAliveVolumeMultiplier = Mathf.Max(0f, randomAliveVolumeMultiplier);
        randomIntervalMin = Mathf.Max(0f, randomIntervalMin);
        randomIntervalMax = Mathf.Max(0f, randomIntervalMax);
        maxDistance = Mathf.Max(0f, maxDistance);
        ApplyProfileDefaults();
    }
}
