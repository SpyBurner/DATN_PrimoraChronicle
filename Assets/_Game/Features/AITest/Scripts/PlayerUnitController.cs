using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ActionMode
{
    None,
    Move,
    Attack,
    Skill1,
    Skill2
}

public class PlayerUnitController : UnitController
{
    private ActionMode _currentMode = ActionMode.None;
    private Action<ActionMode, List<Vector3Int>> _onHighlightTiles;
    private Action _onClearHighlight;

    public ActionMode CurrentMode => _currentMode;

    public PlayerUnitController(Unit unit, BoardController board,
        Action<ActionMode, List<Vector3Int>> onHighlightTiles, Action onClearHighlight)
        : base(unit, board)
    {
        _onHighlightTiles = onHighlightTiles;
        _onClearHighlight = onClearHighlight;
    }

    public override void TakeTurn()
    {
        _currentMode = ActionMode.None;
    }

    public void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) SelectAction(1);
        else if (keyboard.digit2Key.wasPressedThisFrame) SelectAction(2);
        else if (keyboard.digit3Key.wasPressedThisFrame) SelectAction(3);
        else if (keyboard.digit4Key.wasPressedThisFrame) SelectAction(4);
    }

    public void SelectAction(int buttonIndex)
    {
        _onClearHighlight?.Invoke();

        _currentMode = buttonIndex switch
        {
            1 => ActionMode.Move,
            2 => ActionMode.Attack,
            3 => ActionMode.Skill1,
            4 => ActionMode.Skill2,
            _ => ActionMode.None
        };

        var tiles = GetCurrentPatternTiles();
        if (tiles != null && tiles.Count > 0)
            _onHighlightTiles?.Invoke(_currentMode, tiles);
    }

    public void OnTileSelected(Tile tile)
    {
        if (_currentMode == ActionMode.None) return;

        var validTiles = GetCurrentPatternTiles();
        var target = new Vector3Int(tile.P, tile.Q, tile.R);
        if (validTiles == null || !validTiles.Contains(target)) return;

        bool success = false;
        switch (_currentMode)
        {
            case ActionMode.Move:
                success = ExecuteMove(tile);
                break;
            case ActionMode.Attack:
                success = ExecuteAttack(tile);
                break;
            case ActionMode.Skill1:
                ExecuteSkill(0, tile);
                success = true;
                break;
            case ActionMode.Skill2:
                ExecuteSkill(1, tile);
                success = true;
                break;
        }

        if (!success) return;

        _onClearHighlight?.Invoke();
        _currentMode = ActionMode.None;
        _onTurnCompleted?.Invoke();
    }

    private List<Vector3Int> GetCurrentPatternTiles()
    {
        return _currentMode switch
        {
            ActionMode.Move => _unit.GetMoveTiles(),
            ActionMode.Attack => _unit.GetAttackTiles(),
            ActionMode.Skill1 => _unit.GetSkillTargets(0),
            ActionMode.Skill2 => _unit.GetSkillTargets(1),
            _ => null
        };
    }

    private bool ExecuteMove(Tile tile)
    {
        if (tile.OccupiedBy != null) return false;
        _unit.PlaceOnTile(tile);
        Debug.Log($"Unit moved to ({tile.P}, {tile.Q}, {tile.R})");
        return true;
    }

    private bool ExecuteAttack(Tile tile)
    {
        Unit target = tile.OccupiedBy;
        if (target == null || target == _unit) return false;
        target.TakeDamage(_unit.Attack);
        Debug.Log($"Unit attacks ({tile.P}, {tile.Q}, {tile.R})");
        return true;
    }

    private void ExecuteSkill(int skillIndex, Tile tile)
    {
        var applyArea = _unit.GetSkillApplyArea(skillIndex, tile.P, tile.Q);
        _unit.UseSkill(skillIndex);
        Debug.Log($"Unit uses Skill {skillIndex + 1} on ({tile.P}, {tile.Q}, {tile.R}), affects {applyArea.Count} tiles");
    }
}
