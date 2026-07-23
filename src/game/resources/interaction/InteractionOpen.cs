using Godot;

namespace CustomInteraction
{
    public partial class InteractionOpen : Interaction
    {
        public override void Do(InteractionRay interactionRay, Interactable interactable)
        {
            base.Do(interactionRay, interactable);
            PlayerUI playerUI = interactionRay.Root.GetNodeOrNull<PlayerUI>("LocalCanvasLayer/PlayerUI");
            if (playerUI == null) return;

            Inventory inventory = interactionRay.Root.GetNodeOrNull<Inventory>("Inventory");
            if (inventory == null) return;
            
            playerUI.InventoryUIContainer.OpenOther(inventory);
        }
    }
}