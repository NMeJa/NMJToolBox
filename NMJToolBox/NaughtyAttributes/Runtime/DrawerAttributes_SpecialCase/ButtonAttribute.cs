using System;

namespace NMJToolBox
{
    public enum EButtonEnableMode
    {
        /// <summary>
        /// Button should be active always
        /// </summary>
        Always,

        /// <summary>
        /// Button should be active only in editor
        /// </summary>
        Editor,

        /// <summary>
        /// Button should be active only in playmode
        /// </summary>
        Playmode
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ButtonAttribute : SpecialCaseDrawerAttribute
    {
        public string Text { get; private set; }
        public EButtonEnableMode SelectedEnableMode { get; private set; }

        public ButtonAttribute(string text = null, EButtonEnableMode enabledMode = EButtonEnableMode.Always)
        {
            this.Text = text;
            this.SelectedEnableMode = enabledMode;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonBeginHorizontal : SpecialCaseDrawerAttribute
    {
        public string text;

        public ButtonBeginHorizontal()
        {
        }

        public ButtonBeginHorizontal(string text)
        {
            this.text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonEndHorizontal :SpecialCaseDrawerAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonBeginVertical :SpecialCaseDrawerAttribute
    {
        public string text;

        public ButtonBeginVertical()
        {
        }

        public ButtonBeginVertical(string text)
        {
            this.text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonEndVertical :SpecialCaseDrawerAttribute
    {
    }
}