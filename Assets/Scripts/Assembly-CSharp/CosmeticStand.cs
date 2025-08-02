using GorillaNetworking;
using UnityEngine.UI;

public class CosmeticStand : GorillaPressableButton
{
	public CosmeticsController.CosmeticItem thisCosmeticItem;

	public string thisCosmeticName;

	public HeadModel thisHeadModel;

	public Text slotPriceText;

	public Text addToCartText;

	public bool skipMe;

	public void InitializeCosmetic()
	{
		thisCosmeticItem = CosmeticsController.instance.allCosmetics.Find((CosmeticsController.CosmeticItem x) => thisCosmeticName == x.displayName);
		slotPriceText.text = thisCosmeticItem.itemCategory.ToString().ToUpper() + " " + thisCosmeticItem.cost;
	}

	public override void ButtonActivation()
	{
		base.ButtonActivation();
		CosmeticsController.instance.PressCosmeticStandButton(this);
	}
}
