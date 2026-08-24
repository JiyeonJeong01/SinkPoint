using UnityEngine;

/// <summary>
/// 플레이어 리스폰 위치와 물리 상태 초기화를 담당하는 컨트롤러입니다.
/// GameFlowManager가 선택한 리스폰 위치를 넘겨주면, 이 클래스는 플레이어를 그 위치로 이동시키는 일만 맡습니다.
/// </summary>
public class RespawnController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("리스폰시킬 플레이어 루트 Transform입니다.")]
    [SerializeField] private Transform playerRoot;

    [Tooltip("플레이어 Rigidbody입니다. 비워두면 playerRoot에서 자동으로 찾습니다.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private void Awake()
    {
        if (playerRigidbody == null && playerRoot != null)
        {
            playerRigidbody = playerRoot.GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// GameFlowManager가 넘겨준 위치로 플레이어를 이동시키는 핵심 리스폰 함수입니다.
    /// Rigidbody 속도를 먼저 비워서 사망 직전 낙하/넉백 속도가 리스폰 후 이어지지 않게 합니다.
    /// </summary>
    public void RespawnPlayer(Transform respawnPoint)
    {
        if (playerRoot == null || respawnPoint == null)
        {
            Debug.LogWarning("[RespawnController] Cannot respawn. Player or respawn point is missing.", this);
            return;
        }

        ClearPlayerVelocity();

        SetPlayerPose(respawnPoint);

        if (showDebugLog)
        {
            Debug.Log($"[RespawnController] Player respawned at {respawnPoint.name}.", respawnPoint);
        }
    }

    private void ClearPlayerVelocity()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void SetPlayerPose(Transform respawnPoint)
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.position = respawnPoint.position;
            playerRigidbody.rotation = respawnPoint.rotation;
            playerRigidbody.WakeUp();
        }

        playerRoot.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );
    }
}
