using UnityEngine;

public class OverlapWarn : MonoBehaviour
{
    private float timer;

    public void StartWarn()
    {
        gameObject.SetActive(true);
        timer = 0;
    }
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer > 1)
            gameObject.SetActive(false);
    }
}
