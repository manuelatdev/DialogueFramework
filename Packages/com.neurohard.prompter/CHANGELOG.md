# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto sigue [Versionado Semántico](https://semver.org/lang/es/).

## [0.5.0]

El diálogo puede consultar estado que no le pertenece.

### Added
- `IQueryResolver`: puerto para consultar al juego cosas que el diálogo no puede
  saber (inventario, hora del día, estado del mundo). Simétrico a
  `ICommandDispatcher`: si aquel ordena, este pregunta.
- Default `NoQueryResolver`: no resuelve nada y avisa por el log. Las condiciones
  que dependan de una consulta se evalúan como no cumplidas.
- `DialoguePlayerOptions.Queries` y `PrompterBehaviour.Queries`.

### Changed
- `DialogueContext` lleva `Queries` además de `Variables` y `Parameters`. El
  parámetro nuevo va en segunda posición: las llamadas que pasaban el diccionario
  de parámetros ahí deben usar el nombre del argumento.

### Notas de diseño
- El inventario y la economía no son asunto del sistema de diálogos. Modelarlos
  con variables planas obligaba a duplicar el procedimiento de compra por cada
  artículo, porque el destino de una asignación no puede ser dinámico. La
  solución no fue enriquecer el lenguaje del diálogo, sino sacar esa
  responsabilidad al juego mediante comandos y consultas.
- Criterio para decidir dónde vive un dato: si existe aunque no haya ninguna
  conversación en marcha (inventario, quests, clima), es del juego. Si solo
  existe porque hubo una conversación (`mentiste`, `veces_hablado`), es del
  diálogo.

## [0.4.0]

### Added
- `DialogueOption` y `ResolvedOption` llevan `UnavailableReason`: el motivo por
  el que una opción está bloqueada, para que el presentador pueda mostrarlo.

### Changed
- Los `tags` de una línea dejan de usarse como motivo de bloqueo. Vuelven a ser
  lo que eran: marcas de estilo.

## [0.3.0]

### Changed
- `PrompterOptions` pasa a llamarse `DialoguePlayerOptions`, por coherencia con
  `DialoguePlayer`.
- El ensamblado `Neurohard.Prompter` pasa a `Neurohard.Prompter.Unity`: su
  namespace ya era ese y el nombre corto inducía a confusión.

## [0.2.1]

### Fixed
- El código de `Samples~` seguía usando `Prompter` en lugar de `DialoguePlayer`.
  Unity no compila esa carpeta, así que la ruptura pasó desapercibida hasta
  probar la instalación en un proyecto limpio.

### Notas de mantenimiento
- El contenido de `Samples~` no lo compila Unity. Verifícalo antes de publicar,
  o enlázalo con un symlink desde el sandbox para que sí se compile.

## [0.2.0]

Capa Unity y renombrado de la fachada.

### Added
- `Runtime/Unity`: `PrompterBehaviour`, que ata el reproductor al ciclo de vida
  del GameObject y cancela la conversación en `OnDestroy` y `OnDisable`.
- `UnityAwaitableInput`: input pasivo que la UI del juego alimenta llamando a
  `Advance()` y `Select(optionId)`.
- Declaración del bloque `samples` en `package.json`.

### Changed
- La clase `Prompter` pasa a llamarse `DialoguePlayer`. Un tipo con el mismo
  nombre que su namespace padre (`Neurohard.Prompter`) provoca el error
  "'Prompter' is a namespace but is used like a type" en cualquier ensamblado
  externo, incluido Playwright. El paquete sigue llamándose Prompter.
- La asmdef de `Runtime/Core` pasa a `autoReferenced: true`. Las referencias
  entre ensamblados no son transitivas, así que sin esto un script suelto en
  `Assets/` no podía usar el contrato.

## [0.1.0] - 2026-08-08

Primera versión funcional: reproduce un guion lineal de punta a punta.

### Added
- Contract: `DialogueStep` (unión sellada: Line, Options, Command, Complete),
  `DialogueLine`, `DialogueOption`, `ResolvedLine`, `ResolvedOption`, `LineId`,
  `DialogueResult` y `DialogueOutcome`.
- Ports: `IDialogueSource`, `ISerializableSource`, `IDialoguePresenter`,
  `IDialogueInput`, `ILineProvider`, `IVariableStorage`, `ICommandDispatcher`.
- Defaults: `PassthroughLineProvider`, `InMemoryVariableStorage`,
  `LoggingCommandDispatcher`, `ImmediateInput`.
- Sources: `LinearScript` con las factorías `DialogueSource.FromLines` y `FromSteps`.
- Session: `DialogueSession` y la fachada `Prompter` con sus opciones.

### Limitaciones conocidas
- **Skip sin ventana de gracia.** Si el jugador pulsa justo cuando la presentación
  está terminando, esa pulsación puede consumirse en un skip innecesario y hacer
  falta una segunda para avanzar. Pendiente de evaluar con un juego real antes de
  añadir un `TimeProvider` al núcleo.
- **Sin cola de sesiones.** `Play` es reentrante-inseguro por diseño: lanza
  `InvalidOperationException` si ya hay una conversación activa.
- **`LinearScript` ignora la opción elegida.** No hay ramificación; las opciones
  solo sirven para probar la presentación.
- **Heurística de parseo en `FromLines`.** Un hablante con espacios
  ("Doctor Vega: hola") no se detecta como hablante. Es una utilidad de pruebas;
  usa `FromSteps` cuando necesites control exacto.

### Notas de diseño
- Sin `record` ni `init`: requieren un shim de `IsExternalInit` por ensamblado,
  inadecuado en un paquete distribuible. Revisar cuando Unity migre a CoreCLR
  (.NET 10 / C# 14), donde el tipo ya está en la BCL.
- `DialogueStep` usa identidad por referencia a propósito: dos pasos con el mismo
  `LineId` son momentos distintos de la conversación, no el mismo paso.
- `ClearAsync` solo se invoca al terminar la sesión, no entre líneas, para evitar
  parpadeos en la caja de diálogo.
- La limpieza final se ejecuta con `CancellationToken.None`: si la sesión se
  cancela, la UI debe cerrarse igualmente.
- Un presentador que falla se desregistra de la sesión en lugar de tumbarla: un
  diálogo bloqueado suele significar partida bloqueada.