using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Cyberboss.IntelligentInvaders
{
	public sealed class TetrisAI : IDisposable
	{
		public int MaxFitness => tetrisEvaluator.MaxFitness;

		NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(bool load)
		{
			// Create a genome2 factory with our neat genome2 parameters object and the appropriate number of input and output neuron genes.
			var genomeFactory = new NeatGenomeFactory(TetrisEvaluator.NumInputs, TetrisEvaluator.NumOutputs);

			// Create an initial population of randomly generated genomes.
			List<NeatGenome> genomeList = null;
			if (load)
			{
				try
				{
					using (var reader = XmlReader.Create("SavedProgress.xml"))
						genomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(reader, true, genomeFactory);
					Console.WriteLine("Loaded network!");
				}
				catch
				{
					load = false;
				}
			}
			if(!load)
				genomeList = genomeFactory.CreateGenomeList(150, 0);

			var parallelOpts = new ParallelOptions() { MaxDegreeOfParallelism = -1 };
			// Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
			var distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
			var speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, parallelOpts);

			// Create the evolution algorithm.
			var ea = new NeatEvolutionAlgorithm<NeatGenome>(new NeatEvolutionAlgorithmParameters { SpecieCount = 10 }, speciationStrategy, new DefaultComplexityRegulationStrategy(ComplexityCeilingType.Absolute, 50));

			// Create genome2 decoder.
			var genomeDecoder = new NeatGenomeDecoder(NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(2));

			// Create a genome2 list evaluator. This packages up the genome2 decoder with the genome2 evaluator.
			IGenomeListEvaluator<NeatGenome> genomeListEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, tetrisEvaluator, parallelOpts);

			// Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
			// that were in the population in previous generations (elite genomes). This is determiend by examining each genome2's evaluation info object.
			//if (!EvaluateParents)
			//genomeListEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(genomeListEvaluator, SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());

			ea.UpdateEvent += Ea_UpdateEvent;

			// Initialize the evolution algorithm.
			ea.Initialize(genomeListEvaluator, genomeFactory, genomeList);

			// Finished. Return the evolution algorithm
			return ea;
		}

		private void Ea_UpdateEvent(object sender, EventArgs e)
		{
			lock(this)
				if (demoing)
					return;
			Console.WriteLine(string.Format("gen={0:N0} bestFitness={1:N6}", evolutionAlgorithm.CurrentGeneration, evolutionAlgorithm.Statistics._maxFitness));
			if(evolutionAlgorithm.CurrentChampGenome.EvaluationInfo.Fitness > lastBestFitness)
			{
				lastBestFitness = evolutionAlgorithm.CurrentChampGenome.EvaluationInfo.Fitness;
				lastBestGenome = evolutionAlgorithm.CurrentChampGenome;
				onNewBestNetwork?.Invoke(lastBestGenome);
			}
			onNewGen?.Invoke(evolutionAlgorithm.CurrentGeneration);
		}

		readonly TetrisEvaluator tetrisEvaluator;
		readonly NeatEvolutionAlgorithm<NeatGenome> evolutionAlgorithm;
		readonly Action<uint> onNewGen;
		readonly Action<INetworkDefinition> onNewBestNetwork;
		double lastBestFitness;
		INetworkDefinition lastBestGenome;
		bool demoing;

		public TetrisAI(IGameOrchestrator gameOrchestrator, Action<uint> onNewGen, Action<INetworkDefinition> onNewBestNetwork, bool load)
		{
			this.gameOrchestrator = gameOrchestrator ?? throw new ArgumentNullException(nameof(gameOrchestrator));
			tetrisEvaluator = new TetrisEvaluator(gameOrchestrator, false);
			evolutionAlgorithm = CreateEvolutionAlgorithm(load);
			this.onNewGen = onNewGen;
			this.onNewBestNetwork = onNewBestNetwork;
		}

		readonly IGameOrchestrator gameOrchestrator;

		public void Dispose()
		{
			evolutionAlgorithm.Dispose();
		}

		public void StartTraining()
		{
			evolutionAlgorithm.StartContinue();
		}

		public void StopTraining() => PauseTraining();//evolutionAlgorithm.RequestTerminateAndWait();	//bugged, use Pause as a workaround

		public void PauseTraining() => evolutionAlgorithm.RequestPauseAndWait();

		public void Save()
		{
			var previouslyTraining = evolutionAlgorithm.RunState == RunState.Running;
			if (previouslyTraining)
				PauseTraining();
			using (var writer = XmlWriter.Create("SavedProgress.xml"))
				NeatGenomeXmlIO.WriteComplete(writer, evolutionAlgorithm.GenomeList, true);
			if (previouslyTraining)
				StartTraining();
		}

		public void RunDemo()
		{
			var previouslyTraining = evolutionAlgorithm.RunState == RunState.Running;
			if(previouslyTraining)
				PauseTraining();
			var daBes = (IBlackBox)evolutionAlgorithm.CurrentChampGenome.CachedPhenome;
			var demoMode = new TetrisEvaluator(gameOrchestrator, true);
			lock(this)
				demoing = true;
			if (previouslyTraining)
				StartTraining();
			demoMode.Evaluate(daBes);
			lock(this)
				demoing = false;
		}
		
	}
}
