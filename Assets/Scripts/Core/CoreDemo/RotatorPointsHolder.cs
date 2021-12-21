using System.Linq;
using UnityEngine;

namespace Core.CoreDemo
{
    public class RotatorPointsHolder : MonoBehaviour
    {
        [SerializeField] 
        private BasePoint[] _rotatorBasePoints;
        
        private Transform[] _rotatorPoints;
        private bool _rotatorPointsInitialized = false;

        public Transform[] Points => _rotatorPoints;

        public void Init()
        {
            RotatorPointsInitialize();
        }

        private void RotatorPointsInitialize()
        {
            if (_rotatorPointsInitialized) 
                return;
		
            if (_rotatorBasePoints.Length < 3)
                throw new UnityException("Check CameraLookRotator component on object LookPoint. " +
                                         "Minimal value of RotatorPoints array is N (N = playersNumber + 1). Default value for N is 3.");

            foreach (var point in _rotatorBasePoints) 
                point.Init();

            _rotatorPoints = _rotatorBasePoints.ToList().Select(point => point.transform).ToArray();
            _rotatorPointsInitialized = true;
        }

        public Transform GetBasePoint(int index)
        {
	        if ((index < 0) || (index >= _rotatorPoints.Length)) return null;

	        return _rotatorPoints[index];
        }
    }
}