using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace Kabir.CharacterComponents
{
    /// <summary>
    /// Controller to move the character using Unity's CharacterController. Assumes:
    /// - Character will only rotate in the Y axis
    /// - Gravity is in the Y-axis
    /// - Gravity acceleration is Physics.gravity.y
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class MotionController : MonoBehaviour
    {
        /// <summary>
        /// Sets the controller to evaluate movement at every Update()
        /// </summary>
        public bool AutoEvaluate = true;

        /// <summary>
        /// Character controller attached to this motion controller
        /// </summary>
        public CharacterController CharacterController
        {
            get
            {
                if(_characterController == null) _characterController = GetComponent<CharacterController>();
                return _characterController;
            }
        }
        private CharacterController _characterController;

        /// <summary>
        /// Gravity multiplier. Can only positive (includes 0)
        /// </summary>
        public float GravityMultiplier
        {
            get { return _gravityMultiplier; }
            set { _gravityMultiplier = Mathf.Max(0f, value); }
        }
        [SerializeField]
        private float _gravityMultiplier = 1f;

        /// <summary>
        /// Position of the character's center relative to the world
        /// </summary>
        public Vector3 CenterPosition => transform.TransformPoint(CharacterController.center);

        /// <summary>
        /// Layers defined as the ground
        /// </summary>
        public LayerMask GroundLayers;

        /// <summary>
        /// Character will move according to the gradient when moving downhill
        /// </summary>
        [BoxGroup("Terrain Gradient Params")]
        public bool FollowTerrainGradient = true;

        /// <summary>
        /// Maximum angle of the slope that the character can follow. Recommend between the CharacterController's slope limit and 90 degrees.
        /// </summary>
        public float FollowSlopeLimit
        {
            get { return _followSlopeLimit; }
            set { _followSlopeLimit = Mathf.Clamp(value, 0f, 90f); }
        }
        [SerializeField, ShowIf("FollowTerrainGradient"), BoxGroup("Terrain Gradient Params"), Range(0f, 90f)]
        private float _followSlopeLimit = 80f;

        /// <summary>
        /// Vertical velocity of the last evaluation
        /// </summary>
        [field: SerializeField, ReadOnly]
        public float PreviousVerticalVelocity { get; private set; } = 0f;

        private Vector3 _motion = Vector3.zero;
        
        private float _jumpSpeed = 0f;
        private bool _isFullyGrounded = false;
        private RaycastHit _groundHitPoint;

        void Start()
        {
        
        }

        void Update()
        {
            UpdateGrounded();
            if(AutoEvaluate)
            {
                EvaluateMotion(_motion, Time.deltaTime);
            }
        }

        private void OnValidate()
        {
            _gravityMultiplier = Mathf.Max(_gravityMultiplier, 0f);
            _followSlopeLimit = Mathf.Clamp(_followSlopeLimit, 0f, 90f);
        }

        /// <summary>
        /// Sets the motion value for auto evaluate
        /// </summary>
        /// <param name="motion"></param>
        public void SetAutoEvaluateMotion(Vector3 motion) => _motion = motion;

        /// <summary>
        /// Moves the character based on the given settings
        /// </summary>
        /// <param name="motion"></param>
        public void EvaluateMotion(Vector3 motion, float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            float verticalVelocity = GetCurrentVerticalVelocity(deltaTime);
            float verticalMotion = verticalVelocity * deltaTime;
            Vector3 gradientMotion = GetGradientAffectedMotion(motion);

            if(gradientMotion != Vector3.zero)
                DebugExtension.DebugArrow(transform.position, gradientMotion * 200f, Color.green);

            Vector3 netMotion = gradientMotion + new Vector3(0f, verticalMotion, 0f);

            CharacterController.Move(netMotion);

            PreviousVerticalVelocity = verticalVelocity;
            if (GravityMultiplier <= 0f) PreviousVerticalVelocity = 0f;

            _motion = Vector3.zero;
            _jumpSpeed = 0f;
        }

        /// <summary>
        /// Makes the character jump when evaluated
        /// </summary>
        /// <param name="jumpSpeed"></param>
        public void Jump(float jumpSpeed) => _jumpSpeed = Mathf.Max(jumpSpeed, 0f);

        /// <summary>
        /// See if the character has fully grounded
        /// </summary>
        /// <param name="groundHit">Information on the ground hit point</param>
        /// <returns></returns>
        public bool HasFullyGrounded(out RaycastHit groundHit)
        {
            groundHit = _groundHitPoint;
            return _isFullyGrounded;
        }

        /// <summary>
        /// Rotate the character to the targeted direction with a max rotation speed
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="maxRotationSpeed"></param>
        /// <param name="useScaledTime"></param>
        public void FlatClampedRotateTowards(Vector3 direction, float maxRotationSpeed, bool useScaledTime = true)
        {
            Vector3 flatDir = new(direction.x, 0f, direction.z);
            if (flatDir == Vector3.zero || maxRotationSpeed <= 0f) return;

            Vector3 flatFwd = new(transform.forward.x, 0f, transform.forward.z);
            if (flatFwd == Vector3.zero) return;

            flatDir.Normalize();
            flatFwd.Normalize();

            float deltaTime = Time.deltaTime;
            if (!useScaledTime) deltaTime = Time.unscaledDeltaTime;
            float maxAngleAmount = maxRotationSpeed * deltaTime;

            float angleNeeded = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);
            if (Mathf.Abs(angleNeeded) <= maxAngleAmount)
            {
                transform.Rotate(0f, angleNeeded, 0f, Space.World);
                return;
            }

            float angleAmount = maxAngleAmount;
            if (angleNeeded < 0f) angleAmount *= -1f;
            transform.Rotate(0f, angleAmount, 0f, Space.World);
        }

        /// <summary>
        /// Rotate towards the direction immediately
        /// </summary>
        /// <param name="direction"></param>
        public void FlatInstantRotateTowards(Vector3 direction)
        {
            Vector3 flatDir = new(direction.x, 0f, direction.z);
            if (flatDir == Vector3.zero) return;

            Vector3 flatFwd = new(transform.forward.x, 0f, transform.forward.z);
            if (flatFwd == Vector3.zero) return;

            flatDir.Normalize();
            flatFwd.Normalize();

            float angleNeeded = Vector3.SignedAngle(flatFwd, flatDir, Vector3.up);
            transform.Rotate(0f, angleNeeded, 0f, Space.World);
        }

        /// <summary>
        /// Teleports to a position
        /// </summary>
        /// <param name="position"></param>
        public void Warp(Vector3 position)
        {
            bool originalEnabled = CharacterController.enabled;

            CharacterController.enabled = false;
            transform.position = position;
            CharacterController.enabled = originalEnabled;
        }

        public bool HasFlatDirections(out Vector3 flatForward, out Vector3 flatRight)
        {
            flatForward = Vector3.zero;
            flatRight = Vector3.zero;

            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if(fwd == Vector3.zero) return false;

            Vector3 right = transform.right;
            right.y = 0f;
            if(right == Vector3.zero) return false;

            flatForward = fwd.normalized;
            flatRight = right.normalized;

            return true;
        }

        private void UpdateGrounded()
        {
            float checkDistance = (CharacterController.height * 0.5f) + (CharacterController.skinWidth * 2f);

            RaycastHit[] groundHits = new RaycastHit[64];
            int hitCount = Physics.SphereCastNonAlloc(CenterPosition, CharacterController.radius, Vector3.down, groundHits, checkDistance, GroundLayers, QueryTriggerInteraction.Ignore);

            if(hitCount <= 0)
            {
                _isFullyGrounded = false;
                return;
            }

            List<RaycastHit> validHits = new();
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = groundHits[i];
                if (h.transform == null) continue;
                if (h.transform == transform) continue;
                if (h.transform.IsChildOf(transform)) continue;
                if (h.point == Vector3.zero) continue;
                if (h.normal == Vector3.zero) continue;

                validHits.Add(h);
            }

            if (validHits.Count <= 0)
            {
                _isFullyGrounded = false;
                return;
            }

            RaycastHit highestGroundHit = validHits[0];
            for (int i = 1; i < validHits.Count; i++)
            {
                if (validHits[i].point.y > highestGroundHit.point.y)
                    highestGroundHit = validHits[i];
            }

            _isFullyGrounded = true;
            _groundHitPoint = highestGroundHit;

            DebugExtension.DebugPoint(_groundHitPoint.point, Color.magenta, 0.25f);
            DebugExtension.DebugArrow(_groundHitPoint.point, _groundHitPoint.normal * 0.35f, Color.magenta);
            
        }

        private float GetCurrentVerticalVelocity(float deltaTime)
        {
            if (GravityMultiplier <= 0f) return 0f;

            if (_jumpSpeed > 0f)
            {
                return _jumpSpeed;
            }

            float curGravityVelocity = deltaTime * Physics.gravity.y * GravityMultiplier;
            float curVertSpeed = PreviousVerticalVelocity + curGravityVelocity;
            
            // If was previously falling, check if grounded yet
            if(PreviousVerticalVelocity < 0f)
            {
                if(HasFullyGrounded(out _) && CharacterController.isGrounded)
                {
                    curVertSpeed = 0f;
                }
            }

            return curVertSpeed;
        }

        private Vector3 GetGradientAffectedMotion(Vector3 motion)
        {
            if(!FollowTerrainGradient) return motion;
            if(motion ==  Vector3.zero) return motion;

            // Only runs when fully grounded
            if(!HasFullyGrounded(out RaycastHit groundHit)) return motion;

            if(groundHit.normal == Vector3.zero) return motion;

            // No calculation when is currently above the slope limit
            if (Vector3.Angle(groundHit.normal, Vector3.up) >= FollowSlopeLimit) return motion;

            Vector3 projectedMotion = Vector3.ProjectOnPlane(motion, groundHit.normal);
            if (projectedMotion == Vector3.zero) return motion;

            // Motion is going uphill, clamp the motion
            if (projectedMotion.y >= 0f)
            {
                return Vector3.ClampMagnitude(motion, projectedMotion.magnitude);
            }

            return projectedMotion.normalized * motion.magnitude;
        }
    }
}
