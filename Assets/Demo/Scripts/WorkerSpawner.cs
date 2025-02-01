using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Kabir
{
    public class WorkerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _worker;
        [SerializeField] private int _workerAmount = 1;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField, MinMaxSlider(0.1f, 60f)] private Vector2 _spawnInterval;

        private Sequence _spawnSequence;
        private List<GameObject> _currentWorkers;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (_worker == null) return;
            AttemptSpawn();
        }

        private void OnDestroy()
        {
            KillSpawnSequence();
        }

        private void OnValidate()
        {
            _workerAmount = Mathf.Max(1, _workerAmount);
            _spawnRadius = Mathf.Max(1f, _spawnRadius);
        }

        private void OnDrawGizmos()
        {
            DebugExtension.DrawLocalCube(transform, Vector3.one, Color.magenta);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }

        private void KillSpawnSequence()
        {
            if (_spawnSequence == null) return;
            _spawnSequence.Kill();
            _spawnSequence = null;
        }

        private void AttemptSpawn()
        {
            KillSpawnSequence();
            _currentWorkers ??= new();
            if(_currentWorkers.Count >= _workerAmount)
            {
                return;
            }

            SpawnWorker();
            _spawnSequence = DOTween.Sequence().AppendInterval(Random.Range(_spawnInterval.x, _spawnInterval.y)).AppendCallback(AttemptSpawn);
        }

        private void SpawnWorker()
        {
            _currentWorkers ??= new();
            Vector2 flatOffset = Random.insideUnitCircle * _spawnRadius;
            Vector3 offset = new(flatOffset.x, 0f, flatOffset.y);
            Vector3 potentialPos = transform.position + offset;

            if(!NavMesh.SamplePosition(potentialPos, out NavMeshHit hit, offset.magnitude, NavMesh.AllAreas))
            {
                return;
            }

            GameObject g = Instantiate(_worker, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            _currentWorkers.Add(g);

        }
    }
}
