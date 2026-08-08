# Prompter

Reproductor de diálogo agnóstico de la fuente y de la presentación.

Prompter no decide **qué** se dice ni **cómo** se pinta: orquesta la reproducción
de una conversación. Las líneas las produce una fuente (`IDialogueSource`), y las
muestra uno o más presentadores (`IDialoguePresenter`).

## Uso mínimo

```csharp
var prompter = new DialoguePlayer(new DialoguePlayerOptions {
    Presenters = { miPresentador }
});

var result = await prompter.Play(DialogueSource.FromLines(
    "Alba: ¿Y esto qué es?",
    "Nilo: Ni idea. Pero brilla."
));
```

Lo único obligatorio es un presentador. El resto de puertos tienen defaults.

## Puertos

| Interfaz | Obligatorio | Default |
|---|---|---|
| `IDialoguePresenter` | **sí** | — |
| `IDialogueSource` | sí (parámetro de `Play`) | `DialogueSource.FromLines` |
| `IDialogueInput` | no | `ImmediateInput` |
| `ILineProvider` | no | `PassthroughLineProvider` |
| `IVariableStorage` | no | `InMemoryVariableStorage` |
| `ICommandDispatcher` | no | `LoggingCommandDispatcher` |

## Instalación

Package Manager → *Install package from git URL*: https://github.com/USUARIO/DialogueFramework.git?path=/Packages/com.neurohard.prompter#v0.1.0

## Sample

Package Manager → Prompter → Samples → *Ejemplo mínimo*: presentador de consola
e input IMGUI, listo para arrastrar a un GameObject.

## Requisitos

Unity 6.3 LTS o superior. Sin dependencias externas.