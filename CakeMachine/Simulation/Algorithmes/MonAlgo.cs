using System.Runtime.CompilerServices;
using CakeMachine.Fabrication.ContexteProduction;
using CakeMachine.Fabrication.Elements;
using CakeMachine.Fabrication.Opérations;
using CakeMachine.Utils;

namespace CakeMachine.Simulation.Algorithmes;

internal class MonAlgo : Algorithme
{
    public override bool SupportsAsync => true;
    
    public override void ConfigurerUsine(IConfigurationUsine builder)
    {
        builder.NombrePréparateurs = 10;
        builder.NombreFours = 6;
        builder.NombreEmballeuses = 15;
    }

    public override async IAsyncEnumerable<GâteauEmballé> ProduireAsync(Usine usine, CancellationToken token)
    {
        var postesPréparation = usine.Préparateurs.ToArray();
        var postesCuisson = usine.Fours.ToArray();
        var postesEmballage = usine.Emballeuses.ToArray();

        while (!token.IsCancellationRequested)
        {
            var queueGâteauCrus = new Queue<GâteauCru>();
            var queueGâteauCuits = new Queue<GâteauCuit>();


            // PREPARATION
            var tâchesPréparation = new List<Task<GâteauCru>>();
            foreach (var postePréparation in postesPréparation)
            {
                tâchesPréparation.AddRange(usine.StockInfiniPlats.Take(3).Select(postePréparation.PréparerAsync));
            }
            var gâteauxCrus = await Task.WhenAll(tâchesPréparation);
            foreach (var gâteauCru in gâteauxCrus)
                queueGâteauCrus.Enqueue(gâteauCru);


            // CUISSON
            var tâchesCuisson = new List<Task<GâteauCuit[]>>();
            foreach (var posteCuisson in postesCuisson)
            {
                tâchesCuisson.Add(posteCuisson.CuireAsync(DequeueChunkExtensions.Dequeue(queueGâteauCrus, 5).ToArray()));
            }
            var gâteauxCuits = await Task.WhenAll(tâchesCuisson);
            foreach (var listegâteauxCuits in gâteauxCuits)
                foreach (var gâteauCuit in listegâteauxCuits)
                queueGâteauCuits.Enqueue(gâteauCuit);




            // EMBALLAGE
            var tâchesEmballage = new List<Task<GâteauEmballé>>();
            foreach (var posteEmballage in postesEmballage)
            {
                tâchesEmballage.AddRange(DequeueChunkExtensions.Dequeue(queueGâteauCuits, 2).Select(posteEmballage.EmballerAsync));
            }
            var gâteauxEmballés = await Task.WhenAll(tâchesEmballage);
            foreach (var gâteauEmballé in gâteauxEmballés)
                yield return gâteauEmballé;
        }
    }
}