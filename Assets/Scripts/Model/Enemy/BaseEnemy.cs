using System;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


namespace Snake_box
{
    public abstract class BaseEnemy : IEnemy
    {

        #region PrivateData

        protected NavMeshAgent _navMeshAgent;
        protected GameObject prefab;
        protected Transform _transform;
        protected Transform _target;
        protected Vector3 _SpawnCenter;
        protected float _hp;
        protected float _spawnRadius;
        protected float _speed;
        protected float _damage;
        protected bool _isNeedNavMeshUpdate = false;

        #endregion

        
        #region Properties

        public EnemyType Type { get; protected set; }

        #endregion

        
        #region Methods

        public virtual void Spawn()
        {
            var enemy = GameObject.Instantiate(prefab, GetSpawnPoint(_SpawnCenter, _spawnRadius), Quaternion.identity);
            _navMeshAgent = enemy.GetComponent<NavMeshAgent>();
            _navMeshAgent.speed = _speed;
            _transform = enemy.transform;
            _isNeedNavMeshUpdate = true;
            Data.ActiveEnemy.Add(this);
        }

        public void HitCheck()
        {
            Collider[] colliders = new Collider[10];
            Physics.OverlapSphereNonAlloc(_transform.position, 3.1f, colliders);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    if (colliders[i].CompareTag(TagManager.GetTag(TagType.Target)))
                    {
                        Object.Destroy(colliders[i].gameObject);
                    }
            }
        }

        protected virtual void GetTarget()
        {
            _target = GameObject.FindWithTag(TagManager.GetTag(TagType.Target)).transform;
        }

        private Vector3 GetSpawnPoint(Vector3 center, float distance)
        {
            Vector3 randomPos = Random.insideUnitSphere * distance + center;
            NavMesh.SamplePosition(randomPos, out var hit, distance, NavMesh.GetAreaFromName("Spawn"));
            return hit.position;
        }

        public void GetDamage(float damage)
        {
            _hp -= damage;
            if (_hp <= 0)
            {
                Data.ActiveEnemy.Remove(this);
                Object.Destroy(_transform.gameObject);
            }
        }
        
        public virtual void OnUpdate()

        {
            if (_isNeedNavMeshUpdate)
            {
                if(_target!= null)
                    _navMeshAgent.SetDestination(_target.transform.position);
                _isNeedNavMeshUpdate = false;
            }

            HitCheck();
        }
        #endregion
    }
}
