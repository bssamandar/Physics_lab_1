using System;

namespace CircuitLab.Core.Solver
{
    /// <summary>
    /// Tugun potensiallari usuli (Nodal Analysis) asosidagi matematik yadro.
    /// Kirxgofning tok qonuni har bir tugun uchun yoziladi: Σ G·(Vi − Vj) = Σ I_manba,
    /// natijada G·V = I chiziqli tizim hosil bo'ladi va qisman pivotli Gauss usulida yechiladi.
    /// Ketma-ket va parallel ulanishlar alohida holat sifatida emas, umumiy graf sifatida hisoblanadi.
    /// </summary>
    /// <remarks>
    /// GC: barcha massivlar <see cref="EnsureCapacity"/> orqali faqat tugunlar soni oshganda ajratiladi.
    /// Takroriy <see cref="Solve"/> chaqiruvlari xotira ajratmaydi.
    /// Hech qanday tugunga ulanmagan (osilib turgan) tugunlar singulyarlik bermasligi uchun
    /// har bir tugunga yerga nisbatan <see cref="MinConductance"/> (GMIN, SPICE usuli) qo'shiladi.
    /// </remarks>
    public sealed class CircuitSolver
    {
        /// <summary>Har bir tugundan yerga parazit o'tkazuvchanlik (S).</summary>
        public const double MinConductance = 1e-9;

        private const double PivotEpsilon = 1e-18;

        private double[] _matrix = Array.Empty<double>(); // n×n, satr bo'yicha tekis massiv
        private double[] _rhs = Array.Empty<double>();
        private double[] _nodeVoltages = Array.Empty<double>();
        private int _capacity;
        private int _nodeCount;
        private int _groundNode;

        /// <summary>Oxirgi yechimdagi tugunlar soni.</summary>
        public int NodeCount => _nodeCount;

        /// <summary>Oxirgi <see cref="Solve"/> muvaffaqiyatli bo'ldimi.</summary>
        public bool LastSolveSucceeded { get; private set; }

        /// <summary>Kamida <paramref name="nodeCount"/> tugun uchun buferlarni oldindan ajratadi.</summary>
        public void EnsureCapacity(int nodeCount)
        {
            if (nodeCount <= _capacity) return;
            int newCapacity = Math.Max(nodeCount, _capacity * 2);
            int unknowns = Math.Max(newCapacity - 1, 1);
            _matrix = new double[unknowns * unknowns];
            _rhs = new double[unknowns];
            _nodeVoltages = new double[newCapacity];
            _capacity = newCapacity;
        }

        /// <summary>
        /// Zanjirni yechadi.
        /// </summary>
        /// <param name="elements">Elementlar buferi (faqat birinchi <paramref name="elementCount"/> tasi o'qiladi).</param>
        /// <param name="elementCount">Faol elementlar soni.</param>
        /// <param name="nodeCount">Tugunlar soni (0..nodeCount-1).</param>
        /// <param name="groundNode">Potensiali 0 V deb olinadigan tayanch tugun.</param>
        /// <returns>Tizim yechildi bo'lsa true.</returns>
        public bool Solve(CircuitElement[] elements, int elementCount, int nodeCount, int groundNode)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (nodeCount <= 0)
            {
                _nodeCount = 0;
                LastSolveSucceeded = true;
                return true;
            }
            if ((uint)groundNode >= (uint)nodeCount) throw new ArgumentOutOfRangeException(nameof(groundNode));

            EnsureCapacity(nodeCount);
            _nodeCount = nodeCount;
            _groundNode = groundNode;

            int n = nodeCount - 1; // noma'lumlar soni (yer tuguni chiqariladi)
            Array.Clear(_matrix, 0, n * n);
            Array.Clear(_rhs, 0, n);
            Array.Clear(_nodeVoltages, 0, nodeCount);

            for (int i = 0; i < n; i++) _matrix[i * n + i] = MinConductance;

            for (int e = 0; e < elementCount; e++)
            {
                CircuitElement el = elements[e];
                if (el.NodeA == el.NodeB || el.Conductance <= 0d) continue;

                int a = ToUnknown(el.NodeA);
                int b = ToUnknown(el.NodeB);
                double g = el.Conductance;

                if (a >= 0) _matrix[a * n + a] += g;
                if (b >= 0) _matrix[b * n + b] += g;
                if (a >= 0 && b >= 0)
                {
                    _matrix[a * n + b] -= g;
                    _matrix[b * n + a] -= g;
                }

                if (el.SourceCurrent != 0d)
                {
                    if (a >= 0) _rhs[a] += el.SourceCurrent;
                    if (b >= 0) _rhs[b] -= el.SourceCurrent;
                }
            }

            LastSolveSucceeded = GaussianElimination(n);
            if (!LastSolveSucceeded) return false;

            for (int node = 0; node < nodeCount; node++)
            {
                int u = ToUnknown(node);
                _nodeVoltages[node] = u < 0 ? 0d : _rhs[u];
            }
            return true;
        }

        /// <summary>Tugun potensiali (V), yerga nisbatan.</summary>
        public double GetNodeVoltage(int node)
        {
            return (uint)node < (uint)_nodeCount ? _nodeVoltages[node] : 0d;
        }

        /// <summary>
        /// Element orqali A→B yo'nalishdagi tok (A). Batareya uchun manfiy qiymat —
        /// tok tashqi zanjirga "+" qutbdan chiqayotganini bildiradi.
        /// </summary>
        public double GetElementCurrent(in CircuitElement element)
        {
            double drop = GetNodeVoltage(element.NodeA) - GetNodeVoltage(element.NodeB);
            return drop * element.Conductance - element.SourceCurrent;
        }

        private int ToUnknown(int node)
        {
            if (node == _groundNode) return -1;
            return node < _groundNode ? node : node - 1;
        }

        /// <summary>Qisman pivotli Gauss usuli; javob _rhs ichiga yoziladi.</summary>
        private bool GaussianElimination(int n)
        {
            double[] m = _matrix;
            double[] r = _rhs;

            for (int col = 0; col < n; col++)
            {
                int pivotRow = col;
                double best = Math.Abs(m[col * n + col]);
                for (int row = col + 1; row < n; row++)
                {
                    double v = Math.Abs(m[row * n + col]);
                    if (v > best) { best = v; pivotRow = row; }
                }
                if (best < PivotEpsilon) return false;

                if (pivotRow != col)
                {
                    int pr = pivotRow * n, cr = col * n;
                    for (int k = col; k < n; k++)
                    {
                        double t = m[pr + k]; m[pr + k] = m[cr + k]; m[cr + k] = t;
                    }
                    double tr = r[pivotRow]; r[pivotRow] = r[col]; r[col] = tr;
                }

                int colRow = col * n;
                double inv = 1d / m[colRow + col];
                for (int row = col + 1; row < n; row++)
                {
                    int rr = row * n;
                    double f = m[rr + col] * inv;
                    if (f == 0d) continue;
                    m[rr + col] = 0d;
                    for (int k = col + 1; k < n; k++) m[rr + k] -= f * m[colRow + k];
                    r[row] -= f * r[col];
                }
            }

            for (int row = n - 1; row >= 0; row--)
            {
                int rr = row * n;
                double sum = r[row];
                for (int k = row + 1; k < n; k++) sum -= m[rr + k] * r[k];
                r[row] = sum / m[rr + row];
            }
            return true;
        }
    }
}
