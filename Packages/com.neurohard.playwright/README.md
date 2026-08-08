# Playwright

Grafos de conversación como fuente de diálogo para [Prompter](../com.neurohard.prompter).

Playwright implementa `IDialogueSource`: para Prompter es indistinguible de un
guion lineal. Aporta ramificación, condiciones sobre variables y efectos.

## Instalación

> **Importante:** los paquetes instalados por Git URL **no resuelven sus propias
> dependencias Git**. Instala Prompter *primero*, o Unity fallará al resolver
> `com.neurohard.prompter`.

1. Prompter: https://github.com/USUARIO/DialogueFramework.git?path=/Packages/com.neurohard.prompter#v0.1.0
2. Playwright: https://github.com/USUARIO/DialogueFramework.git?path=/Packages/com.neurohard.playwright#v0.1.0

## Formato

Los grafos son JSON. El formato es canónico y estable: el futuro editor gráfico
será una vista sobre él, no un reemplazo.

- `nodes` es un **array**, no un objeto: el orden es estable y los diffs limpios.
- Cada nodo lleva `editor: { x, y }`, que el motor ignora.
- Toda salida es una arista con la misma forma (`to`, `when`, `then`). Una arista
  con `line` es una opción elegible; sin ella, una transición automática.

Ver `Tests/Editor/` para ejemplos completos.

## Tipos de nodo

| Tipo | Contenido | Salidas |
|---|---|---|
| `line` | una línea | una, automática |
| `choice` | opciones | una por opción, con condición |
| `hub` | ninguno | la primera cuya condición se cumpla |

## Estado

v0.1: motor y carga JSON. Autoría a mano. Sin editor gráfico todavía.
