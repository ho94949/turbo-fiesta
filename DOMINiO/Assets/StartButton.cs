using System.Linq;
using UnityEngine;

public class StartButton : MonoBehaviour
{
    private void Start()
    {
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == true)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero, float.PositiveInfinity);

            if (hits.ToList().Exists(_ => _.collider.gameObject == this.gameObject))
                foreach (RigidBodySimulation sim in GameObject.FindObjectsOfType<RigidBodySimulation>())
                    sim.StartSimulation();
        }
    }
}
