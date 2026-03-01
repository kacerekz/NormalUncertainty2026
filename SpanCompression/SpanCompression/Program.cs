using ScottPlot;
using ScottPlot.Colormaps;
using SpanCompression._3rdParty;
using SpanCompression.Meshes;
using SpanCompression.ML;
using SpanCompression.Structures;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Numerics;

namespace SpanCompression
{
    internal class Program
    {
        static NeuralUncertaintyEstimator NeuralNet;

        private static string filename;
        private static float minPrecision;
        private static float maxPrecision;
        private static float decrement;
        private static int measurements;

        public static bool UseNeural { get; private set; } = false;
        public static bool UseHalton { get; private set; } = true;

        static Stopwatch sw = new();

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            using var neuralNet = new NeuralUncertaintyEstimator("model/v7/uncertainty_model_3d.onnx");
            NeuralNet = neuralNet;

            //filename = @"C:\Data\Common\Simplified\bunny2_1000.obj";
            //minPrecision = 0.1f;
            //maxPrecision = 0f;
            //decrement = 0.02f;
            //measurements = 10;
            //Experiment01();

            //filename = @"C:\Data\Common\Simplified\bunny2_2000.obj";
            //minPrecision = 0.04f;
            //maxPrecision = 0.001f;
            //decrement = 0.005f;
            //measurements = 16;
            //Experiment01();
            //var elapsed1 = sw.Elapsed.TotalSeconds;

            //filename = @"C:\Data\Common\Simplified\armadillo2_10000.obj";
            //minPrecision = 0.008f;
            //maxPrecision = 0f;
            //decrement = 0.001f;
            //measurements = 16;
            //Experiment01();
            //var elapsed2 = sw.Elapsed.TotalSeconds;

            //filename = @"C:\Data\Common\lion.obj";
            //minPrecision = 0.05f;
            //maxPrecision = 0f;
            //decrement = 0.01f;
            //measurements = 16;
            //Experiment01();
            //var elapsed3 = sw.Elapsed.TotalSeconds;

            filename = @"C:\Data\Common\fandisk.obj";
            minPrecision = 0.05f;
            maxPrecision = 0.019f;
            decrement = 0.01f;
            measurements = 2;
            Experiment01();
            var elapsed4 = sw.Elapsed.TotalSeconds;

            //filename = @"C:\Data\Common\maxplanck.obj";
            //minPrecision = 0.005f;
            //maxPrecision = 0f;
            //decrement = 0.001f;
            //measurements = 16;
            //Experiment01();
            //var elapsed5 = sw.Elapsed.TotalSeconds;

            //PrettyTime("bunny", elapsed1);
            //PrettyTime("armadillo", elapsed2);
            //PrettyTime("lion", elapsed3);
            //PrettyTime("fandisk", elapsed4);
            //PrettyTime("planck", elapsed5);

            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Corner, "max_corner.obj");
            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Halton, "max_halton.obj");
            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Neural, "max_neural.obj");

            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Corner, "arm_expa_corner.obj");
            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Halton, "arm_expa_halton.obj");
            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Neural, "arm_expa_neural.obj");

            //int refinements = 41130;
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Halton, refinements, $"maxplanck_refined_halton_{refinements}.obj");
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Corner, refinements, $"maxplanck_refined_corner_{refinements}.obj");
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Neural, refinements, $"maxplanck_refined_neural_{refinements}.obj");

            //EvaluateDirectoryMetrics(@"C:\Users\adria\Desktop\imp max\meshes");
        }

        public static void PrettyTime(string name, double totalSeconds)
        {
            Console.WriteLine($"{name}\t.....\t{totalSeconds}s");
        }

        public static void EvaluateDirectoryMetrics(string directoryPath)
        {
            Console.WriteLine($"Starting metric evaluation in directory: {directoryPath}");

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: Directory not found at {directoryPath}");
                return;
            }

            // Define expected file paths
            string basePath = Path.Combine(directoryPath, "base.obj");
            string cornerPath = Path.Combine(directoryPath, "corner.obj");
            string haltonPath = Path.Combine(directoryPath, "halton.obj");
            string neuralPath = Path.Combine(directoryPath, "neural.obj");

            // Validate base mesh exists
            if (!File.Exists(basePath))
            {
                Console.WriteLine($"Error: Base mesh 'base.obj' is missing in {directoryPath}. Cannot compute metrics.");
                return;
            }

            // Set up test configurations
            var testMeshes = new Dictionary<string, string>
    {
        { "Corner", cornerPath },
        { "Halton", haltonPath },
        { "Neural", neuralPath }
    };

            // Prepare lists to store results for file writing
            var resultsLines = new List<string>
    {
        "Mesh,DAME,FMPD,MSDM,MSDM2" // CSV Header
    };

            // Console Table Header
            Console.WriteLine(new string('-', 65));
            Console.WriteLine($"{"Mesh",-10} | {"DAME",-10} | {"FMPD",-10} | {"MSDM",-10} | {"MSDM2",-10}");
            Console.WriteLine(new string('-', 65));

            // Evaluate each available test mesh
            foreach (var meshKvp in testMeshes)
            {
                string meshName = meshKvp.Key;
                string testPath = meshKvp.Value;

                if (!File.Exists(testPath))
                {
                    Console.WriteLine($"{meshName,-10} | File missing, skipping...");
                    continue;
                }

                // Compute metrics
                double dame = Metrics.DAME(basePath, testPath);
                double fmpd = Metrics.FMPD(basePath, testPath);
                double msdm = Metrics.MSDM(basePath, testPath);
                double msdm2 = Metrics.MSDM2(basePath, testPath);

                // Format for console (using :F5 for 5 decimal places)
                Console.WriteLine($"{meshName,-10} | {dame,-10:F6} | {fmpd,-10:F6} | {msdm,-10:F6} | {msdm2,-10:F6}");

                // Format for CSV
                resultsLines.Add($"{meshName},{dame},{fmpd},{msdm},{msdm2}");
            }

            Console.WriteLine(new string('-', 65));

            // Write to CSV
            string csvOutputPath = Path.Combine(directoryPath, "metrics_summary.csv");
            try
            {
                File.WriteAllLines(csvOutputPath, resultsLines);
                Console.WriteLine($"Metrics successfully saved to: {csvOutputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing CSV file: {ex.Message}");
            }
        }

        public static void RunExactRefinementsAndExport(string filename, float precision, EvaluationMode mode, int targetRefinements, string outputPath)
        {
            Console.WriteLine($"Running Edgebreaker for {filename} at precision {precision}...");
            var ebOutput = Edgebreaker.Run(filename, precision);

            if (ebOutput == null)
            {
                Console.WriteLine("Edgebreaker compression failed.");
                return;
            }

            // Initialize meshes and neighborhood lookups
            var originalMesh = ObjLoader.Load(filename);
            var coarseMesh = new CoarseMesh(ebOutput, precision);
            var np = new NeighborhoodProvider(originalMesh);
            var sampler = new CornerSampler();

            // Initialize Predictors
            var predictor1 = new SimplePredictor(coarseMesh, np);
            var predictor2 = new ParallelogramAveragePredictor(coarseMesh, np.cornerTable);

            int cellCount = coarseMesh.Cells.Length;
            int faceCount = coarseMesh.Faces.Length;

            var expa = new ExpectedAdvantage[3 * cellCount];
            var expc = new ExpectedAdvantageComponent[faceCount];
            var pq = new PriorityQueue<ExpectedAdvantage>(Comparer<ExpectedAdvantage>.Default);

            bool useNeural = mode == EvaluationMode.Neural;
            bool useHalton = mode == EvaluationMode.Halton;

            if (useNeural && NeuralNet == null)
                throw new InvalidOperationException("NeuralNet must be initialized before using Neural mode.");

            Console.WriteLine("Initializing components and queue...");

            // 1. Initialize Components
            for (int i = 0; i < faceCount; i++)
            {
                var face = coarseMesh.Faces[i];
                var cells = new Dictionary<int, Cell>
                {
                    [face.i1] = coarseMesh.Cells[face.i1],
                    [face.i2] = coarseMesh.Cells[face.i2],
                    [face.i3] = coarseMesh.Cells[face.i3]
                };

                expc[i] = new ExpectedAdvantageComponent(i, cells, sampler)
                {
                    NeuralBrain = NeuralNet,
                    UseNeural = useNeural,
                    UseHalton = useHalton
                };
            }

            // 2. Initialize Spans and register observers
            for (int i = 0; i < cellCount; i++)
            {
                var cell = coarseMesh.Cells[i];
                var vf = np.VF(cell.ID);

                expa[3 * i + 0] = new ExpectedAdvantage(cell.SpanX);
                expa[3 * i + 1] = new ExpectedAdvantage(cell.SpanY);
                expa[3 * i + 2] = new ExpectedAdvantage(cell.SpanZ);

                foreach (var faceIndex in vf)
                {
                    expc[faceIndex].RegisterObserver(expa[3 * i + 0]);
                    expc[faceIndex].RegisterObserver(expa[3 * i + 1]);
                    expc[faceIndex].RegisterObserver(expa[3 * i + 2]);
                }

                pq.Add(expa[3 * i + 0]);
                pq.Add(expa[3 * i + 1]);
                pq.Add(expa[3 * i + 2]);
            }

            // Array to track how many times a vertex is refined per axis [X, Y, Z]
            var improved = new double[cellCount][];
            for (int i = 0; i < cellCount; i++) improved[i] = new double[3];

            Console.WriteLine($"Starting exact refinement loop ({targetRefinements} iterations)...");

            // 3. The Refinement Loop
            for (int imp = 0; imp < targetRefinements; imp++)
            {
                if ((imp + 1) % 2500 == 0) Console.WriteLine($"Refinement {imp + 1}/{targetRefinements}...");

                // Dequeue highest Expected Advantage
                var max = pq.PopMaximum();
                var id = max.Span.parent.ID;

                // Predict
                var prediction1 = predictor1.Predict(max.Span);
                var prediction2 = predictor2.Predict(max.Span);

                // Fetch truth and correct
                var originalValue = GetTrueValue(max.Span, originalMesh.Vertices[id]);
                var truth = max.Span.Split(originalValue);

                predictor1.Correct(prediction1, truth);
                predictor2.Correct(prediction2, truth);

                // Track improvements
                int axi = (int)max.Span.axis;
                improved[id][axi]++;

                // Update dependencies
                var vf = np.VF(id);
                var vv = np.VV(id);

                foreach (var fi in vf) expc[fi].Update();

                pq.Add(max);

                foreach (var vi in vv)
                {
                    pq.Update(expa[3 * vi + 0]);
                    pq.Update(expa[3 * vi + 1]);
                    pq.Update(expa[3 * vi + 2]);
                }
            }

            Console.WriteLine("Refinements complete. Calculating bitrates...");

            // 4. Bitrate Calculations
            long bitsFirst = ebOutput.GeometryBits; // Base EB cost

            // Predictor 1 (Simple/Neighborhood Average)
            var bits1 = predictor1.GetBooleans();
            using var ms1 = new MemoryStream();
            ArithCoder.encode(bits1, ms1);
            long acBits1 = ms1.Position * 8;
            long usedBits1 = Math.Min(bitsFirst + targetRefinements, bitsFirst + acBits1) + 1;

            // Predictor 2 (Parallelogram)
            var bits2 = predictor2.GetBooleans();
            using var ms2 = new MemoryStream();
            ArithCoder.encode(bits2, ms2);
            long acBits2 = ms2.Position * 8;
            long usedBits2 = Math.Min(bitsFirst + targetRefinements, bitsFirst + acBits2) + 1;

            Console.WriteLine("--- BITRATE RESULTS ---");
            Console.WriteLine($"Base Geometry Bits:      {bitsFirst}");
            Console.WriteLine($"Target Refinements:      {targetRefinements}");
            Console.WriteLine($"Predictor 1 (Simple):    {usedBits1} bits");
            Console.WriteLine($"Predictor 2 (Parallelogram): {usedBits2} bits");
            Console.WriteLine("-----------------------");

            // 5. Color Mapping & Export
            var cmesh = coarseMesh.ToMesh();
            // Maps CMY to Dark/Black for heavy improvements
            cmesh.ColorByInverseChannelIntensity(improved);
            cmesh.Write(outputPath);

            Console.WriteLine($"Mesh exported successfully to: {outputPath}");
        }

        public static void ExportExpectedAdvantageMesh(string filename, float precision, EvaluationMode mode, string outputPath)
        {
            Console.WriteLine($"Running Edgebreaker for {filename} at precision {precision}...");
            var ebOutput = Edgebreaker.Run(filename, precision);

            if (ebOutput == null)
            {
                Console.WriteLine("Edgebreaker compression failed.");
                return;
            }

            var originalMesh = ObjLoader.Load(filename);
            var coarseMesh = new CoarseMesh(ebOutput, precision);
            var np = new NeighborhoodProvider(originalMesh);
            var sampler = new CornerSampler();

            int cellCount = coarseMesh.Cells.Length;
            int faceCount = coarseMesh.Faces.Length;
            var expa = new ExpectedAdvantage[3 * cellCount];
            var expc = new ExpectedAdvantageComponent[faceCount];

            // Map the selected mode to the boolean flags used by ExpectedAdvantageComponent
            bool useNeural = mode == EvaluationMode.Neural;
            bool useHalton = mode == EvaluationMode.Halton;

            if (useNeural && NeuralNet == null)
                throw new InvalidOperationException("NeuralNet must be initialized before using Neural mode.");

            Console.WriteLine($"Initializing Expected Advantage Components for {mode} mode...");

            // 1. Initialize Components (Faces)
            for (int i = 0; i < faceCount; i++)
            {
                var face = coarseMesh.Faces[i];
                var cells = new Dictionary<int, Cell>
                {
                    [face.i1] = coarseMesh.Cells[face.i1],
                    [face.i2] = coarseMesh.Cells[face.i2],
                    [face.i3] = coarseMesh.Cells[face.i3]
                };

                expc[i] = new ExpectedAdvantageComponent(i, cells, sampler)
                {
                    NeuralBrain = NeuralNet,
                    UseNeural = useNeural,
                    UseHalton = useHalton
                };
            }

            Console.WriteLine("Registering Observers (Vertices)...");

            // 2. Initialize Spans and Register Observers
            for (int i = 0; i < cellCount; i++)
            {
                var cell = coarseMesh.Cells[i];
                var vf = np.VF(cell.ID);

                expa[3 * i + 0] = new ExpectedAdvantage(cell.SpanX);
                expa[3 * i + 1] = new ExpectedAdvantage(cell.SpanY);
                expa[3 * i + 2] = new ExpectedAdvantage(cell.SpanZ);

                for (int j = 0; j < vf.Length; j++)
                {
                    expc[vf[j]].RegisterObserver(expa[3 * i + 0]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 1]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 2]);
                }
            }

            Console.WriteLine("Evaluating Initial Expected Advantage...");

            // 3. Evaluate and apply the Global Shift
            var values = new double[cellCount][];
            double globalMin = double.MaxValue;

            // Collect all raw values and find the absolute minimum
            for (int i = 0; i < cellCount; i++)
            {
                values[i] = new double[3];
                values[i][0] = expa[3 * i + 0].Evaluate();
                values[i][1] = expa[3 * i + 1].Evaluate();
                values[i][2] = expa[3 * i + 2].Evaluate();

                globalMin = Math.Min(globalMin, values[i][0]);
                globalMin = Math.Min(globalMin, values[i][1]);
                globalMin = Math.Min(globalMin, values[i][2]);
            }

            Console.WriteLine($"Applying global shift of {-globalMin:F6} to prevent negative clipping...");

            // Shift all values so the minimum is exactly 0.0
            for (int i = 0; i < cellCount; i++)
            {
                values[i][0] -= globalMin;
                values[i][1] -= globalMin;
                values[i][2] -= globalMin;
            }

            // 4. Color and Export
            var cmesh = coarseMesh.ToMesh();
            cmesh.ColorByInverseChannelIntensity(values);
            cmesh.Write(outputPath);

            Console.WriteLine($"Export complete. EA mesh saved to: {outputPath}");
        }

        public enum EvaluationMode
        {
            Corner,
            Halton,
            Neural
        }

        public static void ExportColoredUncertaintyMesh(string filename, float precision, EvaluationMode mode, string outputPath)
        {
            Console.WriteLine($"Running Edgebreaker for {filename} at precision {precision}...");
            var ebOutput = Edgebreaker.Run(filename, precision);

            if (ebOutput == null)
            {
                Console.WriteLine("Edgebreaker compression failed.");
                return;
            }

            // Build the coarse mesh from the Edgebreaker simulation output
            var coarseMesh = new CoarseMesh(ebOutput, precision);

            // Array to hold the accumulated U_v for each vertex
            double[] vertexUncertainties = new double[coarseMesh.Cells.Length];

            Console.WriteLine($"Evaluating Normal Uncertainty using {mode} mode...");

            // Evaluate U_f for each face and distribute it to its vertices
            for (int i = 0; i < coarseMesh.Faces.Length; i++)
            {
                if (i % 1000 == 0) Console.WriteLine($"Processed {i}/{coarseMesh.Faces.Length} faces...");

                var face = coarseMesh.Faces[i];
                Cell c1 = coarseMesh.Cells[face.i1];
                Cell c2 = coarseMesh.Cells[face.i2];
                Cell c3 = coarseMesh.Cells[face.i3];

                double u_f = 0.0;

                switch (mode)
                {
                    case EvaluationMode.Corner:
                        var pts1 = c1.GetCorners();
                        var pts2 = c2.GetCorners();
                        var pts3 = c3.GetCorners();
                        u_f = NormalUncertainty.GetTriangleNormalUncertainty(pts1, pts2, pts3);
                        break;

                    case EvaluationMode.Halton:
                        u_f = FastUncertainty.CalculateAdaptiveHalton(c1, c2, c3);
                        break;

                    case EvaluationMode.Neural:
                        if (NeuralNet == null)
                            throw new InvalidOperationException("NeuralNet must be initialized in Main() first.");
                        u_f = NeuralNet.Predict(c1, c2, c3);
                        break;
                }

                // Add the face's uncertainty to its three constituent vertices
                vertexUncertainties[face.i1] += u_f;
                vertexUncertainties[face.i2] += u_f;
                vertexUncertainties[face.i3] += u_f;
            }

            // Convert to standard mesh, apply coloring, and export
            var mesh = coarseMesh.ToMesh();
            mesh.ColorByValues(vertexUncertainties);
            mesh.Write(outputPath);

            Console.WriteLine($"Export complete. Colored mesh saved to: {outputPath}");
        }

        public static void PrintAndResetHitStats()
        {
            long neuralHits = ExpectedAdvantageComponent.NeuralHits;
            long haltonHits = ExpectedAdvantageComponent.HaltonHits;
            long totalHits = neuralHits + haltonHits;

            Console.WriteLine($"Neural hitcount: {neuralHits}");
            Console.WriteLine($"Halton hitcount: {haltonHits}");

            if (totalHits > 0)
            {
                double neuralPercentage = (double)neuralHits / totalHits * 100;
                Console.WriteLine($"Neural utilization: {neuralPercentage:F2}%");
            }
            else
            {
                Console.WriteLine("Neural utilization: 0.00% (No hits recorded)");
            }

            ExpectedAdvantageComponent.NeuralHits = 0;
            ExpectedAdvantageComponent.HaltonHits = 0;
        }

        public static string CreateOutputDirectory()
        {
            var timestamp = DateTime.Now.ToString("-dd-MM-yyyy-(HH-mm-ss)");
            var outputDir = Directory.CreateDirectory($"output/{timestamp}");
            var outputDirPath = outputDir.FullName;
            return outputDirPath;
        }

        private static void Experiment01()
        {
            // Parameters:
            var outputDir = CreateOutputDirectory();

            var sampler = new CornerSampler();
            var originalMesh = ObjLoader.Load(filename);
            var neighborhoodProvider = new NeighborhoodProvider(originalMesh);

            using (var sw = new StreamWriter($"{outputDir}/log.txt"))
            {
                sw.WriteLine("Input file: " + filename);
                sw.WriteLine("Initial precision: " + minPrecision);
                sw.WriteLine("Target precision: " + maxPrecision);
                sw.WriteLine("Decrement by: " + decrement);
                sw.WriteLine("Measurements: " + measurements);
                sw.WriteLine(sampler);
            }

            Utils.OpenFileExplorer();

            long bitsFirst;
            long bitsLast;

            // First encode meshes with edgebreaker and measure all metrics
            string[] metrics = ["DAME", "DAME -p", "FMPD", "MSDM", "MSDM2", "MSE", "MaxErr", "NUNC", "Improvements", "Time"];

            var x1 = new List<long>();
            var x2 = new List<long>();
            var x3 = new List<long>();

            var y1 = new List<double>[metrics.Length];
            var y2 = new List<double>[metrics.Length];
            var y3 = new List<double>[metrics.Length];

            for (int i = 0; i < metrics.Length; i++)
            {
                y1[i] = [];
                y2[i] = [];
                y3[i] = [];
            }

            RunEdgebreaker(outputDir, filename, minPrecision, maxPrecision, decrement, sampler, originalMesh, metrics, x1, y1);

            bitsFirst = x1[0];
            bitsLast = x1[^1];

            var checkpoints = GetCheckpoints(measurements, bitsFirst, bitsLast);

            long usedBits1 = bitsFirst;
            long usedBits2 = bitsFirst;
            long improvements = 0;
            int checkpoint = 0;

            var edgebreakerOutput = Edgebreaker.Run(filename, minPrecision);

            if (edgebreakerOutput == null)
            {
                Console.WriteLine("Failed to run Edgebreaker.");
                return;
            }

            sw.Reset();
            sw.Start();

            var coarseMesh = new CoarseMesh(edgebreakerOutput, minPrecision);
            var predictor1 = new SimplePredictor(coarseMesh, neighborhoodProvider);
            var predictor2 = new ParallelogramAveragePredictor(coarseMesh, neighborhoodProvider.cornerTable);

            List<APredictor> predictors = [];
            predictors.Add(predictor1);
            predictors.Add(predictor2);

            int cellCount = coarseMesh.Cells.Length;
            int faceCount = coarseMesh.Faces.Length;
            var expa = new ExpectedAdvantage[3 * cellCount];
            var expc = new ExpectedAdvantageComponent[faceCount];
            var cmp = Comparer<ExpectedAdvantage>.Default;
            var pq = new PriorityQueue<ExpectedAdvantage>(cmp);

            //Console.WriteLine("Init EXPC...");

            for (int i = 0; i < faceCount; i++)
            {
                var face = coarseMesh.Faces[i];
                var cells = new Dictionary<int, Cell>
                {
                    [face.i1] = coarseMesh.Cells[face.i1],
                    [face.i2] = coarseMesh.Cells[face.i2],
                    [face.i3] = coarseMesh.Cells[face.i3]
                };
                expc[i] = new ExpectedAdvantageComponent(i, cells, sampler)
                {
                    NeuralBrain = NeuralNet,
                    UseNeural = UseNeural, // <--- Toggle this to switch between Neural and Halton
                    UseHalton = UseHalton // <--- Toggle this to switch between Neural and Halton
                };
            }

            //Console.WriteLine("Init EXPA...");

            for (int i = 0; i < cellCount; i++)
            {
                if (i % 100 == 0)
                    Console.WriteLine($"{i + 1}/{cellCount}");

                var cell = coarseMesh.Cells[i];
                var vf = neighborhoodProvider.VF(cell.ID);

                expa[3 * i + 0] = new ExpectedAdvantage(cell.SpanX);
                expa[3 * i + 1] = new ExpectedAdvantage(cell.SpanY);
                expa[3 * i + 2] = new ExpectedAdvantage(cell.SpanZ);

                for (int j = 0; j < vf.Length; j++)
                {
                    expc[vf[j]].RegisterObserver(expa[3 * i + 0]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 1]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 2]);
                }
            }

            // Color by initial EXPA

            sw.Stop();

            var fn = $"{outputDir}/NU_EXPA.obj";
            var cmesh = coarseMesh.ToMesh();
            var values = new double[cellCount][];

            for (int i = 0; i < cellCount; i++)
            {
                values[i] = new double[3];
                values[i][0] = expa[3 * i + 0].Evaluate();
                values[i][1] = expa[3 * i + 1].Evaluate();
                values[i][2] = expa[3 * i + 2].Evaluate();
            }

            cmesh.ColorByInverseChannelIntensity(values);
            cmesh.Write(fn);

            sw.Start();

            // Init PQ

            for (int i = 0; i < expa.Length; i++)
                pq.Add(expa[i]);

            var improved = new double[cellCount][];
            for (int i = 0; i < cellCount; i++)
                improved[i] = new double[3];

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                if (Math.Min(usedBits1, usedBits2) >= checkpoints[checkpoint])
                {
                    sw.Stop();

                    var time = stopwatch.Elapsed.TotalSeconds;

                    Console.WriteLine($"Checkpoint {checkpoint} at {checkpoints[checkpoint]} bits and {improvements} improvements.");
                    Console.WriteLine($"  - Simple predictor used {usedBits1} bits.");
                    Console.WriteLine($"  - Paral. predictor used {usedBits2} bits.");

                    var fnMesh = $"{outputDir}/NU_{improvements}.obj";
                    var fnLog1 = $"{outputDir}/NU_{usedBits1:000000}_({improvements}).1.txt";
                    var fnLog2 = $"{outputDir}/NU_{usedBits2:000000}_({improvements}).2.txt";

                    cmesh = coarseMesh.ToMesh();
                    cmesh.ColorByInverseChannelIntensity(improved);
                    cmesh.Write(fnMesh);

                    predictor1.Write(fnLog1);
                    predictor2.Write(fnLog2);

                    var dame1 = Metrics.DAME(filename, fnMesh);
                    var dame2 = Metrics.DAME(filename, fnMesh, true);
                    var fmpd = Metrics.FMPD(filename, fnMesh);
                    var msdm1 = Metrics.MSDM(filename, fnMesh);
                    var msdm2 = Metrics.MSDM2(filename, fnMesh);
                    var mse = Metrics.MSE(originalMesh, cmesh);
                    var maxerr = Metrics.MaxErr(originalMesh, cmesh);
                    var nunc = 0; // Metrics.NUNC(coarseMesh, sampler);

                    x2.Add(usedBits1);
                    x3.Add(usedBits2);

                    y2[0].Add(dame1);
                    y2[1].Add(dame2);
                    y2[2].Add(fmpd);
                    y2[3].Add(msdm1);
                    y2[4].Add(msdm2);
                    y2[5].Add(mse);
                    y2[6].Add(maxerr);
                    y2[7].Add(0);
                    //y2[7].Add(nunc.Values.Sum());
                    y2[8].Add(improvements);
                    y2[9].Add(time);

                    y3[0].Add(dame1);
                    y3[1].Add(dame2);
                    y3[2].Add(fmpd);
                    y3[3].Add(msdm1);
                    y3[4].Add(msdm2);
                    y3[5].Add(mse);
                    y3[6].Add(maxerr);
                    y3[7].Add(0);
                    //y3[7].Add(nunc.Values.Sum());
                    y3[8].Add(improvements);
                    y3[9].Add(time);

                    WriteMetrics(x2, y2, metrics, "nunc.predictor1", outputDir, filename);
                    WriteMetrics(x3, y3, metrics, "nunc.predictor2", outputDir, filename);

                    Plot(x1, y1, x2, y2, metrics, "edgebreaker", "nunc.predictor1", outputDir, filename);
                    Plot(x1, y1, x3, y3, metrics, "edgebreaker", "nunc.predictor2", outputDir, filename);

                    sw.Start();
                    stopwatch.Restart();
                    checkpoint++;
                    if (checkpoint >= checkpoints.Length)
                        break;
                }

                improvements++;

                var max = pq.PopMaximum();
                var id = max.Span.parent.ID;

                var prediction1 = predictor1.Predict(max.Span);
                var prediction2 = predictor2.Predict(max.Span);

                var originalValue = GetTrueValue(max.Span, originalMesh.Vertices[id]);
                var truth = max.Span.Split(originalValue);

                predictor1.Correct(prediction1, truth);
                predictor2.Correct(prediction2, truth);

                int axi = (int)max.Span.axis;
                improved[id][axi]++;

                var vf = neighborhoodProvider.VF(id);
                var vv = neighborhoodProvider.VV(id);

                foreach (var fi in vf)
                    expc[fi].Update();

                pq.Add(max);

                foreach (var vi in vv)
                {
                    pq.Update(expa[3 * vi + 0]);
                    pq.Update(expa[3 * vi + 1]);
                    pq.Update(expa[3 * vi + 2]);
                }

                var bits1 = predictor1.GetBooleans();
                var bits2 = predictor2.GetBooleans();

                // Check how many bits we are using right now
                var ms1 = new MemoryStream();
                ArithCoder.encode(bits1.ToArray(), ms1);
                var acBits1 = ms1.Position * 8;

                // Check how many bits we are using right now
                var ms2 = new MemoryStream();
                ArithCoder.encode(bits2.ToArray(), ms2);
                var acBits2 = ms2.Position * 8;

                // For small amounts of improvements and small models etc, Arithcoder may have a bigger overhead
                // than the bit savings it can actually provide
                // Therefore we can pick whether to use it or not (choose the one that uses fewer bits).
                // The toggle is covered by the +1
                // I mean I might be overthinking this ngl but this feels kinda like the free-ish best of both worlds
                usedBits1 = Math.Min(bitsFirst + improvements, bitsFirst + acBits1) + 1;
                usedBits2 = Math.Min(bitsFirst + improvements, bitsFirst + acBits2) + 1;

                if (improvements % 10 == 0)
                    Console.WriteLine($"Used bits: {usedBits1} {usedBits2}, Improvements: {improvements}");
            }

            stopwatch.Stop();
            PrintAndResetHitStats();
        }

        private static void RunEdgebreaker(string outputDir, string filename, float minPrecision, float maxPrecision, float decrement, ISampler sampler, Mesh originalMesh, string[] metrics, List<long> x1, List<double>[] y1)
        {
            for (var precision = minPrecision; precision > maxPrecision; precision -= decrement)
            {
                var edgebreakerOutput = Edgebreaker.Run(filename, precision);

                if (edgebreakerOutput == null)
                {
                    Console.WriteLine("Failed to run Edgebreaker.");
                    return;
                }

                var coarseMesh = new CoarseMesh(edgebreakerOutput, precision);

                var fn = $"{outputDir}/EB_{precision:0.00000}.obj";
                var mesh = ObjLoader.Load(edgebreakerOutput.OutputPath);
                mesh.Write(fn);

                var dame1 = Metrics.DAME(filename, fn);
                var dame2 = Metrics.DAME(filename, fn, true);
                var fmpd = Metrics.FMPD(filename, fn);
                var msdm1 = Metrics.MSDM(filename, fn);
                var msdm2 = Metrics.MSDM2(filename, fn);
                var mse = Metrics.MSE(originalMesh, mesh);
                var maxerr = Metrics.MaxErr(originalMesh, mesh);
                var nunc = Metrics.NUNC(coarseMesh, sampler);

                x1.Add(edgebreakerOutput.GeometryBits);

                y1[0].Add(dame1);
                y1[1].Add(dame2);
                y1[2].Add(fmpd);
                y1[3].Add(msdm1);
                y1[4].Add(msdm2);
                y1[5].Add(mse);
                y1[6].Add(maxerr);
                y1[7].Add(nunc.Values.Sum());
                y1[8].Add(0);
                y1[9].Add(0);

                WriteMetrics(x1, y1, metrics, "edgebreaker", outputDir, filename);
                Plot(x1, y1, [], [], metrics, "edgebreaker", "none", outputDir, filename);
            }
        }

        private static long[] GetCheckpoints(int measurements, long bitsFirst, long bitsLast)
        {
            var checkpoints = new long[measurements];
            var remainingBits = bitsLast - bitsFirst;
            var d = remainingBits / (float)(measurements - 1);
            checkpoints[0] = bitsFirst;
            checkpoints[^1] = bitsLast;

            for (int i = 1; i < checkpoints.Length - 1; i++)
            {
                checkpoints[i] = bitsFirst + (long)(i * d);
            }

            // Due to arithmetic coding the bits required to match edgebreaker bits are unknown up front

            Console.WriteLine("Bits to match Edgebreaker: " + remainingBits);
            Console.WriteLine("Measurement checkpoints:");
            Console.WriteLine("[" + string.Join(", ", checkpoints) + "]");

            return checkpoints;
        }

        private static void WriteMetrics(List<long> x, List<double>[] y, string[] metrics, string method, string outputDir, string filename)
        {
            using StreamWriter sw = new($"{outputDir}/{method}.metrics.txt", false);
            sw.WriteLine(filename);

            sw.Write("bits;");
            foreach (var m in metrics)
                sw.Write(m + ";");
            sw.WriteLine();

            for (int i = 0; i < x.Count; i++)
            {
                sw.Write($"{x[i]};");
                for (int m = 0; m < metrics.Length; m++)
                    sw.Write($"{y[m][i]};");
                sw.WriteLine();
            }
        }

        private static void Plot(List<long> x1, List<double>[] y1, List<long> x2, List<double>[] y2, string[] metrics, string method1, string method2, string outputDir, string filename)
        {
            for (int i = 0; i < metrics.Length; i++)
            {
                var fn = Path.GetFileNameWithoutExtension(filename);

                Plot myPlot = new();
                myPlot.Title($"{metrics[i]} vs. bitrate - {fn}");

                var s1 = myPlot.Add.Scatter(x1.ToArray(), y1[i].ToArray());
                s1.LegendText = method1;

                if (x2.Count > 0)
                {
                    var s2 = myPlot.Add.Scatter(x2.ToArray(), y2[i].ToArray());
                    s2.LegendText = method2;
                }

                myPlot.ShowLegend(Alignment.UpperRight);
                myPlot.SavePng($"{outputDir}/{method1}_{method2}_{metrics[i]}.png", 1000, 750);
            }
        }

        private static void TestImproveEXPA()
        {
            //var filename = @"C:\Data\Common meshes\Simplified\bunny2_1000.obj";
            var filename = @"C:\Data\Common meshes\Simplified\armadillo2_10000.obj";
            //var filename = @"C:\Data\Common meshes\maxplanck.obj";

            var precision = 0.003f;

            var edgebreakerOutput = Edgebreaker.Run(filename, precision);
            if (edgebreakerOutput == null)
                return;

            // Calculate initial normal uncertainty

            var originalMesh = ObjLoader.Load(filename);
            var coarseMesh = new CoarseMesh(edgebreakerOutput, precision);
            var stopwatch = Stopwatch.StartNew();
            var sampler = new CornerSampler();
            //var nunc = Metrics.NUNC(coarseMesh, sampler);

            // Calculate initial EXPA

            int cellCount = coarseMesh.Cells.Length;
            int faceCount = coarseMesh.Faces.Length;
            var expa = new ExpectedAdvantage[3 * cellCount];
            var expc = new ExpectedAdvantageComponent[faceCount];
            var cmp = Comparer<ExpectedAdvantage>.Default;
            var pq = new PriorityQueue<ExpectedAdvantage>(cmp);
            var np = new NeighborhoodProvider(originalMesh);

            Console.WriteLine("Init EXPC...");

            for (int i = 0; i < faceCount; i++)
            {
                var face = coarseMesh.Faces[i];
                var cells = new Dictionary<int, Cell>
                {
                    [face.i1] = coarseMesh.Cells[face.i1],
                    [face.i2] = coarseMesh.Cells[face.i2],
                    [face.i3] = coarseMesh.Cells[face.i3]
                };
                expc[i] = new ExpectedAdvantageComponent(i, cells, sampler);
            }

            Console.WriteLine("Init EXPA...");

            for (int i = 0; i < cellCount; i++)
            {
                Console.WriteLine($"{i + 1}/{cellCount}");
                //Console.WriteLine($"{i}");

                var cell = coarseMesh.Cells[i];
                var vf = np.VF(cell.ID);

                expa[3 * i + 0] = new ExpectedAdvantage(cell.SpanX);
                expa[3 * i + 1] = new ExpectedAdvantage(cell.SpanY);
                expa[3 * i + 2] = new ExpectedAdvantage(cell.SpanZ);

                for (int j = 0; j < vf.Length; j++)
                {
                    expc[vf[j]].RegisterObserver(expa[3 * i + 0]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 1]);
                    expc[vf[j]].RegisterObserver(expa[3 * i + 2]);
                }
            }

            // Color by current EXPA

            var cmesh = coarseMesh.ToMesh();
            var values = new double[cellCount][];

            for (int i = 0; i < cellCount; i++)
            {
                values[i] = new double[3];
                values[i][0] = expa[3 * i + 0].Evaluate();
                values[i][1] = expa[3 * i + 1].Evaluate();
                values[i][2] = expa[3 * i + 2].Evaluate();
            }

            cmesh.ColorByInverseChannelIntensity(values);
            cmesh.Write("initial_expa.obj");
            Utils.OpenFileExplorer();

            // Add all to priority queue by EXPA

            Console.WriteLine("Init PQ...");

            for (int i = 0; i < expa.Length; i++)
            {
                pq.Add(expa[i]);
            }

            //for (int i = 0; i < expa.Length; i++) // Try to take all out - result: sorted from highest to lowest
            //{
            //    var max = pq.PopMaximum();
            //    Console.WriteLine(max.Evaluate());
            //}

            // Run improvement loop

            Console.WriteLine("Improve...");

            int improvements = 42_000;
            var improved = new double[cellCount][];
            for (int i = 0; i < cellCount; i++)
                improved[i] = new double[3];

            for (int imp = 0; imp < improvements; imp++)
            {
                Console.WriteLine($"{imp + 1}/{improvements}");

                var max = pq.PopMaximum();
                var id = max.Span.parent.ID;
                var truth = GetTrueValue(max.Span, originalMesh.Vertices[id]);
                max.Span.Split(truth);

                int axi = (int)max.Span.axis;
                improved[id][axi]++;

                var vf = np.VF(id);
                var vv = np.VV(id);

                foreach (var fi in vf)
                    expc[fi].Update();

                pq.Add(max);

                foreach (var vi in vv)
                {
                    pq.Update(expa[3 * vi + 0]);
                    pq.Update(expa[3 * vi + 1]);
                    pq.Update(expa[3 * vi + 2]);
                }

                if ((imp + 1) % 100 == 0)
                {
                    cmesh = coarseMesh.ToMesh();
                    cmesh.ColorByInverseChannelIntensity(improved);
                    cmesh.Write($"improved_{imp + 1}.obj");
                    Utils.OpenFileExplorer();
                }
            }

            //cmesh = coarseMesh.ToMesh();
            //cmesh.ColorByInverseChannelIntensity(improved);
            //cmesh.Write("improved_expa.obj");
            //Utils.OpenFileExplorer();

            stopwatch.Stop();
            Console.WriteLine("Time: " + stopwatch.Elapsed.TotalSeconds + "s");

            //var cmesh = coarseMesh.ToMesh();
            //var values = nunc
            //    .OrderBy(kv => kv.Key)
            //    .Select(kv => kv.Value)
            //    .ToArray();
            //cmesh.ColorByValues(values);
            //cmesh.Write("improved.obj");
            //Utils.OpenFileExplorer();
        }

        private static float GetTrueValue(Span span, Vector3 v)
        {
            return span.axis switch
            {
                Axis.X => v.X,
                Axis.Y => v.Y,
                Axis.Z => v.Z,
                _ => float.NaN,
            };
        }

        [Obsolete]
        private static void TestImproveNUNC()
        {
            //var filename = @"C:\Data\Common meshes\Simplified\bunny2_1000.obj";
            var filename = @"C:\Data\Common meshes\Simplified\armadillo2_10000.obj";
            //var filename = @"C:\Data\Common meshes\maxplanck.obj";

            var precision = 0.003f;

            var edgebreakerOutput = Edgebreaker.Run(filename, precision);
            if (edgebreakerOutput == null)
                return;

            var realMesh = ObjLoader.Load(filename);
            var coarseMesh = new CoarseMesh(edgebreakerOutput, precision);
            var stopwatch = Stopwatch.StartNew();
            var sampler = new CornerSampler();
            var nunc = Metrics.NUNC(coarseMesh, sampler);
            stopwatch.Stop();

            var comparer = Comparer<Cell>
                .Create((a, b) => nunc[a.ID].CompareTo(nunc[b.ID]));
            var pq = new PriorityQueue<Cell>(comparer);

            for (int i = 0; i < coarseMesh.Cells.Length; i++)
            {
                pq.Add(coarseMesh.Cells[i]);
            }

            var np = new NeighborhoodProvider(realMesh);

            int improvements = 21;
            int[] improved = new int[coarseMesh.Cells.Length];

            for (int impr = 0; impr < improvements; impr++)
            {
                if (impr == 20)
                    Console.WriteLine();

                var max = pq.PopMaximum();
                Console.WriteLine(nunc[max.ID]);
                improved[max.ID]++;

                //Console.WriteLine($"{max.ID}   {nunc[max.ID]}");

                Console.WriteLine("B:");
                Console.WriteLine($"{max.SpanX.Min} {max.SpanX.Max}");
                Console.WriteLine($"{max.SpanY.Min} {max.SpanY.Max}");
                Console.WriteLine($"{max.SpanZ.Min} {max.SpanZ.Max}");

                max.SpanX.Split(realMesh.Vertices[max.ID].X);
                max.SpanY.Split(realMesh.Vertices[max.ID].Y);
                max.SpanZ.Split(realMesh.Vertices[max.ID].Z);

                Console.WriteLine("A:");
                Console.WriteLine($"{max.SpanX.Min} {max.SpanX.Max}");
                Console.WriteLine($"{max.SpanY.Min} {max.SpanY.Max}");
                Console.WriteLine($"{max.SpanZ.Min} {max.SpanZ.Max}");

                int[] vf = np.VF(max.ID);
                Face[] faces = new Face[vf.Length];

                for (int i = 0; i < faces.Length; i++)
                    faces[i] = coarseMesh.Faces[vf[i]];

                HashSet<int> cellIndices = [];

                for (int i = 0; i < faces.Length; i++)
                {
                    var f = faces[i];
                    cellIndices.Add(f.i1);
                    cellIndices.Add(f.i2);
                    cellIndices.Add(f.i3);
                }

                List<Cell> cells = [];
                foreach (var cellIndex in cellIndices)
                {
                    cells.Add(coarseMesh.Cells[cellIndex]);
                }

                var newNunc = Metrics.NUNC([.. cells], faces, sampler);
                nunc[max.ID] = newNunc[max.ID];

                pq.Add(max);
                Console.WriteLine(nunc[max.ID]);
                Console.WriteLine();

                //HashSet<int> cellIndices = [];
                //HashSet<int> faceIndices = [];

                //// Add all neighboring faces to max
                //var vv = np.VV(max.ID);
                //var vf = np.VF(max.ID);

                //foreach (var faceIndex in vf)
                //    faceIndices.Add(faceIndex);

                //// Add all neighboring to neighboring vertices (k=2)
                //foreach (var cellIndex in vv)
                //{
                //    vf = np.VF(cellIndex);
                //    foreach (var faceIndex in vf)
                //        faceIndices.Add(faceIndex);
                //}

                //// Add all cells mentioned in the face list
                //foreach (var faceIndex in faceIndices)
                //{
                //    cellIndices.Add(coarseMesh.Faces[faceIndex].i1);
                //    cellIndices.Add(coarseMesh.Faces[faceIndex].i2);
                //    cellIndices.Add(coarseMesh.Faces[faceIndex].i3);
                //}

                //List<Cell> allCells = [];
                //List<Face> allFaces = [];

                //foreach (var faceIndex in faceIndices)
                //    allFaces.Add(coarseMesh.Faces[faceIndex]);

                //foreach (var cellIndex in cellIndices)
                //    allCells.Add(coarseMesh.Cells[cellIndex]);

                //var improved = Metrics.NUNC([..allCells], [..allFaces], sampler);
                //nunc[max.ID] = improved[max.ID];
                //pq.Update(max);

                //foreach (var cellIndex in vv)
                //{
                //    nunc[cellIndex] = improved[cellIndex];
                //    pq.Update(coarseMesh.Cells[cellIndex]);
                //}
            }

            Console.WriteLine("Time: " + stopwatch.Elapsed.TotalSeconds + "s");

            var cmesh = coarseMesh.ToMesh();
            var values = nunc
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value)
                .ToArray();
            cmesh.ColorByValues(values);
            cmesh.Write("improved.obj");
            Utils.OpenFileExplorer();

            Console.WriteLine("Max improvements:" + improved.Max());
        }

        private static void TestCalculateNormalUncertainty()
        {
            //var filename = @"C:\Data\Common meshes\Simplified\bunny2_1000.obj";
            var filename = @"C:\Data\Common meshes\Simplified\armadillo2_10000.obj";
            //var filename = @"C:\Data\Common meshes\maxplanck.obj";

            var precision = 0.003f;

            var edgebreakerOutput = Edgebreaker.Run(filename, precision);
            if (edgebreakerOutput == null)
                return;

            var coarseMesh = new CoarseMesh(edgebreakerOutput, precision);
            var stopwatch = Stopwatch.StartNew();
            var nunc = Metrics.NUNC(coarseMesh, new RandomSampler(4));
            stopwatch.Stop();

            Console.WriteLine("Time: " + stopwatch.Elapsed.TotalSeconds + "s");

            var cmesh = coarseMesh.ToMesh();
            var values = nunc
                .OrderBy(kv => kv.Key)
                .Select(kv => kv.Value)
                .ToArray();
            cmesh.ColorByValues(values);
            cmesh.Write("normal_uncertainty.obj");
            Utils.OpenFileExplorer();
        }

        private static void TestCoarseMesh()
        {
            var filename = @"C:\Data\Common meshes\bunny2.obj";
            var precision = 0.003f;

            var edgebreakerOutput = Edgebreaker.Run(filename, precision);
            if (edgebreakerOutput == null)
                return;

            //var compressedMesh = ObjLoader.Load(edgebreakerOutput.OutputPath);
            //compressedMesh.Write("coarse.obj");
            //Utils.OpenWithMeshLab("coarse.obj");

            var coarseMesh = new CoarseMesh(edgebreakerOutput, precision);
            var cmesh = coarseMesh.ToMesh();
            var cells = coarseMesh.CellsToMesh();
            cmesh.Write("edgebreaker_cmesh.obj");
            cells.Write("edgebreaker_cells.obj");
            Utils.OpenFileExplorer();
        }

        private static void TestConcat()
        {
            // Define centers and sizes
            List<Vector3> centers =
            [
                new Vector3(0, 0, 0),
                new Vector3(2, 1, -1),
                new Vector3(-3, 0.5f, 2),
                new Vector3(1, 3, 1)
            ];

            List<Vector3> sizes =
            [
                new Vector3(1, 1, 1),
                new Vector3(2, 1, 0.5f),
                new Vector3(1, 2, 3),
                new Vector3(0.5f, 0.5f, 0.5f)
            ];

            List<Mesh> cuboids = [];

            for (int i = 0; i < centers.Count; i++)
            {
                var cuboid = new Cuboid(centers[i], sizes[i]);
                cuboids.Add(cuboid);
            }

            var concat = Mesh.ConcatenateMeshes([.. cuboids]);
            concat.Write("concat.obj");
            Utils.OpenWithMeshLab("concat.obj");
        }

        private static void TestCuboid()
        {
            Vector3 center = new Vector3(-1, 3, 2);
            Vector3 size = new Vector3(2, 1, 3); // Width (X), Height (Y), Depth (Z)
            Mesh cuboid = new Cuboid(center, size);
            cuboid.Write("cuboid.obj");
            Utils.OpenWithMeshLab("cuboid.obj");
        }

        private static void TestEdgebreaker()
        {
            var filename = @"C:\Data\Common meshes\bunny2.obj";
            var output = Edgebreaker.Run(filename, 0.01f);
            Console.WriteLine(output?.ToString());
            Utils.OpenWithMeshLab(output.OutputPath);
        }

        private static void TestMetrics()
        {
            Console.WriteLine("For two different (but similar) meshes:");
            TestMetrics(false);
            Console.WriteLine("For two of the same mesh:");
            TestMetrics(true);
        }

        private static void TestMetrics(bool same)
        {
            string fn1, fn2;

            if (same)
            {
                fn1 = @"C:\Data\Common meshes\armadillo.obj";
                fn2 = @"C:\Data\Common meshes\armadillo.obj";
            }
            else
            {
                fn1 = @"C:\Data\Common meshes\bunny2.obj";
                fn2 = @"C:\Data\Common meshes\bunny2 - Copy.obj";
            }

            var value = Metrics.DAME(fn1, fn2);
            Console.WriteLine("DAME: " + value);

            value = Metrics.DAME(fn1, fn2, true);
            Console.WriteLine("DAME -p: " + value);

            value = Metrics.MSDM(fn1, fn2);
            Console.WriteLine("MSDM: " + value);

            value = Metrics.MSDM2(fn1, fn2);
            Console.WriteLine("MSDM2: " + value);

            value = Metrics.FMPD(fn1, fn2);
            Console.WriteLine("FMPD: " + value);
        }
    }
}
