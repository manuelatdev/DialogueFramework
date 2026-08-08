using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Neurohard.Prompter;
using Neurohard.Prompter.Unity;

[RequireComponent(typeof(UIDocument))]
public sealed class UiToolkitPresenter : MonoBehaviour, IDialoguePresenter
{
    [SerializeField, Range(5f, 120f)] private float charactersPerSecond = 45f;
    
    private VisualElement _dialoguePanel;
    private VisualElement _optionsPanel;
    private VisualElement _optionsList;
    private Label _speaker;
    private Label _text;
    private Label _arrow;

    private volatile bool _skipRequested;

    /// <summary>Input que alimentan los clics de esta UI.</summary>
    public UnityAwaitableInput Input { get; } = new UnityAwaitableInput();

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _dialoguePanel = root.Q<VisualElement>("dialogue-panel");
        _optionsPanel = root.Q<VisualElement>("options-panel");
        _optionsList = root.Q<VisualElement>("options-list");
        _speaker = root.Q<Label>("speaker");
        _text = root.Q<Label>("text");
        _arrow = root.Q<Label>("advance-arrow");

        // Un clic en cualquier parte avanza la línea.
        root.RegisterCallback<PointerDownEvent>(_ =>
        {
            if (_optionsPanel.style.display == DisplayStyle.Flex) return;
            Input.Advance();
        });
    }

    // --- IDialoguePresenter ------------------------------------------------

    public async Task ShowLineAsync(ResolvedLine line, CancellationToken ct)
    {
        _skipRequested = false;

        _dialoguePanel.style.display = DisplayStyle.Flex;
        _arrow.style.display = DisplayStyle.None;

        var hasSpeaker = !string.IsNullOrEmpty(line.SpeakerId);
        _speaker.parent.style.display = hasSpeaker ? DisplayStyle.Flex : DisplayStyle.None;
        _speaker.text = hasSpeaker ? line.SpeakerId.ToUpperInvariant() : string.Empty;

        await TypeAsync(line.Text, ct);

        _arrow.style.display = DisplayStyle.Flex;
    }

    public Task ShowOptionsAsync(
        IReadOnlyList<ResolvedOption> options, ResolvedLine prompt, CancellationToken ct)
    {
        _optionsList.Clear();

        foreach (var option in options)
            _optionsList.Add(BuildOption(option));

        _optionsPanel.style.display = DisplayStyle.Flex;
        _arrow.style.display = DisplayStyle.None;
        return Task.CompletedTask;
    }

    public void SkipCurrentPresentation() => _skipRequested = true;

    public Task ClearAsync(CancellationToken ct)
    {
        _dialoguePanel.style.display = DisplayStyle.None;
        _optionsPanel.style.display = DisplayStyle.None;
        _optionsList.Clear();
        return Task.CompletedTask;
    }

    // --- interno -----------------------------------------------------------

    private VisualElement BuildOption(ResolvedOption option)
    {
        var row = new VisualElement();
        row.AddToClassList("option");
        if (!option.IsAvailable) row.AddToClassList("option--disabled");

        var cursor = new Label("▶");
        cursor.AddToClassList("option-cursor");
        row.Add(cursor);

        var label = new Label(option.Line.Text);
        label.AddToClassList("option-label");
        row.Add(label);

        // Los tags de la línea sirven de pista para opciones bloqueadas.
        if (!option.IsAvailable && option.Line.Tags.Count > 0)
        {
            var hint = new Label($"({option.Line.Tags[0]})");
            hint.AddToClassList("option-hint");
            row.Add(hint);
        }

        if (option.IsAvailable)
        {
            var id = option.OptionId;
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                _optionsPanel.style.display = DisplayStyle.None;
                Input.Select(id);
            });
        }

        return row;
    }

    private async Task TypeAsync(string text, CancellationToken ct)
    {
        _text.text = string.Empty;

        var shown = 0f;
        while (shown < text.Length)
        {
            if (_skipRequested) break;
            ct.ThrowIfCancellationRequested();

            await Awaitable.NextFrameAsync(ct);
            shown += Time.deltaTime * charactersPerSecond;

            var count = Mathf.Min(Mathf.FloorToInt(shown), text.Length);
            _text.text = text.Substring(0, count);
        }

        _text.text = text;
    }
}