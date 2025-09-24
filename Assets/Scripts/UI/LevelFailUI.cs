using UnityEngine;
using UnityEngine.UI;

namespace SlowpokeStudio.UI
{
    public class LevelFailUI : UIAbstractClass
    {
        [Header("Level Fail Reference")]
        [SerializeField] internal GameObject levelFailPanel;
        [SerializeField] internal Button replayButton;
        [SerializeField] internal Button homeButton;
        [SerializeField] internal Button quitButton;

        private void OnEnable()
        {
            replayButton.onClick.AddListener(OnReplayButton);
            homeButton.onClick.AddListener(OnHomeButton);
            quitButton.onClick.AddListener(OnQuitButton);
        }

        private void OnDisable()
        {
            replayButton.onClick.RemoveListener(OnReplayButton);
            homeButton.onClick.RemoveListener(OnHomeButton);
            quitButton.onClick.RemoveListener(OnQuitButton);
        }

        private void OnReplayButton()
        {

        }

        private void OnHomeButton()
        {

        }

        private void OnQuitButton()
        {
            Application.Quit();
        }
    }
}
