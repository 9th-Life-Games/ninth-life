using Godot;

namespace NinthLife.scripts.main_menu;

[GlobalClass]
public partial class SuitSelection : Node
{
    // Signal to indicate that the user has selected a suit for the weapon
    [Signal]
    public delegate void SuitSelectedEventHandler(string suitName);

    public string SelectedSuit { get; private set; }

    // Method to update the selected suit and emit the signal
    public void SetSelectedSuit(string suitName)
    {
        SelectedSuit = suitName;
        EmitSignal(SignalName.SuitSelected, SelectedSuit);
    }
}