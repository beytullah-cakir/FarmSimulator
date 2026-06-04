using UnityEngine;


public class BillboardUI : MonoBehaviour
{

    private Transform targetTransform;




    private void Awake()
    {

        targetTransform = transform;

    }


    private void LateUpdate()
    {
        MainCode();
    }

    public void MainCode()
    {

        Vector3 cameraDirection = Camera.main.transform.forward;
        targetTransform.rotation = Quaternion.LookRotation(cameraDirection, Camera.main.transform.up);

    }
}
