using System.Collections;
using UnityEngine;
using Cinemachine;


public class CameraManager : MonoBehaviour
{
    [field:SerializeField] public CinemachineVirtualCamera MainCamera { get; private set; }

    static Player _player;
    GameObject _cameraTarget; //If want to move camera from player to look at something else

    [SerializeField] Vector2 CameraOffset = new Vector2 (2.0f,3.0f);

    void Start()
    {
        if (!MainCamera) MainCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (!_player) _player = Player.Instance;
        InvokeRepeating(nameof(UpdateOffset), 0f, 0.04f);
    }

    public void SetCameraTarget(Transform target)
    {
        MainCamera.m_LookAt = target;
        MainCamera.m_Follow = target;
    }

    void UpdateOffset()
    {
        Vector2 targetOffset = new Vector2(_player.GetComponent<PlayerMovementComponent>().bIsFacingRight ? CameraOffset.x : -CameraOffset.x, CameraOffset.y);
        MainCamera.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = targetOffset;

    }
}
