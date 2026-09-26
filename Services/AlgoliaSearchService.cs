using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using IncidenciasApp.Models;

namespace IncidenciasApp.Services;

public class AlgoliaIncidenciaHit
{
    public string ObjectID { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Estacion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public interface IAlgoliaSearchService
{
    Task<List<int>> BuscarIdsIncidenciasAsync(string query);
}

public class AlgoliaSearchService : IAlgoliaSearchService
{
    private readonly SearchClient _client;
    private readonly string _indexName;
    private readonly ILogger<AlgoliaSearchService> _logger;

    public AlgoliaSearchService(IConfiguration configuration, ILogger<AlgoliaSearchService> logger)
    {
        _logger = logger;
        var appId = configuration["Algolia:ApplicationId"] ?? "CBBGMIXT8X";
        var apiKey = configuration["Algolia:SearchApiKey"] ?? configuration["Algolia:WriteApiKey"] ?? "ed33f483bd657736bc421f094c2d5ee1";
        _indexName = configuration["Algolia:IndexName"] ?? "incidencias";

        _client = new SearchClient(appId, apiKey);
    }

    public async Task<List<int>> BuscarIdsIncidenciasAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<int>();
        }

        try
        {
            var index = _client.InitIndex(_indexName);
            var searchResult = await index.SearchAsync<AlgoliaIncidenciaHit>(new Query(query));

            var ids = new List<int>();
            foreach (var hit in searchResult.Hits)
            {
                if (hit.Id > 0)
                {
                    ids.Add(hit.Id);
                }
                else if (int.TryParse(hit.ObjectID, out var parsedId))
                {
                    ids.Add(parsedId);
                }
            }

            _logger.LogInformation("Algolia search '{Query}' devolvió {Count} resultados.", query, ids.Count);
            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar en Algolia con query '{Query}'", query);
            return new List<int>();
        }
    }
}
