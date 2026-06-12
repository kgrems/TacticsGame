using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using TacticsGame.Battle;
using TacticsGame.Grid;
using TacticsGame.Input;
using TacticsGame.Maps;
using TacticsGame.Rendering;
using TacticsGame.UI;

namespace TacticsGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly InputManager _inputManager;
    private readonly Camera2D _camera;

    private readonly UnitMovementController _unitMovementController = new();
    private readonly BattleResolver _battleResolver = new();
    private readonly BattleTurnController _battleTurnController = new();

    private readonly List<BattleUnit> _units = new();
    private readonly HashSet<Point> _reachableMovementTiles = new();
    private readonly HashSet<Point> _attackableTiles = new();

    private IReadOnlyList<Point> _previewMovementPath = Array.Empty<Point>();

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _uiFont;

    private LoadedTiledMap? _loadedMap;
    private IsometricMapRenderer? _mapRenderer;
    private TileHighlightRenderer? _tileHighlightRenderer;
    private MovementRangeRenderer? _movementRangeRenderer;
    private AttackRangeRenderer? _attackRangeRenderer;
    private BattleUnitRenderer? _battleUnitRenderer;
    private BattleActionMenu? _battleActionMenu;

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

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _uiFont = Content.Load<SpriteFont>("Fonts/Default");
        _battleHud = new BattleHud(    GraphicsDevice);
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

        _battleUnitRenderer = new BattleUnitRenderer(
            GraphicsDevice,
            _mapRenderer,
            _loadedMap.Map.TileWidth,
            _loadedMap.Map.TileHeight);

        _battleActionMenu = new BattleActionMenu(
            GraphicsDevice);

        _battleGrid = BattleGrid.FromLoadedMap(
            _loadedMap);

        _movementRangeCalculator =
            new MovementRangeCalculator();

        _attackRangeCalculator =
            new AttackRangeCalculator();

        var swordsman = new BattleUnit
        {
            Name = "Swordsman",
            Team = BattleTeam.Player,
            Position = new Point(2, 2),
            RenderGridPosition = new Vector2(2.0f, 2.0f),
            MaximumHealth = 20,
            CurrentHealth = 20,
            AttackDamage = 4,
            AttackRange = 1,
            MovementRange = 4,
            JumpHeight = 1
        };

        var rat = new BattleUnit
        {
            Name = "Rat",
            Team = BattleTeam.Enemy,
            Position = new Point(6, 5),
            RenderGridPosition = new Vector2(6.0f, 5.0f),
            MaximumHealth = 8,
            CurrentHealth = 8,
            AttackDamage = 2,
            AttackRange = 1,
            MovementRange = 5,
            JumpHeight = 1
        };

        _units.Add(swordsman);
        _units.Add(rat);

        _battleGrid.PlaceUnit(swordsman);
        _battleGrid.PlaceUnit(rat);
        _battleTurnController.BeginBattle(_units);

        UpdateWindowTitle();
    }

    protected override void Update(
        GameTime gameTime)
    {
        _inputManager.Update();

        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (keyboardState.IsKeyDown(Keys.Home))
        {
            ResetCamera();
        }

        UpdatePan();
        UpdateZoom();
        UpdateHoveredTile();
        UpdateActionMenuPosition();

        _battleActionMenu?.Update(
            _inputManager.MousePosition);

        UpdateUnitMovement(
            gameTime);

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

        DrawWorld();
        DrawUi();
        
    }

    protected override void UnloadContent()
    {
        Window.ClientSizeChanged -=
            OnClientSizeChanged;

        _battleActionMenu?.Dispose();
        _battleUnitRenderer?.Dispose();
        _attackRangeRenderer?.Dispose();
        _movementRangeRenderer?.Dispose();
        _tileHighlightRenderer?.Dispose();
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

        foreach (var unit in _units)
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

    private void UpdateUnitMovement(
        GameTime gameTime)
    {
        if (_interactionMode !=
            BattleInteractionMode.AnimatingMovement)
        {
            return;
        }

        var didFinishMoving =
            _unitMovementController.Update(
                gameTime);

        if (!didFinishMoving)
        {
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
            ClearSelectedUnit();
            _selectedTile = null;
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

        ClearSelectedUnit();
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

        if (result.WasDefeated)
        {
            _units.Remove(
                target);
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
            _lastCombatMessage =
                "Enemy phase placeholder: skipped";

            _battleTurnController.SkipActiveTeamTurn(
                _units);
        }

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
        var zoomPercentage =
            (int)Math.Round(
                _camera.Zoom * 100.0f);

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
            $" - Zoom: {zoomPercentage}%" +
            $"{commandText}{modeText}{combatText}";
    }
}