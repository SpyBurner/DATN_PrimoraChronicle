using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameController : MonoBehaviour
{
    [SerializeField] private BoardController _boardController;
    [SerializeField] private Unit _unitPrefab;
    [SerializeField] private int _playerCount = 2;
    [SerializeField] private int _totalGames = 1000;

    private List<UnitController> _controllers = new();
    private TurnManager _turnManager = new();
    private int _currentGame;
    private int[] _wins;

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
        public int WinnerPlayer;
        public int TotalMoves;
        public double TotalTimeMs;
        public double AvgMoveTimeMs;
    }

    private UnitStats[] _currentGameStats;

    private void Start()
    {
        _boardController.GenerateBoard();
        _wins = new int[_playerCount];
        _currentGame = 0;

        _turnManager.OnTurnStarted += OnTurnStarted;
        _turnManager.OnCycleEnded += OnCycleEnded;

        StartNewGame();
    }

    private void StartNewGame()
    {
        _currentGame++;

        foreach (var controller in _controllers)
        {
            if (controller.Unit != null)
                Destroy(controller.Unit.gameObject);
        }
        _controllers.Clear();

        _totalMoves = 0;
        _currentGameStats = new UnitStats[_playerCount];
        _gameStopwatch.Restart();

        SpawnAIUnits();

        foreach (var controller in _controllers)
            controller.SetOnTurnCompleted(OnMoveCompleted);

        _turnManager.BuildActionQueue(_controllers);
        _turnManager.StartCurrentTurn();
    }

    private void OnMoveCompleted()
    {
        _totalMoves++;
        _turnManager.EndCurrentTurn();
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

            _currentGameStats[i] = new UnitStats
            {
                HP = unit.HP,
                Attack = unit.Attack,
                Speed = unit.Speed
            };

            var controller = new AIUnitController(unit, _boardController);
            _controllers.Add(controller);
        }
    }

    private void OnTurnStarted(UnitController controller)
    {
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

        if (winnerPlayer >= 0)
            _wins[winnerPlayer]++;

        _results.Add(new GameResult
        {
            GameNumber = _currentGame,
            PlayerStats = (UnitStats[])_currentGameStats.Clone(),
            WinnerPlayer = winnerPlayer,
            TotalMoves = _totalMoves,
            TotalTimeMs = _gameStopwatch.Elapsed.TotalMilliseconds,
            AvgMoveTimeMs = _totalMoves > 0 ? _gameStopwatch.Elapsed.TotalMilliseconds / _totalMoves : 0
        });

        if (_currentGame < _totalGames)
        {
            StartNewGame();
        }
        else
        {
            WriteResultsToFile();
        }
    }

    private void WriteResultsToFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== AI Battle Results ===");
        sb.AppendLine($"Total Games: {_totalGames}");
        sb.AppendLine($"Player 0 Wins: {_wins[0]} ({(float)_wins[0] / _totalGames * 100f:F1}%)");
        sb.AppendLine($"Player 1 Wins: {_wins[1]} ({(float)_wins[1] / _totalGames * 100f:F1}%)");
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

        sb.AppendLine($"Total Moves Across All Games: {totalMovesAll}");
        sb.AppendLine($"Average Time Per Move: {totalAvgMove:F4} ms");
        sb.AppendLine();
        sb.AppendLine("--- Per-Game Details ---");
        sb.AppendLine("Game | P0_HP | P0_ATK | P0_SPD | P1_HP | P1_ATK | P1_SPD | Winner | Moves | AvgMoveTime(ms)");
        sb.AppendLine(new string('-', 100));

        foreach (var r in _results)
        {
            var p0 = r.PlayerStats[0];
            var p1 = r.PlayerStats[1];
            sb.AppendLine($"{r.GameNumber,4} | {p0.HP,5} | {p0.Attack,6} | {p0.Speed,6} | {p1.HP,5} | {p1.Attack,6} | {p1.Speed,6} | {r.WinnerPlayer,6} | {r.TotalMoves,5} | {r.AvgMoveTimeMs:F4}");
        }

        string path = Path.Combine(Application.dataPath, "_Game/Features/AITest/ai_battle_results.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Results written to: {path}");
        Debug.Log($"Player 0 winrate: {(float)_wins[0] / _totalGames * 100f:F1}% | Player 1 winrate: {(float)_wins[1] / _totalGames * 100f:F1}%");
        Debug.Log($"Average move time: {totalAvgMove:F4} ms");
    }

    private void Update()
    {
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
