﻿/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey;
using CodeMonkey.Utils;
using CodeMonkey.MonoBehaviours;
using GridPathfindingSystem;

public class GameHandler_Setup : MonoBehaviour {

    public static GridPathfinding gridPathfinding;

    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Transform followTransform;
    [SerializeField] private bool cameraPositionWithMouse;

    [SerializeField] private CharacterAimHandler characterAimHandler;

    private void Start() {
        //Sound_Manager.Init();
        //cameraFollow.Setup(GetCameraPosition, () => 70f, true, true);

        //FunctionPeriodic.Create(SpawnEnemy, 1.5f);
        //for (int i = 0; i < 1000; i++) SpawnEnemy();
        
        gridPathfinding = new GridPathfinding(new Vector3(-400, -400), new Vector3(400, 400), 5f);
        gridPathfinding.RaycastWalkable();

        EnemyHandler.Create(new Vector3(100, 0));

        /*if (characterAimHandler != null) {
            characterAimHandler.OnShoot += CharacterAimHandler_OnShoot;
        }*/
    }

    private void CharacterAimHandler_OnShoot(object sender, CharacterAimHandler.OnShootEventArgs e) {
        Shoot_Flash.AddFlash(e.gunEndPointPosition);
        WeaponTracer.Create(e.gunEndPointPosition, e.shootPosition);
        UtilsClass.ShakeCamera(.6f, .05f);
        characterAimHandler.transform.Find("ShootLight").position = e.gunEndPointPosition;
        characterAimHandler.transform.Find("ShootLight").gameObject.SetActive(true);
        FunctionTimer.Create(() => characterAimHandler.transform.Find("ShootLight").gameObject.SetActive(false), .04f, "ShootLight", false, true);

        // Any enemy hit?
        RaycastHit2D raycastHit = Physics2D.Raycast(e.gunEndPointPosition, (e.shootPosition - e.gunEndPointPosition).normalized, Vector3.Distance(e.gunEndPointPosition, e.shootPosition));
        if (raycastHit.collider != null) {
            EnemyHandler enemyHandler = raycastHit.collider.gameObject.GetComponent<EnemyHandler>();
            if (enemyHandler != null) {
                enemyHandler.Damage(characterAimHandler);
            }
        }
    }

    private Vector3 GetCameraPosition() {
        if (cameraPositionWithMouse) {
            Vector3 mousePosition = UtilsClass.GetMouseWorldPosition();
            Vector3 playerToMouseDirection = mousePosition - followTransform.position;
            return followTransform.position + playerToMouseDirection * .3f;
        } else {
            return followTransform.position;
        }
    }

    private void SpawnEnemy() {
        Vector3 spawnPosition = Vector3.zero + UtilsClass.GetRandomDir() * 40f;
        if (characterAimHandler != null) {
            spawnPosition = characterAimHandler.GetPosition() + UtilsClass.GetRandomDir() * 40f;
        }
        EnemyHandler.Create(spawnPosition);
    }
}
