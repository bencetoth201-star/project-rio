using UnityEngine;

namespace Rio.Player
{
    /// <summary>
    /// Haelt die Gelenke der Block-Figur und laesst Arme und Beine
    /// beim Laufen pendeln. Bewusst simpel: eine Sinuskurve, kein
    /// Animationssystem. Die Pivots sitzen an Schulter und Huefte.
    /// </summary>
    public class BlockCharacterRig : MonoBehaviour
    {
        [Header("Gelenke")]
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform body;

        [Header("Ausschlag")]
        [Tooltip("Maximaler Beinausschlag in Grad bei voller Laufgeschwindigkeit.")]
        public float legSwing = 42f;
        [Tooltip("Armausschlag relativ zum Bein.")]
        public float armRatio = 0.7f;
        [Tooltip("Geschwindigkeit, ab der voll ausgeschlagen wird (m/s).")]
        public float fullSwingSpeed = 4f;

        float _phase;
        float _bodyRestY;
        float _blend;

        void Awake()
        {
            if (body != null) _bodyRestY = body.localPosition.y;
        }

        /// <summary>Wird jeden Frame mit der waagerechten Geschwindigkeit gefuettert.</summary>
        public void Step(float planarSpeed, float deltaTime)
        {
            float target = Mathf.Clamp01(planarSpeed / Mathf.Max(fullSwingSpeed, 0.01f));
            // weich ein- und ausblenden, damit die Figur beim Stehenbleiben nicht einfriert
            _blend = Mathf.MoveTowards(_blend, target, deltaTime * 6f);
            _phase += deltaTime * (4.5f + planarSpeed * 1.5f);

            float swing = Mathf.Sin(_phase) * legSwing * _blend;
            SetPitch(leftLeg, swing);
            SetPitch(rightLeg, -swing);
            SetPitch(rightArm, swing * armRatio);
            SetPitch(leftArm, -swing * armRatio);

            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(_phase)) * 0.05f * _blend;
                Vector3 p = body.localPosition;
                body.localPosition = new Vector3(p.x, _bodyRestY + bob, p.z);
            }
        }

        static void SetPitch(Transform t, float degrees)
        {
            if (t != null) t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
