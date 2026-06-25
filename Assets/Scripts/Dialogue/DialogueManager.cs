// Runtime dialogue driver for Inky stories.
//
// Pipeline: write .ink in Inky -> drop into Assets/Ink/ -> ink-unity-integration
// auto-compiles a TextAsset (.json) -> assign it here (or call StartDialogue).
//
// Supported inline tags (one per line in Ink, written as "# tag: value"):
//   # speaker: Witch      -> sets the speaker name label
//   # voice: witch_001    -> plays Assets/Audio/Voice/witch_001 via voice event
//   # portrait: witch_sad -> raised through OnPortrait for a portrait system
// Any other tag is forwarded through OnTag(key, value).
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mygame.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("Ink")]
        [Tooltip("Compiled Ink JSON (the TextAsset created next to your .ink file).")]
        [SerializeField] private TextAsset inkJson;
        [SerializeField] private bool playOnStart = false;

        [Header("UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Voice (ElevenLabs)")]
        [SerializeField] private AudioSource voiceSource;
        [Tooltip("Folder under a Resources/ directory holding voice clips, e.g. Resources/Voice.")]
        [SerializeField] private string voiceResourceFolder = "Voice";

        [Header("Events")]
        public UnityEvent OnDialogueStart;
        public UnityEvent OnDialogueEnd;
        /// <summary>Raised for every spoken line (after tags are processed).</summary>
        public UnityEvent<string> OnLine;
        /// <summary>Raised for any unhandled tag: (key, value).</summary>
        public UnityEvent<string, string> OnTag;
        /// <summary>Raised on "# portrait: id".</summary>
        public UnityEvent<string> OnPortrait;

        public bool IsPlaying { get; private set; }
        public Story Story { get; private set; }

        private readonly List<Button> _spawnedChoices = new();

        private void Start()
        {
            // Start playing (which shows the panel) OR hide it until told to start.
            // Never hide unconditionally — that would race with an external caller
            // that already started the dialogue this frame.
            if (playOnStart && inkJson != null) StartDialogue(inkJson);
            else if (dialoguePanel != null && !IsPlaying) dialoguePanel.SetActive(false);
        }

        /// <summary>Start a dialogue from the serialized TextAsset.</summary>
        public void StartDialogue() => StartDialogue(inkJson);

        /// <summary>Start a dialogue from a specific compiled Ink TextAsset.</summary>
        public void StartDialogue(TextAsset compiledInk)
        {
            if (compiledInk == null)
            {
                Debug.LogError("[Dialogue] No compiled Ink JSON assigned.");
                return;
            }

            Story = new Story(compiledInk.text);
            IsPlaying = true;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            Canvas.ForceUpdateCanvases(); // ensure layout/visibility on the first frame
            OnDialogueStart?.Invoke();
            Advance();
        }

        /// <summary>
        /// Advance the story: prints the next line, or shows choices, or ends.
        /// Hook this to a "tap to continue" button / input.
        /// </summary>
        public void Advance()
        {
            if (!IsPlaying || Story == null) return;

            // If choices are on screen, ignore advance until one is picked.
            if (Story.currentChoices.Count > 0) return;

            if (Story.canContinue)
            {
                string line = Story.Continue().Trim();
                HandleTags(Story.currentTags);

                if (dialogueText != null) dialogueText.text = line;
                OnLine?.Invoke(line);

                // A line can be empty (only tags); auto-skip to the next.
                if (string.IsNullOrEmpty(line) && Story.canContinue)
                {
                    Advance();
                    return;
                }

                if (Story.currentChoices.Count > 0) ShowChoices();
            }
            else if (Story.currentChoices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                EndDialogue();
            }
        }

        private void ShowChoices()
        {
            ClearChoices();
            if (choicesContainer == null || choiceButtonPrefab == null)
            {
                Debug.LogWarning("[Dialogue] Choices present but no choice UI assigned.");
                return;
            }

            var choices = Story.currentChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                int index = i; // capture
                Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choices[i].text;
                btn.onClick.AddListener(() => ChooseChoice(index));
                _spawnedChoices.Add(btn);
            }
        }

        /// <summary>Pick a choice by index, then continue the story.</summary>
        public void ChooseChoice(int index)
        {
            if (Story == null || index < 0 || index >= Story.currentChoices.Count) return;
            Story.ChooseChoiceIndex(index);
            ClearChoices();
            Advance();
        }

        private void HandleTags(List<string> tags)
        {
            if (tags == null) return;
            foreach (string raw in tags)
            {
                int colon = raw.IndexOf(':');
                string key = (colon >= 0 ? raw.Substring(0, colon) : raw).Trim().ToLowerInvariant();
                string value = colon >= 0 ? raw.Substring(colon + 1).Trim() : "";

                switch (key)
                {
                    case "speaker":
                        if (speakerText != null) speakerText.text = value;
                        break;
                    case "voice":
                        PlayVoice(value);
                        break;
                    case "portrait":
                        OnPortrait?.Invoke(value);
                        break;
                    default:
                        OnTag?.Invoke(key, value);
                        break;
                }
            }
        }

        private void PlayVoice(string clipName)
        {
            if (voiceSource == null || string.IsNullOrEmpty(clipName)) return;
            var clip = Resources.Load<AudioClip>($"{voiceResourceFolder}/{clipName}");
            if (clip != null) { voiceSource.Stop(); voiceSource.PlayOneShot(clip); }
            else Debug.LogWarning($"[Dialogue] Voice clip not found: {voiceResourceFolder}/{clipName}");
        }

        public void EndDialogue()
        {
            ClearChoices();
            IsPlaying = false;
            Story = null;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            OnDialogueEnd?.Invoke();
        }

        private void ClearChoices()
        {
            foreach (var btn in _spawnedChoices)
                if (btn != null) Destroy(btn.gameObject);
            _spawnedChoices.Clear();
        }

        // ---- Ink variable helpers (for save/load and game state) ----
        public object GetVariable(string name) =>
            Story?.variablesState?[name];

        public void SetVariable(string name, object value)
        {
            if (Story?.variablesState != null) Story.variablesState[name] = value;
        }
    }
}
