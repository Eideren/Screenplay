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
            onCleanup = Object.Destroy;
        }
    }

    public static class DialogUIFieldExtension
    {
        public static UIBase GetDialogUI(this IEventContext context) => context.Get<DialogUIField, UIBase>();
    }
}
