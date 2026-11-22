using UnityEngine;
using UnityEngine.InputSystem;

namespace Rio.Player
{
    /// <summary>
    /// Kamera hinter der Schulter, wie im Konzept festgelegt.
    /// Maus oder rechter Stick drehen, die Kamera rueckt bei Hindernissen
    /// naeher heran, damit man nicht durch Haeuser schaut.
    /// Escape gibt den Mauszeiger wieder frei.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Ziel")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
        public float distance = 6f;

        [Header("Drehen")]
        public float mouseSensitivity = 0.12f;
        public float stickSensitivity = 150f;
        public float minPitch = -20f;
        public float maxPitch = 65f;

        [Header("Hindernisse")]
        public float collisionRadius = 0.3f;
        public float minDistance = 1.3f;

        [Header("Mauszeiger")]
        public bool lockCursor = true;

        float _yaw;
        float _pitch = 14f;
        readonly RaycastHit[] _hits = new RaycastHit[12];

        void Start()
        {
            _yaw = transform.eulerAngles.y;
            if (lockCursor) SetCursor(true);
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) SetCursor(false);

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && lockCursor) SetCursor(true);
        }

        void LateUpdate()
        {
            if (target == null) return;

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * mouseSensitivity;
                _pitch -= delta.y * mouseSensitivity;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.rightStick.ReadValue();
                _yaw += stick.x * stickSensitivity * Time.deltaTime;
                _pitch -= stick.y * stickSensitivity * Time.deltaTime;
            }

            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + targetOffset;
            Vector3 back = rotation * Vector3.back;

            float allowed = ClampAgainstGeometry(focus, back, distance);
            transform.SetPositionAndRotation(focus + back * allowed, rotation);
        }

        /// <summary>
        /// Sucht das naechste Hindernis zwischen Zielpunkt und Kamera.
        /// Treffer an der Figur selbst werden uebersprungen, sonst klebt
        /// die Kamera an deren eigenem Collider fest.
        /// </summary>
        float ClampAgainstGeometry(Vector3 focus, Vector3 direction, float desired)
        {
            int count = Physics.SphereCastNonAlloc(focus, collisionRadius, direction, _hits,
                                                   desired, ~0, QueryTriggerInteraction.Ignore);
            float nearest = desired;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.distance <= 0.0001f) continue;                 // Start steckt im Collider
                if (target != null && hit.transform.IsChildOf(target)) continue;
                if (hit.distance < nearest) nearest = hit.distance;
            }

            return Mathf.Max(nearest - 0.15f, minDistance);
        }

        static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
