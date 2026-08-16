#nullable enable

namespace AutoNether.Services;

internal sealed class NetherStackMechanismValuation
{
    public NetherMechanismValue Evaluate(NetherStackLinkedPayoffInput input)
    {
        if (input == null || input.Recipients == null || input.ValuePerStack < 0)
            return NetherMechanismValue.Missing("stack-linked-input-unavailable");
        if (!input.TriggerKnown)
            return NetherMechanismValue.Missing("stack-trigger-unavailable");
        if (!input.TriggerReachable)
            return NetherMechanismValue.Quantified(
                NetherMechanismQuantityKind.GuaranteedStackPayoff,
                0,
                "stack-trigger-unreachable"
            );

        decimal total = 0;
        foreach (NetherStackLinkedRecipient recipient in input.Recipients)
        {
            if (recipient == null
                || recipient.CharacterId <= 0
                || recipient.LiveStackCount < 0
                || recipient.GuaranteedLowerBound < 0)
            {
                return NetherMechanismValue.Missing("stack-recipient-unavailable");
            }
            if (recipient.GuaranteedLowerBoundKnown)
            {
                total += recipient.GuaranteedLowerBound * input.ValuePerStack;
                continue;
            }
            return NetherMechanismValue.ReachableUnquantified(
                "stack-timeline-or-lower-bound-unavailable"
            );
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.GuaranteedStackPayoff,
            total,
            "native-guaranteed-stack-lower-bound"
        );
    }
}
