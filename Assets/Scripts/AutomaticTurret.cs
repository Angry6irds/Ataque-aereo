
using UnityEngine;

public class AutomaticTurret : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;
    public Transform turretAxisY;   
    public Transform turretAxisX;   
    public Transform shootPoint;    
    public GameObject bulletPrefab;

    [Header("Aiming")]
    [Tooltip("Qué tanto por encima del objetivo se apunta para garantizar ángulo ascendente.")]
    public float aimAboveHeight = 0.5f;
    [Tooltip("Mínimo pitch inicial hacia arriba (grados). 0–5° suele ir bien.")]
    public float minUpPitchDeg = 3f;
    [Tooltip("Velocidad de giro visual de la torreta.")]
    public float rotSpeed = 8f;

    [Header("Ballistic Time Tuning")]
    [Tooltip("Tiempo base por metro para estimar tiempo de vuelo inicial.")]
    public float timePerMeter = 0.055f;
    [Tooltip("Límites de tiempo de vuelo permitidos.")]
    public float minFlightTime = 0.25f, maxFlightTime = 3.5f;
    [Tooltip("Incremento al ajustar para conseguir pitch ascendente.")]
    public float timeAdjustStep = 0.08f;
    [Tooltip("Tope de iteraciones para ajustar tiempo de vuelo.")]
    public int maxSolveIterations = 16;

    private Vector3 _lastSolvedVelocity = Vector3.forward; 

    void Update()
    {
        if (target != null)
        {
            Vector3 p0 = shootPoint.position;
            Vector3 p1 = target.position + Vector3.up * aimAboveHeight;

            if (SolveBallisticGuaranteed(p0, p1, out Vector3 v, out float solvedT))
            {
                _lastSolvedVelocity = v;

                Vector3 flat = new Vector3(v.x, 0f, v.z);
                if (flat.sqrMagnitude > 0.0001f)
                {
                    Quaternion yawRot = Quaternion.LookRotation(flat, Vector3.up);
                    turretAxisY.rotation = Quaternion.Slerp(turretAxisY.rotation, yawRot, rotSpeed * Time.deltaTime);
                }

                Vector3 localV = Quaternion.Inverse(turretAxisY.rotation) * v;
                float horiz = new Vector2(localV.x, localV.z).magnitude;
                float pitchDeg = Mathf.Atan2(localV.y, horiz) * Mathf.Rad2Deg;
                pitchDeg = Mathf.Max(minUpPitchDeg, pitchDeg);
                turretAxisX.localRotation = Quaternion.Slerp(turretAxisX.localRotation,
                                                            Quaternion.Euler(-pitchDeg, 0f, 0f),
                                                            rotSpeed * Time.deltaTime);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public GameObject Fire()
    {
        if (bulletPrefab == null || shootPoint == null || target == null) return null;

        Vector3 p0 = shootPoint.position;
        Vector3 p1 = target.position + Vector3.up * aimAboveHeight;

        if (!SolveBallisticGuaranteed(p0, p1, out Vector3 v, out float solvedT))
        {
            Debug.LogWarning("[AutomaticTurret] No se pudo resolver trayectoria. Cancelando disparo.");
            return null;
        }

        shootPoint.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);

        GameObject bullet = Instantiate(bulletPrefab, p0, shootPoint.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null) SetRbVelocity(rb, v);

        BulletCameraDirector.Instance?.Follow(bullet.transform);

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.OnBulletFinished += () => BulletCameraDirector.Instance?.ReturnToDefault();
        }

        Destroy(bullet, 30f);
        return bullet;
    }

    
    private bool SolveBallisticGuaranteed(Vector3 p0, Vector3 p1, out Vector3 v, out float tFinal)
    {
        Vector3 g = Physics.gravity; 
        Vector3 d = p1 - p0;
        float dist = d.magnitude;

        float t = Mathf.Clamp(dist * timePerMeter, minFlightTime, maxFlightTime);

        v = Vector3.zero;
        bool ok = false;

        for (int i = 0; i < maxSolveIterations; i++)
        {
            v = (d - 0.5f * g * (t * t)) / t;

            Vector3 flat = new Vector3(v.x, 0f, v.z);
            float pitchDeg = Mathf.Atan2(v.y, flat.magnitude) * Mathf.Rad2Deg;

            if (pitchDeg >= minUpPitchDeg)
            {
                ok = true;
                break;
            }

            t += timeAdjustStep;
            if (t > maxFlightTime) break;
        }

        tFinal = t;
        return ok;
    }

    private void SetRbVelocity(Rigidbody rb, Vector3 vel)
    {
        var t = rb.GetType();
        var prop = t.GetProperty("linearVelocity");
        if (prop != null && prop.CanWrite)
            prop.SetValue(rb, vel, null);
        else
            rb.linearVelocity = vel;
    }
}