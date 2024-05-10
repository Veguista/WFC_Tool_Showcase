using UnityEngine.UIElements;

namespace WFC
{
    namespace EditorPack
    {
        public class ScalableButton : VisualElement
        {
            public Toggle myToggle;
            
            public new class UxmlFactory : UxmlFactory<ScalableButton, UxmlTraits> { }

            public ScalableButton()
            {
                myToggle = new Toggle();
                VisualElement toggleCheckmark = myToggle.Q<VisualElement>("unity-checkmark");

                // toggleCheckmark.style.height = new StyleLength(StyleKeyword.Auto);
                // toggleCheckmark.style.width = new StyleLength(StyleKeyword.Auto);

                myToggle.style.height = Length.Percent(100);
                myToggle.style.width = Length.Percent(100);

                toggleCheckmark.style.height = Length.Percent(100);
                toggleCheckmark.style.width = Length.Percent(100);

                Add(myToggle);
            }
        }
    }
}