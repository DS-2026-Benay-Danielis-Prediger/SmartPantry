using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartPantry.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace SmartPantry;

[DependsOn(
    typeof(SmartPantryApplicationModule),
    typeof(SmartPantryDomainTestModule),
    typeof(SmartPantryEntityFrameworkCoreModule)
)]
public class SmartPantryApplicationTestModule : AbpModule
{
    private string? _dbPath;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureSqliteEnArchivoTemporal(context.Services);

        // Deshabilitamos transacciones automáticas para evitar conflictos con SQLite
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }

    /// <summary>
    /// Usa una base SQLite en archivo temporal (única por test) en lugar de una única
    /// conexión en memoria compartida. Cada DbContext abre su propia conexión, así que
    /// las ejecuciones en paralelo no comparten una SqliteConnection (que no es thread-safe).
    /// </summary>
    private void ConfigureSqliteEnArchivoTemporal(IServiceCollection services)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"smartpantry-tests-{Guid.NewGuid():N}.db");

        // Default Timeout: ante escrituras simultáneas, espera en lugar de fallar con "database is locked".
        // Pooling=False: las conexiones se cierran de verdad al liberarlas (permite borrar el archivo al final).
        var connectionString = $"Data Source={_dbPath};Default Timeout=30;Pooling=False";

        CrearBaseDeDatos(connectionString);

        services.Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(abpDbContextConfigurationContext =>
            {
                abpDbContextConfigurationContext.DbContextOptions.UseSqlite(connectionString);
            });
        });
    }

    private static void CrearBaseDeDatos(string connectionString)
    {
        // WAL permite lecturas y escrituras concurrentes con menos bloqueos; el modo queda guardado en el archivo.
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode=WAL;";
            command.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<SmartPantryDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using (var context = new SmartPantryDbContext(options))
        {
            context.Database.EnsureCreated();
        }
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        SqliteConnection.ClearAllPools();

        if (_dbPath == null)
        {
            return;
        }

        foreach (var file in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Archivo temporal: si no se puede borrar ahora, queda en la carpeta temp del sistema.
            }
        }
    }
}