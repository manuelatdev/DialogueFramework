# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto sigue [Versionado Semántico](https://semver.org/lang/es/).

## [0.5.0]

El visor pasa a ser editor.

### Added
- Panel de consultas simuladas: casillas y campos por cada invocación concreta
  (`puede_comprar(cuerda)` es distinta de `puede_comprar(amuleto)`), para poder
  simular grafos que dependen del juego.
- `QueryInventory`: lista las consultas que exige un grafo. Es, de hecho, el
  contrato que el `IQueryResolver` del juego debe implementar.
- Nodos movibles con posiciones persistentes en el bloque `editor` del JSON.
- Guardado al JSON desde la ventana, con la API nativa `hasUnsavedChanges` /
  `SaveChanges` de Unity.
- `GraphHistory`: deshacer y rehacer por instantáneas del JSON completo.
- `DialogueGraphAsset.Save()` genera el `.json` automáticamente si el asset se
  creó sin uno, y `ReplaceGraph` permite al editor sustituir la caché.
- `TryGetGraph`: variante no lanzante para el editor, que un JSON roto no debe
  reventar el bucle de dibujado del inspector.

### Fixed
- `OnValidate` invalidaba la caché en cada recompilación, destruyendo los
  cambios sin guardar. Ahora solo invalida si el TextAsset cambió de verdad.
- El inspector personalizado invalidaba la caché al seleccionar el asset, con
  el mismo efecto.
- Los cambios sin guardar sobreviven a los domain reloads mediante una
  instantánea serializada en la ventana.
- `Condition.Always` anidada dentro de `all`/`any` se serializaba como un objeto
  vacío que el lector no sabía interpretar. Ahora se escribe como
  `{ "always": true }`.

### Limitaciones conocidas
- Sin edición estructural: no se pueden crear, borrar ni conectar nodos.
- El historial se pierde al recompilar; los cambios no.
- Deshacer puede registrar entradas vacías si un arrastre no llega a mover
  nada. Se resolverá al implementar la edición estructural.

## [0.4.0]

El grafo puede preguntar al juego.

### Added
- Condiciones de tipo consulta: `{ "query": "puede_comprar", "args": ["cuerda"] }`.
  Sin `op` ni `value` significan "la consulta devuelve verdadero"; con ellos se
  comparan como cualquier otro valor.
- `EvaluationContext`, que agrupa el almacén de variables y el resolver de
  consultas.

### Changed
- `Condition.Evaluate` recibe un `EvaluationContext` en lugar de un
  `IVariableStorage`. Afecta a las seis variantes, al simulador y al visor.
- `GraphSimulator.Simulate` recibe también el contexto.

### Limitaciones conocidas
- **El visor no puede simular consultas.** Sin un juego detrás, `NoQueryResolver`
  responde falso a todo y las opciones que dependen de consultas aparecen siempre
  bloqueadas. Pendiente decidir entre un panel de consultas simuladas o permitir
  enchufar un resolver del proyecto.

### Notas de diseño
- El inventario y la economía no son asunto del sistema de diálogos. Modelarlos
  con variables planas obligaba a duplicar el procedimiento de compra por cada
  artículo, porque el destino de una asignación no puede ser dinámico. Escribir
  el grafo del mercader lo dejó en evidencia: 8 de 23 nodos eran copia-pega.
  La solución no fue enriquecer el lenguaje del grafo, sino sacar esa
  responsabilidad al juego. Tras el cambio: 15 nodos y cero duplicación.
- Criterio: si un dato existe aunque no haya conversación en marcha (inventario,
  quests, clima), es del juego y se accede por consulta. Si solo existe porque
  hubo conversación (`mentiste`, `veces_hablado`), es del diálogo y vive en
  variables.

## [0.3.0]

### Added
- Las aristas admiten `reason`: el motivo mostrable cuando una opción está
  bloqueada. Viaja hasta `ResolvedOption.UnavailableReason`.
- El validador avisa cuando dos opciones de un mismo `choice` comparten texto:
  suele indicar que debería ser una sola opción seguida de un hub.
- `DialogueGraphAsset.CreateSource` valida el grafo en el editor y reporta los
  errores antes de empezar la conversación, no a mitad.
- El visor numera los puertos de salida en nodos `line` y `hub`, donde el orden
  de las aristas es semántico, y marca las incondicionales como `[siempre]`.

### Changed
- Los `tags` de una línea dejan de usarse como motivo de bloqueo.

## [0.2.0]

### Added
- Referencias a variables como valor: `{ "$var": "precio" }` en el `value` de una
  condición o de un efecto. Permite parametrizar un mismo nodo en lugar de
  duplicarlo por cada valor posible.
- `GraphSimulator`: evalúa un grafo contra un estado de variables sin
  reproducirlo, marcando aristas transitables, bloqueadas y ocultas, y trazando
  el camino determinista hasta la primera decisión del jugador.
- `VariableInventory`: lista las variables que usa un grafo y dónde se leen y se
  escriben. Detecta erratas de nombre, que tras el cambio de semántica de las
  variables sin definir pasan a ser silenciosas.
- Visor de grafos con GraphView: solo lectura, con panel de simulación,
  incidencias clicables y resaltado del nodo activo en Play Mode.
- `Runtime/Unity`: `DialogueGraphAsset`, envoltorio delgado sobre un `TextAsset`.

### Changed
- Una variable sin definir se compara como 0 frente a valores numéricos, y como
  cadena vacía frente a cadenas. Antes cualquier comparación con una variable
  inexistente daba false. Esto permite escribir `contador == 0` en lugar de
  `{any: [{!exists}, {== 0}]}`.
- `exists` / `!exists` pasan a ser la única forma de distinguir "sin definir"
  de "cero".
- `GraphWriter` se reescribe sin estado: construye un árbol de salida
  (`JsonObj`/`JsonArr`) y lo renderiza, en lugar de emitir comas y sangrado con
  una máquina de estados. La versión anterior produjo dos bugs de comas en dos
  iteraciones.

### Fixed
- `VariableMath.Add` degradaba a int cualquier resultado sin decimales,
  cambiando el tipo de variables double a mitad de partida.
- `VariableInventory` marcaba como "nunca leída" una variable usada solo en
  operaciones `+=` o `-=`.

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
- El guardia de transiciones es **por paso**, no acumulativo: cuenta los saltos
  automáticos antes de producir un paso visible, así que la interacción
  prolongada del jugador nunca lo agota.

### Limitaciones conocidas
- El parser acepta comentarios `//`, pero el escritor no los conserva.
- Sin parser de expresiones: las condiciones son comparaciones simples
  combinadas con all/any/not.
- Sin editor gráfico, sin subgrafos y sin conversaciones anidadas.
- Autoría a mano en JSON.