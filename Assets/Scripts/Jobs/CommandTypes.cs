using UnityEngine;

public enum CommandType { Dig, Cultivate, Plant, Harvest, Move, Haul } // 명령 모음

public enum JobStatus { Queued, Reserved, InProgress, Done, Failed, Canceled } // 명령에 대한 상황?

public enum ItemType { Corn, Steel, Build } // 아이템 모음
