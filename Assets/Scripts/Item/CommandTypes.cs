using UnityEngine;

public enum CommandType {Default, Dig, Cultivate, Plant,
    Harvest, Move, Haul, Build, Craft, Mine , Deconstruct
} // 명령 모음

public enum JobStatus { Queued, Reserved, InProgress, 
    Done, Failed, Canceled } // 명령에 대한 상황

public enum ItemType { Corn, Steel ,
    StarMoss, Firebloom, Wood, Stone , Pile} // 아이템 모음
