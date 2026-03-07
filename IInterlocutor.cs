using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

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

            for (int i = 0;
                 // Length - 1 as we don't want to delay after showing the last character
                 i < text.Length - 1 && ui.FastForwardRequested == false;
                 i++)
            {
                ui.SetTypewritingCharacter(i + 1);
                await UniTask.Delay(TimeSpan.FromSeconds(0.05), DelayType.UnscaledDeltaTime, cancellationToken: cancellation, cancelImmediately: true);
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
