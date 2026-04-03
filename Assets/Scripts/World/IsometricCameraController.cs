using UnityEngine;

namespace MonsterWorldLike.World
{
    public class IsometricCameraController : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 8f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minSize = 3f;
        [SerializeField] private float maxSize = 12f;

        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            if (Input.touchCount != 1)
            {
                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Moved)
            {
                return;
            }

            var delta = touch.deltaPosition;
            var move = new Vector3(-delta.x, 0f, -delta.y) * (panSpeed * Time.deltaTime * 0.01f);
            transform.Translate(move, Space.World);
        }

        private void HandleZoom()
        {
            if (cam == null || Input.touchCount != 2)
            {
                return;
            }

            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            var prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            var currentDist = (t0.position - t1.position).magnitude;
            var delta = currentDist - prevDist;

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize - (delta * zoomSpeed * Time.deltaTime * 0.01f),
                minSize,
                maxSize);
        }
    }
}
