#nullable enable

namespace AutoNether.Services;

internal sealed class NetherCrestMechanismValuation
{
    public NetherMechanismValue Evaluate(NetherCrestPayoffInput input)
    {
        if (input == null || input.Recipients == null || input.ValuePerRecipient < 0)
            return NetherMechanismValue.Missing("crest-payoff-input-unavailable");

        decimal total = 0;
        foreach (NetherCrestPayoffRecipient recipient in input.Recipients)
        {
            if (recipient == null || recipient.CharacterId <= 0)
                return NetherMechanismValue.Missing("crest-recipient-unavailable");
            if (!recipient.ProviderPathKnown)
                return NetherMechanismValue.Missing("crest-provider-path-unavailable");
            if (!recipient.ConsumerPathKnown)
                return NetherMechanismValue.Missing("crest-consumer-path-unavailable");
            if (recipient.ProviderReachable && recipient.ConsumerReachable)
                total += input.ValuePerRecipient;
        }
        return NetherMechanismValue.Quantified(
            NetherMechanismQuantityKind.CrestRecipientPayoff,
            total,
            "explicit-crest-provider-consumer-paths"
        );
    }
}
