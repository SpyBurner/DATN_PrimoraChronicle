using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager
{
    private List<UnitController> _actionQueue = new();
    private int _currentIndex;

    public UnitController CurrentUnit => _currentIndex < _actionQueue.Count ? _actionQueue[_currentIndex] : null;
    public int CurrentIndex => _currentIndex;
    public int TotalUnits => _actionQueue.Count;

    public event Action<UnitController> OnTurnStarted;
    public event Action OnCycleEnded;

    public void BuildActionQueue(List<UnitController> controllers)
    {
        _actionQueue.Clear();
        _actionQueue.AddRange(controllers.Where(c => !c.Unit.IsDead));
        _actionQueue.Sort((a, b) =>
        {
            int speedCompare = b.Unit.Speed.CompareTo(a.Unit.Speed);
            if (speedCompare != 0) return speedCompare;
            int hpCompare = a.Unit.HP.CompareTo(b.Unit.HP);
            if (hpCompare != 0) return hpCompare;
            return UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1;
        });
        _currentIndex = 0;
    }

    public void StartCurrentTurn()
    {
        SkipDeadUnits();

        if (CheckCycleEnd()) return;

        if (_currentIndex >= _actionQueue.Count)
        {
            _currentIndex = 0;
            SkipDeadUnits();
            if (CheckCycleEnd()) return;
        }

        var current = _actionQueue[_currentIndex];
        current.Unit.TickCooldowns();
        OnTurnStarted?.Invoke(current);
        current.TakeTurn();
    }

    public void EndCurrentTurn()
    {
        if (CheckCycleEnd()) return;

        _currentIndex++;
        if (_currentIndex >= _actionQueue.Count)
        {
            _currentIndex = 0;
        }

        StartCurrentTurn();
    }

    private void SkipDeadUnits()
    {
        int count = _actionQueue.Count;
        int checks = 0;
        while (_currentIndex < _actionQueue.Count && _actionQueue[_currentIndex].Unit.IsDead && checks < count)
        {
            _currentIndex++;
            checks++;
        }
    }

    private bool CheckCycleEnd()
    {
        int aliveCount = _actionQueue.Count(c => !c.Unit.IsDead);
        if (aliveCount <= 1)
        {
            OnCycleEnded?.Invoke();
            return true;
        }
        return false;
    }
}
