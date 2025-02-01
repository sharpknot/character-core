using Kabir.CharacterComponents;
using Kabir.ScriptableObjects;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;
using DG.Tweening;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Play AnimationList", story: "[PlayableManager] plays [AnimationList]", category: "Action", id: "32aca5bf3c32d6e7b3bed4f33e640113")]
public partial class PlayAnimationListAction : Action
{
    [SerializeReference] public BlackboardVariable<PlayableManager> PlayableManager;
    [SerializeReference] public BlackboardVariable<AnimationClipList> AnimationList;

    [SerializeReference] public BlackboardVariable<MotionController> MotionController;
    [SerializeReference] public BlackboardVariable<bool> UseRootMotion, Loop, CompleteOnEndClip;

    private Sequence _clipSequence;
    private bool _clipComplete;

    protected override Status OnStart()
    {
        if(PlayableManager.Value == null)
        {
            LogFailure("PlayableManager missing!");
            return Status.Failure;
        }

        if(UseRootMotion.Value && MotionController.Value != null)
        {
            PlayableManager.Value.OnAnimatorMoveUpdate += OnAnimatorUpdate;
        }

        AnimationClip clip = GetAnimationClip(out float clipSpeed);
        if(clip == null)
        {
            if (CompleteOnEndClip.Value) return Status.Success;

            LogFailure($"PlayAnimationListAction ({PlayableManager.Value.gameObject.name}): Unable to find random clip!");
            return Status.Failure;
        }

        if (Loop.Value)
            clip.wrapMode = WrapMode.ClampForever;
        else clip.wrapMode = WrapMode.Loop;

        PlayableManager.Value.StartSingleAnimation(clip, 0.25f, clipSpeed);

        KillClipSequence();
        _clipComplete = false;
        if (CompleteOnEndClip.Value)
        {
            float duration = clip.length / clipSpeed;
            _clipSequence = DOTween.Sequence().AppendInterval(duration).AppendCallback(() => { KillClipSequence(); _clipComplete = true; });
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(_clipComplete) return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (PlayableManager.Value != null)
        {
            PlayableManager.Value.OnAnimatorMoveUpdate -= OnAnimatorUpdate;
            //PlayableManager.Value.StopSingleAnimation(0.1f);
        }

        KillClipSequence();
    }

    private AnimationClip GetAnimationClip(out float clipSpeed)
    {
        clipSpeed = 1f;
        if(AnimationList.Value == null) return null;

        Dictionary<AnimationClip, float> clips = AnimationList.Value.GetAnimationList();
        List<AnimationClip> list = new(clips.Keys);
        list.RemoveAll(c => c == null);

        if(list.Count == 0) return null;

        AnimationClip chosenClip = list[UnityEngine.Random.Range(0, clips.Count)];
        clipSpeed = Mathf.Max(0.01f, clips[chosenClip]);

        return chosenClip;
    }

    private void OnAnimatorUpdate(Vector3 deltaPos, Quaternion deltaRot)
    {
        if (MotionController.Value == null) return;
        MotionController.Value.CharacterController.Move(deltaPos);
    }

    private void KillClipSequence()
    {
        if (_clipSequence == null) return;
        _clipSequence.Kill();
        _clipSequence = null;
    }
}

