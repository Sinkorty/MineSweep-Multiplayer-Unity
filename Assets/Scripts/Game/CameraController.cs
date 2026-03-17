using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float scaleSpeed = 2f;
    [SerializeField] private float moveInterpolation = 0.985f;
    [SerializeField] private float scaleInterpolation = 0.985f;
    [SerializeField] private float minSize = 2f;
    [SerializeField] private float maxSize = 20f;
    [SerializeField] private Camera cam;
    private float anchorSize = 11f;
    private Vector3 anchor;
    private float currentSize = 11f;

    private void Update()
    {
        CameraMove();
        CameraScale();
    }
    private void CameraMove()
    {
        Vector3 velocity = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) velocity.y = 1;
        if (Input.GetKey(KeyCode.S)) velocity.y = -1;
        if (Input.GetKey(KeyCode.A)) velocity.x = -1;
        if (Input.GetKey(KeyCode.D)) velocity.x = 1;
        anchor += velocity * Time.deltaTime * moveSpeed * Mathf.Sqrt(anchorSize / 11f);
        Vector3 toMove = Vector3.Lerp(anchor, transform.position, moveInterpolation);
        toMove.z = -10;
        transform.position = toMove;
    }
    private void CameraScale()
    {
        float scrollInput = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            float increasement = scrollInput > 0 ? -1 : 1;
            increasement = increasement * scaleSpeed * Time.deltaTime;
            anchorSize += increasement;
            anchorSize = Mathf.Clamp(anchorSize, minSize, maxSize);
        }
        currentSize = Mathf.Lerp(anchorSize, currentSize, scaleInterpolation);
        cam.orthographicSize = currentSize;
    }
}
