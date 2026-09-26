using System.Text.Json;
using StackExchange.Redis;
using IncidenciasApp.Models;

namespace IncidenciasApp.Services;

public interface IRedisCacheService
{
    Task<List<Incidencia>?> GetIncidenciasAbiertasCacheAsync();
    Task SetIncidenciasAbiertasCacheAsync(List<Incidencia> incidencias, TimeSpan? expiry = null);
    Task InvalidateIncidenciasAbiertasCacheAsync();
}

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private const string CacheKey = "incidencias_abiertas_list";

    public RedisCacheService(IConfiguration configuration, ILogger<RedisCacheService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 5000;
                options.SyncTimeout = 5000;
                _redis = ConnectionMultiplexer.Connect(options);
                _logger.LogInformation("Conexión inicializada con Redis.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar conexión con Redis.");
            }
        }
    }

    public async Task<List<Incidencia>?> GetIncidenciasAbiertasCacheAsync()
    {
        if (_redis == null || !_redis.IsConnected)
        {
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var data = await db.StringGetAsync(CacheKey);
            if (data.HasValue)
            {
                _logger.LogInformation("[REDIS CACHE HIT] Listado de incidencias abiertas obtenido desde Redis Cache.");
                return JsonSerializer.Deserialize<List<Incidencia>>(data.ToString());
            }

            _logger.LogInformation("[REDIS CACHE MISS] Clave no encontrada en Redis. Se consultará la Base de Datos.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al consultar Redis Cache.");
            return null;
        }
    }

    public async Task SetIncidenciasAbiertasCacheAsync(List<Incidencia> incidencias, TimeSpan? expiry = null)
    {
        if (_redis == null || !_redis.IsConnected)
        {
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(incidencias);
            var ttl = expiry ?? TimeSpan.FromSeconds(60);
            await db.StringSetAsync(CacheKey, json, ttl);
            _logger.LogInformation("[REDIS CACHE SET] Listado de incidencias guardado en Redis por {Seconds} segundos.", ttl.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al guardar en Redis Cache.");
        }
    }

    public async Task InvalidateIncidenciasAbiertasCacheAsync()
    {
        if (_redis == null || !_redis.IsConnected)
        {
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(CacheKey);
            _logger.LogInformation("[REDIS CACHE INVALIDATED] Clave de incidencias abiertas invalidada tras cambio de estado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al invalidar clave en Redis Cache.");
        }
    }
}
