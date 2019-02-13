using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowLastProjectile : MonoBehaviour
{
    public Transform projectileNotAvailableTransform;
    public Transform lookTarget;
    public float boomLength = 4.5f;
    public float boomHeight = 2f;
    public Vector3 boomOffset;

    void LateUpdate()
    {
        Transform lastLaunchedProjectile = Projectile.lastLaunchedProjectile;
        if(lastLaunchedProjectile == null)
            lastLaunchedProjectile = projectileNotAvailableTransform;

        if(lastLaunchedProjectile != null && lookTarget != null)
        {
            Transform ft = lastLaunchedProjectile;
            Transform lt = lookTarget;

            Vector3 lookTargetDirection = ft.position - lt.position;
            Vector3 cameraPosition = ft.position + lookTargetDirection.normalized * boomLength;
            cameraPosition = cameraPosition + ft.up * boomHeight;
            transform.position = cameraPosition;
            transform.LookAt(lt);
        }
    }
}
