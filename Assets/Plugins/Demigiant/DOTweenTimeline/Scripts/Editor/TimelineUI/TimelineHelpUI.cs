// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/03/02

using System.Text;
using DG.DemiEditor;
using DG.DemiLib;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor
{
    internal class TimelineHelpUI : ABSTimelineElement
    {
        static readonly Color _BgColor = new DeSkinColor(0.15f);
        static readonly HelpDoc _GeneralHelpDoc = new HelpDoc();
        static readonly HelpDoc _KeyboardCommandsDoc = new HelpDoc();
        static readonly GUIContent[] _GcMode = new[] {new GUIContent("General Help"), new GUIContent("Keyboard Commands")};
        static int _mode;
        Vector2 _scrollP;

        #region Public Methods

        public void Refresh() {}

        public override void Draw(Rect drawArea)
        {
            base.Draw(drawArea);

            DOEGUI.BeginGUI();
            if (!_KeyboardCommandsDoc.HasContent()) GenerateHelp();

            DeGUI.DrawColoredSquare(area, _BgColor);

            // Toolbar
            using (new DeGUILayout.ToolbarScope()) {
                using (var check = new EditorGUI.ChangeCheckScope()) {
                    _mode = GUILayout.SelectionGrid(_mode, _GcMode, 2, DOEGUI.Styles.button.tool, GUILayout.Width(250));
                    if (check.changed) _scrollP = Vector2.zero;
                }
                GUILayout.FlexibleSpace();
                using (new DeGUI.ColorScope(new DeSkinColor(0.7f, 0.3f))) {
                    if (GUILayout.Button("×", DOEGUI.Styles.button.tool, GUILayout.Width(18))) {
                        DOTweenClipTimeline.mode = DOTweenClipTimeline.Mode.Default;
                    }
                }
            }
            switch (_mode) {
            case 1: // Keyboard Commands
                _scrollP = GUILayout.BeginScrollView(_scrollP);
                GUILayout.Label(_KeyboardCommandsDoc.content, DOEGUI.Styles.timeline.helpContent);
                GUILayout.EndScrollView();
                break;
            default: // General Help
                _scrollP = GUILayout.BeginScrollView(_scrollP);
                GUILayout.Label(_GeneralHelpDoc.content, DOEGUI.Styles.timeline.helpContent);
                GUILayout.EndScrollView();
                break;
            }
        }

        #endregion

        #region Methods

        void GenerateHelp()
        {
            // General Help

            _GeneralHelpDoc.AddTitle("Get Started");
            _GeneralHelpDoc.AddParagraph("Press the [A]Add Component[/A] button on a GameObject's Inspector to add a [B]DOTween > Clip[/B] " +
                                         "or a [B]ClipCollection[/B] (there's also [B]ClipVariant[/B] but we'll talk about that later), " +
                                         "then [A]press the EDIT button[/A] on the clip to open the Timeline and create your animation " +
                                         "(aka [B]Tween[/B] or, to be precise a [B]DOTween Sequence[/B])." +
                                         "\n\nAlternatively, you can add a [B]DOTweenClip[/B] serialized field to your own custom MonoBehaviour, " +
                                         "but in that case you'll have to generate the tween manually at runtime (and kill it when necessary), " +
                                         "using [C]clipInstance.GenerateTween()[/C] (or [C]clipInstance.Play()[/C] for a more " +
                                         "straightforward approach).");
            _GeneralHelpDoc.AddTitle("Editing a clip in DOTween Timeline");
            _GeneralHelpDoc.AddParagraph("Each clip's timeline contains a series of clip elements ([B]Tweens/GlobalTweens/Actions/Events/Intervals[/B])." +
                                         "\nYou can [A]drag a GameObject[/A] to the Timeline to create a new [B]Tween clip element[/B] with it, " +
                                         "or right click on an empty space to create other types of clip elements. " +
                                         "\nDon't forget to [A]check the Keyboard Commands[/A] tab for shortcuts/etc.");
            _GeneralHelpDoc.AddTitle("Advanced");
            _GeneralHelpDoc.AddParagraph("You can can [A]pin clip elements in the Timeline[/A] to retrieve them at runtime via " +
                                         "[C]clipInstance.FindClipElementsByPinNoAlloc()[/C] and modify them before creating the tween." +
                                         "\nYou can also generate [B]completely independent tweens[/B] from a [B]DOTweenClip[/B] via " +
                                         "[C]clipInstance.GenerateIndependentTween[/C], which comes with params to replace original targets.");
            _GeneralHelpDoc.AddSubtitle("ClipVariant");
            _GeneralHelpDoc.AddParagraph("[B]ClipVariants[/B] can either be added as a Component to a GameObject or as a serialized field to your own " +
                                         "custom MonoBehaviour. A ClipVariant takes another Clip as a target and shows you all its settings and targets " +
                                         "in the Inspector, allowing you to [B]change them and generate a tween/animation that is based on another tween[/B] " +
                                         "(and even to invert it completely and play it mirrored).");

            _GeneralHelpDoc.FinalizeContent();

            // Keyboard commands
            
            _KeyboardCommandsDoc.AddTitle("Keys + Shortcuts");
            _KeyboardCommandsDoc.AddSubtitle("Timeline - General")
                .AddShortcut("MMB + Drag / ALT+LMB + Drag", "Drag the Timeline")
                .AddShortcut("ScrollWheel", "Resize the Timeline horizontally")
                .AddShortcut("SHIFT+ScrollWheel", "Resize the Timeline vertically")
                .AddShortcut("Drag GameObject", "to Timeline", "Add new Tween element")
                .AddShortcut("LMB + Drag", "on empty space", "Create selection area")
                .AddShortcut("LMB", "on element", "Add element to selection")
                .AddShortcut("RMB", "on empty space", "Add new element with options to choose type (Tween/Action/Event/etc)")
                .AddShortcut("CTRL+A", "Select all elements")
                .AddShortcut("CTRL+D / ESC", "Deselect all elements")
                .AddShortcut("CTRL+C", "Copy selected elements")
                .AddShortcut("CTRL+X", "Cut selected elements")
                .AddShortcut("CTRL+V", "Paste copied elements")
                .AddShortcut("CTRL+SHIFT+V", "Paste copied elements in place (at the same time position they were when copied)")
                .AddShortcut("DEL / Backspace", "Delete selected elements");
            _KeyboardCommandsDoc.AddSubtitle("Timeline - When dragging elements")
                .AddShortcut("CTRL + Drag", "Snap to 0.25 seconds interval")
                .AddShortcut("ALT + Drag", "Snap to other elements' beginning/end (if nearby)")
                .AddShortcut("SHIFT + Drag", "Drag only vertically (between layers) without changing the time position");
            _KeyboardCommandsDoc.AddSubtitle("Layers")
                .AddParagraph("Layers are irrelevant at runtime except for deactivated ones (eye icon), whose elements will be ignored.")
                .AddShortcut("LMB + Drag", "on layer name", "Reorder layer")
                .AddShortcut("Double Click", "on layer name", "Rename layer")
                .AddShortcut("LMB", "left of layer name", "Set layer color");

            _KeyboardCommandsDoc.FinalizeContent();
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        class HelpDoc
        {
            public GUIContent content = new GUIContent();
            readonly StringBuilder _strb = new StringBuilder();

            public bool HasContent()
            {
                return !string.IsNullOrEmpty(content.text);
            }

            public void FinalizeContent()
            {
                content.text = _strb.ToString();
                _strb.Length = 0;
            }

            public HelpDoc AddTitle(string title)
            {
                if (_strb.Length > 0) _strb.Append("\n\n");
                _strb.Append(Tag.Title_Open).Append(title).Append(Tag.Title_Close);
                return this;
            }

            public HelpDoc AddSubtitle(string subtitle)
            {
                _strb.Append('\n').Append(Tag.Subtitle_Open).Append(subtitle).Append(Tag.Subtitle_Close);
                return this;
            }

            public HelpDoc AddParagraph(string text)
            {
                string parsedText = text.Replace("[B]", Tag.BoldTag_Open).Replace("[/B]", Tag.BoldTag_Close)
                    .Replace("[A]", Tag.ActionTag_Open).Replace("[/A]", Tag.ActionTag_Close)
                    .Replace("[C]", Tag.CodeTag_Open).Replace("[/C]", Tag.CodeTag_Close);
                _strb.Append('\n').Append(Tag.Paragraph_Open).Append(parsedText).Append(Tag.Paragraph_Close);
                return this;
            }

            public HelpDoc AddShortcut(string shortcut, string description)
            {
                AddShortcut(shortcut, null, description);
                return this;
            }

            public HelpDoc AddShortcut(string shortcut, string shortcutExtra, string description)
            {
                _strb.Append('\n').Append(Tag.ShortcutDefinitionTerm_Open).Append(shortcut).Append(Tag.ShortcutDefinitionTerm_Close);
                if (shortcutExtra != null) {
                    _strb.Append(' ').Append(Tag.ShortcutDefinitionTerm_Extra_Open).Append(shortcutExtra).Append(Tag.ShortcutDefinitionTerm_Extra_Close);
                }
                _strb.Append(" : ").Append(Tag.ShortcutDefinition_Open).Append(description).Append(Tag.ShortcutDefinition_Close);
                return this;
            }

            // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
            // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
            // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

            static class Tag
            {
                public const string Title_Open = "<color=#94de59><size=18>";
                public const string Title_Close = "</size></color>";
                public const string Paragraph_Open = "";
                public const string Paragraph_Close = "";
                public const string Subtitle_Open = "<color=#ebd400><size=15>";
                public const string Subtitle_Close = "</size></color>";
                public const string ShortcutDefinitionTerm_Open = "<color=#db89ff>";
                public const string ShortcutDefinitionTerm_Close = "</color>";
                public const string ShortcutDefinitionTerm_Extra_Open = "<color=#9f89ff><i>";
                public const string ShortcutDefinitionTerm_Extra_Close = "</i></color>";
                public const string ShortcutDefinition_Open = "<color=#aaaaaa>";
                public const string ShortcutDefinition_Close = "</color>";
                public const string BoldTag_Open = "<color=#6fc5ff><b>";
                public const string BoldTag_Close = "</b></color>";
                public const string ActionTag_Open = "<color=#ff8f06><i>";
                public const string ActionTag_Close = "</i></color>";
                public const string CodeTag_Open = "<color=#e283ff>";
                public const string CodeTag_Close = "</color>";
            }
        }
    }
}