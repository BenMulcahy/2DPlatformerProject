using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

//Manages all player FX, spawning of particales, SFX, Animation etc

public class PlayerFX : MonoBehaviour
{

    [Header("--- Code based Animations ---")]
    [SerializeField] bool bUseCodeBasedAnims = true;
    [Header("Squash")]
    [SerializeField] Vector2 _squashScalePivotPoint = new Vector2(0, -0.95f);
    [SerializeField] Vector2 _maxSquash = new Vector2(1.4f, 0.55f);
    [SerializeField] float _squashDuration = 0.05f;

    [Header("--- Camera Shakes ---")]
    public bool bEnablePlayerCamShakes { private get; set; } = true;
    [Header("Landing")]
    [SerializeField] NoiseSettings _playerLandShake;
    [SerializeField] float _landingCamShakeDuration = 0.2f;
    [SerializeField] float _landingCamShakeIntensityMod = 1f;

    [Header("--- Controller Rumble ---")]
    [SerializeField] float _damageRumbleDuration = 0.3f;
    [SerializeField] float _damageRumbleIntensity = 3f;


    #region Setup
    private void OnEnable()
    {
        PlayerMovementComponent.onPlayerLand += PlayerLandFX;
        PlayerMovementComponent.onPlayerJump += PlayerJumpFX;
        PlayerMovementComponent.onPlayerMove += PlayerMovementFX;
        PlayerMovementComponent.onPlayerWallJump += PlayerWallJumpFX;
        PlayerMovementComponent.onPlayerLandWall += PlayerWallLandFX;
        PlayerMovementComponent.onPlayerWallSlide += PlayerWallSlideFX;
        Player.Instance.GetComponent<HealthComponent>().onTakeDamage += PlayerTakeDamageFX;
    }

    private void OnDisable()
    {
        PlayerMovementComponent.onPlayerLand -= PlayerLandFX;
        PlayerMovementComponent.onPlayerJump -= PlayerJumpFX;
        PlayerMovementComponent.onPlayerMove -= PlayerMovementFX;
        PlayerMovementComponent.onPlayerWallJump -= PlayerWallJumpFX;
        PlayerMovementComponent.onPlayerLandWall -= PlayerWallLandFX;
        PlayerMovementComponent.onPlayerWallSlide -= PlayerWallSlideFX;
        Player.Instance.GetComponent<HealthComponent>().onTakeDamage -= PlayerTakeDamageFX;
    }

    #endregion

    #region bound delegate functions
    private void PlayerLandFX()
    {
        if (bUseCodeBasedAnims) StartCoroutine(Squash());
        CameraManager.Instance.DoCameraShake(_landingCamShakeIntensityMod, _landingCamShakeDuration, _playerLandShake);
        //InputManager.Instance.ControllerRumble(_landingRumbleIntensity, _landingRumbleDuration);

        //TODO Landing Dust Cloud
    }

    private void PlayerWallJumpFX()
    {

    }

    private void PlayerWallLandFX()
    {
        //InputManager.Instance.ControllerRumble(_wallLandRumbleIntensity, _wallLandRumbleDuration);
    }

    private void PlayerWallSlideFX(Vector2 playerVelocity)
    {

    }

    private void PlayerJumpFX()
    {
        //Debug.Log("Player Jump FX");

        //TODO Player Jump Dust

        if (bUseCodeBasedAnims)
        {
            StopCoroutine(Squash());
            transform.localScale = Vector2.one;
            transform.localPosition = Vector2.zero;
        }
    }

    private void PlayerTakeDamageFX()
    {
        Debug.Log("Do Rumble");
        InputManager.Instance.ControllerRumble(_damageRumbleIntensity, _damageRumbleDuration);
    }

    private void PlayerMovementFX(Vector2 playerVelocity)
    {

    }
    #endregion

    IEnumerator Squash()
    {
        Vector2 localPos = transform.localPosition;
        Vector2 pivotPos = _squashScalePivotPoint;

        Vector2 RelScale = _maxSquash / transform.localScale;
        Vector2 finalPos = pivotPos + (localPos - pivotPos) * RelScale;

        float t = 0;

        bool bSquashing = true;

        while (t < _squashDuration) //Squash down
        {
            if (bSquashing)
            {
                transform.localScale = Vector2.Lerp(Vector2.one, _maxSquash, (t / _squashDuration) / 2);
                transform.localPosition = Vector2.Lerp(localPos, finalPos, (t / _squashDuration) / 2);
            }
            else
            {
                transform.localScale = Vector2.Lerp(_maxSquash, Vector2.one, (t / _squashDuration) / 2);
                transform.localPosition = Vector2.Lerp(finalPos, localPos, (t / _squashDuration) / 2);
            }

            t += Time.deltaTime;
            if (t > _squashDuration / 2) bSquashing = false;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = Vector2.one;
        transform.localPosition = Vector2.zero;
    }


}
