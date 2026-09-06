# FinGrow · Backend

API REST de FinGrow, construida en .NET 10 con Clean Architecture.

Es uno de los tres componentes de la plataforma:

| Repositorio | Stack | Responsabilidad |
|---|---|---|
| [FinGrow-FE](https://github.com/NuriaROrquin/FinGrow-FE) | React · Next.js | Interfaz de usuario |
| **FinGrow-BE** | **.NET 10 · PostgreSQL** | **Reglas de negocio, datos y autenticación** |
| [FinGrow-AI](https://github.com/NuriaROrquin/FinGrow-AI) | Python | OCR de tickets y procesamiento de lenguaje natural |

El backend consume la API de IA por HTTP; el frontend nunca la llama directo.

---

## Requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (opcional, para levantar PostgreSQL)

## Puesta en marcha

Con Docker, que levanta la base y la API juntas:

```bash
docker compose up --build
```

En local, contra una PostgreSQL ya disponible:

```bash
dotnet run --project src/FinGrow.Api
```

| Recurso | URL |
|---|---|
| API | http://localhost:8080 |
| Swagger UI (solo en Development) | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |

## Configuración

Los valores por defecto están en `src/FinGrow.Api/appsettings.json` y se pueden pisar con
variables de entorno usando `__` como separador de sección.

| Clave | Variable de entorno | Descripción |
|---|---|---|
| `ConnectionStrings:Database` | `ConnectionStrings__Database` | Cadena de conexión a PostgreSQL |
| `AiService:BaseUrl` | `AiService__BaseUrl` | URL base de FinGrow-AI |
| `AiService:TimeoutSeconds` | `AiService__TimeoutSeconds` | Timeout de las llamadas a IA (default 30) |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | Orígenes habilitados para el frontend |

Los secretos no se commitean. En desarrollo local:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "<cadena>" --project src/FinGrow.Api
```

---

## Arquitectura

Cuatro capas con las dependencias apuntando siempre hacia adentro: cada capa conoce a las
interiores y nunca a las exteriores.

```
FinGrow.Api  ──►  FinGrow.Application  ──►  FinGrow.Domain
     │                     ▲
     └──► FinGrow.Infrastructure ──┘
```

| Proyecto | Depende de | Contiene |
|---|---|---|
| `FinGrow.Domain` | — | Entidades, value objects, interfaces de repositorio |
| `FinGrow.Application` | Domain | Casos de uso, DTOs, validadores, contratos hacia servicios externos |
| `FinGrow.Infrastructure` | Application | EF Core, PostgreSQL, JWT, cliente de FinGrow-AI, integraciones |
| `FinGrow.Api` | Application · Infrastructure | Controllers, middlewares, configuración de arranque |

`FinGrow.Domain` no referencia ningún paquete NuGet ni ningún otro proyecto. Es la
restricción que mantiene las reglas de negocio independientes de la base de datos y del
transporte.

**La API referencia Infrastructure únicamente para registrar las implementaciones en el
contenedor de dependencias** (`Program.cs` → `AddInfrastructure()`). Los controllers hablan
solo con Application; hay un test que lo verifica.

### Estructura de carpetas

```
src/
├── FinGrow.Domain/
│   ├── Common/             Entity, AggregateRoot, ValueObject
│   ├── Entities/           Company, Department, Employee, Transaction, Budget, Goal, Investment
│   ├── ValueObjects/       Money, Email, TaxId
│   ├── Enums/
│   ├── Errors/
│   └── Repositories/       Interfaces, no implementaciones
├── FinGrow.Application/
│   ├── Common/             Result y Error
│   ├── Interfaces/         IUnitOfWork, ICurrentUser, IAiService, IDateTimeProvider
│   ├── Features/           Un subdirectorio por funcionalidad
│   ├── DTOs/
│   └── Validators/
├── FinGrow.Infrastructure/
│   ├── Persistence/        DbContext, Configurations, Repositories, Migrations
│   ├── Identity/           Resolución del usuario autenticado
│   ├── Ai/                 Cliente HTTP hacia FinGrow-AI
│   ├── Integrations/       Telegram, Gmail
│   └── Services/
└── FinGrow.Api/
    ├── Controllers/
    ├── Middleware/         Manejo global de errores → ProblemDetails
    ├── Extensions/
    └── Program.cs

tests/
├── FinGrow.Domain.UnitTests/
└── FinGrow.ArchitectureTests/
```

Las carpetas que siguen vacías con un `.gitkeep` corresponden a tareas todavía no hechas:
repositorios, casos de uso, DTOs, validadores, controllers e integraciones.

---

## Modelo de dominio

Siete entidades y tres value objects. El esquema se crea con la migración `InitialCreate`.

| Tabla | Raíz de agregado | Qué guarda |
|---|---|---|
| `companies` | Company | Empresa contratante: razón social, CUIT, credenciales, moneda por defecto |
| `departments` | (parte de Company) | Departamentos de la empresa; se desactivan, no se borran |
| `employees` | Employee | La persona que usa la app; pertenece a una empresa y opcionalmente a un departamento |
| `transactions` | Transaction | Ingresos y gastos, con importe siempre positivo y el signo dado por `type` |
| `budgets` | Budget | Tope por categoría y período |
| `goals` | Goal | Metas de ahorro con objetivo, avance y fecha límite |
| `investments` | Investment | Posiciones del portafolio: capital invertido y valuación actual |

Decisiones que conviene conocer antes de tocar el modelo:

- **Un importe nunca viaja solo.** `Money` es un value object de monto + moneda que se guarda
  como dos columnas (`amount` / `currency`). Combinar importes de monedas distintas lanza
  `DomainException`.
- **Lo gastado de un presupuesto no se persiste.** Se calcula sumando las transacciones del
  período. Un contador guardado se desincroniza en cuanto alguien edita o borra un movimiento.
- **Un empleado se relaciona con su departamento por id**, no por nombre como en los mocks del
  frontend. Renombrar un departamento no toca a nadie.
- **`ExpenseCategory` es un contrato compartido con FinGrow-AI.** Los diez valores viven también
  en `FinGrow-AI/app/domain/enums.py` y se guardan en la base con el mismo texto
  (`ahorro_inversion`, no `AhorroInversion`). Hay un test que falla si las dos listas se separan.
- **Las reglas críticas están además en la base.** Un gasto con categoría de ingreso, un importe
  cero o una meta con objetivo y progreso en monedas distintas los rechaza un `CHECK`, no solo el
  código C#.
- **Nombres en snake_case.** Se aplican de una sola vez en `OnModelCreating`
  (`SnakeCaseNamingExtensions`), para poder consultar en psql sin comillas dobles.

---

## Desarrollo

```bash
dotnet build FinGrow.sln
```

```bash
dotnet test FinGrow.sln
```

Migraciones de base de datos:

```bash
dotnet ef migrations add <Nombre> --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

```bash
dotnet ef database update --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

### Tests de arquitectura

`tests/FinGrow.ArchitectureTests` verifica en cada build que las reglas de dependencia se
cumplan:

- Domain no depende de ninguna otra capa.
- Domain no depende de EF Core ni de Npgsql.
- Application no depende de Infrastructure ni de Api.
- Infrastructure no depende de Api.
- Ningún controller usa tipos de Infrastructure.

Si alguien las rompe, el build falla. GitHub Actions los ejecuta en cada push y pull request.

### Convenciones

- Identificadores en inglés; documentación y mensajes de error en castellano.
- Namespaces file-scoped y `var` solo para tipos no evidentes (ver `.editorconfig`).
- Los warnings se tratan como errores (`Directory.Build.props`).
- Las versiones de paquetes se declaran una sola vez, en `Directory.Packages.props`. Los
  `.csproj` referencian paquetes sin versión.
- Reglas de negocio con desenlace esperable devuelven `Result`; las excepciones quedan para
  fallas técnicas.
