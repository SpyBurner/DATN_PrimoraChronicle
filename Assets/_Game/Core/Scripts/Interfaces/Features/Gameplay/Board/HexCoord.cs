using System;
using Fusion;

public struct HexCoord : INetworkStruct, IEquatable<HexCoord>
{
    public int P;
    public int Q;
    public int R => -P - Q;

    public HexCoord(int p, int q) { P = p; Q = q; }

    public bool Equals(HexCoord other) => P == other.P && Q == other.Q;
    public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(P, Q);
    public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
    public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
    public override string ToString() => $"({P},{Q})";

    public static HexCoord Invalid => new HexCoord(int.MinValue, int.MinValue);
    public bool IsValid => P != int.MinValue;
}
