using UnityEngine;

public class TargetCube : MonoBehaviour
{
    private NextButton nb;

    private void Awake()
    {
        nb = GameObject.FindObjectOfType<NextButton>();
        Debug.Assert(nb != null);
    }
    public void Collided()
    {
        Debug.Log("Game Set!");

        foreach (RigidBodySimulation sim in GameObject.FindObjectsOfType<RigidBodySimulation>())
            sim.StopSimulation();
        nb.gameObject.SetActive(true);
    }
}
