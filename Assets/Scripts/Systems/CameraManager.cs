using System.Collections;
using UnityEngine;
using Cinemachine;


public class CameraManager : MonoBehaviour
{

    public static CameraManager Instance { get; private set; }
    [field:SerializeField] public CinemachineVirtualCamera MainCamera { get; private set; }

    [Header("--- Camera Shakes ---")]
    NoiseSettings _defaultCamShakeNoiseProfile;
    float cameraShakeTimer;
    public bool bIsCameraShaking { get; private set; }

    static Player _player;
    GameObject _cameraTarget; //If want to move camera from player to look at something else

    [SerializeField] Vector2 CameraOffset = new Vector2 (2.0f,3.0f);

    private void Awake()
    {
        if (!Instance) Instance = this;

        _defaultCamShakeNoiseProfile = Resources.Load<NoiseSettings>("DefaultCameraShake");
    }

    void Start()
    {
        if (!MainCamera) MainCamera = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (!_player) _player = Player.Instance;
        InvokeRepeating(nameof(UpdateOffset), 0f, 0.04f);
    }

    private void Update()
    {
        if (bIsCameraShaking)
        {
            cameraShakeTimer -= Time.deltaTime;
            if(cameraShakeTimer <= 0.0f)
            {
                //Finish Cam shake
                EndCameraShake();
            }
        }
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

    public void DoCameraShake(float intensityMod, float time, NoiseSettings baseNoiseSetting = null)
    {
        if (bIsCameraShaking) //In the event of another cam shake going off -> effecitvley overwrite cam shake
        {
            EndCameraShake();
        }


        if (!baseNoiseSetting) { baseNoiseSetting = _defaultCamShakeNoiseProfile; Debug.LogWarning("Default Cam Shake used! Is this intentional?"); } //Allows for cam shake to be called using default 6d noise
        MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_NoiseProfile = baseNoiseSetting;
        MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = intensityMod;
        cameraShakeTimer = time;
        bIsCameraShaking = true;
    }

    public void EndCameraShake()
    {
        bIsCameraShaking = false;
        cameraShakeTimer = 0;
        MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_NoiseProfile = null;
    }
}
