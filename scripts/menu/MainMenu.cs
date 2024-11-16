using Godot;
using NinthLife.scripts.game;
using NinthLife.scripts.utils;

namespace NinthLife.scripts.menu;

public partial class MainMenu : Control
{
    private static readonly bool ShouldLog = true;

    private readonly Vector2 _drinkerPos = new(210, 248);
    private readonly Vector2 _goblinPos = new(890, 248);
    private readonly Vector2 _goblinThreePos = new(750, 248);
    private readonly Vector2 _goblinTwoPos = new(820, 248);
    private readonly Vector2 _hopePos = new(50, 248);
    private readonly Vector2 _skullPos = new(140, 248);
    private SuitSelection _armorSelection;
    private Button _continueButton;
    private Player _drinker;
    private Player _goblin;
    private Player _goblinThree;
    private Player _goblinTwo;
    private TextureButton _heavyArmorButton;
    private Player _hope;
    private TextureButton _lightArmorButton;
    private TextureButton _longbladeButton;
    private TextureButton _maceButton;
    private AudioStreamPlayer2D _musicPlayer;
    private TextureButton _shortbladeButton;
    private Player _skull;
    private SuitSelection _weaponSelection;

    public override void _Ready()
    {
        Logger.Debug("Initializing main menu", ShouldLog);
        InitializeMenu();
    }

    private void InitializeMenu()
    {
        InitNodes();
        _continueButton.Disabled = true;
        SetupSuitToggles();
        WatchSuitSelections();
        SetupEventHandlers();
        StartBackgroundMusic();
    }

    private void InitNodes()
    {
        Logger.Debug("Setting up menu nodes", ShouldLog);
        InitializeSuitSelections();
        InitializeWeaponButtons();
        InitializeArmorButtons();
        InitializeOtherControls();
    }

    private void InitializeSuitSelections()
    {
        _weaponSelection = GetNode<SuitSelection>("WeaponSelection");
        _armorSelection = GetNode<SuitSelection>("ArmorSelection");
    }

    private void InitializeWeaponButtons()
    {
        _longbladeButton = GetNode<TextureButton>("Longblade");
        _shortbladeButton = GetNode<TextureButton>("Shortblade");
        _maceButton = GetNode<TextureButton>("Mace");
    }

    private void InitializeArmorButtons()
    {
        _lightArmorButton = GetNode<TextureButton>("LightArmor");
        _heavyArmorButton = GetNode<TextureButton>("HeavyArmor");
    }

    private void InitializeOtherControls()
    {
        _continueButton = GetNode<Button>("ContinueButton");
        _musicPlayer = GetNode<AudioStreamPlayer2D>("MusicPlayer");
    }

    private void SetupEventHandlers()
    {
        Logger.Debug("Setting up event handlers", ShouldLog);
        _continueButton.Pressed += OnPlayGamePressed;
    }

    private void StartBackgroundMusic()
    {
        Logger.Debug("Starting background music", ShouldLog);
        // _musicPlayer?.Play();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Logger.Debug("Escape key pressed, quitting game", ShouldLog);
            CleanupAndQuit();
        }
    }

    private void CleanupAndQuit()
    {
        StopBackgroundMusic();
        GetTree().Quit();
    }

    private void StopBackgroundMusic()
    {
        if (_musicPlayer != null)
        {
            Logger.Debug("Stopping background music", ShouldLog);
            _musicPlayer.Stop();
        }
    }

    private void SetupSuitToggles()
    {
        Logger.Debug("Setting up suit toggles", ShouldLog);
        SetupWeaponToggles();
        SetupArmorToggles();
    }

    private void SetupWeaponToggles()
    {
        SetupLongbladeToggle();
        SetupShortbladeToggle();
        SetupMaceToggle();
    }

    private void SetupLongbladeToggle()
    {
        _longbladeButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "longblade")
            {
                Logger.Debug("Deselecting longblade", ShouldLog);
                UpdateSelectedSuit("", _weaponSelection);
                return;
            }

            if (pressed)
            {
                Logger.Debug("Selecting longblade", ShouldLog);
                UpdateSelectedSuit("longblade", _weaponSelection);
                _shortbladeButton.ButtonPressed = false;
                _maceButton.ButtonPressed = false;
            }
        };
    }

    private void SetupShortbladeToggle()
    {
        _shortbladeButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "shortblade")
            {
                Logger.Debug("Deselecting shortblade", ShouldLog);
                UpdateSelectedSuit("", _weaponSelection);
                return;
            }

            if (pressed)
            {
                Logger.Debug("Selecting shortblade", ShouldLog);
                UpdateSelectedSuit("shortblade", _weaponSelection);
                _longbladeButton.ButtonPressed = false;
                _maceButton.ButtonPressed = false;
            }
        };
    }

    private void SetupMaceToggle()
    {
        _maceButton.Toggled += pressed =>
        {
            if (!pressed && _weaponSelection.SelectedSuit == "mace")
            {
                Logger.Debug("Deselecting mace", ShouldLog);
                UpdateSelectedSuit("", _weaponSelection);
                return;
            }

            if (pressed)
            {
                Logger.Debug("Selecting mace", ShouldLog);
                UpdateSelectedSuit("mace", _weaponSelection);
                _shortbladeButton.ButtonPressed = false;
                _longbladeButton.ButtonPressed = false;
            }
        };
    }

    private void SetupArmorToggles()
    {
        SetupLightArmorToggle();
        SetupHeavyArmorToggle();
    }

    private void SetupLightArmorToggle()
    {
        _lightArmorButton.Toggled += pressed =>
        {
            if (!pressed && _armorSelection.SelectedSuit == "lightarmor")
            {
                Logger.Debug("Deselecting light armor", ShouldLog);
                UpdateSelectedSuit("", _armorSelection);
                return;
            }

            if (pressed)
            {
                Logger.Debug("Selecting light armor", ShouldLog);
                UpdateSelectedSuit("lightarmor", _armorSelection);
                _heavyArmorButton.ButtonPressed = false;
            }
        };
    }

    private void SetupHeavyArmorToggle()
    {
        _heavyArmorButton.Toggled += pressed =>
        {
            if (!pressed && _armorSelection.SelectedSuit == "heavyarmor")
            {
                Logger.Debug("Deselecting heavy armor", ShouldLog);
                UpdateSelectedSuit("", _armorSelection);
                return;
            }

            if (pressed)
            {
                Logger.Debug("Selecting heavy armor", ShouldLog);
                UpdateSelectedSuit("heavyarmor", _armorSelection);
                _lightArmorButton.ButtonPressed = false;
            }
        };
    }

    private void WatchSuitSelections()
    {
        Logger.Debug("Setting up suit selection watchers", ShouldLog);
        _weaponSelection.SuitSelected += _ => UpdateContinueButtonState();
        _armorSelection.SuitSelected += _ => UpdateContinueButtonState();
    }

    private void UpdateContinueButtonState()
    {
        bool shouldBeDisabled = string.IsNullOrEmpty(_armorSelection.SelectedSuit) ||
                                string.IsNullOrEmpty(_weaponSelection.SelectedSuit);
        Logger.Debug($"Updating continue button state: {(shouldBeDisabled ? "disabled" : "enabled")}", ShouldLog);
        _continueButton.Disabled = shouldBeDisabled;
    }

    private static void UpdateSelectedSuit(string suitName, SuitSelection suitSelection)
    {
        Logger.Debug($"Updating selected suit to: {(string.IsNullOrEmpty(suitName) ? "none" : suitName)}", ShouldLog);
        suitSelection.SetSelectedSuit(suitName);
    }

    private void OnPlayGamePressed()
    {
        Logger.Debug("Starting game setup", ShouldLog);
        CreateCharacters();
        SetupPlayerDecks();
        AddCharactersToGame();
        StartGame();
    }

    private void CreateCharacters()
    {
        Logger.Debug("Creating characters", ShouldLog);
        CreateAllies();
        CreateEnemies();
    }

    private void CreateAllies()
    {
        _skull = InstantiateAlly("Skull", "res://assets/Character Sprites/Skull/skull_sprite.png", _skullPos);
        _hope = InstantiateAlly("Hope", "res://assets/Character Sprites/Hope/hope_sprite.png", _hopePos);
        _drinker = InstantiateAlly("Drinker", "res://assets/Character Sprites/Drinker/drinker_sprite.png", _drinkerPos);
    }

    private void CreateEnemies()
    {
        _goblin = InstantiateEnemy("Goblin", "res://assets/Character Sprites/enemy/goblin/goblin_sprite_rs.png",
            _goblinPos);
        _goblinTwo = InstantiateEnemy("Goblin 2", "res://assets/Character Sprites/enemy/goblin/goblin_sprite_rs_2.png",
            _goblinTwoPos);
        _goblinThree = InstantiateEnemy("Goblin 3",
            "res://assets/Character Sprites/enemy/goblin/goblin_sprite_rs_3.png", _goblinThreePos);
    }

    private Player InstantiateAlly(string name, string texturePath, Vector2 position)
    {
        Logger.Debug($"Creating ally: {name}", ShouldLog);
        return GameUtils.InstantiateCombatEntity(
            name,
            ResourceManager.Load<Texture>(texturePath),
            position,
            true
        );
    }

    private Player InstantiateEnemy(string name, string texturePath, Vector2 position)
    {
        Logger.Debug($"Creating enemy: {name}", ShouldLog);
        return GameUtils.InstantiateCombatEntity(
            name,
            ResourceManager.Load<Texture>(texturePath),
            position
        );
    }

    private void SetupPlayerDecks()
    {
        Logger.Debug("Setting up player decks", ShouldLog);
        SetupSkullDeck();
        SetupOtherPlayerDecks();
    }

    private void SetupSkullDeck()
    {
        Logger.Debug("Setting up Skull's deck", ShouldLog);
        string[] suitsList = { "suns", "cures", _weaponSelection.SelectedSuit, _armorSelection.SelectedSuit };
        foreach (string suit in suitsList)
        {
            AddSuitToPlayer(_skull, suit);
        }

        FinalizePlayerDeck(_skull);
    }

    private void AddSuitToPlayer(Player player, string suit)
    {
        if (CardLibrary.Suits.TryGetValue(suit, out CardLibrary.CardData suitData))
        {
            Logger.Debug($"Adding {suit} of type {suitData.Type} cards to {player.PlayerName}'s deck", ShouldLog);
            if (suitData.Type == "weapon")
            {
                player.SetWeaponType(suit);
            }

            foreach (Card card in suitData.Cards)
            {
                player.Deck.Add(card.DuplicateCard());
            }
        }
    }

    private void SetupOtherPlayerDecks()
    {
        SetupHopeDeck();
        SetupDrinkerDeck();
        SetupGoblinDecks();
    }

    private void SetupHopeDeck()
    {
        Logger.Debug("Setting up Hope's deck", ShouldLog);
        AddPresetDeckToPlayer(_hope, new[] { "wax", "wicks", "longblade", "lightarmor" });
    }

    private void SetupDrinkerDeck()
    {
        Logger.Debug("Setting up Drinker's deck", ShouldLog);
        AddPresetDeckToPlayer(_drinker, new[] { "oracles", "paths", "shortblade", "lightarmor" });
    }

    private void SetupGoblinDecks()
    {
        Logger.Debug("Setting up Goblin decks", ShouldLog);
        AddPresetDeckToPlayer(_goblin, new[] { "lions", "standards", "longblade", "lightarmor" });
        AddPresetDeckToPlayer(_goblinTwo, new[] { "shadows", "venom", "shortblade", "lightarmor" });
        AddPresetDeckToPlayer(_goblinThree, new[] { "suns", "cures", "mace", "heavyarmor" });
    }

    private void AddPresetDeckToPlayer(Player player, string[] suits)
    {
        foreach (string suit in suits)
        {
            AddSuitToPlayer(player, suit);
        }

        FinalizePlayerDeck(player);
    }

    private void FinalizePlayerDeck(Player player)
    {
        Logger.Debug($"Finalizing deck for {player.PlayerName}", ShouldLog);
        player.ShuffleDeck(4);
        player.CalculateInitiative();
    }

    private void AddCharactersToGame()
    {
        Logger.Debug("Adding characters to game", ShouldLog);
        GameUtils.CombatEntities.AddRange(new[] { _skull, _hope, _drinker, _goblin, _goblinTwo, _goblinThree });
    }

    private void StartGame()
    {
        Logger.Debug("Starting game", ShouldLog);
        GetTree().ChangeSceneToFile("res://scenes/game.tscn");
    }
}
