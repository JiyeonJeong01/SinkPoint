using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioEnvironmentController))]
public sealed class AudioSystemSceneBindings : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField, Tooltip("This scene's GameFlowManager. The AudioSystem prefab does not store a scene object reference.")]
    private GameFlowManager gameFlowManager;

    [SerializeField, Tooltip("This scene Player root's AudioReverbFilter. The AudioSystem prefab does not store a scene object reference.")]
    private AudioReverbFilter playerReverbFilter;

    private AudioEnvironmentController audioEnvironmentController;

    private void Awake()
    {
        audioEnvironmentController = GetComponent<AudioEnvironmentController>();
        audioEnvironmentController.Configure(gameFlowManager, playerReverbFilter);
    }
}
