using System;
using System.Linq;
using UnityEngine;

namespace Core.CoreDemo
{
    public class Planet : MonoBehaviour
    {
        public event Action BoardsReady;

        [SerializeField]
        private CameraLookRotator _cameraLookRotator;
    
        [Space]
        [SerializeField] 
        private Board[] _boardsPool;
        [SerializeField] 
        private DefenceField[] _defenceFields;
    
        private GameObject[] _boardsGO;
        private BoardController[] _boardsControllers;
        private RotatorPointsHolder _rotatorPointsHolder;
        private BuffManager _buffManager;
        private int _boardsCount;

        public Board PlayerBoard { get; private set; }
        public Board EnemyBoard { get; private set; }
    
        public BoardController PlayerBoardController { get; private set; }
        public BoardController EnemyBoardController { get; private set; }

        public Board[] Boards => _boardsPool;
        public BoardController[] BoardsControllers => _boardsControllers;
        public DefenceField[] DefenceFields => _defenceFields;

        public void Init(RotatorPointsHolder rotatorPointsHolder, BuffManager buffManager)
        {
            _buffManager = buffManager;
            _rotatorPointsHolder = rotatorPointsHolder;

            BoardsInitialize();
        }

        public void SetupBoardFillers(ScoresHolder scoresHolder)
        {
            Boards.ToList().ForEach(b => b.SetupFiller(scoresHolder));
        }
    
        private void Start()
        {
            BoardsReady?.Invoke();
        }

        private void BoardsInitialize()
        {
            _boardsCount = Boards.Length;
            _boardsGO = Boards.ToList().Select(board => board.gameObject).ToArray();
            _boardsControllers = Boards.ToList().Select(board => board.GetComponent<BoardController>()).ToArray();
        
            SetupBoard(0, "BOARD_1", 1);
            SetupBoard(1, "BOARD_2", 2);
        
            PlayerBoard = _boardsPool[0];
            EnemyBoard = _boardsPool[1];
            PlayerBoardController = _boardsControllers[0];
            EnemyBoardController = _boardsControllers[1];

            _boardsGO[0].SetActive(true);
            _boardsGO[1].SetActive(true);
        }

        private void SetupBoard(int boardIndexInPool, string boardName, int basePointIndex)
        {
            var board = Boards[boardIndexInPool];
            board.Init(boardIndexInPool);
        
            var boardController = BoardsControllers[boardIndexInPool];
            var boardTransform = board.transform;
            boardController.Init(_buffManager);
            
            _boardsGO[boardIndexInPool].name = boardName;

            var boardSize = Options.boardSizeScaled;
            boardTransform.localScale = new Vector3(boardSize, boardSize, boardSize);
            boardTransform.position = _rotatorPointsHolder.GetBasePoint(basePointIndex).position;
            boardTransform.localPosition -= boardTransform.forward * Options.boardFromGroundOffset;
            boardTransform.LookAt(Vector3.zero);
            boardTransform.rotation *= Quaternion.AngleAxis(Options.boardRotationAngleOffset, transform.up);
        }

        public void SelectNextActivePlayer()
        {
            PlayerBoard = GetCurrentBoard();
            EnemyBoard = GetNextBoard();
            PlayerBoardController = GetCurrentBoardController();
            EnemyBoardController = GetNextBoardController();
        }

        public void InitCosts()
        {
            _boardsControllers.ToList().ForEach(bc => bc.InitCosts());
        }
    
        public GameObject GetCurrentBoardGO()
        {
            return _boardsGO[_cameraLookRotator.LookIndex - 1];
        }
        public BoardController GetCurrentBoardController()
        {
            return BoardsControllers[_cameraLookRotator.LookIndex - 1];
        }
        public Board GetCurrentBoard()
        {
            return Boards[_cameraLookRotator.LookIndex - 1];
        }
        public GameObject GetNextBoardGO()
        {
            return _boardsGO[(_cameraLookRotator.LookIndex + 1) % _boardsCount];
        }
        public BoardController GetNextBoardController()
        {
            return BoardsControllers[(_cameraLookRotator.LookIndex + 1) % _boardsCount];
        }
        public Board GetNextBoard()
        {
            return Boards[(_cameraLookRotator.LookIndex + 1) % _boardsCount];
        }
        public GameObject GetFastViewBoardGO()
        {
            return _boardsGO[_cameraLookRotator.FastLookIndex - 1];
        }
        public BoardController GetFastViewBoardController()
        {
            return BoardsControllers[_cameraLookRotator.FastLookIndex - 1];
        }
        public Board GetFastViewBoard()
        {
            return Boards[_cameraLookRotator.FastLookIndex - 1];
        }

        public bool CanEditBoard()
        {
            return PlayerBoard == GetCurrentBoard();
        }

        public bool CanAttackBoard()
        {
            return EnemyBoard == GetCurrentBoard();
        }

        public int GetIndex(Board board)
        {
            return Boards.ToList().FindIndex(b => b == board);
        }
    
        public int GetIndex(BoardController boardController)
        {
            return BoardsControllers.ToList().FindIndex(bc => bc == boardController);
        }
    }
}
