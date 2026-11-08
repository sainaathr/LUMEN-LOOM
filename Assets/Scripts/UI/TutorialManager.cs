using System;
using UnityEngine;
using LumenLoom.Interaction;
using LumenLoom.Rendering;
using LumenLoom.Puzzle;

namespace LumenLoom.UI
{
    public enum TutorialStep
    {
        SelectNode = 0,
        MoveNode = 1,
        RotateNode = 2,
        OpticalAlignment = 3,
        ConnectTarget = 4,
        ChamberComplete = 5
    }

    /// <summary>
    /// Interactive holographic tutorial and onboarding orchestrator.
    /// Guides players through basic controls: select, drag, rotate, beam alignment, and target connection.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("Tutorial State")]
        [SerializeField] private bool tutorialActive = true;
        [SerializeField] private TutorialStep currentStep = TutorialStep.SelectNode;
        [SerializeField] private bool autoAdvanceOnAction = true;

        [Header("Dependencies")]
        [SerializeField] private NodeInteractionController interactionController;
        [SerializeField] private BeamValidator beamValidator;
        [SerializeField] private LevelManager levelManager;

        public bool IsActive => tutorialActive;
        public TutorialStep CurrentStep => currentStep;

        public event Action<TutorialStep> OnStepChanged;

        private static readonly string[] StepTitles = new string[]
        {
            "STEP 1: SELECT NODE",
            "STEP 2: TRANSLATE NODE",
            "STEP 3: ROTATE LENS",
            "STEP 4: OPTICAL ALIGNMENT",
            "STEP 5: TARGET CONNECTION",
            "PROTOCOL RESTORED"
        };

        private static readonly string[] StepDescriptions = new string[]
        {
            "Left-click on any optical node (Node Alpha or Beta) to activate its holographic control matrix.",
            "Hold Left Mouse Button and drag to reposition the active node across the designated puzzle plane.",
            "Press 'Q' or 'E' (or use Mouse Scroll) to rotate the node's optical lens and redirect incoming light.",
            "Align the node's reception angle to capture the beam emitted from the primary Energy Source.",
            "Chain light through all required nodes and guide the focused beam into the Target Receiver.",
            "Energy resonance established! Chamber complete. Proceed to the next chamber or replay instructions."
        };

        private void Awake()
        {
            if (interactionController == null)
            {
                interactionController = FindFirstObjectByType<NodeInteractionController>();
            }
            if (beamValidator == null)
            {
                beamValidator = FindFirstObjectByType<BeamValidator>();
            }
            if (levelManager == null)
            {
                levelManager = FindFirstObjectByType<LevelManager>();
            }
        }

        private void Start()
        {
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (interactionController != null && interactionController.InputProvider != null)
            {
                interactionController.InputProvider.OnNodeSelected += HandleNodeSelected;
                interactionController.InputProvider.OnNodeDragPositionUpdated += HandleNodeDragged;
                interactionController.InputProvider.OnNodeRotationDelta += HandleNodeRotated;
            }

            if (beamValidator != null)
            {
                beamValidator.OnPuzzleSolved += HandlePuzzleSolved;
            }

            if (levelManager != null)
            {
                levelManager.OnLevelLoaded += HandleLevelLoaded;
            }
        }

        private void UnsubscribeEvents()
        {
            if (interactionController != null && interactionController.InputProvider != null)
            {
                interactionController.InputProvider.OnNodeSelected -= HandleNodeSelected;
                interactionController.InputProvider.OnNodeDragPositionUpdated -= HandleNodeDragged;
                interactionController.InputProvider.OnNodeRotationDelta -= HandleNodeRotated;
            }

            if (beamValidator != null)
            {
                beamValidator.OnPuzzleSolved -= HandlePuzzleSolved;
            }

            if (levelManager != null)
            {
                levelManager.OnLevelLoaded -= HandleLevelLoaded;
            }
        }

        private void HandleNodeSelected(PuzzleNode node)
        {
            if (autoAdvanceOnAction && currentStep == TutorialStep.SelectNode)
            {
                SetStep(TutorialStep.MoveNode);
            }
        }

        private void HandleNodeDragged(Vector3 position)
        {
            if (autoAdvanceOnAction && currentStep == TutorialStep.MoveNode)
            {
                SetStep(TutorialStep.RotateNode);
            }
        }

        private void HandleNodeRotated(float delta)
        {
            if (autoAdvanceOnAction && currentStep == TutorialStep.RotateNode)
            {
                SetStep(TutorialStep.OpticalAlignment);
            }
        }

        private void HandlePuzzleSolved()
        {
            SetStep(TutorialStep.ChamberComplete);
        }

        private void HandleLevelLoaded(Data.LevelData data)
        {
            // Reset to beginning if Chamber 01 is reloaded
            if (levelManager != null && levelManager.CurrentLevelIndex == 0)
            {
                SetStep(TutorialStep.SelectNode);
            }
        }

        public void SetStep(TutorialStep step)
        {
            currentStep = step;
            OnStepChanged?.Invoke(currentStep);
        }

        public void NextStep()
        {
            int next = (int)currentStep + 1;
            if (next < StepTitles.Length)
            {
                SetStep((TutorialStep)next);
            }
        }

        public void PreviousStep()
        {
            int prev = (int)currentStep - 1;
            if (prev >= 0)
            {
                SetStep((TutorialStep)prev);
            }
        }

        public void ToggleTutorial()
        {
            tutorialActive = !tutorialActive;
        }

        public void SetTutorialActive(bool active)
        {
            tutorialActive = active;
        }

        private void OnGUI()
        {
            // Toggle button always available at bottom right
            if (GUI.Button(new Rect(Screen.width - 150, Screen.height - 40, 130, 30), tutorialActive ? "HIDE GUIDE [X]" : "GUIDE / HELP [?]"))
            {
                ToggleTutorial();
            }

            if (!tutorialActive) return;

            // Render holographic sci-fi card on right side of viewport
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.04f, 0.12f, 0.22f, 0.92f);

            float cardWidth = 360f;
            float cardHeight = 160f;
            float cardX = Screen.width - cardWidth - 20f;
            float cardY = 80f;

            GUILayout.BeginArea(new Rect(cardX, cardY, cardWidth, cardHeight), GUI.skin.box);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.2f, 0.9f, 1.0f);

            int stepIdx = (int)currentStep;
            GUILayout.BeginHorizontal();
            GUILayout.Label(StepTitles[stepIdx], titleStyle);
            if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(20)))
            {
                tutorialActive = false;
            }
            GUILayout.EndHorizontal();

            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 11
            };
            descStyle.normal.textColor = new Color(0.85f, 0.95f, 1.0f);
            GUILayout.Label(StepDescriptions[stepIdx], descStyle);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUI.enabled = stepIdx > 0;
            if (GUILayout.Button("<< PREV", GUILayout.Height(25)))
            {
                PreviousStep();
            }
            GUI.enabled = stepIdx < StepTitles.Length - 1;
            if (GUILayout.Button("NEXT >>", GUILayout.Height(25)))
            {
                NextStep();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            GUI.backgroundColor = prevBg;
        }
    }
}

