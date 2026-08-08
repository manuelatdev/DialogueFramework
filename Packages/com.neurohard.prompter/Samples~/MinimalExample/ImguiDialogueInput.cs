using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Neurohard.Prompter.Samples
{
    public sealed class ImguiDialogueInput : MonoBehaviour, IDialogueInput
    {
        private TaskCompletionSource<bool> _advance;
        private TaskCompletionSource<string> _selection;
        private IReadOnlyList<ResolvedOption> _pending;

        public Task WaitForAdvanceAsync(CancellationToken ct)
        {
            _advance = New<bool>();
            ct.Register(() => _advance?.TrySetCanceled());
            return _advance.Task;
        }

        public Task<string> WaitForSelectionAsync(IReadOnlyList<ResolvedOption> options, CancellationToken ct)
        {
            _pending = options;
            _selection = New<string>();
            ct.Register(() => _selection?.TrySetCanceled());
            return _selection.Task;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 380, 400));

            if (_pending != null)
            {
                GUILayout.Label("Elige:");
                foreach (var o in _pending)
                {
                    GUI.enabled = o.IsAvailable;
                    if (GUILayout.Button(o.Line.Text))
                    {
                        var tcs = _selection;
                        _pending = null;
                        _selection = null;
                        tcs?.TrySetResult(o.OptionId);
                    }
                    GUI.enabled = true;
                }
            }
            else if (_advance != null && !_advance.Task.IsCompleted)
            {
                if (GUILayout.Button("Continuar"))
                    _advance.TrySetResult(true);
            }

            GUILayout.EndArea();
        }

        private static TaskCompletionSource<T> New<T>()
            => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}