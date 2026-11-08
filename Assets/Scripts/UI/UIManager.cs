using UnityEngine;
using UnityEngine.UI;
using LumenLoom.Puzzle;
using LumenLoom.Rendering;

namespace LumenLoom.UI
{
    /// <summary>
    /// Manages in-game head-up display (HUD) and puzzle validation status indicators.
    /// Supports Unity UI Text, TextMeshPro, and automatic OnGUI fallback for immediate zero-setup execution.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Element References")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text controlsHelpText;
        [SerializeField] private Image statusBannerBackground;
        [SerializeField] private Button resetButton;

        [Header("Banner Colors")]
        [SerializeField] private Color solvedBannerColor = new Color(0.1f, 0.7f, 0.3f, 0.85f);
        [SerializeField] private Color unsolvedBannerColor = new Color(0.08f, 0.15f, 0.25f, 0.75f);
        [SerializeField] private Color warningBannerColor = new Color(0.7f, 0.3f, 0.1f, 0.85f);

        [Header("Dependencies")]
        [SerializeField] private BeamValidator beamValidator;
        [SerializeField] private PuzzleManager puzzleManager;
        [SerializeField] private LevelManager levelManager;

        private string _currentStatus = "ALIGN THE ENERGY NODES";
        private bool _isSolved;

        private void Start()
        {
            if (beamValidator == null)
            {
                beamValidator = FindFirstObjectByType<BeamValidator>();
            }

            if (puzzleManager == null)
            {
                puzzleManager = FindFirstObjectByType<PuzzleManager>();
            }

            if (levelManager == null)
            {
                levelManager = FindFirstObjectByType<LevelManager>();
            }

            if (beamValidator != null)
            {
                beamValidator.OnStatusMessageChanged += HandleStatusChanged;
                beamValidator.OnPuzzleSolved += HandlePuzzleSolved;
                beamValidator.OnPuzzleUnsolved += HandlePuzzleUnsolved;
            }

            if (resetButton != null && puzzleManager != null)
            {
                resetButton.onClick.AddListener(puzzleManager.ResetPuzzle);
            }

            if (controlsHelpText != null)
            {
                controlsHelpText.text = "LMB: Select & Drag | Q / E or Scroll: Rotate | R: Reset | Esc: Deselect";
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (beamValidator != null)
            {
                beamValidator.OnStatusMessageChanged -= HandleStatusChanged;
                beamValidator.OnPuzzleSolved -= HandlePuzzleSolved;
                beamValidator.OnPuzzleUnsolved -= HandlePuzzleUnsolved;
            }
        }

        private void HandleStatusChanged(string message)
        {
            _currentStatus = message;
            UpdateDisplay();
        }

        private void HandlePuzzleSolved()
        {
            _isSolved = true;
            UpdateDisplay();
        }

        private void HandlePuzzleUnsolved()
        {
            _isSolved = false;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (statusText != null)
            {
                statusText.text = _currentStatus;
            }

            if (statusBannerBackground != null)
            {
                if (_isSolved)
                {
                    statusBannerBackground.color = solvedBannerColor;
                }
                else if (_currentStatus.Contains("LOOP") || _currentStatus.Contains("BLOCKED"))
                {
                    statusBannerBackground.color = warningBannerColor;
                }
                else
                {
                    statusBannerBackground.color = unsolvedBannerColor;
                }
            }

            if (levelTitleText != null && levelManager != null && levelManager.CurrentLevel != null)
            {
                levelTitleText.text = levelManager.CurrentLevel.levelTitle;
            }
        }

        /// <summary>
        /// Built-in fallback GUI ensures immediate visual display even if canvas components are unassigned.
        /// </summary>
        private void OnGUI()
        {
            if (statusText != null && statusText.gameObject.activeInHierarchy) return;

            // Draw clean Sci-Fi overlay banner at top
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.box);
            bannerStyle.fontSize = 18;
            bannerStyle.fontStyle = FontStyle.Bold;
            bannerStyle.alignment = TextAnchor.MiddleCenter;

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = _isSolved ? new Color(0.1f, 0.8f, 0.4f, 0.9f) : new Color(0.05f, 0.15f, 0.25f, 0.9f);

            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 260, 10, 520, 80), GUI.skin.box);
            string chamberHeader = "LUMEN LOOM — PROTOCOL CHAMBER";
            if (levelManager != null && levelManager.CurrentLevel != null)
            {
                chamberHeader = $"{levelManager.CurrentLevel.levelTitle.ToUpper()} ({levelManager.CurrentLevelIndex + 1}/{levelManager.TotalLevels})";
            }
            GUILayout.Label(chamberHeader, GUI.skin.label);
            GUILayout.Label(_currentStatus, bannerStyle);
            GUILayout.EndArea();

            // Chamber navigation controls (top right)
            if (levelManager != null && levelManager.TotalLevels > 1)
            {
                int btnWidth = 90;
                int btnHeight = 35;
                int startX = Screen.width - 320;
                int topY = 15;

                GUI.enabled = levelManager.CurrentLevelIndex > 0;
                if (GUI.Button(new Rect(startX, topY, btnWidth, btnHeight), "<< PREV"))
                {
                    levelManager.PreviousLevel();
                }

                GUI.enabled = levelManager.HasNextLevel && levelManager.IsLevelUnlocked(levelManager.CurrentLevelIndex + 1);
                if (GUI.Button(new Rect(startX + btnWidth + 10, topY, btnWidth, btnHeight), "NEXT >>"))
                {
                    levelManager.NextLevel();
                }

                GUI.enabled = true;
                if (GUI.Button(new Rect(startX + (btnWidth + 10) * 2, topY, btnWidth, btnHeight), "RESET (R)"))
                {
                    if (puzzleManager != null)
                    {
                        puzzleManager.ResetPuzzle();
                    }
                }
            }
            else
            {
                // Reset button at top right
                if (GUI.Button(new Rect(Screen.width - 130, 20, 110, 40), "RESET (R)"))
                {
                    if (puzzleManager != null)
                    {
                        puzzleManager.ResetPuzzle();
                    }
                }
            }

            GUI.backgroundColor = prevBg;
        }
    }
}

