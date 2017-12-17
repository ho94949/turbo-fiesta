using System.Linq;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    private OverlapWarn overlapWarn;
    private RigidBodySimulation[] simArray;

    private void Start()
    {
        overlapWarn = GameObject.FindObjectOfType<OverlapWarn>();
        overlapWarn.gameObject.SetActive(false);
        simArray = GameObject.FindObjectsOfType<RigidBodySimulation>();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == true)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero, float.PositiveInfinity);

            if (simArray[0].UnderSimulation == true)
                return;

            if (hits.ToList().Exists(_ => _.collider.gameObject == this.gameObject))
            {
                float eps1 = 0.001f;
                float eps2 = 0.03f;

                bool isValid = true;

                foreach (RigidBodySimulation lhs in simArray)
                    foreach (RigidBodySimulation rhs in simArray)
                    {
                        float depth;
                        Vector2 deepestPoint, direction;
                        if (lhs != rhs)
                        {
                            CustomPhysics.GetPenetrationDepth(lhs.ToPointArray(), rhs.ToPointArray(), out depth, out deepestPoint, out direction);
                            if (Mathf.Abs(depth) > eps1)
                            {
                                Debug.Log("failed at " + lhs.name + ", " + rhs.name);
                                isValid = false;
                                goto there;
                            }
                        }
                    }

                foreach (RigidBodySimulation sim in simArray)
                    if (sim.LinearVelocity.magnitude > eps2 || Mathf.Abs(sim.AngularVelocity) > eps2)
                    {
                        Debug.Log("failed at " + sim.name);
                        Debug.Log("linvel " + sim.LinearVelocity.magnitude.ToString() + ", angvel " + sim.AngularVelocity.ToString());
                        isValid = false;
                        goto there;
                    }
                there:
                if (isValid == true)
                    foreach (RigidBodySimulation sim in simArray)
                        sim.StartSimulation();
                else
                    overlapWarn.StartWarn();
            }
        }
    }
}
