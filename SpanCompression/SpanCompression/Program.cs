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

        public static bool UseNeural { get; private set; } = true;
        public static bool UseHalton { get; private set; } = true;

        static Stopwatch sw = new();

        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            using var neuralNet = new NeuralUncertaintyEstimator("model/v2/uncertainty_model_3d.onnx");
            NeuralNet = neuralNet;

            var baseDir = @"E:\Workspaces\NormalUncertainty2026\.Revision\Data";

            // Superfast test
            //////////filename = baseDir + @"\bunny_closed_1k.obj";
            //////////minPrecision = 0.1f;
            //////////maxPrecision = 0f;
            //////////decrement = 0.02f;
            //////////measurements = 10;
            //////////Experiment01();

            // Helper for finding good parameters for new meshes being tested
            //PrintPrecisionComparison("Bunny", baseDir + @"\bunny_2k.obj", 0.04f, 0.001f);
            //PrintPrecisionComparison("Armadillo", baseDir + @"\armadillo_10k.obj", 0.008f, 0f);
            //PrintPrecisionComparison("Lion", baseDir + @"\lion.obj", 0.05f, 0f);
            //PrintPrecisionComparison("Fandisk", baseDir + @"\fandisk.obj", 0.05f, 0.019f);
            //PrintPrecisionComparison("Max Planck", baseDir + @"\maxplanck.obj", 0.005f, 0f);

            // Initial submission experiments
            //filename = baseDir + @"\bunny_2k.obj";
            //minPrecision = 0.04f;
            //maxPrecision = 0.001f;
            //decrement = 0.005f;
            //measurements = 16;
            //Experiment01();
            //var elapsed1 = sw.Elapsed.TotalSeconds;

            //filename = baseDir + @"\armadillo_10k.obj";
            //minPrecision = 0.008f;
            //maxPrecision = 0f;
            //decrement = 0.001f;
            //measurements = 16;
            //Experiment01();
            //var elapsed2 = sw.Elapsed.TotalSeconds;

            //filename = baseDir + @"\lion.obj";
            //minPrecision = 0.05f;
            //maxPrecision = 0f;
            //decrement = 0.01f;
            //measurements = 16;
            //Experiment01();
            //var elapsed3 = sw.Elapsed.TotalSeconds;

            //filename = baseDir + @"\fandisk.obj";
            //minPrecision = 0.05f;
            //maxPrecision = 0.001f;
            //decrement = 0.01f;
            //measurements = 16;
            //Experiment01();
            //var elapsed4 = sw.Elapsed.TotalSeconds;

            //filename = baseDir + @"\maxplanck.obj";
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
            //PrettyTime("maxplanck", elapsed5);

            // >100k Verts experiments
            //RecommendExperimentSettings(baseDir + @"\xyzrgb_dragon_100k.obj");
            //RecommendExperimentSettings(baseDir + @"\neptune_100k.obj");



            //filename = baseDir + @"\xyzrgb_dragon_100k.obj";
            //minPrecision = 0.02f;
            //maxPrecision = 0.001f;
            //decrement = 0.002f;
            //measurements = 16;
            //Experiment01();
            //var elapsed8 = sw.Elapsed.TotalSeconds;
            //PrettyTime("dragon", elapsed8);


            filename = baseDir + @"\neptune_100k.obj";
            minPrecision = 0.75f;
            maxPrecision = 0.05f;
            decrement = 0.075f;
            measurements = 16;
            Experiment01();
            var elapsed9 = sw.Elapsed.TotalSeconds;
            PrettyTime("neptune", elapsed9);



            // Generating figures
            //filename = baseDir + @"\maxplanck.obj";
            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Corner, "max_corner.obj");
            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Halton, "max_halton.obj");
            //ExportColoredUncertaintyMesh(filename, 0.004f, EvaluationMode.Neural, "max_neural.obj");

            //filename = baseDir + @"\armadillo_10k.obj";
            //minPrecision = 0.008f;
            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Corner, "arm_expa_corner.obj");
            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Halton, "arm_expa_halton.obj");
            //ExportExpectedAdvantageMesh(filename, minPrecision, EvaluationMode.Neural, "arm_expa_neural.obj");

            //filename = baseDir + @"\maxplanck.obj";
            //minPrecision = 0.005f;
            //int refinements = 41130;
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Halton, refinements, $"maxplanck_refined_halton_{refinements}.obj");
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Corner, refinements, $"maxplanck_refined_corner_{refinements}.obj");
            //RunExactRefinementsAndExport(filename, minPrecision, EvaluationMode.Neural, refinements, $"maxplanck_refined_neural_{refinements}.obj");

            //EvaluateDirectoryMetrics(@"E:\Workspaces\NormalUncertainty2026\.Revision\Figures\06 MaxPlanck Refinements");
        }

        private static void RecommendExperimentSettings(string filepath)
        {
            var mesh = ObjLoader.Load(filepath);

            // 1. Calculate Average Edge Length using your helper method
            double avgEdge = GetAverageEdgeLength(mesh);

            string name = Path.GetFileNameWithoutExtension(filepath);

            Console.WriteLine($"\n*** Recommended Settings for {name} ***");
            Console.WriteLine($"Avg Edge Length: {avgEdge:F5}");

            // 2. Generate Options
            double[] multipliers = { 0.50, 0.35, 0.25 };
            string[] labels = { "Aggressive (0.50x)", "Balanced (0.35x)", "Conservative (0.25x)" };

            for (int i = 0; i < multipliers.Length; i++)
            {
                float recMin = (float)(avgEdge * multipliers[i]);
                float recMax = (float)(avgEdge * 0.01);

                recMin = (float)Math.Round(recMin, 3);
                recMax = (float)Math.Round(recMax, 4);

                float recDec = (float)Math.Round((recMin - recMax) / 15.0f, 4);

                Console.WriteLine($"\n// [{labels[i]}] Copy-paste configuration:");
                Console.WriteLine($"filename = baseDir + @\"\\{Path.GetFileName(filepath)}\";");
                Console.WriteLine($"minPrecision = {recMin}f;");
                Console.WriteLine($"maxPrecision = {recMax}f;");
                Console.WriteLine($"decrement = {recDec}f;");
                Console.WriteLine($"measurements = 16;");
                Console.WriteLine($"Experiment01();");
            }
        }

        private static double GetAverageEdgeLength(Mesh mesh)
        {
            // HashSet ensures we only measure each shared edge exactly once
            var uniqueEdges = new HashSet<(int, int)>();

            foreach (var face in mesh.Faces)
            {
                // Sort indices so (1, 2) and (2, 1) are treated as the same edge
                uniqueEdges.Add(face.i1 < face.i2 ? (face.i1, face.i2) : (face.i2, face.i1));
                uniqueEdges.Add(face.i2 < face.i3 ? (face.i2, face.i3) : (face.i3, face.i2));
                uniqueEdges.Add(face.i3 < face.i1 ? (face.i3, face.i1) : (face.i1, face.i3));
            }

            double totalLength = 0.0;
            foreach (var edge in uniqueEdges)
            {
                var v1 = mesh.Vertices[edge.Item1];
                var v2 = mesh.Vertices[edge.Item2];
                totalLength += Vector3.Distance(v1, v2);
            }

            return totalLength / uniqueEdges.Count;
        }

        private static void PrintPrecisionComparison(string name, string path, float minP, float maxP)
        {
            var mesh = ObjLoader.Load(path);
            var avgEdge = GetAverageEdgeLength(mesh);

            Console.WriteLine($"--- {name} ---");
            Console.WriteLine($"Avg Edge Length: {avgEdge:F5}");
            Console.WriteLine($"Min Precision:   {minP:F5} (Ratio: {minP / avgEdge:F2}x edge length)");
            Console.WriteLine($"Max Precision:   {maxP:F5} (Ratio: {maxP / avgEdge:F2}x edge length)\n");
        }

        public static void PrettyTime(string name, double totalSeconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            Console.WriteLine($"{name}\t.....\t{totalSeconds}s ({(int)t.TotalMinutes}m {t.Seconds}s)");
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

            bool useNeural = false;
            bool useHalton = false;

            switch (mode)
            {
                case EvaluationMode.Corner:
                    useNeural = false;
                    useHalton = false;
                    break;
                case EvaluationMode.Halton:
                    useNeural = false;
                    useHalton = true;
                    break;
                case EvaluationMode.Neural:
                    useNeural = true;
                    useHalton = true;
                    break;
            }

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
                Console.WriteLine($"Neural utilization: {neuralPercentage:F6}%");
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
            var name = Path.GetFileNameWithoutExtension(filename);
            var outputDir = Directory.CreateDirectory($"output/{timestamp} {name}");
            var outputDirPath = outputDir.FullName;
            return outputDirPath;
        }

        private static void Experiment01()
        {
            sw.Reset();

            var outputDir = CreateOutputDirectory();
            Utils.OpenFileExplorer(outputDir);

            using (var logWriter = new StreamWriter($"{outputDir}/log.txt"))
            {
                // Parameters:
                logWriter.WriteLine("Input file: " + filename);
                logWriter.WriteLine("Initial precision: " + minPrecision);
                logWriter.WriteLine("Target precision: " + maxPrecision);
                logWriter.WriteLine("Decrement by: " + decrement);
                logWriter.WriteLine("Measurements: " + measurements);
                logWriter.WriteLine($"UseHalton: {UseHalton}");
                logWriter.WriteLine($"UseNeural: {UseNeural}");
            }

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

            var sampler = new CornerSampler();
            var originalMesh = ObjLoader.Load(filename);

            long bitsFirst;
            long bitsLast;

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

            sw.Start();

            var neighborhoodProvider = new NeighborhoodProvider(originalMesh);
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

            long nextEncodeAt = 0;

            while (true)
            {
                // ==========================================
                // 1. ARITHCODER EVALUATION
                // ==========================================
                if (improvements >= nextEncodeAt)
                {
                    sw.Stop();

                    var bits1 = predictor1.GetBooleans();
                    using var ms1 = new MemoryStream();
                    ArithCoder.encode(bits1, ms1);
                    long acBits1 = ms1.Position * 8;

                    var bits2 = predictor2.GetBooleans();
                    using var ms2 = new MemoryStream();
                    ArithCoder.encode(bits2, ms2);
                    long acBits2 = ms2.Position * 8;

                    usedBits1 = Math.Min(bitsFirst + improvements, bitsFirst + acBits1) + 1;
                    usedBits2 = Math.Min(bitsFirst + improvements, bitsFirst + acBits2) + 1;

                    long currentBits = Math.Min(usedBits1, usedBits2);

                    // Checkpoint Trigger
                    if (currentBits >= checkpoints[checkpoint])
                    {
                        var time = sw.Elapsed.TotalSeconds;

                        SaveCheckpointMetrics(
                            checkpoint, checkpoints, improvements, usedBits1, usedBits2, time,
                            outputDir, filename, coarseMesh, improved,
                            predictor1, predictor2, originalMesh, metrics,
                            x1, y1, x2, y2, x3, y3
                        );

                        checkpoint++;
                        if (checkpoint >= checkpoints.Length)
                            break;
                    }

                    // Calculate the absolute minimum safe distance to sleep
                    long distance = checkpoints[checkpoint] - Math.Min(usedBits1, usedBits2);

                    // Take a step covering 50% of the remaining distance
                    long step = (long)(distance * 0.5);

                    if (distance <= 30)
                        step = 1;

                    nextEncodeAt = improvements + Math.Max(1L, step);

                    sw.Start();
                }

                // ==========================================
                // 2. REFINEMENT STEP
                // ==========================================
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
            }

            sw.Stop();
            PrintAndResetHitStats();
        }

        private static void SaveCheckpointMetrics(
            int checkpoint, long[] checkpoints, long improvements, long usedBits1, long usedBits2, double time,
            string outputDir, string filename, CoarseMesh coarseMesh, double[][] improved,
            APredictor predictor1, APredictor predictor2, Mesh originalMesh, string[] metrics,
            List<long> x1, List<double>[] y1, List<long> x2, List<double>[] y2, List<long> x3, List<double>[] y3)
        {
            Console.WriteLine($"Checkpoint {checkpoint} at {checkpoints[checkpoint]} bits and {improvements} improvements.");
            Console.WriteLine($"  - Simple predictor used {usedBits1} bits.");
            Console.WriteLine($"  - Paral. predictor used {usedBits2} bits.");

            var fnMesh = $"{outputDir}/NU_{improvements}.obj";
            var fnLog1 = $"{outputDir}/NU_{usedBits1:000000}_({improvements}).1.txt";
            var fnLog2 = $"{outputDir}/NU_{usedBits2:000000}_({improvements}).2.txt";

            var cmesh = coarseMesh.ToMesh();
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
            y3[8].Add(improvements);
            y3[9].Add(time);

            WriteMetrics(x2, y2, metrics, "nunc.predictor1", outputDir, filename);
            WriteMetrics(x3, y3, metrics, "nunc.predictor2", outputDir, filename);

            Plot(x1, y1, x2, y2, metrics, "edgebreaker", "nunc.predictor1", outputDir, filename);
            Plot(x1, y1, x3, y3, metrics, "edgebreaker", "nunc.predictor2", outputDir, filename);
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
            using StreamWriter metricWriter = new($"{outputDir}/{method}.metrics.txt", false);
            metricWriter.WriteLine(filename);

            metricWriter.Write("bits;");
            foreach (var m in metrics)
                metricWriter.Write(m + ";");
            metricWriter.WriteLine();

            for (int i = 0; i < x.Count; i++)
            {
                metricWriter.Write($"{x[i]};");
                for (int m = 0; m < metrics.Length; m++)
                    metricWriter.Write($"{y[m][i]};");
                metricWriter.WriteLine();
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
        
    }
}
