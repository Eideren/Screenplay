using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Screenplay;

public interface IInterlocutor
{
    UniTask RunDialog(IEventContext context, IEnumerable<string> lines, bool previewMode, CancellationToken cancellation);

    public static async UniTask DefaultRunDialog(IEventContext context, IEnumerable<string> lines, bool previewMode, CancellationToken cancellation)
    {
        var ui = context.GetDialogUI();
        ui.StartDialogPresentation();
        foreach (var text in lines)
        {
            ui.StartLineTypewriting(text);
            ui.SetTypewritingCharacter(0);

            float delay = 0;
            // Length - 1 as we don't want to delay after showing the last character
            for (int i = 0; i < text.Length - 1; i++)
            {
                ui.SetTypewritingCharacter(i + 1);
                delay += 0.05f;
                while ((delay -= Time.unscaledDeltaTime) > 0 && ui.FastForwardRequested == false)
                    await UniTask.NextFrame(cancellationToken: cancellation, cancelImmediately: true);

                if (ui.FastForwardRequested) // After a skip in the middle of typewriting, wait for another skip signal before continuing
                {
                    await UniTask.NextFrame(cancellation, cancelImmediately: true);
                    break;
                }
            }

            ui.SetTypewritingCharacter(text.Length);
            ui.FinishedTypewriting();

            while (previewMode || ui.DialogAdvancesAutomatically == false)
            {
                if (ui.FastForwardRequested)
                {
                    await UniTask.NextFrame(cancellation, cancelImmediately:true);
                    break;
                }

                await UniTask.NextFrame(cancellation, cancelImmediately:true);
            }
        }
        ui.EndDialogPresentation();
    }
}
