using UnityEngine;
using UnityEngine.InputSystem;

namespace Rio.Player
{
    /// <summary>
    /// Dritte-Person-Steuerung fuer die Block-Figur.
    /// Bewegung ist kamerarelativ: vorwaerts ist immer dorthin, wo die
    /// Kamera schaut. Laeuft ueber den CharacterController, also ohne
    /// Rigidbody-Physik - gut genug fuers Blockout und berechenbar.
    ///
    /// Tastatur: WASD laufen, Shift rennen, Leertaste springen.
    /// Gamepad: linker Stick laufen, Sued-Taste springen.
    /// Touch kommt spaeter ueber das Input-System-Asset dazu.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class BlockCharacterController : MonoBehaviour
    {
        [Header("Tempo")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 7.5f;
        [Tooltip("Sekunden, die das Drehen in die Laufrichtung braucht.")]
        public float turnSmoothing = 0.08f;

        [Header("Sprung und Schwerkraft")]
        public float jumpHeight = 1.25f;
        public float gravity = -22f;

        [Header("Bezug")]
        public Transform cameraTransform;
        [Tooltip("Faellt die Figur tiefer als das, wird sie zum Startpunkt zurueckgesetzt.")]
        public float killHeight = -30f;

        CharacterController _controller;
        BlockCharacterRig _rig;
        Vector3 _spawnPoint;
        float _verticalSpeed;
        float _turnVelocity;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _rig = GetComponentInChildren<BlockCharacterRig>();
            _spawnPoint = transform.position;

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 input = ReadMoveInput();

            // Laufrichtung relativ zur Kamera
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }

            Vector3 move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            // In Laufrichtung drehen
            if (move.sqrMagnitude > 0.0001f)
            {
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                    ref _turnVelocity, turnSmoothing);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Schwerkraft und Sprung
            if (_controller.isGrounded && _verticalSpeed < 0f)
                _verticalSpeed = -2f;

            if (JumpPressed() && _controller.isGrounded)
                _verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

            _verticalSpeed += gravity * dt;

            float speed = SprintHeld() ? sprintSpeed : walkSpeed;
            Vector3 velocity = move * speed;
            velocity.y = _verticalSpeed;
            _controller.Move(velocity * dt);

            if (_rig != null)
            {
                Vector3 planar = _controller.velocity;
                planar.y = 0f;
                _rig.Step(planar.magnitude, dt);
            }

            if (transform.position.y < killHeight)
                Respawn();
        }

        public void Respawn()
        {
            _controller.enabled = false;
            transform.position = _spawnPoint;
            _verticalSpeed = 0f;
            _controller.enabled = true;
        }

        // ---- Eingabe. Direkt ueber die Geraete, damit kein Asset verdrahtet sein muss. ----

        static Vector2 ReadMoveInput()
        {
            Vector2 result = Vector2.zero;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) result.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) result.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) result.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) result.x -= 1f;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > result.sqrMagnitude) result = stick;
            }

            return Vector2.ClampMagnitude(result, 1f);
        }

        static bool JumpPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            Gamepad pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        static bool SprintHeld()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.leftShiftKey.isPressed) return true;

            Gamepad pad = Gamepad.current;
            return pad != null && pad.leftStickButton.isPressed;
        }
    }
}
