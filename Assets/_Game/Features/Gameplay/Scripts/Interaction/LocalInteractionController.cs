using System.Collections.Generic;
using UnityEngine;

public class LocalInteractionController : MonoBehaviour
{
    public static LocalInteractionController Instance { get; private set; }

    [Header("Active Selection")]
    public NetworkUnit activeUnit;
    public GenericSkillBehaviorSO selectedSkill;

    [Header("Color Bindings")]
    public Color RangeColor = new Color(1f, 0.92f, 0.016f); // Neutral normal (Yellow)
    public Color SuccessColor = new Color(0.2f, 0.8f, 0.2f); // Success Normal (Green)
    public Color ErrorColor = new Color(1f, 0.2f, 0.2f); // Error Normal (Red)

    private BoardManager _boardManager;
    private NetworkGameplayManager _gameplayManager;
    private List<HexTile> _rangeTiles = new List<HexTile>();
    private List<HexTile> _aoeTiles = new List<HexTile>();
    private HexTile _lastHoveredTile;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _boardManager = FindObjectOfType<BoardManager>();
        _gameplayManager = FindObjectOfType<NetworkGameplayManager>();
    }

    public void SelectSkill(GenericSkillBehaviorSO skill, NetworkUnit caster)
    {
        ClearHighlights();
        selectedSkill = skill;
        activeUnit = caster;

        if (selectedSkill == null || activeUnit == null || _boardManager == null) return;

        // Gather range tiles
        foreach (var tile in _boardManager.GetComponentsInChildren<HexTile>())
        {
            int dist = NetworkUnit.GetDistance(activeUnit.P, activeUnit.Q, tile.p, tile.q);
            if (dist <= selectedSkill.range)
            {
                _rangeTiles.Add(tile);
                tile.SetVisualColor(RangeColor);
            }
        }
    }

    public void ClearHighlights()
    {
        if (_boardManager == null) return;

        foreach (var tile in _boardManager.GetComponentsInChildren<HexTile>())
        {
            tile.ResetVisualColor();
        }

        _rangeTiles.Clear();
        _aoeTiles.Clear();
        _lastHoveredTile = null;
        selectedSkill = null;
    }

    private void Update()
    {
        if (selectedSkill == null || activeUnit == null) return;

        // Perform raycast to find tile under mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        HexTile currentHoveredTile = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            currentHoveredTile = hit.collider.GetComponentInParent<HexTile>();
        }

        if (currentHoveredTile != _lastHoveredTile)
        {
            // Restore range color to previously highlighted AOE tiles
            foreach (var tile in _aoeTiles)
            {
                if (_rangeTiles.Contains(tile))
                {
                    tile.SetVisualColor(RangeColor);
                }
                else
                {
                    tile.ResetVisualColor();
                }
            }
            _aoeTiles.Clear();

            _lastHoveredTile = currentHoveredTile;

            if (_lastHoveredTile != null)
            {
                int dist = NetworkUnit.GetDistance(activeUnit.P, activeUnit.Q, _lastHoveredTile.p, _lastHoveredTile.q);
                bool inRange = dist <= selectedSkill.range;

                if (inRange)
                {
                    // Check target type matching bits
                    bool isValid = selectedSkill.IsTileValidTarget(_gameplayManager, activeUnit, _lastHoveredTile, selectedSkill.targetCondition);

                    if (isValid)
                    {
                        // Success Normal for valid AOE
                        foreach (var tile in _boardManager.GetComponentsInChildren<HexTile>())
                        {
                            int d = NetworkUnit.GetDistance(_lastHoveredTile.p, _lastHoveredTile.q, tile.p, tile.q);
                            if (d <= selectedSkill.aoe)
                            {
                                _aoeTiles.Add(tile);
                                tile.SetVisualColor(SuccessColor);
                            }
                        }
                    }
                    else
                    {
                        // Error Normal for invalid targets filtered out by bitmask flags
                        _aoeTiles.Add(_lastHoveredTile);
                        _lastHoveredTile.SetVisualColor(ErrorColor);
                    }
                }
                else
                {
                    // Highlight hovered tile as error color if out of range
                    _aoeTiles.Add(_lastHoveredTile);
                    _lastHoveredTile.SetVisualColor(ErrorColor);
                }
            }
        }

        // On Mouse Click, execute action
        if (Input.GetMouseButtonDown(0) && currentHoveredTile != null)
        {
            int dist = NetworkUnit.GetDistance(activeUnit.P, activeUnit.Q, currentHoveredTile.p, currentHoveredTile.q);
            if (dist <= selectedSkill.range)
            {
                bool isValid = selectedSkill.IsTileValidTarget(_gameplayManager, activeUnit, currentHoveredTile, selectedSkill.targetCondition);
                if (isValid)
                {
                    // Dispatch RPC to State Authority on NetworkGameplayManager
                    if (_gameplayManager != null)
                    {
                        _gameplayManager.RPC_RequestSkillExecution(activeUnit.Object.Id, selectedSkill.behaviorId, currentHoveredTile.p, currentHoveredTile.q);
                    }
                    ClearHighlights();
                }
            }
        }
    }
}
