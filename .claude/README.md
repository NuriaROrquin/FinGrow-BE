# Claude Code en FinGrow-BE

Este directorio (`.claude/`) tiene la configuración de Claude Code para este repo: el servidor
MCP de Atlassian (declarado en `.mcp.json`, en la raíz) y las skills del proyecto
(`.claude/skills/`).

## MCP de Atlassian: qué es y para qué sirve

Permite que Claude Code lea, edite, cambie de estado y comente tickets de Jira del proyecto
FinGrow directamente desde la sesión, sin salir a la web. Se usa a través de la skill
[`gestionar-tickets-jira`](skills/gestionar-tickets-jira/SKILL.md), que dado un número o clave
(`SCRUM-XX`) trae su detalle, edita campos, mueve el estado (transición) o agrega comentarios.

## Instalación (una vez por persona)

No hay nada que instalar a mano: el servidor ya está declarado en `.mcp.json` en la raíz del
repo, así que Claude Code lo detecta solo al arrancar una sesión en esta carpeta.

Lo único que hace falta es **autenticarse con tu propia cuenta de Atlassian**:

1. Abrí Claude Code en la raíz de `FinGrow-BE`.
2. Corré el comando `/mcp`.
3. Elegí `atlassian` y seguí el flujo de login (OAuth) con tu cuenta de Atlassian —
   la misma con la que entrás a `fingrow-app.atlassian.net`.
4. Cuando el login termine vas a ver `Authentication successful. Connected to atlassian.`

Listo. No se comparten tokens ni contraseñas: cada persona se loguea con su propia cuenta y ve
los tickets según sus propios permisos de Jira.

### Ejemplo de uso

```
Leeme el ticket 20
```

```
Qué dice el SCRUM-97?
```

```
Pasá el SCRUM-20 a En curso
```

```
Cambiale la prioridad al SCRUM-45 a Highest
```

```
Comentá en el SCRUM-8 que ya está en review
```

## Si algo falla

- **"requires re-authorization (token expired)"**: el token OAuth venció. Volvé a correr `/mcp`
  y reloguéate; después reintentá el pedido.
- **No aparecen herramientas de `atlassian`**: revisá que estés parado en la raíz del repo (ahí
  vive `.mcp.json`) y que el archivo no se haya movido o borrado.
- Cualquier otro error de conexión suele ser temporal del lado del servicio — reintentá en unos
  minutos.

## Estado actual y pendientes

- ✅ Leer tickets por número/clave.
- ✅ Editar campos de un ticket (descripción, prioridad, story points, etc.).
- ✅ Mover el estado de un ticket (transición de columna).
- ✅ Comentar un ticket.

Todo esto vive en la skill `gestionar-tickets-jira` (`.claude/skills/gestionar-tickets-jira/SKILL.md`).
Detalle en [SCRUM-110](https://fingrow-app.atlassian.net/browse/SCRUM-110).
