using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Manages all player FX, spawning of particales, SFX, Animation etc

public class PlayerFX : MonoBehaviour
{
    [SerializeField] bool bUseCodeBasedAnims = true;

    [SerializeField] Vector2 _squashScalePivotPoint = new Vector2(0,-0.95f);
    [SerializeField] Vector2 _maxSquash = new Vector2(1.4f, 0.55f);
    [SerializeField] float _squashDuration = 0.05f;


    private void OnEnable()
    {
        PlayerMovementComponent.onPlayerLand += PlayerLandFX;
        PlayerMovementComponent.onPlayerJump += PlayerJumpFX;
        PlayerMovementComponent.onPlayerMove += PlayerMovementFX;
    }

    private void OnDisable()
    {
        PlayerMovementComponent.onPlayerLand -= PlayerLandFX;
        PlayerMovementComponent.onPlayerJump -= PlayerJumpFX;
        PlayerMovementComponent.onPlayerMove -= PlayerMovementFX;
    }

    private void PlayerLandFX()
    {
        Debug.Log("Player Land FX");
        if (bUseCodeBasedAnims)
        {
            StartCoroutine(Squash());
        }
    }

    private void PlayerJumpFX()
    {
        Debug.Log("Player Jump FX");

        if (bUseCodeBasedAnims)
        {
            StopCoroutine(Squash());
            transform.localScale = Vector2.one;
            transform.localPosition = Vector2.zero;
        }
    }

    private void PlayerMovementFX(Vector2 playerVelocity)
    {

    }

    IEnumerator Squash()
    {
        Vector2 localPos = transform.localPosition;
        Vector2 pivotPos = _squashScalePivotPoint;

        Vector2 RelScale = _maxSquash / transform.localScale;
        Vector2 finalPos = pivotPos + (localPos - pivotPos) * RelScale;

        float t = 0;

        bool bSquashing = true;

        while(t < _squashDuration) //Squash down
        {
            if (bSquashing)
            { 
                transform.localScale = Vector2.Lerp(Vector2.one, _maxSquash, (t / _squashDuration)/2);
                transform.localPosition = Vector2.Lerp(localPos, finalPos, (t / _squashDuration)/2);
            }
            else
            {
                transform.localScale = Vector2.Lerp(_maxSquash, Vector2.one, (t / _squashDuration)/2);
                transform.localPosition = Vector2.Lerp(finalPos, localPos, (t / _squashDuration)/2);
            }

            t += Time.deltaTime;
            if (t > _squashDuration / 2) bSquashing = false;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = Vector2.one;
        transform.localPosition = Vector2.zero;
    }

}
