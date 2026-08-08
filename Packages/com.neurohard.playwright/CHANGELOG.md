# Changelog

## [0.1.0] - 2026-08-08

Motor de grafos de conversación como `IDialogueSource` para Prompter.

### Added
- Model: `DialogueGraph` mutable con `Add`/`Remove`/`Rename`, `GraphNode`,
  `GraphEdge`, `GraphLine`, `Condition` (Compare, All, Any, Not, Always),
  `Effect` (Assign, Command).
- Traversal: `GraphDialogueSource`, cursor que implementa `IDialogueSource`
  e `ISerializableSource`.
- Io: parser JSON propio con números de línea en los errores, `GraphReader`,
  `GraphWriter` y `GraphValidator`.

### Notas de diseño
- Sin dependencias externas: `JsonUtility` no soporta polimorfismo ni valores
  sin tipar, y Newtonsoft haría de Playwright un paquete con dependencias.
- Las condiciones y efectos van en las aristas, no en los nodos: evita la
  ambigüedad de si un efecto se aplica al entrar o al salir de un nodo.
- Toda salida tiene la misma forma (`to`, `when`, `then`). Una arista con
  `line` es una opción elegible; sin ella, una transición automática.
- `nodes` es un array y cada nodo lleva `editor: {x, y}`: el formato es
  canónico y el futuro editor gráfico será una vista sobre él.
- El escritor omite los valores por defecto para mantener los diffs limpios.

### Limitaciones conocidas
- El parser acepta comentarios `//`, pero el escritor no los conserva.
- Sin parser de expresiones: las condiciones son comparaciones simples
  combinadas con all/any/not.
- Sin editor gráfico, sin subgrafos y sin conversaciones anidadas.
- Autoría a mano en JSON.


## [Unreleased]

### Changed
- Una variable sin definir se compara como 0 frente a valores numéricos, y como
  cadena vacía frente a cadenas. Antes cualquier comparación con una variable
  inexistente daba false. Esto permite escribir `contador == 0` en lugar de
  `{any: [{!exists}, {== 0}]}`.
- `exists` / `!exists` pasan a ser la única forma de distinguir "sin definir"
  de "cero".

### Fixed
- `Add` degradaba a int cualquier resultado sin decimales, cambiando el tipo de
  variables double a mitad de partida.
- `VariableInventory` marcaba como "nunca leída" una variable usada solo en
  operaciones `+=` o `-=`.