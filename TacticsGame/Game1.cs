using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TacticsGame.Battle;
using TacticsGame.Campaign;
using TacticsGame.Grid;
using TacticsGame.Input;
using TacticsGame.Items;
using TacticsGame.Maps;
using TacticsGame.Rendering;
using TacticsGame.UI;

namespace TacticsGame;

public sealed class Game1 : Game
{
    private const int PartySize = 3;
    private const int StartingWorldNodeId = 0;

    private static readonly Point[] PlayerStartPositions =
    {
        new(2, 6),
        new(3, 6),
        new(2, 7)
    };

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputManager _inputManager;
    private readonly Camera2D _camera;

    private readonly UnitMovementController _unitMovementController = new();
    private readonly BattleResolver _battleResolver = new();
    private readonly BattleTurnController _battleTurnController = new();

    private readonly IReadOnlyList<WorldMapNode> _worldMapNodes =
        CreateWorldMapNodes();

    private readonly List<BattleUnit> _playerParty = new();
    private readonly List<BattleUnit> _units = new();
    private readonly List<EquipmentItem> _availableGear = new();
    private readonly HashSet<Point> _reachableMovementTiles = new();
    private readonly HashSet<Point> _attackableTiles = new();
    private readonly HashSet<int> _completedBattleNodeIds = new();

    private IReadOnlyList<Point> _previewMovementPath = Array.Empty<Point>();

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _uiFont;
    private Texture2D? _batAtlasTexture;
    private readonly Dictionary<string, Texture2D> _unitAtlasTextures =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D> _enemyAtlasTextures =
        new(StringComparer.OrdinalIgnoreCase);

    private LoadedTiledMap? _loadedMap;
    private IsometricMapRenderer? _mapRenderer;
    private TileHighlightRenderer? _tileHighlightRenderer;
    private MovementRangeRenderer? _movementRangeRenderer;
    private AttackRangeRenderer? _attackRangeRenderer;
    private BattleUnitRenderer? _battleUnitRenderer;
    private BattleActionMenu? _battleActionMenu;
    private PartyManagementScreen? _partyManagementScreen;
    private MainMenuScreen? _mainMenuScreen;
    private TextMenuScreen? _instructionsScreen;
    private TextMenuScreen? _optionsScreen;
    private TeamSelectionScreen? _teamSelectionScreen;
    private WorldMapScreen? _worldMapScreen;
    private VictoryScreen? _victoryScreen;

    private GameScreen _currentScreen = GameScreen.MainMenu;
    private GameScreen _partyManagementReturnScreen = GameScreen.Battle;
    private readonly EnemyAiController _enemyAiController = new();

    private BattleHud? _battleHud;
    private BattleUnit? _hoveredUnit;
    private BattleGrid? _battleGrid;
    private MovementRangeCalculator? _movementRangeCalculator;
    private AttackRangeCalculator? _attackRangeCalculator;
    private MovementSearchResult? _movementSearchResult;

    private BattleUnit? _selectedUnit;
    private BattleAction? _lastSelectedAction;

    private Point? _hoveredTile;
    private Point? _selectedTile;

    private string? _lastCombatMessage;
    private bool _isEnemyTurnRunning;
    private int _currentWorldNodeId = StartingWorldNodeId;
    private int? _activeBattleNodeId;

    private BattleInteractionMode _interactionMode =
        BattleInteractionMode.Idle;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1600,
            PreferredBackBufferHeight = 900
        };

        _inputManager = new InputManager();
        _camera = new Camera2D();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Tactics Game";
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        Window.TextInput += OnTextInput;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _uiFont = Content.Load<SpriteFont>("Fonts/Default");
        _battleHud = new BattleHud(
            GraphicsDevice);

        var jobDefinitions =
            CreateJobDefinitions();

        var mapFilePath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Maps",
            "StarterField.tmj");

        _loadedMap = TiledMapLoader.Load(
            GraphicsDevice,
            mapFilePath);

        _mapRenderer = new IsometricMapRenderer(
            _loadedMap,
            mapOrigin: GetDefaultMapOrigin());

        _tileHighlightRenderer = new TileHighlightRenderer(
            GraphicsDevice,
            _mapRenderer,
            _loadedMap.Map.TileWidth,
            _loadedMap.Map.TileHeight);

        _movementRangeRenderer = new MovementRangeRenderer(
            GraphicsDevice,
            _mapRenderer,
            _loadedMap.Map.TileWidth,
            _loadedMap.Map.TileHeight);

        _attackRangeRenderer = new AttackRangeRenderer(
            GraphicsDevice,
            _mapRenderer,
            _loadedMap.Map.TileWidth,
            _loadedMap.Map.TileHeight);

        _batAtlasTexture =
            LoadTextureFromAsset(
                "Assets",
                "Sprites",
                "Monsters",
                "BatAtlas.png");

        LoadUnitAtlasTextures(
            jobDefinitions);

        LoadEnemyAtlasTextures();

        _battleUnitRenderer = new BattleUnitRenderer(
            GraphicsDevice,
            _mapRenderer,
            _loadedMap.Map.TileWidth,
            _loadedMap.Map.TileHeight,
            _batAtlasTexture,
            _unitAtlasTextures,
            _enemyAtlasTextures);

        _battleActionMenu = new BattleActionMenu(
            GraphicsDevice);

        _partyManagementScreen = new PartyManagementScreen(
            GraphicsDevice);

        _mainMenuScreen = new MainMenuScreen(
            GraphicsDevice);

        _instructionsScreen = new TextMenuScreen(
            GraphicsDevice,
            "Instructions",
            CreateInstructionLines());

        _optionsScreen = new TextMenuScreen(
            GraphicsDevice,
            "Options",
            CreateOptionLines());

        _teamSelectionScreen = new TeamSelectionScreen(
            GraphicsDevice,
            jobDefinitions,
            PartySize);

        _worldMapScreen = new WorldMapScreen(
            GraphicsDevice);

        _victoryScreen = new VictoryScreen(
            GraphicsDevice);

        _movementRangeCalculator =
            new MovementRangeCalculator();

        _attackRangeCalculator =
            new AttackRangeCalculator();

        _availableGear.Clear();
        _availableGear.AddRange(
            CreateStarterGear());

        UpdateWindowTitle();
    }

    protected override void Update(
        GameTime gameTime)
    {
        _inputManager.Update();

        if (_currentScreen == GameScreen.PartyManagement)
        {
            if (_inputManager.IsKeyPressed(Keys.P) ||
                _inputManager.IsKeyPressed(Keys.Escape))
            {
                ClosePartyManagementScreen();
                base.Update(gameTime);
                return;
            }

            UpdatePartyManagementScreen();

            base.Update(gameTime);
            return;
        }

        if (_currentScreen != GameScreen.Battle)
        {
            UpdateMenuScreens();

            base.Update(gameTime);
            return;
        }

        if (_inputManager.IsKeyPressed(Keys.P))
        {
            OpenPartyManagementScreen();

            base.Update(gameTime);
            return;
        }

        if (_inputManager.IsKeyPressed(Keys.Escape))
        {
            Exit();
        }

        if (_inputManager.IsKeyPressed(Keys.Home))
        {
            ResetCamera();
        }

        UpdatePan();
        UpdateZoom();
        UpdateHoveredTile();
        UpdateActionMenuPosition();

        _battleActionMenu?.Update(
            _inputManager.MousePosition);

        var wasEnemyTurnActive =
            _isEnemyTurnRunning ||
            _battleTurnController.ActiveTeam == BattleTeam.Enemy;

        UpdateUnitMovement(
            gameTime);

        UpdateUnitAnimations(
            gameTime);

        if (wasEnemyTurnActive ||
            _isEnemyTurnRunning ||
            _battleTurnController.ActiveTeam == BattleTeam.Enemy)
        {
            base.Update(gameTime);
            return;
        }

        if (_interactionMode !=
            BattleInteractionMode.AnimatingMovement)
        {
            UpdateRightMouseClick();
            UpdateLeftMouseClick();
        }

        base.Update(gameTime);
    }

    protected override void Draw(
        GameTime gameTime)
    {
        GraphicsDevice.Clear(
            Color.CornflowerBlue);

        if (_spriteBatch is null ||
            _uiFont is null)
        {
            base.Draw(gameTime);
            return;
        }

        if (_currentScreen != GameScreen.Battle &&
            _currentScreen != GameScreen.PartyManagement)
        {
            DrawMenuScreens();

            base.Draw(gameTime);
            return;
        }

        DrawWorld();
        DrawUi();
        
    }

    protected override void UnloadContent()
    {
        Window.ClientSizeChanged -=
            OnClientSizeChanged;
        Window.TextInput -=
            OnTextInput;

        _battleActionMenu?.Dispose();
        _partyManagementScreen?.Dispose();
        _mainMenuScreen?.Dispose();
        _instructionsScreen?.Dispose();
        _optionsScreen?.Dispose();
        _teamSelectionScreen?.Dispose();
        _worldMapScreen?.Dispose();
        _victoryScreen?.Dispose();
        _battleUnitRenderer?.Dispose();
        _attackRangeRenderer?.Dispose();
        _movementRangeRenderer?.Dispose();
        _tileHighlightRenderer?.Dispose();
        foreach (var texture in _unitAtlasTextures.Values)
        {
            texture.Dispose();
        }

        _unitAtlasTextures.Clear();

        foreach (var texture in _enemyAtlasTextures.Values)
        {
            texture.Dispose();
        }

        _enemyAtlasTextures.Clear();
        _batAtlasTexture?.Dispose();
        _loadedMap?.Dispose();
        _battleHud?.Dispose();
        base.UnloadContent();
    }

    private void DrawWorld()
    {
        if (_spriteBatch is null ||
            _mapRenderer is null ||
            _tileHighlightRenderer is null ||
            _movementRangeRenderer is null ||
            _attackRangeRenderer is null ||
            _battleUnitRenderer is null)
        {
            return;
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetTransform(
                GraphicsDevice.Viewport));

        _mapRenderer.Draw(
            _spriteBatch);

        _movementRangeRenderer.Draw(
            _spriteBatch,
            _reachableMovementTiles,
            _previewMovementPath);

        _attackRangeRenderer.Draw(
            _spriteBatch,
            _attackableTiles);

        var shouldHideNormalHighlights =
            _interactionMode ==
                BattleInteractionMode.ChoosingMovementDestination ||
            _interactionMode ==
                BattleInteractionMode.AnimatingMovement ||
            _interactionMode ==
                BattleInteractionMode.ChoosingAttackTarget;

        _tileHighlightRenderer.Draw(
            _spriteBatch,
            hoveredTile:
                shouldHideNormalHighlights
                    ? null
                    : _hoveredTile,
            selectedTile:
                shouldHideNormalHighlights
                    ? null
                    : _selectedTile);

        foreach (var unit in _units
                     .OrderBy(unit =>
                         unit.RenderGridPosition.X +
                         unit.RenderGridPosition.Y))
        {
            _battleUnitRenderer.Draw(
                _spriteBatch,
                unit);
        }

        _spriteBatch.End();
    }

    private void DrawUi()
    {
        if (_spriteBatch is null ||
            _uiFont is null ||
            _battleActionMenu is null)
        {
            return;
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        if (_currentScreen == GameScreen.PartyManagement)
        {
            _partyManagementScreen?.Draw(
                _spriteBatch,
                _uiFont,
                GraphicsDevice.Viewport);

            _spriteBatch.End();
            return;
        }

        _battleActionMenu.Draw(
            _spriteBatch,
            _uiFont);

        _battleHud?.Draw(
            _spriteBatch,
            _uiFont,
            _battleTurnController,
            _selectedUnit,
            _hoveredUnit);

        _spriteBatch.End();
    }

    private void UpdateMenuScreens()
    {
        switch (_currentScreen)
        {
            case GameScreen.MainMenu:
                UpdateMainMenuScreen();
                break;

            case GameScreen.Instructions:
                UpdateTextMenuScreen(
                    _instructionsScreen);
                break;

            case GameScreen.Options:
                UpdateTextMenuScreen(
                    _optionsScreen);
                break;

            case GameScreen.TeamSelection:
                UpdateTeamSelectionScreen();
                break;

            case GameScreen.WorldMap:
                UpdateWorldMapScreen();
                break;

            case GameScreen.Victory:
                UpdateVictoryScreen();
                break;
        }
    }

    private void UpdateMainMenuScreen()
    {
        if (_mainMenuScreen is null)
        {
            return;
        }

        if (_inputManager.IsKeyPressed(
                Keys.Escape))
        {
            Exit();
            return;
        }

        var action =
            _mainMenuScreen.Update(
                _inputManager.MousePosition,
                _inputManager.IsLeftMouseButtonPressed,
                GraphicsDevice.Viewport);

        switch (action)
        {
            case MainMenuAction.Start:
                _currentScreen =
                    GameScreen.TeamSelection;
                break;

            case MainMenuAction.Instructions:
                _currentScreen =
                    GameScreen.Instructions;
                break;

            case MainMenuAction.Options:
                _currentScreen =
                    GameScreen.Options;
                break;

            case MainMenuAction.Quit:
                Exit();
                break;
        }

        UpdateWindowTitle();
    }

    private void UpdateTextMenuScreen(
        TextMenuScreen? screen)
    {
        if (screen is null)
        {
            return;
        }

        var shouldReturn =
            _inputManager.IsKeyPressed(
                Keys.Escape) ||
            screen.Update(
                _inputManager.MousePosition,
                _inputManager.IsLeftMouseButtonPressed,
                GraphicsDevice.Viewport);

        if (!shouldReturn)
        {
            return;
        }

        _currentScreen =
            GameScreen.MainMenu;

        UpdateWindowTitle();
    }

    private void UpdateTeamSelectionScreen()
    {
        if (_teamSelectionScreen is null)
        {
            return;
        }

        if (_inputManager.IsKeyPressed(
                Keys.Escape))
        {
            _currentScreen =
                GameScreen.MainMenu;

            UpdateWindowTitle();
            return;
        }

        var action =
            _teamSelectionScreen.Update(
                _inputManager.MousePosition,
                _inputManager.IsLeftMouseButtonPressed,
                _inputManager.IsKeyPressed(Keys.Back),
                GraphicsDevice.Viewport);

        switch (action)
        {
            case TeamSelectionAction.Start:
                StartCampaign(
                    _teamSelectionScreen.GetSelections());
                break;

            case TeamSelectionAction.Back:
                _currentScreen =
                    GameScreen.MainMenu;
                break;
        }

        UpdateWindowTitle();
    }

    private void UpdateWorldMapScreen()
    {
        if (_worldMapScreen is null)
        {
            return;
        }

        if (_inputManager.IsKeyPressed(
                Keys.Escape))
        {
            _currentScreen =
                GameScreen.MainMenu;

            UpdateWindowTitle();
            return;
        }

        if (_inputManager.IsKeyPressed(
                Keys.P))
        {
            OpenPartyManagementScreen();
            return;
        }

        var action =
            _worldMapScreen.Update(
                _worldMapNodes,
                _currentWorldNodeId,
                _completedBattleNodeIds,
                _inputManager.MousePosition,
                _inputManager.IsLeftMouseButtonPressed,
                GraphicsDevice.Viewport);

        switch (action.Type)
        {
            case WorldMapActionType.Move:
                _currentWorldNodeId =
                    action.NodeId;
                break;

            case WorldMapActionType.StartBattle:
                StartBattle(
                    action.NodeId);
                break;

            case WorldMapActionType.OpenParty:
                OpenPartyManagementScreen();
                break;

            case WorldMapActionType.MainMenu:
                _currentScreen =
                    GameScreen.MainMenu;
                break;
        }

        UpdateWindowTitle();
    }

    private void UpdateVictoryScreen()
    {
        if (_victoryScreen is null)
        {
            return;
        }

        var shouldContinue =
            _inputManager.IsKeyPressed(
                Keys.Enter) ||
            _victoryScreen.Update(
                _inputManager.MousePosition,
                _inputManager.IsLeftMouseButtonPressed,
                isEnterPressed: false,
                GraphicsDevice.Viewport);

        if (!shouldContinue)
        {
            return;
        }

        _currentScreen =
            GameScreen.WorldMap;

        UpdateWindowTitle();
    }

    private void DrawMenuScreens()
    {
        if (_spriteBatch is null ||
            _uiFont is null)
        {
            return;
        }

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp);

        switch (_currentScreen)
        {
            case GameScreen.MainMenu:
                _mainMenuScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    GraphicsDevice.Viewport);
                break;

            case GameScreen.Instructions:
                _instructionsScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    GraphicsDevice.Viewport);
                break;

            case GameScreen.Options:
                _optionsScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    GraphicsDevice.Viewport);
                break;

            case GameScreen.TeamSelection:
                _teamSelectionScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    GraphicsDevice.Viewport);
                break;

            case GameScreen.WorldMap:
                _worldMapScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    _worldMapNodes,
                    _currentWorldNodeId,
                    _completedBattleNodeIds,
                    GraphicsDevice.Viewport);
                break;

            case GameScreen.Victory:
                _victoryScreen?.Draw(
                    _spriteBatch,
                    _uiFont,
                    GraphicsDevice.Viewport);
                break;
        }

        _spriteBatch.End();
    }

    private void StartCampaign(
        IReadOnlyList<TeamMemberSelection> teamSelections)
    {
        _playerParty.Clear();
        _units.Clear();
        _completedBattleNodeIds.Clear();
        _currentWorldNodeId =
            StartingWorldNodeId;
        _activeBattleNodeId =
            null;

        _availableGear.Clear();
        _availableGear.AddRange(
            CreateStarterGear());

        ClearActionOverlays();

        _selectedUnit = null;
        _hoveredUnit = null;
        _hoveredTile = null;
        _selectedTile = null;
        _lastCombatMessage = null;
        _lastSelectedAction = null;
        _isEnemyTurnRunning = false;
        _interactionMode =
            BattleInteractionMode.Idle;

        _battleActionMenu?.Hide();

        for (var index = 0;
             index < teamSelections.Count;
             index++)
        {
            _playerParty.Add(
                CreatePlayerUnit(
                    teamSelections[index],
                    Point.Zero));
        }

        _currentScreen =
            GameScreen.WorldMap;

        UpdateWindowTitle();
    }

    private void StartBattle(
        int worldNodeId)
    {
        if (_loadedMap is null)
        {
            return;
        }

        var encounterNode =
            GetWorldMapNode(
                worldNodeId);

        if (encounterNode is null ||
            encounterNode.Type != WorldMapNodeType.Battle)
        {
            return;
        }

        _currentWorldNodeId =
            worldNodeId;

        _activeBattleNodeId =
            worldNodeId;

        _units.Clear();
        ClearActionOverlays();

        _selectedUnit = null;
        _hoveredUnit = null;
        _hoveredTile = null;
        _selectedTile = null;
        _lastCombatMessage = null;
        _lastSelectedAction = null;
        _isEnemyTurnRunning = false;
        _interactionMode =
            BattleInteractionMode.Idle;

        _battleActionMenu?.Hide();

        _battleGrid =
            BattleGrid.FromLoadedMap(
                _loadedMap);

        for (var index = 0;
             index < _playerParty.Count &&
             index < PlayerStartPositions.Length;
             index++)
        {
            var unit =
                _playerParty[index];

            unit.Position =
                PlayerStartPositions[index];

            unit.RenderGridPosition =
                new Vector2(
                    unit.Position.X,
                    unit.Position.Y);

            unit.TurnState.Reset();

            _units.Add(
                unit);

            _battleGrid.PlaceUnit(
                unit);
        }

        foreach (var enemy in CreateEnemyUnits(
                     worldNodeId))
        {
            _units.Add(
                enemy);

            _battleGrid.PlaceUnit(
                enemy);
        }

        _currentScreen =
            GameScreen.Battle;

        _battleTurnController.BeginBattle(
            _units);

        SelectActivePlayerUnit();
        ResetCamera();
        UpdateWindowTitle();
    }

    private static BattleUnit CreatePlayerUnit(
        TeamMemberSelection selection,
        Point position)
    {
        var job =
            selection.Job;

        return new BattleUnit
        {
            Name = selection.Name,
            JobName = job.Name,
            SpriteKey = job.Name,
            Team = BattleTeam.Player,
            Position = position,
            RenderGridPosition =
                new Vector2(
                    position.X,
                    position.Y),
            MaximumHealth = job.MaximumHealth,
            CurrentHealth = job.MaximumHealth,
            AttackDamage = job.AttackDamage,
            AttackRange = job.AttackRange,
            MovementRange = job.MovementRange,
            JumpHeight = job.JumpHeight
        };
    }

    private static IReadOnlyList<BattleUnit> CreateEnemyUnits(
        int worldNodeId)
    {
        if (worldNodeId == 4)
        {
            return new List<BattleUnit>
            {
                new()
                {
                    Name = "Golem",
                    SpriteKey = "Golem",
                    Team = BattleTeam.Enemy,
                    Position = new Point(13, 10),
                    RenderGridPosition = new Vector2(13.0f, 10.0f),
                    MaximumHealth = 28,
                    CurrentHealth = 28,
                    AttackDamage = 6,
                    AttackRange = 1,
                    MovementRange = 3,
                    JumpHeight = 1
                },
                new()
                {
                    Name = "Wisp",
                    SpriteKey = "Wisp",
                    Team = BattleTeam.Enemy,
                    Position = new Point(14, 7),
                    RenderGridPosition = new Vector2(14.0f, 7.0f),
                    MaximumHealth = 12,
                    CurrentHealth = 12,
                    AttackDamage = 5,
                    AttackRange = 2,
                    MovementRange = 5,
                    JumpHeight = 2
                },
                new()
                {
                    Name = "Slime",
                    SpriteKey = "Slime",
                    Team = BattleTeam.Enemy,
                    Position = new Point(11, 9),
                    RenderGridPosition = new Vector2(11.0f, 9.0f),
                    MaximumHealth = 16,
                    CurrentHealth = 16,
                    AttackDamage = 3,
                    AttackRange = 1,
                    MovementRange = 3,
                    JumpHeight = 1
                },
                new()
                {
                    Name = "Bat",
                    SpriteKey = "Bat",
                    Team = BattleTeam.Enemy,
                    Position = new Point(12, 4),
                    RenderGridPosition = new Vector2(12.0f, 4.0f),
                    MaximumHealth = 12,
                    CurrentHealth = 12,
                    AttackDamage = 4,
                    AttackRange = 1,
                    MovementRange = 6,
                    JumpHeight = 3
                }
            };
        }

        return new List<BattleUnit>
        {
            new()
            {
                Name = "Bat",
                SpriteKey = "Bat",
                Team = BattleTeam.Enemy,
                Position = new Point(12, 4),
                RenderGridPosition = new Vector2(12.0f, 4.0f),
                MaximumHealth = 10,
                CurrentHealth = 10,
                AttackDamage = 3,
                AttackRange = 1,
                MovementRange = 6,
                JumpHeight = 3
            },
            new()
            {
                Name = "Slime",
                SpriteKey = "Slime",
                Team = BattleTeam.Enemy,
                Position = new Point(11, 9),
                RenderGridPosition = new Vector2(11.0f, 9.0f),
                MaximumHealth = 12,
                CurrentHealth = 12,
                AttackDamage = 2,
                AttackRange = 1,
                MovementRange = 3,
                JumpHeight = 1
            },
            new()
            {
                Name = "Wisp",
                SpriteKey = "Wisp",
                Team = BattleTeam.Enemy,
                Position = new Point(14, 7),
                RenderGridPosition = new Vector2(14.0f, 7.0f),
                MaximumHealth = 9,
                CurrentHealth = 9,
                AttackDamage = 4,
                AttackRange = 2,
                MovementRange = 5,
                JumpHeight = 2
            }
        };
    }

    private static IReadOnlyList<WorldMapNode> CreateWorldMapNodes()
    {
        return new List<WorldMapNode>
        {
            new()
            {
                Id = 0,
                Name = "Crossroads",
                Description = "A quiet fork in the old trade road.",
                Type = WorldMapNodeType.Empty,
                NormalizedPosition = new Vector2(0.10f, 0.56f),
                ConnectedNodeIds = new[]
                {
                    1,
                    2
                }
            },
            new()
            {
                Id = 1,
                Name = "Moss Road",
                Description = "An empty stretch of road with clear sightlines.",
                Type = WorldMapNodeType.Empty,
                NormalizedPosition = new Vector2(0.34f, 0.35f),
                ConnectedNodeIds = new[]
                {
                    0,
                    2,
                    3
                }
            },
            new()
            {
                Id = 2,
                Name = "Bat Hollow",
                Description = "Wingbeats echo from the low trees.",
                Type = WorldMapNodeType.Battle,
                NormalizedPosition = new Vector2(0.36f, 0.73f),
                ConnectedNodeIds = new[]
                {
                    0,
                    1,
                    3
                }
            },
            new()
            {
                Id = 3,
                Name = "Wayfarer Camp",
                Description = "A safe place to check gear and catch a breath.",
                Type = WorldMapNodeType.Empty,
                NormalizedPosition = new Vector2(0.64f, 0.50f),
                ConnectedNodeIds = new[]
                {
                    1,
                    2,
                    4
                }
            },
            new()
            {
                Id = 4,
                Name = "Golem Gate",
                Description = "A ruined archway guarded by heavier monsters.",
                Type = WorldMapNodeType.Battle,
                NormalizedPosition = new Vector2(0.88f, 0.38f),
                ConnectedNodeIds = new[]
                {
                    3
                }
            }
        };
    }

    private WorldMapNode? GetWorldMapNode(
        int nodeId)
    {
        return _worldMapNodes
            .FirstOrDefault(node =>
                node.Id == nodeId);
    }

    private static BattleReward CreateBattleReward(
        WorldMapNode encounterNode)
    {
        return encounterNode.Id switch
        {
            4 => new BattleReward
            {
                EncounterName = encounterNode.Name,
                Experience = 125,
                Gear = new EquipmentItem
                {
                    Name = "Golem Plate",
                    Slot = EquipmentSlot.Chest,
                    HealthBonus = 6,
                    DefenseBonus = 3
                }
            },

            _ => new BattleReward
            {
                EncounterName = encounterNode.Name,
                Experience = 85,
                Gear = new EquipmentItem
                {
                    Name = "Scout Boots",
                    Slot = EquipmentSlot.Legs,
                    MovementBonus = 1,
                    DefenseBonus = 1
                }
            }
        };
    }

    private bool TryCompleteBattleVictory()
    {
        if (_currentScreen != GameScreen.Battle)
        {
            return false;
        }

        var hasEnemyStillStanding =
            _units.Any(unit =>
                unit.Team == BattleTeam.Enemy &&
                !unit.IsDefeated);

        if (hasEnemyStillStanding)
        {
            return false;
        }

        CompleteBattleVictory();

        return true;
    }

    private void CompleteBattleVictory()
    {
        var encounterNode =
            _activeBattleNodeId.HasValue
                ? GetWorldMapNode(
                    _activeBattleNodeId.Value)
                : null;

        if (encounterNode is null)
        {
            return;
        }

        var reward =
            CreateBattleReward(
                encounterNode);

        AwardBattleReward(
            reward);

        _completedBattleNodeIds.Add(
            encounterNode.Id);

        _battleActionMenu?.Hide();
        _units.Clear();
        ClearActionOverlays();

        _battleGrid = null;
        _selectedUnit = null;
        _hoveredUnit = null;
        _hoveredTile = null;
        _selectedTile = null;
        _lastSelectedAction = null;
        _lastCombatMessage =
            $"{encounterNode.Name} cleared";
        _isEnemyTurnRunning = false;
        _activeBattleNodeId = null;
        _interactionMode =
            BattleInteractionMode.Idle;

        _victoryScreen?.Show(
            reward,
            _playerParty);

        _currentScreen =
            GameScreen.Victory;

        UpdateWindowTitle();
    }

    private void AwardBattleReward(
        BattleReward reward)
    {
        foreach (var unit in _playerParty)
        {
            unit.AddExperience(
                reward.Experience);

            unit.TurnState.Reset();

            if (reward.FullyRecovered)
            {
                unit.RecoverAllHealth();
            }
        }

        if (reward.Gear is not null)
        {
            _availableGear.Add(
                reward.Gear);
        }
    }

    private void UpdatePartyManagementScreen()
    {
        if (_partyManagementScreen is null)
        {
            return;
        }

        _partyManagementScreen.Update(
            _inputManager.MousePosition,
            _inputManager.IsLeftMouseButtonPressed,
            GraphicsDevice.Viewport);

        if (_partyManagementScreen.WantsClose)
        {
            ClosePartyManagementScreen();
        }
    }

    private void OpenPartyManagementScreen()
    {
        if (_currentScreen == GameScreen.Battle)
        {
            if (_interactionMode ==
                    BattleInteractionMode.AnimatingMovement ||
                _battleTurnController.ActiveTeam !=
                    BattleTeam.Player)
            {
                return;
            }

            ClearSelectedUnit();
            _selectedTile = null;
            _hoveredTile = null;
            _hoveredUnit = null;
        }
        else if (_currentScreen != GameScreen.WorldMap)
        {
            return;
        }

        _partyManagementReturnScreen =
            _currentScreen;

        _partyManagementScreen?.Show(
            GetPlayerUnits(),
            _availableGear);

        _currentScreen =
            GameScreen.PartyManagement;

        UpdateWindowTitle();
    }

    private void ClosePartyManagementScreen()
    {
        _currentScreen =
            _partyManagementReturnScreen;

        if (_currentScreen == GameScreen.Battle)
        {
            SelectActivePlayerUnit();
        }

        UpdateWindowTitle();
    }

    private IReadOnlyList<BattleUnit> GetPlayerUnits()
    {
        if (_playerParty.Count > 0)
        {
            return _playerParty
                .ToList();
        }

        return _units
            .Where(unit =>
                unit.Team == BattleTeam.Player)
            .ToList();
    }

    private static IReadOnlyList<EquipmentItem> CreateStarterGear()
    {
        return new List<EquipmentItem>
        {
            new()
            {
                Name = "Leather Cap",
                Slot = EquipmentSlot.Head,
                HealthBonus = 2,
                DefenseBonus = 1
            },
            new()
            {
                Name = "Iron Helm",
                Slot = EquipmentSlot.Head,
                DefenseBonus = 2
            },
            new()
            {
                Name = "Padded Vest",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 4,
                DefenseBonus = 1
            },
            new()
            {
                Name = "Brigandine",
                Slot = EquipmentSlot.Chest,
                HealthBonus = 2,
                DefenseBonus = 3
            },
            new()
            {
                Name = "Traveler Pants",
                Slot = EquipmentSlot.Legs,
                MovementBonus = 1
            },
            new()
            {
                Name = "Greaves",
                Slot = EquipmentSlot.Legs,
                DefenseBonus = 2
            },
            new()
            {
                Name = "Leather Gloves",
                Slot = EquipmentSlot.Arms,
                AttackBonus = 1
            },
            new()
            {
                Name = "Guard Bracers",
                Slot = EquipmentSlot.Arms,
                DefenseBonus = 1
            },
            new()
            {
                Name = "Lucky Coin",
                Slot = EquipmentSlot.Charm1,
                HealthBonus = 1,
                AttackBonus = 1
            },
            new()
            {
                Name = "Wind Thread",
                Slot = EquipmentSlot.Charm2,
                MovementBonus = 1
            }
        };
    }

    private Texture2D LoadTextureFromAsset(
        params string[] relativePathParts)
    {
        var filePath =
            Path.Combine(
                new[]
                {
                    AppContext.BaseDirectory
                }.Concat(relativePathParts).ToArray());

        using var stream =
            File.OpenRead(
                filePath);

        return Texture2D.FromStream(
            GraphicsDevice,
            stream);
    }

    private void LoadUnitAtlasTextures(
        IEnumerable<UnitJobDefinition> jobDefinitions)
    {
        _unitAtlasTextures.Clear();

        foreach (var jobDefinition in jobDefinitions)
        {
            _unitAtlasTextures[jobDefinition.Name] =
                LoadTextureFromAsset(
                    "Assets",
                    "Sprites",
                    "Units",
                    $"{jobDefinition.Name}Atlas.png");
        }
    }

    private void LoadEnemyAtlasTextures()
    {
        _enemyAtlasTextures.Clear();

        foreach (var enemySpriteKey in new[]
                 {
                     "Slime",
                     "Wisp",
                     "Golem"
                 })
        {
            _enemyAtlasTextures[enemySpriteKey] =
                LoadTextureFromAsset(
                    "Assets",
                    "Sprites",
                    "Monsters",
                    $"{enemySpriteKey}Atlas.png");
        }
    }

    private static IReadOnlyList<UnitJobDefinition> CreateJobDefinitions()
    {
        return new List<UnitJobDefinition>
        {
            new()
            {
                Name = "Swordsman",
                Description = "Balanced front-line fighter.",
                MaximumHealth = 20,
                AttackDamage = 4,
                AttackRange = 1,
                MovementRange = 5,
                JumpHeight = 1
            },
            new()
            {
                Name = "Archer",
                Description = "Ranged attacker with lighter armor.",
                MaximumHealth = 16,
                AttackDamage = 3,
                AttackRange = 3,
                MovementRange = 4,
                JumpHeight = 2
            },
            new()
            {
                Name = "Mage",
                Description = "Fragile unit with strong ranged magic.",
                MaximumHealth = 12,
                AttackDamage = 5,
                AttackRange = 3,
                MovementRange = 3,
                JumpHeight = 1
            },
            new()
            {
                Name = "Cleric",
                Description = "Sturdy support caster.",
                MaximumHealth = 17,
                AttackDamage = 2,
                AttackRange = 2,
                MovementRange = 4,
                JumpHeight = 1
            },
            new()
            {
                Name = "Knight",
                Description = "Durable guard with short reach.",
                MaximumHealth = 24,
                AttackDamage = 4,
                AttackRange = 1,
                MovementRange = 3,
                JumpHeight = 1
            },
            new()
            {
                Name = "Thief",
                Description = "Fast skirmisher with modest damage.",
                MaximumHealth = 15,
                AttackDamage = 3,
                AttackRange = 1,
                MovementRange = 7,
                JumpHeight = 2
            }
        };
    }

    private static IReadOnlyList<string> CreateInstructionLines()
    {
        return new List<string>
        {
            "Start a campaign, assemble your team, then travel between connected world-map nodes.",
            "Entering an uncleared battle node starts a tactics encounter.",
            "Win battles to earn XP, recover the party, unlock gear, and return to the world map.",
            "Select the active unit, then choose Move, Attack, or Wait from the command menu.",
            "Move shows reachable tiles. Left-click a highlighted tile to travel there.",
            "Attack shows targetable tiles. Left-click an enemy inside the highlighted range.",
            "Wait ends the commanded unit's turn and advances to the next ready party member.",
            "Press P on the world map or during player turns to assign gear.",
            "Middle-drag pans the camera. Mouse wheel zooms. Home resets the camera."
        };
    }

    private static IReadOnlyList<string> CreateOptionLines()
    {
        return new List<string>
        {
            "Display: Windowed",
            "Battle Speed: Normal",
            "Camera: Mouse pan and wheel zoom",
            "Audio: Off"
        };
    }

    private void UpdateUnitMovement(
        GameTime gameTime)
    {
        if (_interactionMode !=
            BattleInteractionMode.AnimatingMovement)
        {
            return;
        }

        var movingUnit =
            _unitMovementController.MovingUnit;

        var didFinishMoving =
            _unitMovementController.Update(
                gameTime);

        if (!didFinishMoving)
        {
            return;
        }

        if (_isEnemyTurnRunning &&
            movingUnit is not null &&
            movingUnit.Team == BattleTeam.Enemy)
        {
            movingUnit.TurnState.MarkMoved();
            _selectedTile =
                movingUnit.Position;

            _interactionMode =
                BattleInteractionMode.Idle;

            EndEnemyUnitTurn(
                movingUnit);

            ContinueEnemyTurn();
            UpdateWindowTitle();
            return;
        }

        if (_selectedUnit is not null)
        {
            _selectedUnit.TurnState.MarkMoved();
            _selectedTile = _selectedUnit.Position;

            ShowActionMenuForSelectedUnit();
        }

        _interactionMode =
            _selectedUnit is null
                ? BattleInteractionMode.Idle
                : BattleInteractionMode.UnitSelected;

        UpdateWindowTitle();
    }

    private void UpdateUnitAnimations(
        GameTime gameTime)
    {
        var elapsedSeconds =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (var unit in _units)
        {
            var animationState =
                GetAnimationStateForUnit(
                    unit);

            unit.SetAnimationState(
                animationState);

            unit.AdvanceAnimation(
                elapsedSeconds);
        }
    }

    private BattleUnitAnimationState GetAnimationStateForUnit(
        BattleUnit unit)
    {
        if (unit.IsDefeated)
        {
            return BattleUnitAnimationState.Prone;
        }

        if (_unitMovementController.MovingUnit == unit)
        {
            return BattleUnitAnimationState.Walk;
        }

        if (unit.AnimationState ==
                BattleUnitAnimationState.Attack &&
            unit.AnimationElapsedSeconds < 0.45f)
        {
            return BattleUnitAnimationState.Attack;
        }

        return BattleUnitAnimationState.Idle;
    }

    private void UpdatePan()
    {
        if (!_inputManager.IsMiddleMouseButtonDown)
        {
            return;
        }

        var mouseDelta =
            _inputManager.MousePositionDelta;

        if (mouseDelta.X == 0 &&
            mouseDelta.Y == 0)
        {
            return;
        }

        _camera.Pan(
            new Vector2(
                mouseDelta.X,
                mouseDelta.Y));
    }

    private void UpdateZoom()
    {
        var didZoomChange =
            _camera.AdjustZoom(
                _inputManager.ScrollWheelDelta);

        if (didZoomChange)
        {
            UpdateWindowTitle();
        }
    }

    private void UpdateHoveredTile()
    {
        if (_mapRenderer is null)
        {
            _hoveredTile = null;
            UpdateMovementPathPreview();
            return;
        }

        var mouseScreenPosition =
            new Vector2(
                _inputManager.MousePosition.X,
                _inputManager.MousePosition.Y);

        var mouseWorldPosition =
            _camera.ScreenToWorld(
                mouseScreenPosition,
                GraphicsDevice.Viewport);

        var isOverTile =
            _mapRenderer.TryScreenToGrid(
                mouseWorldPosition,
                out var hoveredTile);

        _hoveredTile =
            isOverTile
                ? hoveredTile
                : null;

        UpdateMovementPathPreview();
        _hoveredUnit = null;

        if (_hoveredTile.HasValue &&
            _battleGrid != null)
        {
            _hoveredUnit =
                _battleGrid.GetUnitAt(
                    _hoveredTile.Value);
        }
    }

    private void UpdateMovementPathPreview()
    {
        if (_interactionMode !=
                BattleInteractionMode.ChoosingMovementDestination ||
            !_hoveredTile.HasValue ||
            _selectedUnit is null ||
            _movementSearchResult is null)
        {
            _previewMovementPath =
                Array.Empty<Point>();

            return;
        }

        _previewMovementPath =
            _movementSearchResult.BuildPath(
                _selectedUnit.Position,
                _hoveredTile.Value);
    }

    private void UpdateActionMenuPosition()
    {
        if (_selectedUnit is null ||
            _battleActionMenu is null ||
            _battleUnitRenderer is null)
        {
            return;
        }

        var worldPosition =
            _battleUnitRenderer.GetMenuAnchorWorld(
                _selectedUnit);

        var screenPosition =
            _camera.WorldToScreen(
                worldPosition,
                GraphicsDevice.Viewport);

        _battleActionMenu.SetPosition(
            screenPosition);
    }

    private void UpdateRightMouseClick()
    {
        if (!_inputManager.IsRightMouseButtonPressed)
        {
            return;
        }

        if (_interactionMode ==
                BattleInteractionMode.ChoosingMovementDestination ||
            _interactionMode ==
                BattleInteractionMode.ChoosingAttackTarget)
        {
            EndActionSelection(
                showMenu: true);
        }
    }

    private void UpdateLeftMouseClick()
    {
        if (!_inputManager.IsLeftMouseButtonPressed)
        {
            return;
        }

        if (TrySelectBattleAction())
        {
            return;
        }

        if (_interactionMode ==
            BattleInteractionMode.ChoosingMovementDestination)
        {
            TryMoveSelectedUnit();
            return;
        }

        if (_interactionMode ==
            BattleInteractionMode.ChoosingAttackTarget)
        {
            TryAttackSelectedTarget();
            return;
        }

        if (!_hoveredTile.HasValue)
        {
            if (!SelectActivePlayerUnit())
            {
                ClearSelectedUnit();
                _selectedTile = null;
            }

            return;
        }

        _selectedTile = _hoveredTile;

        var clickedUnit =
            _battleGrid?.GetUnitAt(
                _hoveredTile.Value);

        if (clickedUnit is not null &&
            _battleTurnController.CanSelectUnit(clickedUnit))
        {
            SelectUnit(clickedUnit);
            return;
        }

        if (!SelectActivePlayerUnit())
        {
            ClearSelectedUnit();
        }
    }

    private void BeginEnemyTurn()
    {
        _isEnemyTurnRunning = true;
        _lastSelectedAction = null;
        _selectedUnit = null;
        _hoveredUnit = null;
        _battleActionMenu?.Hide();
        ClearActionOverlays();

        ContinueEnemyTurn();
    }

    private void ContinueEnemyTurn()
    {
        while (_isEnemyTurnRunning)
        {
            if (_battleTurnController.ActiveTeam !=
                BattleTeam.Enemy)
            {
                FinishEnemyTurn();
                return;
            }

            if (_battleGrid is null ||
                _movementRangeCalculator is null)
            {
                _battleTurnController.SkipActiveTeamTurn(
                    _units);

                FinishEnemyTurn();
                return;
            }

            var enemy =
                _battleTurnController.ActiveUnit;

            if (enemy is null)
            {
                _battleTurnController.SkipActiveTeamTurn(
                    _units);

                FinishEnemyTurn();
                return;
            }

            _selectedTile =
                enemy.Position;

            var decision =
                _enemyAiController.DecideAction(
                    _battleGrid,
                    enemy,
                    _units,
                    _movementRangeCalculator);

            switch (decision.Type)
            {
                case EnemyTurnDecisionType.Attack:

                    if (decision.Target is not null)
                    {
                        ResolveEnemyAttack(
                            enemy,
                            decision.Target);
                    }

                    EndEnemyUnitTurn(
                        enemy);

                    continue;

                case EnemyTurnDecisionType.Move:

                    if (decision.Destination.HasValue &&
                        TryBeginEnemyMovement(
                            enemy,
                            decision.Destination.Value))
                    {
                        return;
                    }

                    EndEnemyUnitTurn(
                        enemy);

                    continue;

                case EnemyTurnDecisionType.Wait:
                    EndEnemyUnitTurn(
                        enemy);

                    continue;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private bool TryBeginEnemyMovement(
        BattleUnit enemy,
        Point destination)
    {
        if (_battleGrid is null ||
            _movementRangeCalculator is null)
        {
            return false;
        }

        var searchResult =
            _movementRangeCalculator
                .CalculateReachableTiles(
                    _battleGrid,
                    enemy);

        var path =
            searchResult.BuildPath(
                enemy.Position,
                destination);

        if (path.Count == 0)
        {
            return false;
        }

        ClearActionOverlays();

        _lastCombatMessage =
            $"{enemy.Name} moves";

        _interactionMode =
            BattleInteractionMode.AnimatingMovement;

        _unitMovementController.BeginMove(
            _battleGrid,
            enemy,
            path);

        UpdateWindowTitle();

        return true;
    }

    private void ResolveEnemyAttack(
        BattleUnit enemy,
        BattleUnit target)
    {
        if (_battleGrid is null)
        {
            return;
        }

        FaceUnitToward(
            enemy,
            target.Position);

        enemy.SetAnimationState(
            BattleUnitAnimationState.Attack);

        var result =
            _battleResolver.Attack(
                _battleGrid,
                enemy,
                target);

        enemy.TurnState.MarkActed();

        _lastCombatMessage =
            result.WasDefeated
                ? $"{result.TargetName} was defeated"
                : $"{result.TargetName}: {result.RemainingHealth} HP remaining";

    }

    private void EndEnemyUnitTurn(
        BattleUnit enemy)
    {
        _battleTurnController.EndUnitTurn(
            enemy,
            _units);
    }

    private static void FaceUnitToward(
        BattleUnit unit,
        Point targetPosition)
    {
        var deltaX =
            targetPosition.X -
            unit.Position.X;

        var deltaY =
            targetPosition.Y -
            unit.Position.Y;

        if (Math.Abs(deltaX) >=
            Math.Abs(deltaY))
        {
            unit.Facing =
                deltaX >= 0
                    ? UnitFacing.FrontRight
                    : UnitFacing.BackLeft;

            return;
        }

        unit.Facing =
            deltaY >= 0
                ? UnitFacing.FrontLeft
                : UnitFacing.BackRight;
    }

    private void FinishEnemyTurn()
    {
        _isEnemyTurnRunning = false;
        _selectedTile = null;

        if (_interactionMode ==
            BattleInteractionMode.AnimatingMovement)
        {
            _interactionMode =
                BattleInteractionMode.Idle;
        }

        SelectActivePlayerUnit();
        UpdateWindowTitle();
    }

    private bool TrySelectBattleAction()
    {
        if (_battleActionMenu is null ||
            !_battleActionMenu.Contains(_inputManager.MousePosition))
        {
            return false;
        }

        var selectedAction =
            _battleActionMenu.TrySelectAction(
                _inputManager.MousePosition);

        if (!selectedAction.HasValue)
        {
            return true;
        }

        _lastSelectedAction = selectedAction;

        switch (selectedAction.Value)
        {
            case BattleAction.Move:
                BeginMovementSelection();
                break;

            case BattleAction.Attack:
                BeginAttackSelection();
                break;

            case BattleAction.Wait:
                EndSelectedUnitTurn();
                break;

            case BattleAction.Spells:
            case BattleAction.Items:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        UpdateWindowTitle();

        return true;
    }
    private void BeginMovementSelection()
    {
        if (_selectedUnit is null ||
            _battleGrid is null ||
            _movementRangeCalculator is null)
        {
            return;
        }

        if (_selectedUnit.TurnState.HasMoved)
        {
            return;
        }

        ClearActionOverlays();

        _movementSearchResult =
            _movementRangeCalculator.CalculateReachableTiles(
                _battleGrid,
                _selectedUnit);

        _reachableMovementTiles.UnionWith(
            _movementSearchResult.ReachableTiles);

        _interactionMode =
            BattleInteractionMode.ChoosingMovementDestination;

        _battleActionMenu?.Hide();

        UpdateMovementPathPreview();
        UpdateWindowTitle();
    }

    private void BeginAttackSelection()
    {
        if (_selectedUnit is null ||
            _battleGrid is null ||
            _attackRangeCalculator is null)
        {
            return;
        }

        if (_selectedUnit.TurnState.HasActed)
        {
            return;
        }

        ClearActionOverlays();

        _attackableTiles.UnionWith(
            _attackRangeCalculator.CalculateAttackableTiles(
                _battleGrid,
                _selectedUnit));

        _interactionMode =
            BattleInteractionMode.ChoosingAttackTarget;

        _battleActionMenu?.Hide();

        UpdateWindowTitle();
    }

    private void TryMoveSelectedUnit()
    {
        if (_selectedUnit is null ||
            _battleGrid is null ||
            _movementSearchResult is null ||
            !_hoveredTile.HasValue)
        {
            return;
        }

        var destination =
            _hoveredTile.Value;

        if (!_movementSearchResult.CanReach(
                destination))
        {
            return;
        }

        var path =
            _movementSearchResult.BuildPath(
                _selectedUnit.Position,
                destination);

        if (path.Count == 0)
        {
            return;
        }

        ClearActionOverlays();

        _battleActionMenu?.Hide();

        _interactionMode =
            BattleInteractionMode.AnimatingMovement;

        _unitMovementController.BeginMove(
            _battleGrid,
            _selectedUnit,
            path);

        UpdateWindowTitle();
    }

    private void TryAttackSelectedTarget()
    {
        if (_selectedUnit is null ||
            _battleGrid is null ||
            !_hoveredTile.HasValue)
        {
            return;
        }

        var targetPosition =
            _hoveredTile.Value;

        if (!_attackableTiles.Contains(
                targetPosition))
        {
            return;
        }

        var target =
            _battleGrid.GetUnitAt(
                targetPosition);

        if (target is null ||
            target.Team ==
                _selectedUnit.Team)
        {
            return;
        }

        FaceUnitToward(
            _selectedUnit,
            target.Position);

        _selectedUnit.SetAnimationState(
            BattleUnitAnimationState.Attack);

        var result =
            _battleResolver.Attack(
                _battleGrid,
                _selectedUnit,
                target);
        _selectedUnit.TurnState.MarkActed();
        _lastCombatMessage =
            result.WasDefeated
                ? $"{result.TargetName} was defeated"
                : $"{result.TargetName}: {result.RemainingHealth} HP remaining";

        if (TryCompleteBattleVictory())
        {
            return;
        }

        EndActionSelection(
            showMenu: true);
    }

    private void EndActionSelection(
        bool showMenu)
    {
        ClearActionOverlays();

        _interactionMode =
            _selectedUnit is null
                ? BattleInteractionMode.Idle
                : BattleInteractionMode.UnitSelected;

        if (showMenu &&
            _selectedUnit is not null)
        {
            ShowActionMenuForSelectedUnit();
        }

        UpdateWindowTitle();
    }

    private void ShowActionMenuForSelectedUnit()
    {
        if (_selectedUnit is null ||
            _battleActionMenu is null)
        {
            return;
        }

        var disabledActions =
            new List<BattleAction>
            {
            BattleAction.Spells,
            BattleAction.Items
            };

        if (_selectedUnit.TurnState.HasMoved)
        {
            disabledActions.Add(
                BattleAction.Move);
        }

        if (_selectedUnit.TurnState.HasActed)
        {
            disabledActions.Add(
                BattleAction.Attack);
        }

        _battleActionMenu.Show(
            _selectedUnit.Name);

        _battleActionMenu.SetDisabledActions(
            disabledActions);

        UpdateActionMenuPosition();
    }

    private void EndSelectedUnitTurn()
    {
        if (_selectedUnit is null)
        {
            return;
        }

        _battleTurnController.EndUnitTurn(
            _selectedUnit,
            _units);

        ClearSelectedUnit();

        if (_battleTurnController.ActiveTeam ==
            BattleTeam.Enemy)
        {
            BeginEnemyTurn();
            return;
        }

        SelectActivePlayerUnit();
        UpdateWindowTitle();
    }

    private void ClearActionOverlays()
    {
        _reachableMovementTiles.Clear();
        _attackableTiles.Clear();
        _previewMovementPath =
            Array.Empty<Point>();
        _movementSearchResult =
            null;
    }

    private bool SelectActivePlayerUnit()
    {
        if (_battleTurnController.ActiveTeam !=
                BattleTeam.Player ||
            _battleTurnController.ActiveUnit is null)
        {
            return false;
        }

        var activeUnit =
            _battleTurnController.ActiveUnit;

        if (!_battleTurnController.CanSelectUnit(
                activeUnit))
        {
            return false;
        }

        if (_selectedUnit == activeUnit)
        {
            _selectedTile =
                activeUnit.Position;

            if (_interactionMode ==
                BattleInteractionMode.Idle)
            {
                _interactionMode =
                    BattleInteractionMode.UnitSelected;
            }

            if (_interactionMode ==
                BattleInteractionMode.UnitSelected)
            {
                ShowActionMenuForSelectedUnit();
            }

            return true;
        }

        SelectUnit(
            activeUnit);

        return true;
    }

    private void SelectUnit(BattleUnit unit)
    {
        if (!_battleTurnController.CanSelectUnit(unit))
        {
            return;
        }

        EndActionSelection(
            showMenu: false);

        _selectedUnit = unit;
        _selectedTile = unit.Position;

        _interactionMode =
            BattleInteractionMode.UnitSelected;

        ShowActionMenuForSelectedUnit();
    }

    private void ClearSelectedUnit()
    {
        EndActionSelection(
            showMenu: false);

        _selectedUnit =
            null;

        _interactionMode =
            BattleInteractionMode.Idle;

        _battleActionMenu?.Hide();
    }

    private void ResetCamera()
    {
        _camera.Reset();
        UpdateWindowTitle();
    }

    private void OnClientSizeChanged(
        object? sender,
        EventArgs e)
    {
        if (_mapRenderer is null)
        {
            return;
        }

        _mapRenderer.MapOrigin =
            GetDefaultMapOrigin();
    }

    private void OnTextInput(
        object sender,
        TextInputEventArgs e)
    {
        if (_currentScreen !=
            GameScreen.TeamSelection)
        {
            return;
        }

        _teamSelectionScreen?.HandleTextInput(
            e.Character);
    }

    private Vector2 GetDefaultMapOrigin()
    {
        if (_loadedMap is null)
        {
            return Vector2.Zero;
        }

        var map =
            _loadedMap.Map;

        var viewport =
            GraphicsDevice.Viewport;

        var halfTileWidth =
            map.TileWidth / 2.0f;

        var halfTileHeight =
            map.TileHeight / 2.0f;

        var mapCenterOffsetX =
            (map.Width - map.Height) *
            halfTileWidth /
            2.0f;

        var mapCenterOffsetY =
            (map.Width + map.Height) *
            halfTileHeight /
            2.0f;

        return new Vector2(
            x:
                (viewport.Width / 2.0f) -
                mapCenterOffsetX,
            y:
                (viewport.Height / 2.0f) -
                mapCenterOffsetY);
    }

    private void UpdateWindowTitle()
    {
        if (_currentScreen == GameScreen.MainMenu)
        {
            Window.Title =
                "Tactics Game - Main Menu";

            return;
        }

        if (_currentScreen == GameScreen.Instructions)
        {
            Window.Title =
                "Tactics Game - Instructions";

            return;
        }

        if (_currentScreen == GameScreen.Options)
        {
            Window.Title =
                "Tactics Game - Options";

            return;
        }

        if (_currentScreen == GameScreen.TeamSelection)
        {
            Window.Title =
                "Tactics Game - Team Selection";

            return;
        }

        if (_currentScreen == GameScreen.WorldMap)
        {
            var node =
                GetWorldMapNode(
                    _currentWorldNodeId);

            Window.Title =
                node is null
                    ? "Tactics Game - World Map"
                    : $"Tactics Game - World Map - {node.Name}";

            return;
        }

        if (_currentScreen == GameScreen.PartyManagement)
        {
            Window.Title =
                "Tactics Game - Party Management";

            return;
        }

        if (_currentScreen == GameScreen.Victory)
        {
            Window.Title =
                "Tactics Game - Victory";

            return;
        }

        var zoomPercentage =
            (int)Math.Round(
                _camera.Zoom * 100.0f);

        var activeUnitText =
            _battleTurnController.ActiveUnit is null
                ? string.Empty
                : $" - Unit: {_battleTurnController.ActiveUnit.Name}";

        var commandText =
            _lastSelectedAction.HasValue
                ? $" - Command: {_lastSelectedAction.Value}"
                : string.Empty;

        var modeText =
            _interactionMode switch
            {
                BattleInteractionMode.ChoosingMovementDestination =>
                    " - Select Movement Destination",

                BattleInteractionMode.AnimatingMovement =>
                    " - Moving",

                BattleInteractionMode.ChoosingAttackTarget =>
                    " - Select Attack Target",

                _ =>
                    string.Empty
            };

        var combatText =
            string.IsNullOrWhiteSpace(
                _lastCombatMessage)
                ? string.Empty
                : $" - {_lastCombatMessage}";

        Window.Title =
            $"Tactics Game - Round: {_battleTurnController.RoundNumber}" +
            $" - Team: {_battleTurnController.ActiveTeam}" +
            $"{activeUnitText}" +
            $" - Zoom: {zoomPercentage}%" +
            $"{commandText}{modeText}{combatText}";
    }
}
