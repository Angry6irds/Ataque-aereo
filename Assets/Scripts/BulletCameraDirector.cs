using UnityEngine;

public class BulletCameraDirector : MonoBehaviour
{
    public static BulletCameraDirector Instance { get; private set; }

    [Header("Camera")]
    public Camera mainCam;
    public Vector3 localOffset = new Vector3(0f, 0f, 0.0f); 
    public float fovWhileFollowing = 60f;

    Transform _defaultParent;
    Vector3 _defaultPos;
    Quaternion _defaultRot;
    float _defaultFov;

    Transform _followingTarget;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (mainCam == null) mainCam = Camera.main;
        CacheDefault();
    }

    void CacheDefault()
    {
        if (mainCam == null) return;
        _defaultParent = mainCam.transform.parent;
        _defaultPos = mainCam.transform.position;
        _defaultRot = mainCam.transform.rotation;
        _defaultFov = mainCam.fieldOfView;
    }

    public void Follow(Transform bullet)
    {
        if (mainCam == null || bullet == null) return;

        _followingTarget = bullet;
        mainCam.transform.SetParent(bullet, worldPositionStays: false);
        mainCam.transform.localPosition = localOffset;
        mainCam.transform.localRotation = Quaternion.identity;
        mainCam.fieldOfView = fovWhileFollowing;
    }

    public void ReturnToDefault()
    {
        if (mainCam == null) return;

        mainCam.transform.SetParent(_defaultParent, worldPositionStays: false);
        mainCam.transform.position = _defaultPos;
        mainCam.transform.rotation = _defaultRot;
        mainCam.fieldOfView = _defaultFov;
        _followingTarget = null;
    }
}