using System.Collections.Generic;
using Godot;
using NinthLife.scripts.game;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.main_menu;

public partial class MainMenu : Control
{
    private readonly Vector2 _goblinPos = new(863, 162);
    private readonly Vector2 _hopePos = new(60, 150);
    private readonly Vector2 _skullPos = new(130, 160);
    private SuitSelection _armorSelection;
    private Button _continueButton;
    private Player _goblin;
    private TextureButton _heavyArmorButton;
    private Player _hope;
    private TextureButton _lightArmorButton;
    private TextureButton _longbladeButton;
    private TextureButton _maceButton;
    private TextureButton _shortbladeButton;
    private Player _skull;
    private SuitSelection _weaponSelection;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) GetTree().Quit();
    }

    public override void _Ready()
    {
        InitNodes();

        _continueButton.Disabled = true;

        SetupSuitToggles();

        WatchSuitSelections();

        _continueButton.Pressed += OnPlayGamePressed;
    }

    private void InitNodes()
    {
        _weaponSelection = GetNode<SuitSelection>("WeaponSelection");
        _armorSelection = GetNode<SuitSelection>("ArmorSelection");

        _longbladeButton = GetNode<TextureButton>("Longblade");
        _shortbladeButton = GetNode<TextureButton>("Shortblade");
        _maceButton = GetNode<TextureButton>("Mace");

        _lightArmorButton = GetNode<TextureButton>("LightArmor");
        _heavyArmorButton = GetNode<TextureButton>("HeavyArmor");

        _continueButton = GetNode<Button>("ContinueButton");
    }

    private void SetupSuitToggles()
    {
        _longbladeButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "longblade")
                UpdateSelectedSuit("", _weaponSelection);
            if (!pressed) return;
            UpdateSelectedSuit("longblade", _weaponSelection);
            _shortbladeButton.ButtonPressed = false;
            _maceButton.ButtonPressed = false;
        };
        _shortbladeButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "shortblade")
                UpdateSelectedSuit("", _weaponSelection);
            if (!pressed) return;
            UpdateSelectedSuit("shortblade", _weaponSelection);
            _longbladeButton.ButtonPressed = false;
            _maceButton.ButtonPressed = false;
        };
        _maceButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "mace")
                UpdateSelectedSuit("", _weaponSelection);
            if (!pressed) return;
            UpdateSelectedSuit("mace", _weaponSelection);
            _shortbladeButton.ButtonPressed = false;
            _longbladeButton.ButtonPressed = false;
        };

        _lightArmorButton.Toggled += pressed =>
        {
            if (!pressed && _armorSelection.SelectedSuit == "lightarmor")
                UpdateSelectedSuit("", _armorSelection);
            if (!pressed) return;
            UpdateSelectedSuit("lightarmor", _armorSelection);
            _heavyArmorButton.ButtonPressed = false;
        };
        _heavyArmorButton.Toggled += pressed =>
        {
            if (!pressed && _armorSelection.SelectedSuit == "heavyarmor")
                UpdateSelectedSuit("", _armorSelection);
            if (!pressed) return;
            UpdateSelectedSuit("heavyarmor", _armorSelection);
            _lightArmorButton.ButtonPressed = false;
        };
    }

    private void WatchSuitSelections()
    {
        _weaponSelection.SuitSelected += _ =>
        {
            if (!string.IsNullOrEmpty(_armorSelection.SelectedSuit) &&
                !string.IsNullOrEmpty(_weaponSelection.SelectedSuit))
                _continueButton.Disabled = false;
            else
                _continueButton.Disabled = true;
        };
        _armorSelection.SuitSelected += _ =>
        {
            if (!string.IsNullOrEmpty(_armorSelection.SelectedSuit) &&
                !string.IsNullOrEmpty(_weaponSelection.SelectedSuit))
                _continueButton.Disabled = false;
            else
                _continueButton.Disabled = true;
        };
    }

    private static void UpdateSelectedSuit(string suitName, SuitSelection suitSelection)
    {
        suitSelection.SetSelectedSuit(suitName);
    }

    private void OnPlayGamePressed()
    {
        _skull = GameUtils.InstantiateCombatEntity(
            "skull",
            GD.Load<Texture>("res://assets/Character Sprites/Skull/skull_sprite.png"),
            _skullPos,
            true
        );

        _hope = GameUtils.InstantiateCombatEntity(
            "hope",
            GD.Load<Texture>("res://assets/Character Sprites/Hope/hope_sprite.png"),
            _hopePos,
            true,
            1
        );

        _goblin = GameUtils.InstantiateCombatEntity(
            "goblin",
            GD.Load<Texture>("res://assets/Character Sprites/enemy/goblin/goblin_sprite.png"),
            _goblinPos
        );

        object[] suitsList = { "suns", "cures", _weaponSelection.SelectedSuit, _armorSelection.SelectedSuit };
        foreach (var suite in suitsList)
            switch (suite)
            {
                case "heavyarmor":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["heavyarmor"].Cards);
                    break;
                case "lightarmor":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["lightarmor"].Cards);
                    break;
                case "longblade":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["longblade"].Cards);
                    break;
                case "shortblade":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["shortblade"].Cards);
                    break;
                case "mace":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["mace"].Cards);
                    break;
                case "suns":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["suns"].Cards);
                    break;
                case "cures":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["cures"].Cards);
                    break;
                case "shadows":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["shadows"].Cards);
                    break;
                case "venom":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["venom"].Cards);
                    break;
                case "oracles":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["oracles"].Cards);
                    break;
                case "paths":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["paths"].Cards);
                    break;
                case "lions":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["lions"].Cards);
                    break;
                case "standards":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["standards"].Cards);
                    break;
                case "wicks":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["wicks"].Cards);
                    break;
                case "wax":
                    AddCardsToPlayerDeck(_skull, CardLibrary.Suits["wax"].Cards);
                    break;
            }

        _skull.ShuffleDeck(4);
        _skull.CalculateInitiative();

        AddCardsToPlayerDeck(_hope, CardLibrary.Suits["wax"].Cards);
        AddCardsToPlayerDeck(_hope, CardLibrary.Suits["wicks"].Cards);
        AddCardsToPlayerDeck(_hope, CardLibrary.Suits["longblade"].Cards);
        AddCardsToPlayerDeck(_hope, CardLibrary.Suits["lightarmor"].Cards);

        _hope.ShuffleDeck(4);
        _hope.CalculateInitiative();

        AddCardsToPlayerDeck(_goblin, CardLibrary.Suits["lions"].Cards);
        AddCardsToPlayerDeck(_goblin, CardLibrary.Suits["standards"].Cards);
        AddCardsToPlayerDeck(_goblin, CardLibrary.Suits["mace"].Cards);
        AddCardsToPlayerDeck(_goblin, CardLibrary.Suits["heavyarmor"].Cards);

        _goblin.ShuffleDeck(4);
        _goblin.CalculateInitiative();

        GD.Print("skull deck size ", _skull.Deck.Count);
        GameUtils.CombatEntities.Add(_skull);
        GameUtils.CombatEntities.Add(_hope);
        GameUtils.CombatEntities.Add(_goblin);

        GetTree().ChangeSceneToFile("res://scenes/game.tscn");
    }

    private static void AddCardsToPlayerDeck(Player player, List<Card> cards)
    {
        foreach (var card in cards) player.Deck.Add((Card)card.Duplicate());
    }
}