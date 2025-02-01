using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using NaughtyAttributes;
using DG.Tweening;

namespace Kabir
{
    public class PickupSpawner : MonoBehaviour
    {
        [SerializeField] private List<SpawnPoint> _spawnPoints;
        [SerializeField] private GameObject _pickup;
        [SerializeField] private int _maxPickupAmount = 5;
        [SerializeField, MinMaxSlider(0.1f, 60f)] private Vector2 _pickupSpawnInterval;
        [SerializeField, ReadOnly] private List<GameObject> _currentPickups;

        private SpawnPoint[] _validSpawnPoints;
        private Sequence _spawnSequence;

        void Start()
        {
            SpawnPickup();
        }

        private void OnDestroy()
        {
            KillSpawnSequence();
        }

        private void OnValidate()
        {
            _maxPickupAmount = Mathf.Max(1, _maxPickupAmount);

            if (_spawnPoints != null)
            {
                foreach (SpawnPoint spawnPoint in _spawnPoints)
                {
                    spawnPoint?.OnValidate();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_spawnPoints != null)
            {
                foreach(var sp in _spawnPoints)
                {
                    if (sp == null) continue;
                    if(sp.Position == null) continue;

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(sp.Position.position, sp.Radius);
                    DebugExtension.DrawLocalCube(sp.Position, new(0.1f, 0.1f, 0.1f), Color.magenta);
                }
            }
        }

        public GameObject SelectRandomPickup()
        {
            if(_currentPickups == null) return null;
            _currentPickups.RemoveAll(c => c == null);
            if(_currentPickups.Count <= 0) return null;
            
            GameObject pickup = _currentPickups[Random.Range(0, _currentPickups.Count)];
            _currentPickups.RemoveAll(c=>c== pickup);   

            return pickup;
        }


        private void SpawnPickup()
        {
            if(_pickup == null)
            {
                KillSpawnSequence();
                return;
            }

            _validSpawnPoints ??= GetValidSpawnPoints();

            if(_validSpawnPoints.Length <= 0)
            {
                StartSpawnSequence();
                return;
            }

            _currentPickups ??= new();
            _currentPickups.RemoveAll(c => c == null);
            if(_currentPickups.Count >= _maxPickupAmount)
            {
                StartSpawnSequence();
                return;
            }

            List<SpawnPoint> availableSpawnPoint = new(_validSpawnPoints);
            Vector3 spawnPos = Vector3.zero;
            while(availableSpawnPoint.Count > 0)
            {
                int chosenIndex = Random.Range(0, availableSpawnPoint.Count);
                SpawnPoint spawnPoint = availableSpawnPoint[chosenIndex];

                if(spawnPoint.HasRandomPosition(out spawnPos)) break;

                availableSpawnPoint.RemoveAt(chosenIndex);
            }

            if(availableSpawnPoint.Count <= 0)
            {
                StartSpawnSequence();
                return;
            }

            GameObject g = Instantiate(_pickup, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            _currentPickups.Add(g);

            StartSpawnSequence();
        }

        private void StartSpawnSequence()
        {
            KillSpawnSequence();

            _spawnSequence = DOTween.Sequence().AppendInterval(Random.Range(_pickupSpawnInterval.x, _pickupSpawnInterval.y)).AppendCallback(SpawnPickup);
        }

        private void KillSpawnSequence()
        {
            if (_spawnSequence == null) return;
            _spawnSequence.Kill();
            _spawnSequence = null;
        }

        private SpawnPoint[] GetValidSpawnPoints()
        {
            List<SpawnPoint> result = new();
            if(_spawnPoints == null) return result.ToArray();

            foreach(var sp in _spawnPoints)
            {
                if (sp == null) continue;
                if(sp.Position == null) continue;   
                result.Add(sp);
            }

            return result.ToArray();
        }

        [System.Serializable]
        private class SpawnPoint
        {
            [field: SerializeField] public Transform Position { get; private set; }
            [field: SerializeField] public float Radius { get; private set; } = 1f;

            public void OnValidate()
            {
                Radius = Mathf.Max(Radius, 0.1f);
            }

            public bool HasRandomPosition(out Vector3 position)
            {
                position = Vector3.zero;
                if(Position == null) return false;

                Vector3 randomPos = (Random.insideUnitSphere * Radius) + Position.position;

                if(NavMesh.SamplePosition(randomPos, out NavMeshHit hit, Radius, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }

                return false;
            }
        }
    }
}
