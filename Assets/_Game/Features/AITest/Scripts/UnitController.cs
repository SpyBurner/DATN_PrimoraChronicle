using System;
using UnityEngine;

public abstract class UnitController
{
    protected Unit _unit;
    protected BoardController _board;
    protected Action _onTurnCompleted;

    public Unit Unit => _unit;

    public UnitController(Unit unit, BoardController board)
    {
        _unit = unit;
        _board = board;
    }

    public void SetOnTurnCompleted(Action onTurnCompleted)
    {
        _onTurnCompleted = onTurnCompleted;
    }

    public abstract void TakeTurn();
}
