using UnityEngine;

namespace MonsterWorldLike.World
{
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Enable/Disable")]
        [SerializeField] private bool enablePan = true;
        [SerializeField] private bool enableZoom = true;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 8f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minSize = 3f;
        [SerializeField] private float maxSize = 12f;

        [Header("World Limits")]
        [SerializeField] private bool clampPosition = true;
        [SerializeField] private Vector2 limitX = new(-10f, 10f);
        [SerializeField] private Vector2 limitY = new(-10f, 10f);
        [SerializeField] private float defaultOrthographicSize = 6f;

        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Clamp(defaultOrthographicSize, minSize, maxSize);
            }
        }

        private void Update()
        {
            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            if (!enablePan)
            {
                return;
            }

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
            if (clampPosition)
            {
                var pos = transform.position;
                pos.x = Mathf.Clamp(pos.x, Mathf.Min(limitX.x, limitX.y), Mathf.Max(limitX.x, limitX.y));
                pos.y = Mathf.Clamp(pos.y, Mathf.Min(limitY.x, limitY.y), Mathf.Max(limitY.x, limitY.y));
                transform.position = pos;
            }
        }

        private void HandleZoom()
        {
            if (!enableZoom)
            {
                return;
            }

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
