using Kabir.CharacterComponents;
using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Kabir.ScriptableObjects;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Playables;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move to destination", story: "Moves [MotionController] and [Agent] to [Destination] at [Speed] .Will notify [IsJumping]", category: "Action", id: "56f5ea759237cba5a8159db4fcb175a6")]
public partial class MoveToDestinationAction : Action
{
    [SerializeReference] public BlackboardVariable<MotionController> MotionController;
    [SerializeReference] public BlackboardVariable<NavMeshAgent> Agent;
    [SerializeReference] public BlackboardVariable<Vector3> Destination;
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<bool> IsJumping;
    [SerializeReference] public BlackboardVariable<float> ArrivalDistance = new(1f);
    [SerializeReference] public BlackboardVariable<float> RotationSpeed = new(480f);

    [SerializeReference] public BlackboardVariable<PlayableManager> PlayableManager;
    [SerializeReference] public BlackboardVariable<AnimationBlends> AnimMotionBlends;
    [SerializeReference] public BlackboardVariable<AnimationClipList> JumpingAnimations, FallingAnimations, LandingAnimations;


    private Sequence _navRefreshSequence, _landingSequence, _jumpAnimSequence;
    private float _minDistance;

    private static readonly float _navRefreshInterval = 0.5f;
    private static readonly float _jumpGravityAcceleration = -9.81f * 3f;
    private enum State
    {
        NormalMove,
        Jump,
        Landing
    }

    private State _currentState;
    private bool _jumpStarted;
    private Vector3 _initialJumpVelocity, _startJumpPosition, _endJumpPosition;
    private float _jumpDuration, _currentJumpDuration;

    protected override Status OnStart()
    {
        if(MotionController.Value == null)
        {
            LogFailure($"Missing MotionController");
            return Status.Failure;
        }

        if(Agent.Value == null)
        {
            LogFailure($"Missing NavmeshAgent");
            return Status.Failure;
        }

        _minDistance = Mathf.Max(ArrivalDistance.Value, 0.5f);

        InitMotion();
        InitAnimation();
        KillLandingSequence();

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        float deltaTime = Time.deltaTime;

        UpdateCurrentJumpState();
        switch(_currentState)
        {
            case State.Jump: 
                UpdateJumping(deltaTime);
                UpdateJumpRotation();
                return Status.Running;
            
            case State.Landing: 
                UpdateLanding(deltaTime);
                UpdateJumpRotation();
                return Status.Running;
        }

        ResetJumpParameters();
        float curDistance = Vector3.Distance(MotionController.Value.transform.position, Destination.Value);
        if(curDistance < _minDistance)
        {
            if(PlayableManager.Value != null) PlayableManager.Value.SetBlendPosition(Vector2.zero, 10f);
            
            Agent.Value.Warp(MotionController.Value.transform.position);
            Agent.Value.SetDestination(MotionController.Value.transform.position);
            Agent.Value.isStopped = true;
            Agent.Value.ResetPath();
            Agent.Value.speed = 0f;

            return Status.Success;
        }

        UpdateMotion(deltaTime);
        UpdateAnimation();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        KillNavSequence();
        KillLandingSequence();
        KillJumpAnimSequence();
    }

    private void InitMotion()
    {
        MotionController.Value.AutoEvaluate = false;
        MotionController.Value.GravityMultiplier = 0f;
        MotionController.Value.FollowTerrainGradient = false;

        Agent.Value.Warp(MotionController.Value.transform.position);
        Agent.Value.autoTraverseOffMeshLink = false;
        Agent.Value.speed = Speed.Value;
        Agent.Value.updatePosition = false;
        Agent.Value.updateRotation = false;
        Agent.Value.ResetPath();
        Agent.Value.isStopped = false;

        Agent.Value.SetDestination(Destination.Value);

        KillNavSequence();
        _navRefreshSequence = DOTween.Sequence().AppendInterval(_navRefreshInterval).AppendCallback(RefreshDestination);

        ResetJumpParameters();
    }

    private void InitAnimation()
    {
        if (PlayableManager.Value == null) return;
        if (AnimMotionBlends.Value == null) return;

        PlayableManager.Value.StopSingleAnimation(0.25f);
        PlayableManager.Value.SetCurrentAnimationBlend(AnimMotionBlends.Value, 0.1f);
        PlayableManager.Value.SetBlendPosition(Vector2.zero);
    }

    private void RefreshDestination()
    {
        KillNavSequence();
        if (Agent.Value != null && _currentState == State.NormalMove)
        {
            //Agent.Value.Warp(MotionController.Value.transform.position);
            Agent.Value.isStopped = false;
            Agent.Value.SetDestination(Destination.Value);
        }

        _navRefreshSequence = DOTween.Sequence().AppendInterval(_navRefreshInterval).AppendCallback(RefreshDestination);
    }

    private void UpdateMotion(float deltaTime)
    {
        if (MotionController.Value == null) return;
        if (Agent.Value == null) return;

        Agent.Value.speed = Mathf.Max(0f, Speed.Value);
        Vector3 motion = Agent.Value.nextPosition - MotionController.Value.transform.position;

        MotionController.Value.EvaluateMotion(motion, deltaTime);
        if (motion != Vector3.zero) 
        {
            MotionController.Value.FlatClampedRotateTowards(motion.normalized, Mathf.Max(0f, RotationSpeed.Value));
        }        
    }

    private void UpdateAnimation()
    {
        if (MotionController.Value == null) return;
        if (PlayableManager.Value == null) return;

        if(!MotionController.Value.HasFlatDirections(out Vector3 flatFwd, out Vector3 flatRight) || Speed.Value <= 0f)
        {
            PlayableManager.Value.SetBlendPosition(Vector2.zero, 10f);
            return;
        }

        Vector3 normVelocity = MotionController.Value.CharacterController.velocity / Speed.Value;
        float fwd = Vector3.Dot(normVelocity, flatFwd.normalized);
        float right = Vector3.Dot(normVelocity, flatRight.normalized);

        PlayableManager.Value.SetBlendPosition(new(right, fwd), 10f);
    }

    private void KillNavSequence()
    {
        if (_navRefreshSequence == null) return;
        _navRefreshSequence.Kill();
        _navRefreshSequence = null;
    }

    private void KillLandingSequence()
    {
        if (_landingSequence == null) return;
        _landingSequence.Kill();
        _landingSequence = null;
    }

    private void KillJumpAnimSequence()
    {
        if (_jumpAnimSequence == null) return;
        _jumpAnimSequence.Kill();
        _jumpAnimSequence = null;
    }

    private void UpdateCurrentJumpState()
    {
        if (_currentState != State.NormalMove) return;
        if(Agent.Value.isOnOffMeshLink)
        {
            _currentState = State.Jump;
        }
    }

    private void ResetJumpParameters()
    {
        _currentState = State.NormalMove;
        _jumpStarted = false;
        _initialJumpVelocity = Vector3.zero;
        _jumpDuration = 0f;
        _currentJumpDuration = 0f;
    }

    private void UpdateJumping(float deltaTime)
    {
        if (PlayableManager.Value != null) PlayableManager.Value.SetBlendPosition(Vector2.zero, 10f);

        if (!_jumpStarted) InitJumping();
        if (_currentJumpDuration < _jumpDuration)
        {
            UpdateJumpMotion();
            _currentJumpDuration += deltaTime;
            return;
        }

        StartLanding();
    }

    private void UpdateLanding(float deltaTime)
    {
        if (PlayableManager.Value != null) PlayableManager.Value.SetBlendPosition(Vector2.zero, 10f);
    }

    private void InitJumping()
    {
        KillNavSequence();
        KillLandingSequence();

        OffMeshLinkData linkData = Agent.Value.currentOffMeshLinkData;
        
        _startJumpPosition = MotionController.Value.transform.position;
        _endJumpPosition = linkData.endPos;

        _initialJumpVelocity = GetJumpVelocity(_startJumpPosition, _endJumpPosition, out _jumpDuration);

        _jumpStarted = true;

        StartJumpingAnimation();
    }

    /// <summary>
    /// Uses "Angle \theta required to hit coordinate (x,y)" calculation from: https://ipfs.io/ipfs/QmXoypizjW3WknFiJnKLwHCnL72vedxjQkDDP1mXWo6uco/wiki/Trajectory_of_a_projectile.html
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private static Vector3 GetJumpVelocity(Vector3 startPos, Vector3 endPos, out float jumpDuration)
    {
        jumpDuration = 0f;
        Vector3 jumpDir = endPos - startPos;
        Vector2 horizontalDir = new(jumpDir.x, jumpDir.z);

        float y = jumpDir.y;
        float x = horizontalDir.magnitude;

        float xSq = x * x;

        float initialSpeed = 1f;
        float g = Mathf.Abs(_jumpGravityAcceleration);

        // Incrementally increase potential jumpspeed (forever). Potential for infinite loop error here
        for(float v = initialSpeed; true; v += 0.5f)
        {
            float vSq = v * v;
            float vQuad = vSq * vSq;
            float n = g * ((g * xSq) + (2 * y * vSq));
            float m = vQuad - n;

            if(m <= 0f) continue;

            float o = Mathf.Sqrt(m);

            float gravMultHorDisp = g * x;
            float val1 = (vSq + o) / gravMultHorDisp;
            float val2 = (vSq - o) / gravMultHorDisp;

            float radian1 = Mathf.Atan(val1);
            float radian2 = Mathf.Atan(val2);

            float time1 = x / (v * Mathf.Cos(radian1));
            float time2 = x / (v * Mathf.Cos(radian2));

            if (time1 <= 0f && time2 <= 0f) continue;

            if(time1 > time2 && time1 > 0f)
            {
                jumpDuration = time1;
                return GetFinalJumpVelocity(v, radian1, horizontalDir);
            }

            if(time2 > 0f)
            {
                jumpDuration = time2;
                return GetFinalJumpVelocity(v, radian2, horizontalDir);
            }
        }
    }

    private static Vector3 GetFinalJumpVelocity(float speed, float radian, Vector2 horizontalDir)
    {
        float vertical = Mathf.Sin(radian) * speed;
        Vector2 horizontalMotion = Vector2.zero;

        if(horizontalDir != Vector2.zero) 
            horizontalMotion = Mathf.Cos(radian) * speed * horizontalDir.normalized;

        return new Vector3(horizontalMotion.x, vertical, horizontalMotion.y);
    }

    private void UpdateJumpMotion()
    {
        Vector3 horizontalVelocity = new(_initialJumpVelocity.x, 0f, _initialJumpVelocity.z);
        Vector3 horizontalOffset = horizontalVelocity * _currentJumpDuration;

        float verticalVelocity = _initialJumpVelocity.y;
        float verticalOffset = (verticalVelocity * _currentJumpDuration) - (0.5f * Mathf.Abs(_jumpGravityAcceleration) * _currentJumpDuration * _currentJumpDuration);

        Vector3 netOffset = horizontalOffset + (Vector3.up * verticalOffset);
        Vector3 targetPos = _startJumpPosition + netOffset;

        Vector3 motion = targetPos - MotionController.Value.transform.position;
        MotionController.Value.CharacterController.Move(motion);
    }

    private void UpdateJumpRotation()
    {
        Vector3 jumpDir = _endJumpPosition - _startJumpPosition;
        jumpDir.y = 0f;
        if (jumpDir == Vector3.zero) return;

        MotionController.Value.FlatClampedRotateTowards(jumpDir.normalized, Mathf.Max(0f, RotationSpeed.Value));
    }
    
    private void StartLanding()
    {
        _currentState = State.Landing;
        MotionController.Value.Warp(_endJumpPosition);
        KillJumpAnimSequence();
        AnimationClip landingClip = GetAnimationClip(LandingAnimations.Value, out float clipSpeed);
        float landingDuration = 0.1f;
        if (landingClip != null)
        {
            landingDuration = landingClip.length / clipSpeed;
            if(PlayableManager.Value != null)
            {
                PlayableManager.Value.StartSingleAnimation(landingClip, 0.1f, clipSpeed);
            }
        }

        KillLandingSequence();
        _landingSequence = DOTween.Sequence().AppendInterval(landingDuration).AppendCallback(FinishLanding);
        
    }

    private void FinishLanding()
    {
        KillLandingSequence();
        KillJumpAnimSequence();

        if (PlayableManager.Value != null) PlayableManager.Value.StopSingleAnimation(0.25f);

        _currentState = State.NormalMove;
        InitMotion();
    }

    private void StartJumpingAnimation()
    {
        KillJumpAnimSequence();
        if (PlayableManager.Value == null) return;

        AnimationClip jumpClip = GetAnimationClip(JumpingAnimations.Value, out float jumpClipSpeed);
        if(jumpClip ==null)
        {
            StartFallingAnimation();
            return;
        }

        float jumpClipDuration = jumpClip.length / jumpClipSpeed;
        jumpClip.wrapMode = WrapMode.ClampForever;

        PlayableManager.Value.StartSingleAnimation(jumpClip, 0.1f, jumpClipSpeed);
        _jumpAnimSequence = DOTween.Sequence().AppendInterval(jumpClipDuration).AppendCallback(StartFallingAnimation);
    }

    private void StartFallingAnimation()
    {
        KillJumpAnimSequence();
        if (PlayableManager.Value == null) return;
        AnimationClip clip = GetAnimationClip(FallingAnimations.Value, out float clipSpeed);
        if(clip == null)
        {
            PlayableManager.Value.StopSingleAnimation(0.1f);
            return;
        }

        clip.wrapMode = WrapMode.Loop;
        PlayableManager.Value.StartSingleAnimation(clip, 0.1f, clipSpeed);
    }

    

    private static AnimationClip GetAnimationClip(AnimationClipList clipList, out float clipSpeed)
    {
        clipSpeed = 1f;
        if (clipList == null) return null;

        Dictionary<AnimationClip, float> clipWSpeeds = clipList.GetAnimationList();
        List<AnimationClip> potentialClips = new(clipWSpeeds.Keys);
        potentialClips.RemoveAll(c => c == null);

        while (potentialClips.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, potentialClips.Count);
            AnimationClip clip = potentialClips[randomIndex];

            float speed = clipWSpeeds[clip];
            if (speed > 0f)
            {
                clipSpeed = speed;
                return clip;
            }

            potentialClips.RemoveAt(randomIndex);
        }

        return null;
    }
}

