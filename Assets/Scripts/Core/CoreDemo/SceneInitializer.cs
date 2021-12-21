using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.CoreDemo
{
    public class SceneInitializer : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private AbilityController _abilityController;
        [SerializeField]
        private UIController _uiController;
        [SerializeField]
        private ModesUISwitcher _modesUISwitcher;
    
        [Space]
        [SerializeField]
        private WindowsController _windowsController;
        [SerializeField]
        private InfoPanelView _infoPanelView;
        [SerializeField]
        private EditModeControlButtonsController _editModeControlButtonsController;
        [SerializeField]
        private EditModeUnitButtonsController _editModeUnitButtonsController;

        [Space]
        [SerializeField]
        private ButtonsLeftPanelSwitcher _editModeLeftPanelSwitcher;
        [SerializeField]
        private ButtonsLeftPanelSwitcher _gameModeLeftPanelSwitcher;
        [SerializeField]
        private ButtonsRightPanelSwitcher _editModeRightPanelSwitcher;
        [SerializeField]
        private ButtonsRightPanelSwitcher _gameModeRightPanelSwitcher;
        [SerializeField]
        private ButtonsRightDownPanelSwitcher _editModeRightDownPanelSwitcher;
        [SerializeField]
        private ButtonsRightDownPanelSwitcher _gameModeModeRightDownPanelSwitcher;

        [Space]
        [SerializeField]
        private GameModeRightDownButton[] _gameModeRightDownButtons;
        [SerializeField]
        private AbilityDataSO[] _abilitiesData;


        [Header("Core References")]
        [SerializeField]
        private RotatorPointsHolder _rotatorPointsHolder;
        [SerializeField]
        private Planet _planet;
        [SerializeField]
        private Camera _playerCamera;
        [SerializeField]
        private CameraLookRotator _cameraLookRotator;
        [SerializeField]
        private ScoresHolder _scoresHolder;
        [SerializeField]
        private ShotManager _shotManager;
        [SerializeField]
        private BuffManager _buffManager;


        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            DOTween.SetTweensCapacity(500, 50);
        
            InitializeCoreReferences();
            InitializeUIReferences();
            InitializeLate();
        }

        private void InitializeCoreReferences()
        {
            _rotatorPointsHolder.Init();
            _planet.Init(_rotatorPointsHolder, _buffManager);
            _cameraLookRotator.Init(_planet, _rotatorPointsHolder, _windowsController);
            _scoresHolder.Init(_planet, _cameraLookRotator, _abilityController); // ! cyclic dependence
            _buffManager.Init(_planet, _cameraLookRotator);
            _shotManager.Init(_planet);
        }

        private void InitializeUIReferences()
        {
            _abilityController.Init(_planet, _cameraLookRotator, _shotManager, _buffManager, _windowsController, _scoresHolder); // ! cyclic dependence
            _uiController.Init();
            _modesUISwitcher.Init();
            _infoPanelView.Init(_scoresHolder);
        
            _editModeLeftPanelSwitcher.Init(_planet, _cameraLookRotator);
            _gameModeLeftPanelSwitcher.Init(_planet, _cameraLookRotator);
            _editModeRightPanelSwitcher.Init(_planet, _cameraLookRotator);
            _gameModeRightPanelSwitcher.Init(_planet, _cameraLookRotator);
            _editModeRightDownPanelSwitcher.Init(_planet, _cameraLookRotator, _shotManager, _abilityController);
            _gameModeModeRightDownPanelSwitcher.Init(_planet, _cameraLookRotator, _shotManager, _abilityController);
        
            _gameModeRightDownButtons.ToList().ForEach(button => button.Init(_windowsController));
            _abilitiesData.ToList().ForEach(data => data.Init(_cameraLookRotator, _shotManager, _buffManager));
        }

        private void InitializeLate()
        {
            _planet.SetupBoardFillers(_scoresHolder);
            _planet.InitCosts();
        }
    }
}
