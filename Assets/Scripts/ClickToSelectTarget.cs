using UnityEngine;

public class ClickToSelectTarget : MonoBehaviour
{
    public Camera cam;
    public AutomaticTurret turret;
    public LayerMask clickableLayers = ~0; 
    public bool fireOnSelect = true;

    void Reset()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (cam == null || turret == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f, clickableLayers, QueryTriggerInteraction.Ignore))
            {
                turret.SetTarget(hit.collider.transform);

                if (fireOnSelect)
                {
                    turret.Fire();
                }
            }
        }
    }
}