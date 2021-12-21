using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.CoreDemo
{
	public class Board : MonoBehaviour
	{
		[SerializeField] 
		private Hexagon hexPrefab;
		[SerializeField] 
		private GameObject defenceField;

		[Space]
		[Header("Hex State Markers")]
		[SerializeField] 
		private HexMissMarker _hexMissMarkerPrefab;
		[SerializeField] 
		private HexHitMarker _hexHitMarkerPrefab;
		[SerializeField] 
		private GameObject _hexFoggedPrefab;

		[FormerlySerializedAs("HexagonsContainer")]
		[Space]
		[Header("Containers")]
		[SerializeField]
		private Transform _hexagonsContainer;
		[SerializeField]
		private Transform _unitsContainer;
		[SerializeField]
		private Transform _destroyedUnitsContainer;
		[SerializeField]
		private Transform _switchableEnvironmentContainer;

		public Filler Filler;
		public List<GameObject> AttachedVehicles { get; private set; }
		public List<GameObject> DamagedVehicles { get; private set; }
		public List<GameObject> FoggedHexagons { get; private set; }
		public VehicleLibrary VehicleLibrary { get; private set; }

		public int Index => _index;

		[HideInInspector] 
		public GameObject virtualCenter;


		private int[,,] _hexagonIndexes = new int [6, Options.dimensions + 1, Options.dimensions];
		private GameObject[] _hexagonsGO;
		private GameObject[] nearObjects = new GameObject[6];
		private Transform _transform;
		private Hexagon[] _hexagons;

		private int _index;
		private int _currentHexagonIndex = -1;
		private bool _hexagonsCreatedFlag;

		public void Init (int index)
		{
			_index = index;
		
			_hexagonsGO = new GameObject[Helper.GetHexagonsCount(Options.dimensions)];
			_hexagons = new Hexagon[Helper.GetHexagonsCount(Options.dimensions)];
			AttachedVehicles = new List<GameObject>();
			DamagedVehicles = new List<GameObject>();
			FoggedHexagons = new List<GameObject>();
		
		
			_transform = transform;
			VehicleLibrary = GetComponent<VehicleLibrary>();
		
			_hexagonsCreatedFlag = false;

			DisableDefence();
			SwitchVisibilityTo(Options.Visibility.Normal);
		}

		public void SetupFiller(ScoresHolder scoresHolder)
		{
			Filler = new Filler(_index, scoresHolder);
		}
	
		private void Start ()
		{
			StartCoroutine ( HexagonsCreate() );
		}
	
		public Transform GetTransform()
		{
			return _transform;
		}

		public bool GetHexagonsCreatedFlag()
		{
			return _hexagonsCreatedFlag;

		}

		public void ValidConfigsCalculation()
		{
			Options.ContainState cs;

			Filler.AllowedConfigsRefreshData();

			foreach (var hex in _hexagons)
			{
				cs = hex.GetContainState();
				hex.UpdateAreaValidData(cs == Options.ContainState.Free);
			}
		}

		public void AddHexMissMarker(Hexagon targetHex)
		{
			targetHex.AddHexMissMarker(_hexMissMarkerPrefab, _switchableEnvironmentContainer);
		}

		public void AddHexHitMarker(Hexagon targetHex)
		{
			targetHex.AddHexHitMarker(_hexHitMarkerPrefab, _destroyedUnitsContainer);
			targetHex.LinkedHexHitMarker.Init(GameManager.Instance.Turn);
		}

		public void AttachToDestroyedUnitsContainer(Transform unitTransform)
		{
			unitTransform.parent = _destroyedUnitsContainer;
		}
	
		public void AttachToUnitsContainer(Transform unitTransform)
		{
			unitTransform.parent = _unitsContainer;
			unitTransform.up = -1 * _unitsContainer.forward;
		}
	
		public void EnableDefence()
		{
			defenceField.SetActive(true);
		}
		public void DisableDefence()
		{
			defenceField.SetActive(false);
		}
	
		public void HandleTurnGive(int turn)
		{
			foreach (var hex in _hexagons) 
				hex.HandleTurnGive(turn);
		}
	
		public void SwitchVisibilityTo(Options.Visibility visibility)
		{
			Debug.Log($"Board [{gameObject.name}]   SwitchVisibilityTo : {visibility}");
        
			if(visibility == Options.Visibility.Normal)
			{
				_unitsContainer.gameObject.SetActive(true);
				_switchableEnvironmentContainer.gameObject.SetActive(false);
			} 
			else if(visibility == Options.Visibility.Hidden)
			{
				_unitsContainer.gameObject.SetActive(false);
				_switchableEnvironmentContainer.gameObject.SetActive(true);
			}
		}
	
		private IEnumerator HexagonsCreate()
		{
			int sector, ring, offset;

			for (sector = 0; sector < 6; sector++)
			{
				for (ring = 1; ring <= Options.dimensions; ring++)
				{
					for (offset = 0; offset < ring; offset++)
					{
						OneHexCreate(sector, ring, offset);
						yield return null;
					}
				}
			}

			OneHexCreate(0, 0, 0);

			_hexagonsCreatedFlag = true;

			StartCoroutine(NearHexagonsCalculate());
		}

		private void OneHexCreate(int sector, int ring, int offset)
		{
			var hexagon = Instantiate(hexPrefab);
			var hexagonGO = hexagon.gameObject;

			if (hexagon.TryInitialize(sector, ring, offset) == false)
				return;

			hexagon.SetParentInfo(_hexagonsContainer);
			hexagon.SetOrigin(_transform.position, _transform.rotation);
			hexagon.SetPosition();
			hexagon.SetScale();

			if (ring == 0)
			{
				hexagonGO.name = "VirtualCenter";
				virtualCenter = hexagonGO;
			}
			else
			{
				hexagonGO.name = "HexagonObject_" + _currentHexagonIndex.ToString() + "_" + sector.ToString() + ring.ToString() + offset.ToString();
			
				_currentHexagonIndex++;

				_hexagonsGO[_currentHexagonIndex] = hexagonGO;
				_hexagons[_currentHexagonIndex] = hexagon;
				_hexagonIndexes[sector, ring, offset] = _currentHexagonIndex;
			}
			
			hexagon.AddFoggedHex(_hexFoggedPrefab, _switchableEnvironmentContainer);
			FoggedHexagons.Add(hexagon.LinkedFogHex);
		}

		private IEnumerator NearHexagonsCalculate()
		{
			int sector = 0, ring = 0, offset = 0;

			for (sector = 0; sector < 6; sector++)
			{
				for (ring = 1; ring <= Options.dimensions; ring++)
				{
					for (offset = 0; offset < ring; offset++)
					{
						NearCalculate(sector, ring, offset);
						yield return null;
					}
				}
			}

			yield return null;

			NearCalculate(0, 0, 0);
			yield return null;
		
			ValidConfigsCalculation();
		}

		private void NearCalculate(int sector, int ring, int offset)
		{
			Hexagon hexagonScript;

			if (ring == 0)
			{
				hexagonScript = virtualCenter.GetComponent<Hexagon>();
				for (int i = 0; i < 6; i++)
				{
					nearObjects[i] = _hexagonsGO[_hexagonIndexes[i, 1, 0]];
				}

				hexagonScript.SetNearHex(nearObjects);

				return;
			}

			GameObject tempObj, tempNearObj;
			hexagonScript = _hexagons[_hexagonIndexes[sector, ring, offset]];

			tempNearObj = null;

			//r+1, o
			nearObjects[0] = null;
			if ((ring + 1) <= Options.dimensions)
			{
				tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring + 1, offset]];
				nearObjects[0] = tempNearObj;

			}

			//r+1, o+1
			nearObjects[1] = null;
			if ((ring + 1) <= Options.dimensions)
			{

				if ((offset + 1) > (ring))
				{
					tempNearObj = _hexagonsGO[_hexagonIndexes[(sector + 1) % 6, ring + 1, 0]];
					nearObjects[1] = tempNearObj;

				}
				else
				{
					tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring + 1, offset + 1]];
					nearObjects[1] = tempNearObj;
				}
			}

			//r, o+1
			nearObjects[2] = null;
			if ((offset + 1) > (ring - 1))
			{
				tempNearObj = _hexagonsGO[_hexagonIndexes[(sector + 1) % 6, ring, 0]];
				nearObjects[2] = tempNearObj;

			}
			else
			{
				tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring, offset + 1]];
				nearObjects[2] = tempNearObj;
			}

			//r-1, o
			nearObjects[3] = null;
			if ((ring - 1) >= 1)
			{
				if ((offset) > (ring - 2))
				{
					tempNearObj = _hexagonsGO[_hexagonIndexes[(sector + 1) % 6, ring - 1, 0]];
					nearObjects[3] = tempNearObj;

				}
				else
				{
					tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring - 1, offset]];
					nearObjects[3] = tempNearObj;
				}
			}
			else
			{
				nearObjects[3] = virtualCenter;
			}

			//r, o-1
			nearObjects[4] = null;
			if ((offset - 1) < 0)
			{
				tempNearObj = _hexagonsGO[_hexagonIndexes[(sector + 5) % 6, ring, ring - 1]];
				nearObjects[4] = tempNearObj;

			}
			else
			{
				tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring - 1, offset - 1]]; // r  ->  r-1
				nearObjects[4] = tempNearObj;
			}

			//r+1, o-1
			nearObjects[5] = null;
			if ((ring) <= Options.dimensions)  // r+1   ->  r
			{
				if ((offset - 1) < 0)
				{
					if (ring < Options.dimensions)
					{
						tempNearObj = _hexagonsGO[_hexagonIndexes[(sector + 5) % 6, ring + 1, ring]];
						nearObjects[5] = tempNearObj;
					}
				}
				else
				{
					tempNearObj = _hexagonsGO[_hexagonIndexes[sector, ring, offset - 1]]; // r+1   ->  r
					nearObjects[5] = tempNearObj;
				}
			}

			Helper.ArrayRoll(nearObjects, sector);
			hexagonScript.SetNearHex(nearObjects);
		}
	}
}
