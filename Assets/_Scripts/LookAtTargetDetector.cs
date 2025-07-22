using UnityEngine;

public class LookAtTargetDetector : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _maxDistance = 3f;

    public bool TryRaycast(out RaycastHit hit)
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        return Physics.Raycast(ray, out hit, _maxDistance);
    }
}