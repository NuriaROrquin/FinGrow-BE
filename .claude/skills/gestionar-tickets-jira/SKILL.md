---
name: gestionar-tickets-jira
description: Usar cuando alguien pida leer, buscar, editar, cambiar el estado o comentar un ticket de Jira del proyecto FinGrow a partir de su número (ej. "dame el ticket 45", "qué dice el SCRUM-12", "pasá el SCRUM-20 a En curso", "cambiale la prioridad al ticket 20", "comentá en el SCRUM-8 que ya está en review"). Usa el servidor MCP de Atlassian.
---

# Gestionar tickets de Jira — proyecto FinGrow

Este proyecto usa Jira (tablero SCRUM) en `https://fingrow-app.atlassian.net`. Todos los tickets tienen el prefijo de proyecto **SCRUM**, seguido de un número (ej. `SCRUM-45`).

La persona te va a pasar solo un número, o el número con el prefijo (ej. "traeme el 45", "dame el SCRUM-45"). Si ya te dan la clave completa, usala tal cual. Si solo dan un número, anteponé `SCRUM-`.

Usá las herramientas del servidor MCP de Atlassian (aparecen como `atlassian` / `mcp__atlassian__*`) contra el sitio `fingrow-app.atlassian.net`. Llamá `getAccessibleAtlassianResources` una sola vez por sesión y reutilizá el `cloudId` que devuelve para el resto de las llamadas.

## Leer un ticket

1. Traé el issue con `getJiraIssue` (clave completa como `issueIdOrKey`).
2. Mostrá un resumen claro con:
   - Título y clave del ticket
   - Tipo (historia, tarea, bug, épica) y estado (columna del tablero)
   - Prioridad y story points si tiene
   - Asignado/a
   - Épica/Principal al que pertenece
   - Descripción completa
   - Comentarios relevantes, si los pidieron o si aportan contexto importante

## Editar un ticket

Para cambiar campos (descripción, resumen, prioridad, story points, etiquetas, asignado/a, etc.) usá `editJiraIssue` con `issueIdOrKey` y `fields` (o `additional_fields` para campos personalizados, por nombre — no hace falta averiguar el `customfield_*` a mano).

Si la persona ya fue explícita sobre el valor exacto (ej. "cambiale la prioridad a Highest"), aplicá el cambio directo. Si el pedido es abierto (ej. "mejorá la descripción", "actualizá el alcance"), mostrá el texto final antes de guardarlo y esperá confirmación — es un cambio visible para todo el equipo en un sistema compartido.

## Mover el estado (transición)

1. Las transiciones válidas dependen de en qué columna está el ticket hoy; no cualquier estado es alcanzable desde cualquier otro. Si no sabés las transiciones disponibles, usá `discover` describiendo el objetivo (ej. "listar transiciones disponibles de un issue de Jira") para encontrar la operación `listJiraIssueTransitions`, y ejecutala con `executeRead`.
2. Elegí la transición cuyo nombre o estado destino coincida con lo pedido — **`transitionName` es el nombre de la transición, no el estado destino** (pueden diferir, ej. "Review->Done" no matchea con "Done").
3. Aplicá el cambio con `transitionJiraIssue` (`transitionId` o `transitionName`).
4. Si el estado pedido no es alcanzable directamente desde la columna actual, avisale a la persona en lugar de forzar una transición que no corresponde.
5. No uses el parámetro `update` de `transitionJiraIssue` para agregar comentarios: puede descartarse en silencio. Para comentar, usá `addOrEditJiraIssueComment` aparte.

## Comentar un ticket

Usá `addOrEditJiraIssueComment` con `issueIdOrKey` y `commentBody`. Omití `commentId` para agregar un comentario nuevo; pasalo (buscándolo antes con `listJiraIssueComments`) para editar uno existente — tené en cuenta que editar reemplaza el comentario entero, así que hay que mandar el texto completo.

## Reglas generales para editar, transicionar o comentar

- Estas acciones modifican un sistema compartido y visible para todo el equipo (a diferencia de leer, que no tiene efecto). Confirmá el ticket exacto y el cambio antes de aplicarlo, salvo que la persona ya haya sido explícita sobre ambos.
- Después de aplicar el cambio, confirmá qué se hizo (campo, estado o comentario) y en qué ticket, con el link o la clave.
- Si te piden trabajar sobre el ticket (por ejemplo "implementá lo que dice el SCRUM-45"), primero traé el detalle como se explica arriba y confirmá el alcance antes de tocar código, salvo que la persona ya haya sido explícita sobre qué hacer.

## Qué no se puede hacer

El servidor MCP de Atlassian no expone ninguna operación para **eliminar** un issue (se probó
con `discover` sobre todo el catálogo, sin resultado). Si te piden borrar un ticket, avisale a
la persona que lo tiene que hacer a mano desde la interfaz de Jira.

## Si el servidor de Jira no está conectado

Si no ves herramientas de `atlassian` disponibles, o falla la conexión (incluyendo "requires re-authorization" por token vencido), decile a la persona que corra `/mcp` dentro de la sesión de Claude Code y siga el flujo de login con su cuenta de Atlassian (cada integrante del equipo se loguea con su propia cuenta — es un login individual, no se comparten credenciales). Después de loguearse, reintentá.

## Notas

- Cada persona del equipo ve y puede modificar los tickets según sus propios permisos de Jira, porque la autenticación es por OAuth individual (no hay tokens ni contraseñas compartidas en este repo).
- No hace falta configurar nada más: el servidor MCP ya está declarado en `.mcp.json` en la raíz del repo, así que Claude Code lo detecta solo al arrancar. Ver `.claude/README.md` para la guía de instalación paso a paso.
