using SpanCompression.ML;
using SpanCompression.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public class ExpectedAdvantage(Span span) : IComparable<ExpectedAdvantage>
    {
        public Span Span { get; init; } = span;

        private Dictionary<int, double> before = [];        
        private Dictionary<int, double> after0 = [];        
        private Dictionary<int, double> after1 = [];

        public double ValueBefore { get; private set; }
        public double ValueAfter0 { get; private set; }
        public double ValueAfter1 { get; private set; }

        public int CompareTo(ExpectedAdvantage? other)
        {
            if (other == null)
                throw new InvalidOperationException();
            double a = Evaluate();
            double b = other.Evaluate();
            return a.CompareTo(b);
        }

        public void Update(ExpectedAdvantagePublisher cmp)
        {
            var values = cmp.Evaluate(Span.parent.ID, Span.axis);

            if (before.ContainsKey(cmp.ID))
            {
                ValueBefore -= before[cmp.ID];
                ValueAfter0 -= after0[cmp.ID];
                ValueAfter1 -= after1[cmp.ID];
            }

            before[cmp.ID] = values.Item1;
            after0[cmp.ID] = values.Item2;
            after1[cmp.ID] = values.Item3;

            ValueBefore += values.Item1;
            ValueAfter0 += values.Item2;
            ValueAfter1 += values.Item3;
        }
        
        public double Evaluate()
        {
            return ValueBefore - (ValueAfter0 + ValueAfter1) * 0.5f;
        }
    }

    public class ExpectedAdvantageComponent : ExpectedAdvantagePublisher
    {
        public static int NeuralHits = 0;
        public static int HaltonHits = 0;

        public Dictionary<int, Cell> Cells { get; }
        public NeuralUncertaintyEstimator NeuralBrain { get; init; }

        // Configuration
        public bool UseNeural { get; init; } = false;
        public bool UseHalton { get; init; } = true;

        public ISampler Sampler { get; init; }

        public ExpectedAdvantageComponent(int ID, Dictionary<int, Cell> cells, ISampler sampler)
            : base(ID)
        {
            Cells = cells;
            Sampler = sampler;
        }

        public override (double, double, double) Evaluate(int id, Axis axis)
        {
            // 1. Create the hypothetical cells (Clone existing)
            Dictionary<int, Cell> cellsBefore = [];
            Dictionary<int, Cell> cellsAfter0 = [];
            Dictionary<int, Cell> cellsAfter1 = [];

            foreach (var cell in Cells)
            {
                cellsBefore.Add(cell.Key, cell.Value.Clone());
                cellsAfter0.Add(cell.Key, cell.Value.Clone());
                cellsAfter1.Add(cell.Key, cell.Value.Clone());
            }

            // 2. Perform the split
            cellsAfter0[id].GetSpan(axis).Split(0);
            cellsAfter1[id].GetSpan(axis).Split(1);

            // 3. Extract the triplet of cells (Strictly deterministic)
            // b1 MUST be the cell being split (id).
            // b2 and b3 must be consistently ordered.
            var otherKeys = Cells.Keys.Where(k => k != id).OrderBy(k => k).ToArray();

            int kA = id;
            int kB = otherKeys[0];
            int kC = otherKeys[1];

            // 1. Prepare the Before Scenario
            Cell b1 = cellsBefore[kA];
            Cell b2 = cellsBefore[kB];
            Cell b3 = cellsBefore[kC];

            // 2. Prepare After0 (Left)
            Cell l1 = cellsAfter0[kA];
            Cell l2 = cellsAfter0[kB];
            Cell l3 = cellsAfter0[kC];

            // 3. Prepare After1 (Right)
            Cell r1 = cellsAfter1[kA];
            Cell r2 = cellsAfter1[kB];
            Cell r3 = cellsAfter1[kC];

            // 4. Validate Training Distribution Limits
            bool isWithinDistribution = IsWithinTrainingDistribution(b1, b2, b3) &&
                                        IsWithinTrainingDistribution(l1, l2, l3) &&
                                        IsWithinTrainingDistribution(r1, r2, r3);

            if (UseNeural && NeuralBrain != null && isWithinDistribution)
            {
                // --- FAST PATH: Neural ---
                NeuralHits++;
                var (before, after0, after1) =
                    NeuralBrain.PredictBatch(b1, b2, b3, l1, l2, l3, r1, r2, r3);

                return (before, after0, after1);
            }
            else if (UseHalton)
            {
                // --- MEDIUM PATH: Halton Stream ---
                // Serves as mathematical fallback if outside the neural net's reliable distribution
                HaltonHits++;
                double before, after0, after1;

                before = FastUncertainty.CalculateAdaptiveHalton(b1, b2, b3);
                after0 = FastUncertainty.CalculateAdaptiveHalton(l1, l2, l3);
                after1 = FastUncertainty.CalculateAdaptiveHalton(r1, r2, r3);
                return (before, after0, after1);
            }
            else
            {
                return EvaluateLegacy(id, axis);
            }
        }

        //private bool IsWithinTrainingDistribution(Cell a, Cell b, Cell c)
        //{
        //    const float minSize = 0.01f;
        //    const float maxSize = 10.0f;
        //    const float minDistance = 0.01f;
        //    const float maxDistance = 50.0f;

        //    // Apply the new max-span scaling logic
        //    float dx = a.SpanX.Size;
        //    float dy = a.SpanY.Size;
        //    float dz = a.SpanZ.Size;
        //    float maxSpan = MathF.Max(dx, MathF.Max(dy, dz));
        //    maxSpan = MathF.Max(maxSpan, 1e-7f);

        //    float scale = 1.0f / maxSpan;

        //    // Check A's dimensions (X is no longer guaranteed to be 1.0)
        //    if (!IsInRange(dx * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(dy * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(dz * scale, minSize, maxSize)) return false;

        //    // Check B's dimensions
        //    if (!IsInRange(b.SpanX.Size * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(b.SpanY.Size * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(b.SpanZ.Size * scale, minSize, maxSize)) return false;

        //    // Check C's dimensions
        //    if (!IsInRange(c.SpanX.Size * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(c.SpanY.Size * scale, minSize, maxSize)) return false;
        //    if (!IsInRange(c.SpanZ.Size * scale, minSize, maxSize)) return false;

        //    // Check Distances from A's center
        //    float distB = (b.Center - a.Center).Length() * scale;
        //    if (!IsInRange(distB, minDistance, maxDistance)) return false;

        //    float distC = (c.Center - a.Center).Length() * scale;
        //    if (!IsInRange(distC, minDistance, maxDistance)) return false;

        //    return true;
        //}

        private bool IsWithinTrainingDistribution(Cell a, Cell b, Cell c)
        {
            // NEW BOUNDS: 
            // Min Ratio = 0.01 / 10.0 = 0.001
            // Max Ratio = 10.0 / 0.01 = 1000.0
            // Max Normalized Distance = 50.0 / 0.01 = 5000.0
            const float minSize = 0.001f;
            const float maxSize = 1000.0f;
            const float minDistance = 0.0f;
            const float maxDistance = 5000.0f;

            float dx = Math.Max(a.SpanX.Size, 1e-7f);
            float dy = Math.Max(a.SpanY.Size, 1e-7f);
            float dz = Math.Max(a.SpanZ.Size, 1e-7f);
            float maxSpan = MathF.Max(dx, MathF.Max(dy, dz));

            float scale = 1.0f / maxSpan;

            // Check A's dimensions
            if (!IsInRange(dx * scale, minSize, maxSize)) return false;
            if (!IsInRange(dy * scale, minSize, maxSize)) return false;
            if (!IsInRange(dz * scale, minSize, maxSize)) return false;

            // Check B's dimensions
            if (!IsInRange(b.SpanX.Size * scale, minSize, maxSize)) return false;
            if (!IsInRange(b.SpanY.Size * scale, minSize, maxSize)) return false;
            if (!IsInRange(b.SpanZ.Size * scale, minSize, maxSize)) return false;

            // Check C's dimensions
            if (!IsInRange(c.SpanX.Size * scale, minSize, maxSize)) return false;
            if (!IsInRange(c.SpanY.Size * scale, minSize, maxSize)) return false;
            if (!IsInRange(c.SpanZ.Size * scale, minSize, maxSize)) return false;

            // Check Distances from A's center
            float distB = (b.Center - a.Center).Length() * scale;
            if (!IsInRange(distB, minDistance, maxDistance)) return false;

            float distC = (c.Center - a.Center).Length() * scale;
            if (!IsInRange(distC, minDistance, maxDistance)) return false;

            return true;
        }

        private bool IsInRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        public (double, double, double) EvaluateLegacy(int id, Axis axis)
        {
            double before;
            double after0;
            double after1;

            Dictionary<int, Cell> cellsBefore = [];
            Dictionary<int, Cell> cellsAfter0 = [];
            Dictionary<int, Cell> cellsAfter1 = [];

            foreach (var cell in Cells)
            {
                cellsBefore.Add(cell.Key, cell.Value.Clone());
                cellsAfter0.Add(cell.Key, cell.Value.Clone());
                cellsAfter1.Add(cell.Key, cell.Value.Clone());
            }

            cellsAfter0[id].GetSpan(axis).Split(0);
            cellsAfter1[id].GetSpan(axis).Split(1);

            var samplesBefore = Sampler.Sample([.. cellsBefore.Values]).Values.ToArray();
            var samplesAfter0 = Sampler.Sample([.. cellsAfter0.Values]).Values.ToArray();
            var samplesAfter1 = Sampler.Sample([.. cellsAfter1.Values]).Values.ToArray();

            before = NormalUncertainty.GetTriangleNormalUncertainty(samplesBefore[0], samplesBefore[1], samplesBefore[2]);
            after0 = NormalUncertainty.GetTriangleNormalUncertainty(samplesAfter0[0], samplesAfter0[1], samplesAfter0[2]);
            after1 = NormalUncertainty.GetTriangleNormalUncertainty(samplesAfter1[0], samplesAfter1[1], samplesAfter1[2]);

            return (before, after0, after1);
        }
    }

    public abstract class ExpectedAdvantagePublisher(int ID)
    {
        public int ID { get; init; } = ID;  // ID of Face

        private List<ExpectedAdvantage> Observers { get; } = [];

        public void Update()
        {
            foreach (var observer in Observers)
                observer.Update(this);
        }

        public void RegisterObserver(ExpectedAdvantage observer)
        {
            Observers.Add(observer);
            observer.Update(this);
        }
        public abstract (double, double, double) Evaluate(int id, Axis axis);

    }
}
