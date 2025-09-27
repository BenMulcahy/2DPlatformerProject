using System.Collections;
using UnityEngine;
using Unity.Cinemachine;


public class CameraManager : MonoBehaviour
{
    //TODO: If player within Y bounds of the screen space then dont track in the Y

    public static CameraManager Instance { get; private set; }
    [field:SerializeField] public CinemachineCamera MainCamera { get; private set; }

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
        if (!MainCamera) MainCamera = FindFirstObjectByType<CinemachineCamera>();
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
        MainCamera.LookAt = target;
        MainCamera.Follow = target;

        //Depricated
        //MainCamera.m_LookAt = target;
        //MainCamera.m_Follow = target;
    }

    void UpdateOffset()
    {
        Vector2 targetOffset = new Vector2(_player.bIsRightInput ? CameraOffset.x : -CameraOffset.x, CameraOffset.y);

        MainCamera.GetCinemachineComponent(CinemachineCore.Stage.Body).GetComponent<CinemachinePositionComposer>().TargetOffset = targetOffset;
        
        ///Depricated
        //MainCamera.CinemachinePositionComposer.m_TrackedObjectOffset = targetOffset;

    }

    public void DoCameraShake(float intensityMod, float time, NoiseSettings baseNoiseSetting = null)
    {
        if (bIsCameraShaking) //In the event of another cam shake going off -> effecitvley overwrite cam shake
        {
            EndCameraShake();
        }

        if (!baseNoiseSetting) { baseNoiseSetting = _defaultCamShakeNoiseProfile; Debug.LogWarning("Default Cam Shake used! Is this intentional?"); } //Allows for cam shake to be called using default 6d noise
        
        CinemachineBasicMultiChannelPerlin cineNoise = MainCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise).GetComponent<CinemachineBasicMultiChannelPerlin>();

        cineNoise.NoiseProfile = baseNoiseSetting;
        cineNoise.AmplitudeGain = intensityMod;

        ///Depricated
        //MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_NoiseProfile = baseNoiseSetting;
        //MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = intensityMod;
        
        cameraShakeTimer = time;
        bIsCameraShaking = true;
    }

    public void EndCameraShake()
    {
        bIsCameraShaking = false;
        cameraShakeTimer = 0;
        MainCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise).GetComponent<CinemachineBasicMultiChannelPerlin>().NoiseProfile = null;
        
        ///Depricated
        //MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_NoiseProfile = null;
    }
}
