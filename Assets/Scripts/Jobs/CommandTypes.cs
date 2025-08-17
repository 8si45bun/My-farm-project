using UnityEngine;

public enum CommandType { Dig, Cultivate, Plant, Harvest, Move, Haul }

public enum JobStatus { Queued, Reserved, InProgress, Done, Failed, Canceled }

// 간단한 아이템 타입 예시(초기 1종만 써도 됨)
public enum ItemType { Generic }
