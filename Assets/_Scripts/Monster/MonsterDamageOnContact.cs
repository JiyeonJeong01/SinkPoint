using UnityEngine;

/// <summary>
/// 몬스터가 몸으로 부딪혔을 때 플레이어 피해 시스템으로 이어질 임시 접촉 공격 컴포넌트입니다.
/// 아직 PlayerHealth가 없으므로 지금은 쿨타임과 충돌 감지만 보관합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterDamageOnContact : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool showDebugLog;

    private float nextDamageTime;
    private Transform playerRoot;

    public int Damage => damage;

    private void Start()
    {
        ResolvePlayerRoot();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    /// <summary>
    /// 접촉한 Collider가 플레이어인지 확인하고, 쿨타임이 끝났으면 공격 이벤트 자리에 진입합니다.
    /// </summary>
    private void TryDamage(Collider other)
    {
        if (other == null || Time.time < nextDamageTime)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        nextDamageTime = Time.time + cooldown;

        // PlayerHealth가 생기면 여기서 ApplyDamage 같은 명시적인 API를 호출합니다.
        if (showDebugLog)
        {
            Debug.Log($"[MonsterDamageOnContact] Hit player for {damage} damage.", this);
        }
    }

    /// <summary>
    /// 씬에 Player 태그 오브젝트가 있으면 접촉 판정용 루트로 캐시합니다.
    /// 플레이어 자식 Collider가 닿는 구조도 처리하기 위한 보조 참조입니다.
    /// </summary>
    private void ResolvePlayerRoot()
    {
        if (playerRoot != null || string.IsNullOrWhiteSpace(playerTag))
        {
            return;
        }

        Transform player = FindPlayerRoot();
        if (player != null)
        {
            playerRoot = player;
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return false;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        ResolvePlayerRoot();
        return playerRoot != null && other.transform.IsChildOf(playerRoot);
    }

    private Transform FindPlayerRoot()
    {
        if (!string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }
        }

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        return playerInput != null ? playerInput.transform : null;
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        cooldown = Mathf.Max(0f, cooldown);
    }
}
