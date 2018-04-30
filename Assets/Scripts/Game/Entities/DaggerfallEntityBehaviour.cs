﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop;

namespace DaggerfallWorkshop.Game.Entity
{
    /// <summary>
    /// Hosts DaggerfallEntity for living GameObjects.
    /// </summary>
    public class DaggerfallEntityBehaviour : MonoBehaviour
    {
        #region Fields

        public EntityTypes EntityType = EntityTypes.None;

        EntityTypes lastEntityType = EntityTypes.None;
        DaggerfallEntity entity = null;
        DaggerfallLoot corpseLootContainer = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets entity as PlayerEntity.
        /// </summary>
        public DaggerfallEntity Entity
        {
            get { return entity; }
            set { SetEntityValue(value); }
        }

        /// <summary>
        /// Gets or sets reference to loot container spawned at time of entity death.
        /// </summary>
        public DaggerfallLoot CorpseLootContainer
        {
            get { return corpseLootContainer; }
            set { corpseLootContainer = value; }
        }

        #endregion

        #region Unity

        private void Awake()
        {
            SetEntityType(EntityType);
        }

        void Update()
        {
            // Change entity type
            if (EntityType != lastEntityType)
            {
                SetEntityType(EntityType);
                lastEntityType = EntityType;
            }

            // Exit when no entity set
            if (Entity == null)
                return;

            // Update entity
            Entity.Update(this);
        }

        #endregion

        #region Special Damage Methods

        /// <summary>
        /// Cause fatigue damage to entity with additional logic.
        /// </summary>
        /// <param name="source">Source entity.</param>
        /// <param name="amount">Amount to damage fatigue.</param>
        public void DamageFatigueFromSource(DaggerfallEntityBehaviour source, int amount)
        {
            // Remove fatigue amount
            Entity.DecreaseFatigue(amount);

            // Determine if source is player
            if (source == GameManager.Instance.PlayerEntityBehaviour)
                HandleAttackByPlayer();
        }

        /// <summary>
        /// Cause damage to entity health with additional logic.
        /// </summary>
        /// <param name="source">Source entity.</param>
        /// <param name="amount">Amount to damage health.</param>
        public void DamageHealthFromSource(DaggerfallEntityBehaviour source, int amount, bool showBlood, Vector3 bloodPosition)
        {
            // Remove health amount
            Entity.DecreaseHealth(amount);

            // Determine if source is player
            if (source == GameManager.Instance.PlayerEntityBehaviour)
                HandleAttackByPlayer();

            // Show blood
            if (showBlood)
            {
                EnemyBlood blood = transform.GetComponent<EnemyBlood>();
                if (blood)
                    blood.ShowBloodSplash(0, bloodPosition);
            }
        }

        /// <summary>
        /// Cause spell point damage to entity with additional logic.
        /// </summary>
        /// <param name="source">Source entity.</param>
        /// <param name="amount">Amount to damage spell points.</param>
        public void DamageMagickaFromSource(DaggerfallEntityBehaviour source, int amount)
        {
            // Remove fatigue amount
            Entity.DecreaseMagicka(amount);

            // Determine if source is player
            if (source == GameManager.Instance.PlayerEntityBehaviour)
                HandleAttackByPlayer();
        }

        /// <summary>
        /// Handle shared logic when player attacks entity.
        /// </summary>
        public void HandleAttackByPlayer()
        {
            // Handle civilian NPC crime reporting
            if (EntityType == EntityTypes.CivilianNPC)
            {
                PlayerEntity playerEntity = GameManager.Instance.PlayerEntity;
                MobilePersonNPC mobileNpc = transform.GetComponent<MobilePersonNPC>();
                if (mobileNpc)
                {
                    playerEntity.TallyCrimeGuildRequirements(false, 5);
                    // TODO: LOS check from each townsperson. If seen, register crime and start spawning guards as below.
                    playerEntity.CrimeCommitted = PlayerEntity.Crimes.Murder;
                    playerEntity.SpawnCityGuards(true);

                    // Disable when dead
                    // Civilians usually only take one hit, but this just respects entity health value
                    if (Entity.CurrentHealth == 0)
                        mobileNpc.Motor.gameObject.SetActive(false);
                }
            }

            // Handle mobile enemy aggro
            if (EntityType == EntityTypes.EnemyClass || EntityType == EntityTypes.EnemyMonster)
            {
                // Make enemy aggressive to player
                EnemyMotor enemyMotor = transform.GetComponent<EnemyMotor>();
                if (enemyMotor)
                {
                    if (!enemyMotor.IsHostile)
                    {
                        GameManager.Instance.MakeEnemiesHostile();
                    }
                    enemyMotor.MakeEnemyHostileToPlayer(GameManager.Instance.PlayerObject);
                }
            }
        }

        #endregion

        #region Private Methods

        void SetEntityType(EntityTypes type)
        {
            switch(type)
            {
                case EntityTypes.None:
                    Entity = null;
                    break;
                case EntityTypes.Player:
                    Entity = new PlayerEntity();
                    break;
                case EntityTypes.CivilianNPC:
                    Entity = new CivilianEntity();
                    break; 
            }

            lastEntityType = type;

            if (Entity != null)
                Entity.SetEntityDefaults();
        }

        void SetEntityValue(DaggerfallEntity value)
        {
            RaiseOnSetEntityHandler(entity, value);
            entity = value;
        }

        #endregion

        #region Events

        public delegate void OnSetEntityHandler(DaggerfallEntity oldEntity, DaggerfallEntity newEntity);
        public event OnSetEntityHandler OnSetEntity;
        void RaiseOnSetEntityHandler(DaggerfallEntity oldEntity, DaggerfallEntity newEntity)
        {
            if (OnSetEntity != null)
                OnSetEntity(oldEntity, newEntity);
        }

        #endregion
    }
}