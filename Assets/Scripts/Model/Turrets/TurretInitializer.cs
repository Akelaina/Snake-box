﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Assets.Scripts.Model.Turrets;
using UnityEngine;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


namespace Snake_box
{
    public sealed class TurretInitializer : TurretBaseAbs, IInitialization
    {
        #region Fields

        public GameObject TurretInstance;
        public string TurretSpritePath = "Prefabs/Turrets/Turret3D";
        public EnemyType PreferredArmorType = EnemyType.None;
        public TurretBehaviour TurretBehaviour;

        private List<IEnemy> _dummyEnemies = new List<IEnemy>();
        private Quaternion _haltTurretRotation;
        //todo use TimeRemaining
        private float _frameRateLock = 0;

        #endregion


        #region Properties

        public float Cooldown => Data.Instance.TurretData.PlainTurretCooldown;
        public float TurretRange => Data.Instance.TurretData.PlainTurretRange;

        #endregion


        #region ClassLifeCycle

        public TurretInitializer()
        {
            Initialization();
        }

        #endregion


        #region IInitialization

        public void Initialization()
        {
            //todo move that to turret builder
            TurretBehaviour = Resources.Load<TurretBehaviour>(TurretSpritePath);
            TurretInstance = Object.Instantiate(TurretBehaviour.gameObject, Vector3.zero, TurretBehaviour.transform.rotation);
            _haltTurretRotation = TurretInstance.transform.rotation;
            TurretBehaviour = TurretInstance.GetComponent<TurretBehaviour>();
        }


        #endregion


        #region TurretBaseAbs

        public override void SetParentTransform(Transform parentTransform)
        {
            TurretInstance.transform.parent = parentTransform;
            TurretInstance.transform.localPosition = Vector3.zero;
        }

        public override void Execute()
        {
            RecoilEnemies();
            LockTarget();
            ContinueShooting();
            HaltTurret();
        }

        #endregion


        #region Methods

        public void ContinueShooting()
        {
            if (Time.frameCount - _frameRateLock > Cooldown)
            {
                IEnemy nearestEnemy = NearestEnemy();

                if (nearestEnemy == null)
                    return;

                GetProjectile().Build(TurretBehaviour.FirePoint, nearestEnemy);

                _frameRateLock = Time.frameCount + Mathf.Round(Random.value * 10);
            }
        }

        private ProjectileBuilderAbs GetProjectile() => new CannonShellBuilder();

        private Quaternion RotateAroundAxis(Vector3 pointA, Vector3 pointB, Quaternion startRotation)
        {
            Vector3 direction3d = pointA - pointB;
            float angle = Mathf.Atan2(direction3d.z, direction3d.x) * Mathf.Rad2Deg;
            Quaternion rotateAround = Quaternion.AngleAxis(angle, Vector3.forward);

            Quaternion rotation = Quaternion.Euler(rotateAround.eulerAngles);
            Debug.Log(rotateAround.eulerAngles);

            return rotation;
        }

        public void LockTarget()
        {
            IEnemy nearestEnemy = NearestEnemy();

            if (nearestEnemy == null)
                return;

            Vector3 lookAngles = Quaternion.LookRotation(nearestEnemy.GetPosition() - TurretInstance.transform.position).eulerAngles;
            lookAngles.x = _haltTurretRotation.eulerAngles.x;
            lookAngles.z = _haltTurretRotation.eulerAngles.z;
            lookAngles.y = lookAngles.y + _haltTurretRotation.eulerAngles.y;

            TurretInstance.transform.rotation = Quaternion.Euler(lookAngles);
        }

        private void CollectKilledEnemies() => _dummyEnemies = _dummyEnemies.Where((element) => !element.AmIDestroyed()).ToList();

        private IEnemy NearestEnemy()
        {
            CollectKilledEnemies();

            if (_dummyEnemies.Count < 1)
                return null;

            IEnemy nearestEnemy = null;
            float closestDistance = TurretRange;
            EnemyType enemyArmorType = EnemyType.None;

            foreach (IEnemy enemy in _dummyEnemies)
            {
                float checkingDistance = Vector3.Distance(
                    enemy.GetPosition(),
                    TurretInstance.transform.position);

                if (checkingDistance > TurretRange)
                {
                    continue;
                }
                else if (enemyArmorType == PreferredArmorType 
                         && enemy.GetEnemyType() != PreferredArmorType 
                         && PreferredArmorType != EnemyType.None)
                {
                    continue;
                }
                
                if (checkingDistance < closestDistance || enemyArmorType != PreferredArmorType && enemy.GetEnemyType() == PreferredArmorType)
                {
                    closestDistance = checkingDistance;
                    nearestEnemy = enemy;
                    enemyArmorType = enemy.GetEnemyType();
                }
            }

            return nearestEnemy;
        }

        private void HaltTurret()
        {
            IEnemy nearestEnemy = NearestEnemy();

            if (nearestEnemy == null)
                TurretInstance.transform.rotation = _haltTurretRotation;
        }

        public void AddEnemy(IEnemy newDummyEnemy) => _dummyEnemies.Add(newDummyEnemy);

        public void RemoveEnemy(IEnemy newDummyEnemy) => _dummyEnemies.Remove(newDummyEnemy);

        private void RecoilEnemies() => _dummyEnemies = Services.Instance.LevelService.ActiveEnemies;

        #endregion
    }
}
