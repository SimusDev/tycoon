using Godot;

namespace CustomInteraction
{
    public partial class InteractionOpen : Interaction
    {
        public override void Do(InteractionRay interactionRay, Interactable interactable, GodotObject collider)
        {
            base.Do(interactionRay, interactable, collider);
            PlayerUI playerUI = interactionRay.Root.GetNodeOrNull<PlayerUI>("LocalCanvasLayer/PlayerUI");
            if (playerUI == null) return;

            if (collider is Node node)
            {
                Inventory inventory = node.GetNodeOrNull<Inventory>("Inventory");
                if (inventory == null) return;
                
                playerUI.InventoryUIContainer.OpenOther(inventory);
            }
        }
    }
}