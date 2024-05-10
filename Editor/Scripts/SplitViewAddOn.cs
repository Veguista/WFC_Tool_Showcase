using UnityEngine.UIElements;

namespace WFC
{
    namespace EditorPack
    {
        public class SplitViewAddOn : TwoPaneSplitView
        {
            public new class UxmlFactory : UxmlFactory<SplitViewAddOn, UxmlTraits> { }
        }
    }
}