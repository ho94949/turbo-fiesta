using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextButton : MonoBehaviour
{
    public string NextSceneName;
    public bool ActiveOnStart;

    private void Start()
    {
        gameObject.SetActive(ActiveOnStart);
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == true)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero, float.PositiveInfinity);

            if (hits.ToList().Exists(_ => _.collider.gameObject == this.gameObject))
            {
                Debug.Log("Moving to " + NextSceneName);
                SceneManager.LoadScene(NextSceneName);
            }
        }
    }
}
