using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookTarget : MonoBehaviour
{
    public Transform foregroundTarget;
    public Transform lookTarget;
    public bool switchTargets = false;
    public float boomLength = 4.5f;
    public float boomHeight = 2f;
    public Vector3 boomOffset;
    public Vector3 lookTargetOffset;

    void LateUpdate()
    {
        if(foregroundTarget != null && lookTarget != null)
        {
            Transform ft = foregroundTarget;
            Transform lt = lookTarget;

            if(switchTargets)
            {
                ft = lookTarget;
                lt = foregroundTarget;
            }

            Vector3 lookTargetDirection = ft.position - lt.position;
            Vector3 cameraPosition = ft.position + lookTargetDirection.normalized * boomLength;
            cameraPosition = cameraPosition + ft.up * boomHeight;
            transform.position = cameraPosition;
            transform.LookAt(lt);
        }
        else if(lookTarget != null)
        {
            transform.LookAt(lookTarget.position + lookTargetOffset);
        }
    }
}
