using Godot;

namespace NinthLife.scripts.menu;

[GlobalClass]
public partial class SuitSelection : Node
{
    [Signal]
    public delegate void SuitSelectedEventHandler(string suitName);

    public string SelectedSuit { get; private set; }

    public void SetSelectedSuit(string suitName)
    {
        SelectedSuit = suitName;
        _ = EmitSignal(SignalName.SuitSelected, SelectedSuit);
    }
}
