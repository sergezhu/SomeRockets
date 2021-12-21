using System;
using DG.Tweening;
using UnityEngine;

namespace Core.CoreDemo
{
	public class CameraLookRotator : MonoBehaviour
	{
		public event Action AnimationStarted;
		public event Action AnimationCompleted;
		public event Action PlayerSwapStarted;
		public event Action PlayerSwapCompleted;
		public event Action FastViewStarted;
		public event Action FastViewCompleted;
		public event Action TurnGived;
		public event Action EnableHexagonsSelectableInFastView;
		public event Action DisableHexagonsSelectableInFastView;

		[Space]
		[SerializeField] 
		private GameObject _cameraGameObject;
		[SerializeField] 
		private GameObject _lookPointContainer;
		[SerializeField] 
		private Renderer _waterRenderer;

		private Renderer waterRenderer;

		private GameObject _lookPoint;
		private Board[] _playerBoards;
		private Transform _lookPointContainerTransform;
		private Transform _cameraTransform;
		private Camera _camera;
		private UIFader _uiFader;

		public int LookIndex { get; private set; }
		public int FastLookIndex { get; private set; }
		public bool Moving { get; private set; }
		public bool? IsFastViewMode { get; private set; } = null;
	

		private WindowsController _windowsController;
		private Planet _planet;

		private Board _targetBoard = null;
		private Board _currentBoard = null;


		private Vector3 _lookPointDirectionStart;
		private Vector3 _lookPointDirectionEnd;
		private Vector3 _cameraDistanceToLookPointStart;
		private Vector3 _cameraDistanceToLookPointEnd;
		private bool _startedFlag;
    
		private bool _boardsInitialized;

		private Vector3 _farCamPosition;
		private Vector3 _nearCamPosition;
		private Vector3[] _directionToBasePoint;

		private Quaternion _deltaQ;
		private Transform _lookPointTransform;
		private Quaternion _lookPointRotation;
		private Quaternion _lookPointRotationTilted;

		private MaterialPropertyBlock _block;
		private RotatorPointsHolder _rotatorPointsHolder;
		private Transform[] _rotatorPoints;
		private bool _isSubscribe;

		public void Init(Planet planet, RotatorPointsHolder rotatorPointsHolder, WindowsController windowsController)
		{
			_planet = planet;
			_rotatorPointsHolder = rotatorPointsHolder;
			_windowsController = windowsController;

			_rotatorPoints = _rotatorPointsHolder.Points;
		
			_boardsInitialized = false;
			_block = new MaterialPropertyBlock();

			LookIndex = 1;
			FastLookIndex = LookIndex;

			//RotatorPoints = _rotatorBasePoints;
			waterRenderer = _waterRenderer;

			TrySubscribe();
		}
	
		private void TrySubscribe()
		{
			if (_isSubscribe)
				return;

			_isSubscribe = true;
		
			_planet.BoardsReady += OnBoardsReady;
			_windowsController.EditModeDoneConfirmWindow.NextLookIndexRequirement += OnNextLookIndexRequirement;
			_windowsController.GameModeDoneConfirmWindow.NextLookIndexRequirement += OnNextLookIndexRequirement;
			_windowsController.TurnConfirmWindow.ChangeLookIndexRequirement += OnChangeLookIndexRequirement;
		}
		private void UnsubscribeAll()
		{
			_isSubscribe = false;
		
			_planet.BoardsReady -= OnBoardsReady;
			_windowsController.EditModeDoneConfirmWindow.NextLookIndexRequirement -= OnNextLookIndexRequirement;
			_windowsController.GameModeDoneConfirmWindow.NextLookIndexRequirement -= OnNextLookIndexRequirement;
			_windowsController.TurnConfirmWindow.ChangeLookIndexRequirement -= OnChangeLookIndexRequirement;
		}

		private void OnDisable()
		{
			UnsubscribeAll();
		}

		private void Update()
		{
			if (_boardsInitialized == false) 
				return;

			if (Moving)
			{
				float progress = (_cameraTransform.localPosition.z - _nearCamPosition.z) / (_farCamPosition.z - _nearCamPosition.z);
				ShaderParamsCalculate(progress);
			}
		}

		private void MainInitialize()
		{
			_startedFlag = true;
			Moving = false;	

			_lookPoint = gameObject;
			_lookPointTransform = _lookPoint.transform;

			_lookPointContainerTransform = _lookPointContainer.transform;
			_cameraTransform = _cameraGameObject.transform;

			_playerBoards = _planet.Boards;

			_camera = _cameraGameObject.GetComponent<Camera>();
			if (_camera == null) throw new NullReferenceException("GameObject 'PlayerCamera' should to have component Camera. ");

			_camera.fieldOfView = Options.cameraFOV;

			_uiFader = _camera.GetComponent<UIFader>();

			_farCamPosition = Options.farDistanceToBoard; 
			_nearCamPosition = Options.nearDistanceToBoard;
			
			_lookPointTransform.localPosition = new Vector3(0, 0, _rotatorPoints[LookIndex].position.magnitude);
			_cameraGameObject.transform.localPosition = _farCamPosition;

			_directionToBasePoint = new Vector3[_rotatorPoints.Length];

			for (int i = _directionToBasePoint.Length - 1; i >= 0; i--) 
			{
				_lookPointContainerTransform.LookAt(_rotatorPoints[i].position, Vector3.up);
				_directionToBasePoint[i] = _lookPointContainerTransform.forward;
			}

			var localRotation = _lookPointTransform.localRotation;
			_lookPointRotation = localRotation;
			_lookPointRotationTilted = Quaternion.Euler(Options.cameraTiltXToBoard, 0, 0) * localRotation;
		}

		private void OnBoardsReady()
		{
			if (_boardsInitialized) 
				return;
		
			_boardsInitialized = true;
		
			MainInitialize();
			MoveToNextLook();
		}

		private void OnNextLookIndexRequirement()
		{
			MoveToNextLook();
		}

		private void MoveToNextLook()
		{
			float duration;

			if (FastLookIndex != LookIndex)
				throw new UnityException("You should to back an camera to LookIndex position");

			IsFastViewMode = null;

			if (_startedFlag)
			{
				duration = 1f;
			}
			else
			{
				duration = 4f;
				LookIndex = 1 + (LookIndex % Options.players.Length);
				FastLookIndex = LookIndex;
			}

			if (Moving == false)
			{
				Moving = true;

				AnimationStarted?.Invoke();
				Debug.Log($"CameraLookRotator - animationStarted");
				PlayerSwapStarted?.Invoke();
				Debug.Log($"CameraLookRotator - playerSwapStarted");

				_lookPointTransform.DOLocalRotateQuaternion(_lookPointRotation, duration).SetEase(Ease.OutCubic);

				_lookPointContainerTransform.DORotateQuaternion(Quaternion.LookRotation(_directionToBasePoint[0]), duration).SetEase(Ease.InOutCubic)
					.OnComplete(() =>
					{
						_windowsController.TurnConfirmWindowShow();

						if (_startedFlag == false)
						{
							switch (GameManager.Instance.GameStage)
							{
								case Options.GameStage.EditMode:
									GameManager.Instance.TryActivateGameMode();
									break;

								case Options.GameStage.GameMode:
									GameManager.Instance.TryStartNextTurn();
									break;
							}
						}

						SwitchGravityVector(LookIndex);

						TurnGived?.Invoke();
					
						GameManager.Instance.ResetUsedAbilitiesCount();
						GameManager.Instance.HandleTurnGive();
					});
			}
		}

		private void OnChangeLookIndexRequirement()
		{					
			_targetBoard = _playerBoards[LookIndex - 1];

			_lookPointDirectionStart = _rotatorPoints[0].position;
			_cameraDistanceToLookPointStart = new Vector3(0, 0, _lookPointDirectionStart.magnitude);
			_lookPointDirectionEnd = _rotatorPoints[LookIndex].position;
			_cameraDistanceToLookPointEnd = new Vector3(0, 0, _lookPointDirectionEnd.magnitude);

			_deltaQ = Quaternion.AngleAxis(-30, _targetBoard.transform.forward) * Quaternion.LookRotation(_directionToBasePoint[LookIndex], _targetBoard.transform.up) * Quaternion.Inverse(_lookPointContainerTransform.rotation);
			
			if (_startedFlag) 
				_startedFlag = false;

			Debug.Log("OnChangeLookIndex - BEFORE animations");
			_cameraGameObject.transform.DOLocalMove(_farCamPosition, 2.4f).SetEase(Ease.OutCubic);
			_lookPointTransform.DOLocalRotateQuaternion(_lookPointRotationTilted, 2.4f).SetDelay(3.3f).SetEase(Ease.OutCubic);
			_lookPointTransform.DOLocalMove(_cameraDistanceToLookPointEnd, 4.5f).SetEase(Ease.OutCubic);
			_cameraGameObject.transform.DOLocalMove(_nearCamPosition, 2.5f).SetDelay(2.5f).SetEase(Ease.OutCubic);
			_lookPointContainerTransform.DORotateQuaternion(_deltaQ * _lookPointContainerTransform.rotation, 5.5f).SetEase(Ease.InOutCubic)
				.OnComplete(() => {
					Moving = false;
					IsFastViewMode = false;
					Debug.Log($"CameraLookRotator - OnChangeLookIndex - OnComplete");
					PlayerSwapCompleted?.Invoke();
					Debug.Log($"CameraLookRotator - playerSwapCompleted");
					AnimationCompleted?.Invoke();
					Debug.Log($"CameraLookRotator - animationCompleted");

					Debug.Log("Animation end");
				});
		}

		public void FastEnemyBoardView()
		{		
			int targetLookIndex = 1 + (LookIndex % Options.players.Length);

			if (targetLookIndex != FastLookIndex)
			{
				FastBoardView(targetLookIndex);
				FastViewStarted?.Invoke();
				Debug.Log("CameraLookRotator - Fast View Started");
			}			
		}
		public void FastPlayerBoardView()
		{
			int targetLookIndex = LookIndex;

			if (targetLookIndex != FastLookIndex)
			{
				FastBoardView(targetLookIndex);
			}					
		}
		private void FastBoardView(int targetLookIndex)
		{
			float fadeDuration = 1f;
			float glideDuration = 1.9f;

			Debug.Log("targetLI: " + targetLookIndex + "  LI: " + LookIndex + "  fastLI: " + FastLookIndex);
		
			FastLookIndex = targetLookIndex;

			if (Moving == false)
			{
				Moving = true;

				AnimationStarted?.Invoke();
				Debug.Log("CameraLookRotator - Animation Started");

				var tempLookPointDirectionEnd = _rotatorPoints[targetLookIndex].position;
				var tempCameraDistanceToLookPointEnd = new Vector3(0, 0, tempLookPointDirectionEnd.magnitude);
				var tempTargetBoard = _playerBoards[targetLookIndex-1];
				
				_uiFader.FadeIn(fadeDuration, glideDuration - fadeDuration);

				_cameraGameObject.transform.DOLocalMove(_nearCamPosition + 0.33f * (_farCamPosition - _nearCamPosition), glideDuration).SetEase(Ease.InOutCubic)
					.OnComplete(() => 
					{
						IsFastViewMode = null;
					
						SwitchGravityVector(targetLookIndex);
					
						_uiFader.FadeOut(fadeDuration);

						_lookPointTransform.localPosition = tempCameraDistanceToLookPointEnd;
						_lookPointContainerTransform.rotation = Quaternion.AngleAxis(-30, tempTargetBoard.transform.forward) * Quaternion.LookRotation(_directionToBasePoint[targetLookIndex]);

						_cameraGameObject.transform.DOLocalMove(_nearCamPosition, glideDuration).SetEase(Ease.InOutCubic)
							.OnComplete(() =>
							{
								Moving = false;
								if (targetLookIndex != LookIndex)
								{
									EnableHexagonsSelectableInFastView?.Invoke();
									Debug.Log("CameraLookRotator - Enable Hexagons Selectable In FastView");
									IsFastViewMode = true;
								}
								else
								{
									IsFastViewMode = false;
									FastViewCompleted?.Invoke();
									Debug.Log("CameraLookRotator - Fast View Completed");
								}
						
								AnimationCompleted?.Invoke();
								Debug.Log("CameraLookRotator - Animation Completed");
							});
					});
			}
		}

		private void SwitchGravityVector(int lookIndex)
		{
			var pos = _playerBoards[lookIndex - 1].transform.position;
			Physics.gravity = -pos.normalized;
		}

		private void ShaderParamsCalculate(float lerpFactor)
		{
			var value = Mathf.Lerp(Options.depthMobileCorrectionLowEdge, Options.depthMobileCorrectionHighEdge, lerpFactor);

			_block.SetFloat("_DepthMobileCorrection", value);
			waterRenderer.SetPropertyBlock(_block);
		}

		public void ResetData()
		{
			LookIndex = 1;
			FastLookIndex = 1;
			IsFastViewMode = false;
		}
	}
}
