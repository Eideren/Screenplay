using System;
using Screenplay.Component;
using Object = UnityEngine.Object;

namespace Screenplay
{
    public class DialogUIField : ICustomField<UIBase>
    {
        [PrefabWithComponent]
        public required UIBase DialogUIPrefab;

        public void GetValue(ScreenplayGraph graph, out UIBase value, out Action<UIBase>? onCleanup)
        {
            value = Object.Instantiate(DialogUIPrefab);
            onCleanup = o =>
            {
                if (o == null || o.gameObject == null)
                    return;

                if (UnityEngine.Application.isPlaying)
                    Object.Destroy(o.gameObject);
                else
                    Object.DestroyImmediate(o.gameObject);
            };
        }
    }

    public static class DialogUIFieldExtension
    {
        public static UIBase GetDialogUI(this IEventContext context) => context.Get<DialogUIField, UIBase>();
    }
}
