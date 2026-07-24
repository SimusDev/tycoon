using Godot;
using System;

[GlobalClass]
public partial class PlayerUI : Control
{
    [Export] public InventoryUIContainer InventoryUIContainer;
    [Export] public Label LabelSelection;
}
