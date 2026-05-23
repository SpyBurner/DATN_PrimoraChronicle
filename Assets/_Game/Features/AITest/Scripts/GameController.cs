using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

public class GameController : MonoBehaviour
{
    [SerializeField] private BoardController _boardController;
    [SerializeField] private Unit _unitPrefab;
    [SerializeField] private int _playerCount = 2;
    [SerializeField] private int _totalGames = 1000;
    [SerializeField] private bool _autoStart = true;

    private List<UnitController> _controllers = new();
    private TurnManager _turnManager = new();
    private EffectController _effectController;
    private int _currentGame;
    private int[] _wins;
    private bool _pendingNewGame;
    [SerializeField] private int _gamesPerFrame = 1;

    private PlayerUnitController _playerController;
    private bool _isPlayerTurn;

    // Per-game tracking
    private int _totalMoves;
    private Stopwatch _gameStopwatch = new();
    private List<GameResult> _results = new();

    private struct UnitStats
    {
        public int HP;
        public int Attack;
        public int Speed;
    }

    private struct GameResult
    {
        public int GameNumber;
        public UnitStats[] PlayerStats;
        public string[] PlayerSkills;
        public int WinnerPlayer;
        public int TotalMoves;
        public double TotalTimeMs;
        public double AvgMoveTimeMs;
    }

    private UnitStats[] _currentGameStats;
    private string[] _currentGameSkills;

    private bool _started;

    private void Start()
    {
        _boardController.GenerateBoard();
        _effectController = new EffectController(_boardController);
        _wins = new int[_playerCount];
        _currentGame = 0;

        _turnManager.OnTurnStarted += OnTurnStarted;
        _turnManager.OnCycleEnded += OnCycleEnded;

        if (!_autoStart)
            _boardController.TileClicked += OnTileClicked;

        if (_autoStart)
        {
            _started = true;
            StartNewGame();
        }
    }

    private void StartNewGame()
    {
        _currentGame++;
        _effectController.Clear();

        foreach (var controller in _controllers)
        {
            if (controller.Unit != null)
                Destroy(controller.Unit.gameObject);
        }
        _controllers.Clear();

        _totalMoves = 0;
        _currentGameStats = new UnitStats[_playerCount];
        _currentGameSkills = new string[_playerCount];
        _gameStopwatch.Restart();

        if (_autoStart)
            SpawnAIUnits();
        else
            SpawnManualUnits();

        foreach (var controller in _controllers)
            controller.SetOnTurnCompleted(OnMoveCompleted);

        _turnManager.BuildActionQueue(_controllers);
        _turnManager.StartCurrentTurn();
    }

    private const int MAX_MOVES_PER_GAME = 1000;

    private void OnMoveCompleted()
    {
        _totalMoves++;
        if (_totalMoves >= MAX_MOVES_PER_GAME)
        {
            ForceEndGame();
            return;
        }
        _turnManager.EndCurrentTurn();
    }

    private void ForceEndGame()
    {
        _gameStopwatch.Stop();

        int winnerPlayer = -1;
        int bestHP = -1;
        bool tied = false;

        foreach (var c in _controllers)
        {
            if (c.Unit.IsDead) continue;
            if (c.Unit.HP > bestHP)
            {
                bestHP = c.Unit.HP;
                winnerPlayer = c.Unit.OwnerPlayer;
                tied = false;
            }
            else if (c.Unit.HP == bestHP)
            {
                tied = true;
            }
        }

        if (tied)
            winnerPlayer = Random.Range(0, _playerCount);

        if (winnerPlayer >= 0)
            _wins[winnerPlayer]++;

        _results.Add(new GameResult
        {
            GameNumber = _currentGame,
            PlayerStats = (UnitStats[])_currentGameStats.Clone(),
            PlayerSkills = (string[])_currentGameSkills.Clone(),
            WinnerPlayer = winnerPlayer,
            TotalMoves = _totalMoves,
            TotalTimeMs = _gameStopwatch.Elapsed.TotalMilliseconds,
            AvgMoveTimeMs = _totalMoves > 0 ? _gameStopwatch.Elapsed.TotalMilliseconds / _totalMoves : 0
        });

        if (_currentGame < _totalGames)
            _pendingNewGame = true;
        else
            WriteResultsToFile();
    }

    private void SpawnAIUnits()
    {
        var corners = GetCornerCoords();

        for (int i = 0; i < _playerCount && i < corners.Count; i++)
        {
            var (p, q) = corners[i];
            Unit unit = Instantiate(_unitPrefab, _boardController.transform);
            unit.OwnerPlayer = i;
            unit.HP = Random.Range(30, 80);
            unit.MaxHP = unit.HP;
            unit.Attack = Random.Range(5, 20);
            unit.Speed = Random.Range(1, 10);
            unit.Init(_boardController, p, q);

            AssignRandomSkills(unit);

            _currentGameStats[i] = new UnitStats
            {
                HP = unit.HP,
                Attack = unit.Attack,
                Speed = unit.Speed
            };
            _currentGameSkills[i] = GetSkillNames(unit);

            var controller = new AIUnitController(unit, _boardController, _effectController);
            _controllers.Add(controller);
        }
    }

    private void SpawnManualUnits()
    {
        var corners = GetCornerCoords();

        for (int i = 0; i < _playerCount && i < corners.Count; i++)
        {
            var (p, q) = corners[i];
            Unit unit = Instantiate(_unitPrefab, _boardController.transform);
            unit.OwnerPlayer = i;
            unit.HP = Random.Range(30, 80);
            unit.MaxHP = unit.HP;
            unit.Attack = Random.Range(5, 20);
            unit.Speed = Random.Range(1, 10);
            unit.Init(_boardController, p, q);

            AssignRandomSkills(unit);

            _currentGameStats[i] = new UnitStats
            {
                HP = unit.HP,
                Attack = unit.Attack,
                Speed = unit.Speed
            };
            _currentGameSkills[i] = GetSkillNames(unit);

            UnitController controller;
            if (i == 0)
                controller = new PlayerUnitController(unit, _boardController, _effectController, OnHighlightTiles, OnClearHighlight);
            else
                controller = new AIUnitController(unit, _boardController, _effectController);

            _controllers.Add(controller);
        }
    }

    private void OnHighlightTiles(ActionMode mode, List<Vector3Int> tiles)
    {
        Color color = mode switch
        {
            ActionMode.Move => Color.cyan,
            ActionMode.Attack => Color.red,
            ActionMode.Skill1 => Color.magenta,
            ActionMode.Skill2 => new Color(1f, 0.5f, 0f),
            _ => Color.green
        };

        foreach (var coord in tiles)
        {
            var tile = _boardController.GetTile(coord.x, coord.y, coord.z);
            if (tile != null)
                tile.SetHighlight(true, color);
        }
    }

    private void OnClearHighlight()
    {
        var corners = GetCornerCoords();
        foreach (var tile in _boardController.GetComponentsInChildren<Tile>())
            tile.SetHighlight(false);
    }

    private void AssignRandomSkills(Unit unit)
    {
        int skillCount = Random.Range(1, 3); // 1 or 2 skills
        var skills = unit.Skills;

        for (int i = 0; i < skillCount && i < skills.Length; i++)
        {
            var behavior = SkillBehaviorRegistry.GetRandom();

            if (skills[i] == null)
                skills[i] = new Skill();

            skills[i].Behavior = behavior;
            skills[i].Name = behavior.Name;
            skills[i].Cooldown = behavior.DefaultCooldown;
            skills[i].CurrentCooldown = 0;
            skills[i].OneTime = behavior.IsOneTime;
            skills[i].ChoosePattern = new HexPattern
            {
                Offsets = new System.Collections.Generic.List<HexOffset>
                {
                    new HexOffset(1, 0), new HexOffset(-1, 0),
                    new HexOffset(0, 1), new HexOffset(0, -1),
                    new HexOffset(1, -1), new HexOffset(-1, 1),
                    new HexOffset(2, 0), new HexOffset(-2, 0),
                    new HexOffset(0, 2), new HexOffset(0, -2),
                    new HexOffset(2, -2), new HexOffset(-2, 2)
                }
            };
        }

        // Clear unused skill slots
        for (int i = skillCount; i < skills.Length; i++)
            skills[i] = null;
    }

    private string GetSkillNames(Unit unit)
    {
        var names = new List<string>();
        foreach (var skill in unit.Skills)
        {
            if (skill?.Behavior != null)
                names.Add(skill.Behavior.Name);
        }
        return names.Count > 0 ? string.Join(", ", names) : "None";
    }

    private void OnTurnStarted(UnitController controller)
    {
        _effectController.TickAllEffects();

        if (!_autoStart)
        {
            _isPlayerTurn = controller is PlayerUnitController;
            if (_isPlayerTurn)
                _playerController = (PlayerUnitController)controller;
        }
    }

    private void OnCycleEnded()
    {
        _gameStopwatch.Stop();

        int winnerPlayer = -1;
        foreach (var c in _controllers)
        {
            if (!c.Unit.IsDead)
            {
                winnerPlayer = c.Unit.OwnerPlayer;
                break;
            }
        }

        if (winnerPlayer < 0)
            winnerPlayer = Random.Range(0, _playerCount);

        _wins[winnerPlayer]++;

        _results.Add(new GameResult
        {
            GameNumber = _currentGame,
            PlayerStats = (UnitStats[])_currentGameStats.Clone(),
            PlayerSkills = (string[])_currentGameSkills.Clone(),
            WinnerPlayer = winnerPlayer,
            TotalMoves = _totalMoves,
            TotalTimeMs = _gameStopwatch.Elapsed.TotalMilliseconds,
            AvgMoveTimeMs = _totalMoves > 0 ? _gameStopwatch.Elapsed.TotalMilliseconds / _totalMoves : 0
        });

        if (_currentGame < _totalGames)
        {
            _pendingNewGame = true;
        }
        else
        {
            WriteResultsToFile();
        }
    }

    private void WriteResultsToFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AI Battle Results");
        sb.AppendLine();
        sb.AppendLine($"**Total Games:** {_totalGames}");
        sb.AppendLine($"**Player 0 Wins:** {_wins[0]} ({(float)_wins[0] / _totalGames * 100f:F1}%)");
        sb.AppendLine($"**Player 1 Wins:** {_wins[1]} ({(float)_wins[1] / _totalGames * 100f:F1}%)");
        sb.AppendLine();

        double totalAvgMove = 0;
        int totalMovesAll = 0;
        double totalTimeAll = 0;
        foreach (var r in _results)
        {
            totalMovesAll += r.TotalMoves;
            totalTimeAll += r.TotalTimeMs;
        }
        totalAvgMove = totalMovesAll > 0 ? totalTimeAll / totalMovesAll : 0;

        sb.AppendLine($"**Total Moves Across All Games:** {totalMovesAll}");
        sb.AppendLine($"**Average Time Per Move:** {totalAvgMove:F4} ms");
        sb.AppendLine();
        sb.AppendLine("## Per-Game Details");
        sb.AppendLine();
        sb.AppendLine("| Game | P0_HP | P0_ATK | P0_SPD | P0_Skills | P1_HP | P1_ATK | P1_SPD | P1_Skills | Winner | Moves | AvgMoveTime(ms) |");
        sb.AppendLine("|------|-------|--------|--------|-----------|-------|--------|--------|-----------|--------|-------|-----------------|");

        foreach (var r in _results)
        {
            var p0 = r.PlayerStats[0];
            var p1 = r.PlayerStats[1];
            string p0Skills = r.PlayerSkills[0] ?? "None";
            string p1Skills = r.PlayerSkills[1] ?? "None";
            sb.AppendLine($"| {r.GameNumber} | {p0.HP} | {p0.Attack} | {p0.Speed} | {p0Skills} | {p1.HP} | {p1.Attack} | {p1.Speed} | {p1Skills} | {r.WinnerPlayer} | {r.TotalMoves} | {r.AvgMoveTimeMs:F4} |");
        }

        string path = Path.Combine(Application.dataPath, "_Game/Features/AITest/ai_battle_results.md");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Results written to: {path}");
        Debug.Log($"Player 0 winrate: {(float)_wins[0] / _totalGames * 100f:F1}% | Player 1 winrate: {(float)_wins[1] / _totalGames * 100f:F1}%");
        Debug.Log($"Average move time: {totalAvgMove:F4} ms");
    }

    private void Update()
    {
        if (!_started && !_autoStart && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _started = true;
            StartNewGame();
            return;
        }

        if (!_autoStart && _isPlayerTurn && _playerController != null)
            _playerController.HandleInput();

        if (!_pendingNewGame) return;

        for (int i = 0; i < _gamesPerFrame && _pendingNewGame; i++)
        {
            _pendingNewGame = false;
            StartNewGame();
        }
    }

    private void OnTileClicked(Tile tile)
    {
        if (!_isPlayerTurn || _playerController == null) return;
        _playerController.OnTileSelected(tile);
    }

    private List<(int p, int q)> GetCornerCoords()
    {
        int s = _boardController.Size;
        return new List<(int, int)>
        {
            (s, -s),
            (-s, s),
            (s, 0),
            (-s, 0),
            (0, s),
            (0, -s)
        };
    }
}
