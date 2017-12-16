using System.Linq;
using UnityEngine;

public class NextButton : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) == true)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero, float.PositiveInfinity);
            
            if (hits.ToList().Exists(_ => _.collider.gameObject == this.gameObject))
                Debug.Log("TO NEXT SCENE!");
        }
    }
}
