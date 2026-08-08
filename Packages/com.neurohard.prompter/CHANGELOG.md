# Changelog

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).
Este proyecto sigue [Versionado Semántico](https://semver.org/lang/es/).

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
- Session: `DialogueSession` y la fachada `Prompter` con `PrompterOptions`.

### Limitaciones conocidas
- **Skip sin ventana de gracia.** Si el jugador pulsa justo cuando la presentación
  está terminando, esa pulsación puede consumirse en un skip innecesario y hacer
  falta una segunda para avanzar. Pendiente de evaluar con un juego real antes de
  añadir un `TimeProvider` al núcleo.
- **Sin cola de sesiones.** `Play` es reentrante-inseguro por diseño: lanza
  `InvalidOperationException` si ya hay una conversación activa. La cola con
  prioridades (barks que interrumpen o se encolan) llegará en 0.2.
- **`LinearScript` ignora la opción elegida.** No hay ramificación; las opciones
  solo sirven para probar la presentación. Las ramas son competencia de la
  futura fuente de árboles.
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

  ### Changed
- La clase `Prompter` pasa a llamarse `DialoguePlayer`. Un tipo con el mismo
  nombre que su namespace padre (`Neurohard.Prompter`) provoca el error
  "'Prompter' is a namespace but is used like a type" en cualquier ensamblado
  externo, incluido Playwright. El paquete sigue llamándose Prompter.