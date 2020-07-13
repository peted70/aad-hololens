using UnityEngine;

public class Spinner : MonoBehaviour
{
    private bool spinning;

    private void OnEnable()
    {
        spinning = true;    
    }

    private void OnDisable()
    {
        spinning = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (spinning)
        {
            // Rotate the object around its local X axis at 1 degree per second
            transform.Rotate(Vector3.right * Time.deltaTime * 100);

            // ...also rotate around the World's Y axis
            transform.Rotate(Vector3.up * Time.deltaTime * 100, Space.World);
        }
    }
}
