using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.CoreDemo
{
    public class ScoresHolder : MonoBehaviour
    {
        public event Action<int, Options.ShipConfig, SafeInt, SafeInt> CostAndScoresInEditModeChanged;
        public event Action<int, SafeInt> ScoresInGameModeChanged;
        
        private Planet _planet;
        private CameraLookRotator _cameraLookRotator;
        private AbilityController _abilityController;

        private SafeInt _editModeScoresInitValue;
        private SafeInt _gameModeScoresInitValue;
        private SafeInt _costIncreaseStep;
        private List<SafeInt> _editModeCurrentScores;
        private List<SafeInt> _gameModeCurrentScores;
        private Dictionary<Options.ShipConfig, SafeInt> _vehicleInitCost;
        private List<Dictionary<Options.ShipConfig, SafeInt>> _editModeVehicleCurrentCosts;

        public void Init(Planet planet, CameraLookRotator cameraLookRotator, AbilityController abilityController)
        {
            _planet = planet;
            _cameraLookRotator = cameraLookRotator;
            _abilityController = abilityController;
            
            Setup();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Setup()
        {
            SetupScores();
            SetupCosts();
        }

        private void SetupScores()
        {
            _editModeScoresInitValue = new SafeInt(12); //40
            _gameModeScoresInitValue = new SafeInt(15); //7

            _editModeCurrentScores = new List<SafeInt>();
            _gameModeCurrentScores = new List<SafeInt>();

            _planet.Boards.ToList().ForEach(b =>
            {
                _editModeCurrentScores.Add(_editModeScoresInitValue);
                _gameModeCurrentScores.Add(_gameModeScoresInitValue);
            });
        }

        private void SetupCosts()
        {
            _costIncreaseStep = new SafeInt(1);

            _vehicleInitCost = new Dictionary<Options.ShipConfig, SafeInt>
            {
                {Options.ShipConfig.TwoHex, new SafeInt(2)},
                {Options.ShipConfig.ThreeHexLinear, new SafeInt(3)},
                {Options.ShipConfig.ThreeHexTriangle, new SafeInt(3)},
                {Options.ShipConfig.FourHexDiamond, new SafeInt(4)},
                {Options.ShipConfig.FiveHexXType, new SafeInt(5)},
                {Options.ShipConfig.FiveHexWType, new SafeInt(5)},
                {Options.ShipConfig.SixHexRing, new SafeInt(6)}
            };

            _editModeVehicleCurrentCosts = new List<Dictionary<Options.ShipConfig, SafeInt>>();
            
            _planet.Boards.ToList().ForEach(b => 
                _editModeVehicleCurrentCosts.Add(new Dictionary<Options.ShipConfig, SafeInt>(_vehicleInitCost)));
        }

        private void Subscribe()
        {
            _cameraLookRotator.TurnGived += OnTurnGived;
            _abilityController.GameModeScoresUpdateReady += OnGameModeScoresUpdateReady;
            _planet.BoardsControllers[0].EditModeCostUpdateRequested += OnEditModeCostUpdateRequested;
            _planet.BoardsControllers[1].EditModeCostUpdateRequested += OnEditModeCostUpdateRequested;
        }

        private void Unsubscribe()
        {
            _cameraLookRotator.TurnGived -= OnTurnGived;
            _abilityController.GameModeScoresUpdateReady -= OnGameModeScoresUpdateReady;
            _planet.BoardsControllers[0].EditModeCostUpdateRequested -= OnEditModeCostUpdateRequested;
            _planet.BoardsControllers[1].EditModeCostUpdateRequested -= OnEditModeCostUpdateRequested;
        }

        public SafeInt GetEditModeScoresInitValue() => 
            new SafeInt((int)_editModeScoresInitValue);

        public SafeInt GetGameModeScoresInitValue() => 
            new SafeInt((int)_gameModeScoresInitValue);

        public SafeInt GetCostIncreaseStep() => 
            new SafeInt((int)_costIncreaseStep);

        public SafeInt GetVehicleInitCost(Options.ShipConfig sc) => 
            new SafeInt((int)_vehicleInitCost[sc]);

        public int GetScores() => 
            GetScoresListByMode()[GameManager.Instance.CurrentPlayerIndex];

        public Dictionary<Options.ShipConfig, SafeInt> GetVehicleCosts(int playerIndex) => 
            _editModeVehicleCurrentCosts[playerIndex];

        public Dictionary<Options.ShipConfig, SafeInt> GetVehicleCosts() =>
            GetVehicleCosts(GameManager.Instance.CurrentPlayerIndex);

        private void OnEditModeCostUpdateRequested(int playerIndex, Options.ShipConfig sc, Options.EditModeUnitOperation editModeUnitOperation)
        {
            UpdateCostAndScoresInEditMode(playerIndex, sc, editModeUnitOperation);
        }

        private void OnTurnGived()
        {
            var playerIndex = GameManager.Instance.CurrentPlayerIndex;
            
            if(GameManager.Instance.GameStage == Options.GameStage.EditMode)
            {
                var costsData = _editModeVehicleCurrentCosts;
                
                costsData.ForEach(costData =>
                {
                    foreach (var data in costData) 
                        UpdateCostAndScoresInEditMode(playerIndex, data.Key, Options.EditModeUnitOperation.None);
                });
            }
            else if(GameManager.Instance.GameStage == Options.GameStage.GameMode)
                ResetScoresInGameMode(playerIndex);
        }

        private void UpdateCostAndScoresInEditMode(int playerIndex, Options.ShipConfig sc, Options.EditModeUnitOperation editModeUnitOperation)
        {
            SafeInt delta = 0;
        
            switch (editModeUnitOperation)
            {
                case Options.EditModeUnitOperation.Remove:
                    delta = new SafeInt(_editModeVehicleCurrentCosts[playerIndex][sc] * (-1));
                    _editModeVehicleCurrentCosts[playerIndex][sc] += _costIncreaseStep;
                    break;

                case Options.EditModeUnitOperation.Add:
                    _editModeVehicleCurrentCosts[playerIndex][sc] -= _costIncreaseStep;
                    delta = new SafeInt(_editModeVehicleCurrentCosts[playerIndex][sc]);
                    break;

                case Options.EditModeUnitOperation.None:
                    delta = new SafeInt(0);
                    break;
            }

            _editModeCurrentScores[playerIndex] += delta;
            
            var initCost = _vehicleInitCost[sc];
            if(_editModeVehicleCurrentCosts[playerIndex][sc] < initCost) 
                _editModeVehicleCurrentCosts[playerIndex][sc] = new SafeInt(initCost);

            CostAndScoresInEditModeChanged?.Invoke(playerIndex, sc, _editModeVehicleCurrentCosts[playerIndex][sc], _editModeCurrentScores[playerIndex]);
        }
        
        private void ResetScoresInGameMode(int playerIndex)
        {
            _gameModeCurrentScores[playerIndex] = _gameModeScoresInitValue;
            
            ScoresInGameModeChanged?.Invoke(playerIndex,  _gameModeCurrentScores[playerIndex]);
        }
        
        private void OnGameModeScoresUpdateReady(SafeInt delta)
        {
            UpdateScoresInGameMode(delta);
        }

        private void UpdateScoresInGameMode(SafeInt delta)
        {
            var playerIndex = GameManager.Instance.CurrentPlayerIndex;
            var scores = GetScoresListByMode();

            var enoughScoresToCreateVehicle = scores[playerIndex] + delta >= 0;
            
            if (enoughScoresToCreateVehicle)
            {
                scores[playerIndex] = (SafeInt) ((int) scores[playerIndex] + (int) delta);

                if (GameManager.Instance.GameStage == Options.GameStage.EditMode)
                    ScoresInGameModeChanged?.Invoke(playerIndex, _editModeCurrentScores[playerIndex]);
                else if (GameManager.Instance.GameStage == Options.GameStage.GameMode)
                    ScoresInGameModeChanged?.Invoke(playerIndex, _gameModeCurrentScores[playerIndex]);
            }
        }
        
        private List<SafeInt> GetScoresListByMode()
        {
            List<SafeInt> scores = null;

            if (GameManager.Instance.GameStage == Options.GameStage.EditMode)
                scores = _editModeCurrentScores;
            else if (GameManager.Instance.GameStage == Options.GameStage.GameMode)
                scores = _gameModeCurrentScores;
            
            if (scores == null)
                throw new NullReferenceException($"{GameManager.Instance.GameStage} : Score list is null");
            
            return scores;
        }
    }
}