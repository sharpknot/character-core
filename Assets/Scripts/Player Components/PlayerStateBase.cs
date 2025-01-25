using UnityEngine;

namespace Kabir.PlayerComponents
{
    /// <summary>
    /// Base class for a player state
    /// </summary>
    public class PlayerStateBase : ScriptableObject
    {
        protected PlayerStateManager StateManager { get; private set; }

        public virtual void StartState(PlayerStateManager stateManager)
        {
            StateManager = stateManager;
        }

        public virtual void UpdateState(float deltaTime) { }
        public virtual void AnimatorMove(Animator animator) { }
        public virtual void StopState() { }

        /// <summary>
        /// Converts player motion input to the flattened world direction
        /// </summary>
        /// <param name="inputForward"></param>
        /// <param name="inputRight"></param>
        /// <returns></returns>
        protected Vector3 GetWorldFlatInputClamped(float inputForward, float inputRight)
        {
            if(StateManager == null) return Vector3.zero;
            if(StateManager.CameraManager == null) return Vector3.zero;

            if(!StateManager.CameraManager.HasCameraFlatDirections(out Vector3 camFlatForward, out Vector3 cameraFlatRight))
            {
                return Vector3.zero;
            }

            float netFwd = Mathf.Clamp(inputForward, -1f, 1f);
            float netRight = Mathf.Clamp(inputRight, -1f, 1f);

            Vector3 worldInputFwd = camFlatForward.normalized * netFwd;
            Vector3 worldInputRight = cameraFlatRight.normalized * netRight;

            return Vector3.ClampMagnitude(worldInputFwd + worldInputRight, 1f);
        }

        /// <summary>
        /// Translates the player movement input to a flat motion value
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="maxSpeed"></param>
        /// <returns></returns>
        protected Vector3 GetNetMotion(float deltaTime, float maxSpeed)
        {
            if (StateManager == null) return Vector3.zero;
            if (StateManager.PlayerInput == null) return Vector3.zero;

            float dt = Mathf.Max(deltaTime, 0f);

            Vector2 playerInput = StateManager.PlayerInput.Primary.Movement.ReadValue<Vector2>();
            Vector3 worldFlatInput = GetWorldFlatInputClamped(playerInput.y, playerInput.x);

            DebugExtension.DebugArrow(StateManager.transform.position, worldFlatInput * 2f, Color.magenta);

            return Mathf.Max(maxSpeed, 0f) * dt * worldFlatInput;
        }

        /// <summary>
        /// Applies the motion to the motion controller (auto evaluate)
        /// </summary>
        /// <param name="motion"></param>
        protected void ApplyMotion(Vector3 motion)
        {
            if (StateManager == null) return;
            if (StateManager.MotionController == null) return;
            StateManager.MotionController.SetAutoEvaluateMotion(motion);
        }

        /// <summary>
        /// Rotates the player to the direction of motion, clamped at the max rotation speed
        /// </summary>
        /// <param name="motion"></param>
        /// <param name="maxRotationSpeed"></param>
        protected void UpdateRotation(Vector3 motion, float maxRotationSpeed)
        {
            if (motion == Vector3.zero) return;
            if (StateManager == null) return;
            if (StateManager.MotionController == null) return;

            StateManager.MotionController.FlatClampedRotateTowards(motion.normalized, Mathf.Max(maxRotationSpeed, 0f));
        }

    }
}
