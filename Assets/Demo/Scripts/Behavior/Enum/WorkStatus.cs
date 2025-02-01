using System;
using Unity.Behavior;

[BlackboardEnum]
public enum WorkStatus
{
    Idling,
	MovingToPickup,
	PickingUp,
	MovingToDeposit,
	Depositing
}
