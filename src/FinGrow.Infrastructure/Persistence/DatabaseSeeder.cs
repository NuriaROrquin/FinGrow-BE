namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Datos de demostracion minimos para poder probar el login y mostrar la app: una empresa,
/// dos departamentos y cinco empleados. A proposito no siembra movimientos, presupuestos,
/// metas ni inversiones.
///
/// Es idempotente: la empresa demo se identifica por su CUIT (clave natural, indice unico), y
/// si ya existe el seed no inserta nada. Todo se inserta en un unico SaveChanges, que es
/// atomico, asi que nunca queda una empresa sin sus empleados.
/// </summary>
public sealed partial class DatabaseSeeder
{
    // CUIT con digito verificador valido (lo exige TaxId.From). Es la clave con la que se
    // detecta si el seed ya corrio.
    private const string DemoTaxId = "30712345671";

    // Password compartida de las cuentas demo. Se hashea con el mismo IPasswordHasher que usa
    // el login, por eso las credenciales sirven para iniciar sesion de verdad.
    private const string DemoPassword = "Demo1234!";

    // Fecha fija (no DateTimeOffset.UtcNow) para que el seed sea reproducible corrida a corrida.
    private static readonly DateTimeOffset SeedTimestamp = new(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly FinGrowDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        FinGrowDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var taxId = TaxId.From(DemoTaxId);

        if (await _dbContext.Companies.AnyAsync(company => company.TaxId == taxId, cancellationToken))
        {
            LogAlreadySeeded(_logger);
            return;
        }

        var passwordHash = _passwordHasher.Hash(DemoPassword);

        var company = Company.Create(
            "FinGrow Demo S.A.",
            taxId,
            Email.From("empresa@fingrow-demo.com"),
            passwordHash,
            Currency.ARS,
            SeedTimestamp);

        // Los departamentos se agregan por el agregado Company (generan su propio Id en memoria),
        // asi despues puedo asignar empleados a cada uno sin haber guardado todavia.
        var tecnologia = company.AddDepartment("Tecnología", "Desarrollo de producto y sistemas", "Laura Fernández", SeedTimestamp);
        var administracion = company.AddDepartment("Administración", "Finanzas, compras y RRHH", "Diego Suárez", SeedTimestamp);

        var employees = new[]
        {
            Employee.Create(company.Id, tecnologia.Id, "Ana Gómez", Email.From("ana.gomez@fingrow-demo.com"), "+541150000001", passwordHash, Currency.ARS, new DateOnly(2023, 3, 1), SeedTimestamp),
            Employee.Create(company.Id, tecnologia.Id, "Bruno Díaz", Email.From("bruno.diaz@fingrow-demo.com"), "+541150000002", passwordHash, Currency.ARS, new DateOnly(2023, 6, 15), SeedTimestamp),
            Employee.Create(company.Id, tecnologia.Id, "Carla Ruiz", Email.From("carla.ruiz@fingrow-demo.com"), null, passwordHash, Currency.USD, new DateOnly(2024, 1, 10), SeedTimestamp),
            Employee.Create(company.Id, administracion.Id, "Diego López", Email.From("diego.lopez@fingrow-demo.com"), "+541150000004", passwordHash, Currency.ARS, new DateOnly(2022, 11, 20), SeedTimestamp),
            Employee.Create(company.Id, administracion.Id, "Elena Prat", Email.From("elena.prat@fingrow-demo.com"), null, passwordHash, Currency.ARS, new DateOnly(2024, 5, 5), SeedTimestamp),
        };

        _dbContext.Companies.Add(company);
        _dbContext.Employees.AddRange(employees);

        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSeeded(_logger, company.Departments.Count, employees.Length);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "El seed de demostracion ya estaba aplicado; no se inserto nada.")]
    private static partial void LogAlreadySeeded(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed de demostracion aplicado: 1 empresa, {Departments} departamentos y {Employees} empleados.")]
    private static partial void LogSeeded(ILogger logger, int departments, int employees);
}
