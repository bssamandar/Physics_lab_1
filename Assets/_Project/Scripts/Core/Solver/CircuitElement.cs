namespace CircuitLab.Core.Solver
{
    /// <summary>
    /// Solver uchun ikki qutbli chiziqli element (Norton ekvivalenti):
    /// A va B tugunlari orasida <see cref="Conductance"/> o'tkazuvchanlik va unga parallel
    /// B'dan A'ga yo'nalgan <see cref="SourceCurrent"/> tok manbai.
    /// Rezistor: SourceCurrent = 0. Batareya: Conductance = 1/r, SourceCurrent = E/r.
    /// </summary>
    /// <remarks>UnityEngine'ga bog'liq emas — EditMode testlarda to'g'ridan-to'g'ri ishlatiladi.</remarks>
    public struct CircuitElement
    {
        /// <summary>Birinchi (musbat) tugun indeksi.</summary>
        public int NodeA;
        /// <summary>Ikkinchi (manfiy) tugun indeksi.</summary>
        public int NodeB;
        /// <summary>O'tkazuvchanlik, Simens (1/Om). Har doim &gt; 0.</summary>
        public double Conductance;
        /// <summary>Ichki tok manbai, Amper (tok tashqi zanjirga A tugun orqali chiqadi).</summary>
        public double SourceCurrent;

        /// <summary>Element yaratadi.</summary>
        public CircuitElement(int nodeA, int nodeB, double conductance, double sourceCurrent = 0d)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Conductance = conductance;
            SourceCurrent = sourceCurrent;
        }

        /// <summary>Qarshilik (Om) bo'yicha passiv element.</summary>
        public static CircuitElement Resistor(int a, int b, double ohms)
        {
            return new CircuitElement(a, b, 1d / ohms, 0d);
        }

        /// <summary>EYK va ichki qarshilikka ega manba (A = "+", B = "−").</summary>
        public static CircuitElement Source(int plus, int minus, double emf, double internalOhms)
        {
            return new CircuitElement(plus, minus, 1d / internalOhms, emf / internalOhms);
        }
    }
}
