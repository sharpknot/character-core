using Kabir.CharacterComponents;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.Animations;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Run carrying animation", story: "[Playable] plays holding [animation] at [speed]", category: "Action", id: "b99c920146cf6122973b21413f363db8")]
public partial class RunCarryingAnimationAction : Action
{
    [SerializeReference] public BlackboardVariable<PlayableManager> Playable;
    [SerializeReference] public BlackboardVariable<AnimationClip> Animation;
    [SerializeReference] public BlackboardVariable<float> Speed;
    [SerializeReference] public BlackboardVariable<AvatarMask> Mask;

    private AnimationMixerPlayable _resultMixer;

    protected override Status OnStart()
    {
        Animation.Value.wrapMode = WrapMode.Loop;
        _resultMixer = Playable.Value.AddMaskedPlayable(Animation.Value, Mask.Value, false, 0.1f, out _);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Playable.Value != null)
        {
            Playable.Value.RemoveMaskedPlayable(_resultMixer, 0.1f);
        }
    }
}

