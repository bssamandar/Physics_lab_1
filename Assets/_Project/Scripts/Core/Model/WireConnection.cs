namespace CircuitLab.Core
{
    /// <summary>
    /// Ikki terminal orasidagi sim (graf qirrasi). Obyektlar <see cref="CircuitBoard"/> ichida
    /// pool qilinadi, shuning uchun havolani <see cref="CircuitBoard.WireRemoved"/> dan keyin saqlamang.
    /// </summary>
    public sealed class WireConnection
    {
        /// <summary>Boshlang'ich terminal.</summary>
        public CircuitTerminal TerminalA { get; private set; }
        /// <summary>Oxirgi terminal.</summary>
        public CircuitTerminal TerminalB { get; private set; }
        /// <summary>A→B yo'nalishdagi tok (A). Manfiy — tok B→A oqadi.</summary>
        public float Current { get; internal set; }
        /// <summary>Sim faolmi (pool'dan olingan).</summary>
        public bool IsActive { get; private set; }

        internal void Bind(CircuitTerminal a, CircuitTerminal b)
        {
            TerminalA = a;
            TerminalB = b;
            Current = 0f;
            IsActive = true;
        }

        internal void Unbind()
        {
            TerminalA = null;
            TerminalB = null;
            Current = 0f;
            IsActive = false;
        }

        /// <summary>Sim shu ikki terminalni (istalgan tartibda) bog'laydimi.</summary>
        public bool Connects(CircuitTerminal a, CircuitTerminal b)
        {
            return (TerminalA == a && TerminalB == b) || (TerminalA == b && TerminalB == a);
        }

        /// <summary>Sim shu terminalga ulanganmi.</summary>
        public bool Touches(CircuitTerminal t)
        {
            return TerminalA == t || TerminalB == t;
        }
    }
}
