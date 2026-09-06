# FinGrow · Backend

API REST en .NET 10 con Clean Architecture y PostgreSQL. Es uno de tres repos: FinGrow-FE
(Next.js), **FinGrow-BE** (este) y FinGrow-AI (Python, OCR y lenguaje natural). El backend
consume la API de IA por HTTP; el frontend nunca la llama directo.

## Comandos

```bash
dotnet build FinGrow.sln
dotnet test FinGrow.sln
docker compose up -d db          # PostgreSQL 16 en localhost:5432
dotnet ef migrations add <Nombre> --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

La herramienta `dotnet-ef` tiene que ser 10.x: una versión anterior a la de EF Core no puede
leer el modelo.

## Dónde está el modelo de datos

| Documento | Qué contiene |
|---|---|
| [docs/database-schema.md](docs/database-schema.md) | **El esquema que existe hoy**: 7 tablas, sus restricciones y su comportamiento al borrar |
| [docs/database-schema-target.md](docs/database-schema-target.md) | El DER completo de las 65 historias: 31 tablas en 7 módulos, y las divergencias con lo construido |
| [docs/backlog-huecos.md](docs/backlog-huecos.md) | Huecos del backlog detectados al modelar |

**Regla: toda migración nueva actualiza `docs/database-schema.md` en el mismo commit.** La
fuente de verdad es la migración; el documento es una foto que se desactualiza sola si nadie la
mueve, y un DER que miente es peor que no tener ninguno.

## Reglas del dominio que se violan sin querer

Estas son las que no se deducen leyendo el código, y por las que conviene preguntar antes de
"arreglar" algo que parece faltar.

**Un total no se persiste: se deriva de sus partes.** El gastado de un presupuesto se calcula
sumando las transacciones del período, y no existe como columna. La tentación de agregar un
`spent` para "evitar el cálculo" es exactamente el bug: un total guardado y sus partes se
desincronizan en cuanto alguien edita o borra una parte, y después nadie sabe cuál de los dos
números es el bueno. Lo mismo aplica a `goals` e `investments` una vez hecha T-23.

**`ExpenseCategory` es un contrato con FinGrow-AI.** Los diez valores viven también en
`FinGrow-AI/app/domain/enums.py` y se guardan en la base con el texto que usa la IA
(`ahorro_inversion`, no `AhorroInversion`). Hay un test que rompe el build si las dos listas se
separan: si falla, no lo ajustes sin cambiar los dos lados.

**Un importe nunca viaja solo.** `Money` es un value object de monto + moneda que se guarda como
dos columnas. Combinar importes de monedas distintas lanza `DomainException`, a propósito.

**Un movimiento propuesto no cuenta hasta que lo confirman.** Lo que entra por Gmail, por el bot
o por el OCR nace con `status = Pending`. Saldo, presupuestos y reportes miran solo los
confirmados.

**Las reglas críticas están además en la base.** Hay `CHECK` que rechazan un gasto con categoría
de ingreso, un importe cero y una meta con objetivo y progreso en monedas distintas. La
duplicación con el dominio es deliberada: el código protege del bug, el `CHECK` protege de la
carga masiva y del `UPDATE` hecho a mano.

## Arquitectura

Cuatro capas con las dependencias hacia adentro: `Api` → `Application` → `Domain`, e
`Infrastructure` → `Application`. `FinGrow.Domain` no referencia ningún paquete NuGet ni ningún
otro proyecto: es lo que mantiene las reglas de negocio independientes de la base y del
transporte.

`tests/FinGrow.ArchitectureTests` verifica esas reglas en cada build y falla si alguien las
rompe. Si uno de esos tests falla, el problema es la dependencia nueva, no el test.

## Convenciones

- Identificadores en inglés; documentación, comentarios y mensajes de error en castellano. La
  excepción son los enums de categorías, en castellano por ser contrato con FinGrow-AI.
- Los warnings se tratan como errores. Las migraciones están exentas de las reglas de estilo en
  `.editorconfig` porque son código generado.
- Las versiones de paquetes se declaran una sola vez en `Directory.Packages.props`; los `.csproj`
  referencian sin versión.
- Los nombres en la base son snake_case, aplicados de una sola vez en `OnModelCreating`
  (`SnakeCaseNamingExtensions`). No hace falta `HasColumnName` salvo para columnas de value
  objects.
- Reglas de negocio con desenlace esperable devuelven `Result`; las excepciones quedan para
  fallas técnicas.
- Las claves primarias son `Guid.CreateVersion7()`, no `Guid.NewGuid()`: son ordenables por
  tiempo y no fragmentan el índice.

## Trabajo pendiente que afecta al modelo

Tres tablas de T-01 guardan un total que debería derivarse, y hay historias del backlog que lo
contradicen. Se corrige en **T-23** (SCRUM-107). Antes de tomar HU-21, HU-22, HU-27, HU-32 o
HU-33, revisá esa tarjeta: cada una arrastra una migración sobre tablas ya construidas.

El detalle completo de las divergencias está en
[docs/database-schema-target.md](docs/database-schema-target.md).
