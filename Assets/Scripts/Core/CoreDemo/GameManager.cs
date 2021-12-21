using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.CoreDemo
{
    public class GameManager : Singleton<GameManager>
    {
        public event Action GameModeActivated;
        public event Action GameEnded;

        private Options.GameStage _gameStage = Options.GameStage.Menu; 
        private Options.GameType _gameType = Options.GameType.Local;

        private readonly bool[] _editIsDone = new bool[Options.players.Length];
        private readonly bool[] _turnIsDone = new bool[Options.players.Length];
        
        private SafeInt _turn;
        private SafeInt _usedAbilitiesCount;
        private int _currentPlayerIndex;
        private int _previousPlayerIndex;
        private Planet _planet;
        private CameraLookRotator _cameraLookRotator;
        
        public int CurrentPlayerIndex => _currentPlayerIndex;
        public int UsedAbilitiesCount => _usedAbilitiesCount;
        public int Turn => _turn;
        
        public Options.GameStage GameStage
        {
            get { return _gameStage; }
            set { _gameStage = value; }
        }

        public Options.GameType GameType => _gameType;

        private void Awake() => 
            Init();

        private void Init()
        {
            _turn = new SafeInt(0);
            _usedAbilitiesCount = new SafeInt(0);
        
            _planet = FindObjectOfType<Planet>();
            _cameraLookRotator = FindObjectOfType<CameraLookRotator>();
        }

        private void UpdateDoneStatus()
        {
            _currentPlayerIndex = _cameraLookRotator.LookIndex - 1;
            _previousPlayerIndex = (_currentPlayerIndex + Options.players.Length - 1) % Options.players.Length;

            switch (GameStage)
            {
                case Options.GameStage.EditMode:
                    _editIsDone[_previousPlayerIndex] = true;
                    break;

                case Options.GameStage.GameMode:
                    _turnIsDone[_previousPlayerIndex] = true;
                    break;
            }
        }

        public void HandleTurnGive()
        {
           _planet.SelectNextActivePlayer();

            if (GameStage == Options.GameStage.GameMode)
            {
                _planet.Boards[_currentPlayerIndex].SwitchVisibilityTo(Options.Visibility.Normal);
                _planet.Boards[_currentPlayerIndex].HandleTurnGive(Turn);
            
                _planet.Boards[_previousPlayerIndex].SwitchVisibilityTo(Options.Visibility.Hidden);
                _planet.Boards[_previousPlayerIndex].HandleTurnGive(Turn);
            }
        }

        public bool IsPlayerEditDone(int index)
        {
            return _editIsDone[index];
        }
        private bool IsAllPlayersDoneEdit()
        {
            var result = true;

            foreach (var t in _editIsDone) 
                result = result && t;

            //Debug.Log("IsAllPlayersEditDone: " + result);
            return result;
        }

        public bool IsPlayerTurnDone(int index)
        {
            return _turnIsDone[index];
        }
        private bool IsAllPlayersTurnDone()
        {
            var result = true;

            foreach (var value in _turnIsDone) 
                result = result && value;

            //Debug.Log("IsAllPlayersEditDone: " + result);
            return result;
        }

        public void TryActivateGameMode()
        {
            UpdateDoneStatus();

            if (IsAllPlayersDoneEdit())
            {
                ChangeStageTo(Options.GameStage.GameMode);
                Debug.Log("====== GAME MODE IS ACTIVATED ======");
            }
        }
    
        public void TryStartNextTurn()
        {
            UpdateDoneStatus();

            if (IsAllPlayersTurnDone()) 
                _turn = new SafeInt(_turn + 1);
        }

        public void ChangeStageTo(Options.GameStage targetStage)
        {
            if (_gameStage == targetStage) return;

            Debug.Log("gameStage: " + _gameStage + "   targetStage: " + targetStage);
            if(Options.GameStage.Menu == targetStage)
            {
                _gameStage = targetStage;
                EndGame();
            }
            else if(Options.GameStage.Menu == _gameStage && Options.GameStage.EditMode == targetStage)
            {
                _gameStage = targetStage;
                StartGame();
            }
            else if(Options.GameStage.EditMode == _gameStage && Options.GameStage.GameMode == targetStage)
            {
                _gameStage = targetStage;
                GoToPlayMode();
            }
            else throw new UnityException($"State changing from {_gameStage} to {targetStage} is denied");
        }

        public void ChangeTypeTo(Options.GameType targetType)
        {
            if (Options.GameStage.Menu == _gameStage) _gameType = targetType;
            else throw new UnityException("You can to change Game Type only through Menu");
        }

        public void IncreaseUsedAbilitiesCount()
        {
            _usedAbilitiesCount = new SafeInt(_usedAbilitiesCount + 1);
        }
        public void ResetUsedAbilitiesCount()
        {
            _usedAbilitiesCount = new SafeInt(0);
        }

        private void StartGame()
        {
            SceneManager.LoadScene(1);

            for (int i = 0; i < Options.players.Length; i++)
            {
                _editIsDone[i] = false;
            }
            _cameraLookRotator.ResetData();      
        }

        private void EndGame()
        {
            GameEnded?.Invoke();
            SceneManager.LoadScene(0);

            /*GameObject bo1 = BoardReference.Instance.playerBoard;
        GameObject bo2 = BoardReference.Instance.enemyBoard;

        BoardController bc1 = bo1.GetComponent<BoardController>();
        BoardController bc2 = bo2.GetComponent<BoardController>();

        counter = Options.players.Length;

        bc1.BoardClear();
        BoardController.boardClearCompleted += OnBoardClearCompleted;
        bc2.BoardClear();
        BoardController.boardClearCompleted += OnBoardClearCompleted;*/


            //Меню: Начать соло игру / Начать игру с оппонентом на 1 устройстве / Начать игру по сети со случайным игроком
            //      Настройки 
            //          Основные
            //              Уровень звука
            //              Радиус поля 
            //              Отключить кат-сцены
            //          Скины (приобретенные)
            //      Магазин (скины)
            //      Помощь
            //      об игре ( + контакты для донатов )


        }

        /*private static void OnBoardClearCompleted()
    {
        counter--;

        if (counter == 0)
        {
            BoardController.boardClearCompleted -= OnBoardClearCompleted;
            BoardController.boardClearCompleted -= OnBoardClearCompleted;
            SceneManager.LoadScene(0);
        }          
    }*/
        private void GoToPlayMode()
        {
            _turn = 1;
            GameModeActivated?.Invoke();
        }
    }
}
