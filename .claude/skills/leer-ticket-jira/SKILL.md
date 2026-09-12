---
name: leer-ticket-jira
description: Usar cuando alguien pida leer, buscar o ver el detalle de un ticket de Jira del proyecto FinGrow a partir de su número (ej. "dame el ticket 45", "qué dice el SCRUM-12", "leé el ticket 20", "traeme la HU-18"). Trae el issue desde Jira a través del servidor MCP de Atlassian.
---

# Leer ticket de Jira — proyecto FinGrow

Este proyecto usa Jira (tablero SCRUM) en `https://fingrow-app.atlassian.net`. Todos los tickets tienen el prefijo de proyecto **SCRUM**, seguido de un número (ej. `SCRUM-45`).

## Cómo usarla

1. La persona te va a pasar solo un número, o el número con el prefijo (ej. "traeme el 45", "dame el SCRUM-45", "qué dice el ticket 45"). En cualquier caso, la clave completa del issue es:
   Si ya te pasan la clave completa (con guion y prefijo), usala tal cual. Si solo te dan un número, anteponé `SCRUM-`.

2. Usá las herramientas del servidor MCP de Atlassian (aparecen como `atlassian` / `mcp__atlassian__*` en las tools disponibles) para buscar y traer el issue con esa clave, en el sitio `fingrow-app.atlassian.net`.

3. Mostrá un resumen claro con:
   - Título y clave del ticket
   - Tipo (historia, tarea, bug, épica) y estado (columna del tablero: Por hacer, En curso, Bloqueada, Finalizado, etc.)
   - Prioridad y story points si tiene
   - Asignado/a
   - Épica/Principal al que pertenece
   - Descripción completa
   - Comentarios relevantes, si los pidieron o si aportan contexto importante

4. Si te piden trabajar sobre el ticket (por ejemplo "implementá lo que dice el SCRUM-45"), primero traé el detalle como se explica arriba y confirmá el alcance antes de tocar código, salvo que la persona ya haya sido explícita sobre qué hacer.

## Si el servidor de Jira no está conectado

Si no ves herramientas de `atlassian` disponibles, o falla la conexión, decile a la persona que corra `/mcp` dentro de la sesión de Claude Code y siga el flujo de login con su cuenta de Atlassian (cada integrante del equipo se loguea con su propia cuenta la primera vez que lo usa en su máquina — es un login individual, no se comparten credenciales). Después de loguearse, reintentá.

## Notas

- Cada persona del equipo ve los tickets según sus propios permisos de Jira, porque la autenticación es por OAuth individual (no hay tokens ni contraseñas compartidas en este repo).
- No hace falta configurar nada más: el servidor MCP ya está declarado en `.mcp.json` en la raíz del repo, así que Claude Code lo detecta solo al arrancar.
