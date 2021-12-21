using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Core.CoreDemo
{
	public class Hexagon : MonoBehaviour
	{
		private Dictionary<Options.ShipConfig, bool> _vehicleTypeValidationAreaData = new Dictionary<Options.ShipConfig, bool>
		{
			{ Options.ShipConfig.TwoHex, true },
			{ Options.ShipConfig.ThreeHexLinear, true },
			//{ Options.ShipConfig.ThreeHexTriangle, true },
			{ Options.ShipConfig.FourHexDiamond, true },
			{ Options.ShipConfig.FiveHexXType, true },
			//{ Options.ShipConfig.FiveHexWType, true },
			{ Options.ShipConfig.SixHexRing, true }
		};

		private Renderer _renderer;
		private Transform _transform;
		private Transform _colliderTransform;
		private GameObject _gameObject;
		private BoardController _boardController;

		private readonly Color _defaultColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
		private readonly Color _greenColor = new Color(0.1f, 0.85f, 0.1f, 0.75f);
		private readonly Color _redColor = new Color(0.85f, 0.1f, 0.1f, 0.75f);
		private readonly Color _yellowColor = new Color(0.85f, 0.85f, 0.0f, 0.75f);

		private Color _targetColor;
		public HPosition HexagonPosition { get; set; }
		public GameObject LinkedFogHex { get; private set; }
		public HexMissMarker LinkedMissMarker { get; private set; }
		public HexHitMarker LinkedHexHitMarker { get; private set; }
		public Board ParentBoard { get; private set; }

		private GameObject _linkedVehicle;
		private List<GameObject> _nearLinkedVehicles;
		private GameObject[] _nearHexes;
		private GameObject[] _nearHex2;
		private GameObject[] _nearHex3;

		private List<Hexagon> _freeHexagonsRef;

		private Options.State _currentState;
		private Options.OverlayState _currentOverlayState;
		private Options.ContainState _currentContainState;

		private bool _vehicleStatusVisible;
		private readonly bool _containsAsFree = false;

		// Use this for initialization
		private void Awake()
		{		
			HexagonPosition = new HPosition();
			_renderer = GetComponent<Renderer>();
			_transform = transform;
			_colliderTransform = _transform.GetChild(0);
			_gameObject = gameObject;

			_colliderTransform.localScale = Vector3.one / Options.hexCorrectedRelativeSize;
		}

		private void Start()
		{
			_nearHexes = new GameObject[6];
			_nearHex2 = new GameObject[12];
			_nearHex3 = new GameObject[18];

			ChangeStateTo(Options.State.Normal);
			ChangeOverlayStateTo(Options.OverlayState.Default);
			ChangeContainStateTo(Options.ContainState.Free);

			_linkedVehicle = null;
			_nearLinkedVehicles = new List<GameObject>();
		}

		public void ChangeStateTo (Options.State s)
		{
			_currentState = s;

			//template for handle an hex view
			switch (_currentState)
			{
				case Options.State.Normal:
					break;

				case Options.State.Attacked:
					break;

				case Options.State.Miss:
					break;

				case Options.State.Damaged:
					break;

				case Options.State.Destroyed:
					break;
			}

		}

		public void ChangeOverlayStateTo (Options.OverlayState os)
		{
			_currentOverlayState = os;				
        
			switch (_currentOverlayState)
			{
				case Options.OverlayState.Default:
					_targetColor = new Color(0.3f, 0.4f - (HexagonPosition.GetSRO()[0] % 3) * 0.075f, 0.3f, 0.5f);
					break;

				case Options.OverlayState.RedHover:
					_targetColor = _redColor;
					break;

				case Options.OverlayState.GreenHover:
					_targetColor = _greenColor;
					break;
				
				case Options.OverlayState.YellowTest:
					_targetColor = _yellowColor;
					break;

				default:
					_targetColor = _defaultColor;
					break;
			}

			_renderer.material.DOColor(_targetColor, Options.buttonTransitionDuration);
		}

		public void ChangeContainStateTo(Options.ContainState cs) => _currentContainState = cs;
		
		public Options.State GetState() => _currentState;
		public Options.OverlayState GetOverlayState () => _currentOverlayState;
		public Options.ContainState GetContainState() => _currentContainState;
		public GameObject GetLinkedVehicle() => _linkedVehicle;
		public void SetLinkedVehicle(GameObject lv) => _linkedVehicle = lv;

		public void AddNearLinkedVehicle(GameObject nearLinkedVehicle)
		{
			if (nearLinkedVehicle == null) 
				return;
			
			if (nearLinkedVehicle.GetComponent<Unit>() == null || _nearLinkedVehicles.Contains(nearLinkedVehicle)) 
				return;

			_nearLinkedVehicles.Add(nearLinkedVehicle);
		}
		
		public void RemoveNearLinkedVehicle(GameObject nearLinkedVehicle)
		{
			if (nearLinkedVehicle == null) 
				return;
			
			if (nearLinkedVehicle.GetComponent<Unit>() == null || _nearLinkedVehicles.Contains(nearLinkedVehicle) == false) 
				return;
			
			_nearLinkedVehicles.Remove(nearLinkedVehicle);
		}
		
		public bool ContainsInNearLinkedVehicle(GameObject nearLinkedVehicle)
		{
			if (nearLinkedVehicle == null) 
				return false;
			
			if (nearLinkedVehicle.GetComponent<Unit>() == null) 
				return false;

			return _nearLinkedVehicles.Contains(nearLinkedVehicle);
		}
		
		public int CountNearLinkedVehicle()
		{
			return _nearLinkedVehicles.Count;
		}
		
		public void SetVehicleStatusVisible(bool visible)
		{
			_vehicleStatusVisible = visible;
		}
		
		public bool GetVehicleStatusVisible()
		{
			return _vehicleStatusVisible;
		}

		public GameObject[] GetNearHexes()
		{
			return _nearHexes;
		}
		public void SetNearHex(GameObject[] nearHexesSource)
		{
			for(int i=0; i<_nearHexes.Length; i++) 
				_nearHexes[i] = nearHexesSource[i];
		}
	
		public bool TryInitialize (int s, int r, int o)
		{
			return (r == 0) || HexagonPosition.SROInitialize (s, r, o);
		}
		public void SetPosition ()
		{
			_transform.localRotation = Quaternion.identity;
			
			var localPosition = _transform.localPosition;
			_transform.RotateAround (localPosition, _transform.forward, Options.hexRotationOffset);

			var tempPos = HexagonPosition.GetCoordsOnBoard ();
			localPosition = tempPos / Options.boardSizeScaled;
			localPosition += new Vector3 (0, 0, Options.hexFromBoardOffsetZ);
			_transform.localPosition = localPosition;
		}
		public void SetScale ()
		{
			var hexSize = Options.hexCorrectedSize;
			_transform.localScale = hexSize * Vector3.one;
		}
		public void SetOrigin (Vector3 v, Quaternion q)
		{
		}

		public void SetParentInfo(Transform parentTransform)
		{
			_transform.parent = parentTransform;

			var boardGO = parentTransform.parent.gameObject;
			
			_boardController = boardGO.GetComponent<BoardController>();
			ParentBoard = boardGO.GetComponent<Board>();

			_freeHexagonsRef = ParentBoard.Filler.FreeHexagons;
		}

		public bool IsVirtualCenter()
		{
			return ParentBoard.virtualCenter == _gameObject;
		}

		public void AddFoggedHex(GameObject prefab, Transform container)
		{
			if (LinkedFogHex) 
				return;

			var instantiatedPrefab = Instantiate(prefab);
			var instantiatedTransform = instantiatedPrefab.transform;
			instantiatedTransform.parent = container;
			instantiatedTransform.localPosition = _transform.localPosition + new Vector3(0, 0, Options.FogHexOffsetZ);
			instantiatedTransform.localRotation = _transform.localRotation;

			var hexSize = Options.hexCorrectedSize * .01f;
			instantiatedTransform.localScale = new Vector3(1, 1, -.33f) * hexSize;
			LinkedFogHex = instantiatedPrefab;
		}

		public void RemoveFoggedHex()
		{
			Destroy(LinkedFogHex);
		}

		public void AddHexMissMarker(HexMissMarker prefab, Transform container)
		{
			if (LinkedMissMarker) 
				return;
		
			var instantiatedPrefab = Instantiate(prefab);
			var instantiatedTransform = instantiatedPrefab.transform;
		
			instantiatedTransform.parent = container;
			instantiatedTransform.localPosition = _transform.localPosition;
			instantiatedTransform.localRotation = _transform.localRotation;

			var hexSize = Options.hexCorrectedSize * .01f;
			instantiatedTransform.localScale = new Vector3(1, 1, 1f) * hexSize;

			LinkedMissMarker = instantiatedPrefab;
		}

		public void AddHexHitMarker(HexHitMarker prefab, Transform container)
		{
			if (LinkedHexHitMarker) 
				return;
		
			var instantiatedPrefab = Instantiate(prefab);

			var instantiatedTransform = instantiatedPrefab.transform;
			instantiatedTransform.parent = container;
			instantiatedTransform.localPosition = _transform.localPosition; 
			instantiatedTransform.localRotation = Quaternion.LookRotation(instantiatedTransform.up);

			var hexSize = Options.hexCorrectedSize * .01f;
			instantiatedTransform.localScale = new Vector3(1, 1, 1f) * hexSize;

			LinkedHexHitMarker = instantiatedPrefab;
		}

		public void DictAreaValidElementSet(Options.ShipConfig key, bool value)
		{
			if (_vehicleTypeValidationAreaData.ContainsKey(key)) 
				_vehicleTypeValidationAreaData[key] = value;
		}
		public bool ValidateAreaOfVehicleType(Options.ShipConfig key)
		{
			if (_vehicleTypeValidationAreaData.ContainsKey(key)) 
				return _vehicleTypeValidationAreaData[key];
			
			return false;
		}

		public void UpdateAreaValidData(bool checkingNeed = true)
		{
			Dictionary<Options.ShipConfig, bool> tempDict = new Dictionary<Options.ShipConfig, bool>();
		
			if (checkingNeed == false)
			{			
				foreach (var pair in _vehicleTypeValidationAreaData)
				{
					tempDict.Add(pair.Key, false);				
				}

				_vehicleTypeValidationAreaData = tempDict;
			}
			else
			{
				bool checkFree;
				bool checkOutBoard;
				bool validExpression;
				bool validExpressionCheck = false;

				foreach (var pair in _vehicleTypeValidationAreaData) 
				{
					var conformedHexagons = _boardController.ConformedHexagonsCalculateByType(_gameObject, pair.Key);
					var conformedHexagonsGO = BoardController.ConformedHexagonExtractGameobjects(conformedHexagons);
					
					checkFree = _boardController.CheckFree(conformedHexagonsGO);
					checkOutBoard = _boardController.CheckOutBoard(conformedHexagonsGO);

					validExpression = (checkFree == true) && (checkOutBoard == false) && (ParentBoard.Filler.AllowedConfigsContains(pair.Key));
					tempDict.Add(pair.Key, validExpression);

					validExpressionCheck = (validExpression) ? true : validExpressionCheck;
				}

				_vehicleTypeValidationAreaData = tempDict;			

				if (validExpressionCheck) 
				{
					if (_containsAsFree == false) _freeHexagonsRef.Add(this);
				}			
			}
		}

		public void HandleTurnGive(int turn) => 
			UpdateParticles(turn);

		private void UpdateParticles(int turn)
		{
			if(LinkedHexHitMarker != null)
				LinkedHexHitMarker.UpdateSmokeSize(turn);
		}
	}
}
