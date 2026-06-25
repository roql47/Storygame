// Minimal driver for the dialogue demo scene.
// Starts the dialogue and advances on tap / click / space.
// (Project uses the legacy Input handler, so UnityEngine.Input is used here.)
using Mygame.Dialogue;
using UnityEngine;

namespace Mygame.GameSystem
{
    public class DialogueTester : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogue;
        [SerializeField] private bool autoStart = true;

        private void Start()
        {
            if (autoStart && dialogue != null) dialogue.StartDialogue();
        }

        private void Update()
        {
            if (dialogue == null || !dialogue.IsPlaying) return;

            bool tap = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) tap = true;

            // Advance() no-ops while choices are on screen, so clicking a choice
            // button won't also skip the line.
            if (tap) dialogue.Advance();
        }
    }
}
