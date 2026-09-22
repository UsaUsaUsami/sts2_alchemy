using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace Alchemist;

/// The rewards-screen entry for one material slot.
///
/// The offer itself lives in AlchemyState, not in this object: the game only serializes its own reward
/// types, and BaseLib strips unknown ones when a room is restored. Keeping the slot in the relic's saved
/// state means a save taken on the rewards screen still restores the choice, and the map travel guard
/// presents it again before the next room. This object is only the button.
public sealed class MaterialReward(Player player, MaterialBox box, string offerId) : Reward(player)
{
    public string OfferId => offerId;

    // No RewardType fits a modded reward; None is the value BaseLib already expects to drop on load.
    protected override RewardType RewardType => MegaCrit.Sts2.Core.Rewards.RewardType.None;

    // Gold 1, potion 2, relic 3, card 5: the material slot sits between the relic and the card choice.
    public override int RewardsSetIndex => 4;

    public override LocString Description =>
        new("relics", $"{ModelDb.Relic<MaterialBox>().Id.Entry}.{MaterialBox.RewardLocKey}");

    // Candidates are rolled when the slot is recorded in AlchemyState, so there is nothing to populate.
    public override bool IsPopulated => true;
    public override void Populate() { }
    public override void MarkContentAsSeen() { }

    protected override string IconPath => MaterialBox.RewardIconPath;

    protected override Task<bool> OnSelect() => WorkshopUi.ChooseOffer(box, offerId);
}
