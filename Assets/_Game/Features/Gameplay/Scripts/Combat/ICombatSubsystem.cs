using System;

public interface ICombatSubsystem : IDisposable{
    ICombatModel Model { get; }
    ICombatController Controller { get; }
}
