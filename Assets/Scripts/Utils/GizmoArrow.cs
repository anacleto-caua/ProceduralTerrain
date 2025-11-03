using UnityEngine;

// Thanks to https://discussions.unity.com/u/anomalusundrdog for the base arrow code :)
public static class GizmoArrow
{
    public static void Draw(
        Vector3 origin,
        Vector3 target, 
        float arrowHeadLength = 0.25f,
        float arrowHeadAngle = 20.0f
        )
    {
        float arrowScale = Vector3.Distance(origin, target);
        Vector3 targetRot = (target - origin).normalized;

        // Draw arrow body
        Gizmos.DrawRay(origin, targetRot * arrowScale);

        // Needed to draw arrow fins
        Vector3 right = Quaternion.LookRotation(targetRot) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(targetRot) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
        
        // Draw arrow fins
        
        // At origin
        Gizmos.DrawRay(origin + targetRot, right * arrowHeadLength);
        Gizmos.DrawRay(origin + targetRot, left * arrowHeadLength);
        // At target
        Gizmos.DrawRay(target + targetRot, right * arrowHeadLength);
        Gizmos.DrawRay(target + targetRot, left * arrowHeadLength);
    }

    public static void FatArrow(
        Vector3 origin,
        Vector3 target,
        float arrowHeadLength = 0.25f,
        float arrowHeadAngle = 20.0f
        )
    {
        float arrowScale = Vector3.Distance(origin, target);
        Vector3 cubeScale = new(1, 1, arrowScale);

        Vector3 targetRot = (target - origin).normalized;

        // Draw arrow body
        Gizmos.DrawWireCube(origin, cubeScale);

        // Needed to draw arrow fins
        Vector3 right = Quaternion.LookRotation(targetRot) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(targetRot) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;

        // Draw arrow fins

        // At origin
        Gizmos.DrawRay(origin + targetRot, right * arrowHeadLength);
        Gizmos.DrawRay(origin + targetRot, left * arrowHeadLength);
        // At target
        Gizmos.DrawRay(target + targetRot, right * arrowHeadLength);
        Gizmos.DrawRay(target + targetRot, left * arrowHeadLength);
    }
}