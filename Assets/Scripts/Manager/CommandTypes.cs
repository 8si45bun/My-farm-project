using UnityEngine;

public enum CommandType {Default, Dig, Cultivate, Plant,
    Harvest, Move, Haul, Build, Craft, Mine , Deconstruct, Repair} // ���� ����

public enum JobStatus { Queued, Reserved, InProgress, 
    Done, Failed, Canceled } // ���ɿ� ���� ��Ȳ

public enum RobotState { Idle, Working, Moving, Charging, Emergency, Fleeing }

public enum EnemyState { Idle, Moving, Attacking, Dead }

public enum ItemType { Corn, Steel ,
    StarMoss, Firebloom, Wood, Stone , Pile} // ������ ����
